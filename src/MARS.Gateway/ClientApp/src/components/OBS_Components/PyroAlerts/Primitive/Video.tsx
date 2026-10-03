import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Textfit } from "react-textfit";

import {
  MediaDto,
  MediaFileInfo,
  MediaInfo,
  MediaMetaInfo,
  MediaPositionInfo,
  MediaStylesInfo,
  MediaTextInfo,
  TelegramusHubSignalRContext as SignalRContext,
} from "@/shared/api";
import { KeyWordText } from "@/shared/components/KeyWordText";
import { getCoordinates, getRandomRotation } from "@/shared/Utils";

import common from "../../OBSCommon.module.scss";
import styles from "./Media.module.scss";
import { getMediaFrameStyle } from "./mediaFrameStyle";
import { useAlertLifecycle } from "./useAlertLifecycle";

declare global {
  interface Window {
    webkitAudioContext?: typeof AudioContext;
  }
}

interface Properties {
  callback: () => void;
  MediaInfo: MediaDto;
  isHighPrior?: boolean;
}

export function Video({ MediaInfo, callback, isHighPrior }: Properties) {
  const { fileInfo, id, positionInfo, textInfo, metaInfo } =
    MediaInfo.mediaInfo;
  const frameStyle = getMediaFrameStyle(MediaInfo);
  const player = useRef<HTMLVideoElement>(null);
  const containerReference = useRef<HTMLDivElement>(null);
  const [, setBackupTimer] = useState<NodeJS.Timeout>();
  const [videoProgress, setVideoProgress] = useState(0);
  const audioContextReference = useRef<AudioContext | null>(null);
  const gainNodeReference = useRef<GainNode | null>(null);
  const sourceNodeReference = useRef<MediaElementAudioSourceNode | null>(null);

  useAlertLifecycle({
    mediaInfo: MediaInfo,
    containerRef: containerReference,
    isEnabled: positionInfo.randomCoordinates,
  });

  // Мемоизируем positionInfo для стабильности зависимостей
  const memoizedPositionInfo = useMemo<MediaPositionInfo>(
    () => ({
      isUseOriginalWidthAndHeight: positionInfo.isUseOriginalWidthAndHeight,
      isProportion: positionInfo.isProportion,
      width: positionInfo.width,
      height: positionInfo.height,
      isHorizontalCenter: positionInfo.isHorizontalCenter,
      isResizeRequires: positionInfo.isResizeRequires,
      isRotated: positionInfo.isRotated,
      isVerticallCenter: positionInfo.isVerticallCenter,
      randomCoordinates: positionInfo.randomCoordinates,
      rotation: positionInfo.rotation,
      xCoordinate: positionInfo.xCoordinate,
      yCoordinate: positionInfo.yCoordinate,
    }),
    [
      positionInfo.isUseOriginalWidthAndHeight,
      positionInfo.isProportion,
      positionInfo.width,
      positionInfo.height,
      positionInfo.isHorizontalCenter,
      positionInfo.isResizeRequires,
      positionInfo.isRotated,
      positionInfo.isVerticallCenter,
      positionInfo.randomCoordinates,
      positionInfo.rotation,
      positionInfo.xCoordinate,
      positionInfo.yCoordinate,
    ]
  );

  const initialStyles = useMemo<React.CSSProperties>(
    () =>
      memoizedPositionInfo.isProportion
        ? {
            maxWidth: memoizedPositionInfo.width + "px",
            maxHeight: memoizedPositionInfo.height + "px",
          }
        : {
            width: memoizedPositionInfo.width + "px",
            height: memoizedPositionInfo.height + "px",
            maxWidth: "none",
            maxHeight: "max-content",
          },
    [memoizedPositionInfo]
  );

  const [baseStyles, setBaseStyles] =
    useState<React.CSSProperties>(initialStyles);

  // Мемоизируем metaInfo для стабильности зависимостей
  const memoizedMetaInfo = useMemo(
    () => ({
      duration: metaInfo.duration,
      isLooped: metaInfo.isLooped,
    }),
    [metaInfo.duration, metaInfo.isLooped]
  );

  // Мемоизируем MediaInfo для стабильности зависимостей
  const memoizedMediaInfo = useMemo(
    () => ({
      id: MediaInfo.mediaInfo.id,
      filePath: fileInfo.filePath,
    }),
    [MediaInfo.mediaInfo.id, fileInfo.filePath]
  );

  // Мемоизируем positionInfo для функций getCoordinates и getRandomRotation
  const memoizedPositionInfoForFunctions = useMemo(
    () => memoizedPositionInfo,
    [memoizedPositionInfo]
  );

  // Мемоизируем id для стабильности зависимостей
  const memoizedId = useMemo(
    () => MediaInfo.mediaInfo.id,
    [MediaInfo.mediaInfo.id]
  );

  // Мемоизируем textInfo для стабильности зависимостей
  const memoizedTextInfo = useMemo(
    () => ({
      keyWordsColor: textInfo.keyWordsColor,
      textColor: textInfo.textColor,
      keySymbol: textInfo.keyWordSybmolDelimiter,
      text: textInfo.text,
    }),
    [
      textInfo.keyWordsColor,
      textInfo.textColor,
      textInfo.keyWordSybmolDelimiter,
      textInfo.text,
    ]
  );

  // Обновляем baseStyles при изменении initialStyles
  useEffect(() => {
    setBaseStyles(initialStyles);
  }, [initialStyles]);

  // Очистка ресурсов при размонтировании
  useEffect(
    () => () => {
      if (audioContextReference.current) {
        audioContextReference.current.close();
      }
    },
    []
  );

  const muteAll = useCallback(() => {
    if (isHighPrior) {
      SignalRContext.invoke("MuteAll", []);
    }
  }, [isHighPrior]);

  const unmuteAll = useCallback(() => {
    if (isHighPrior) {
      SignalRContext.invoke("UnmuteSessions");
    }
  }, [isHighPrior]);

  const isFreezeRequired = MediaInfo.mediaInfo.metaInfo.isFreezeRequired;

  const freeze = useCallback(() => {
    if (isFreezeRequired) {
      SignalRContext.invoke("ObsFreeze");
    }
  }, [isFreezeRequired]);

  const unfreeze = useCallback(() => {
    if (isFreezeRequired) {
      SignalRContext.invoke("ObsUnfreeze");
    }
  }, [isFreezeRequired]);

  const setupAudioContext = useCallback(() => {
    if (!player.current) return;

    try {
      // Создаем AudioContext если его еще нет
      if (!audioContextReference.current) {
        const AudioContextClass =
          globalThis.AudioContext || globalThis.webkitAudioContext;
        if (AudioContextClass) {
          audioContextReference.current = new AudioContextClass();
        } else {
          throw new Error("AudioContext is not supported in this browser");
        }
      }

      // Создаем источник из video элемента
      if (!sourceNodeReference.current) {
        sourceNodeReference.current =
          audioContextReference.current.createMediaElementSource(
            player.current
          );
      }

      // Создаем GainNode если его еще нет
      if (!gainNodeReference.current) {
        gainNodeReference.current = audioContextReference.current.createGain();
      }

      // Подключаем цепочку: источник -> GainNode -> выход
      sourceNodeReference.current.connect(gainNodeReference.current);
      gainNodeReference.current.connect(
        audioContextReference.current.destination
      );

      // Устанавливаем громкость (поддерживаем значения больше 100%)
      const volume = metaInfo.volume / 100;
      gainNodeReference.current.gain.value = volume;
    } catch (error) {
      console.warn("Failed to setup AudioContext for video:", error);
    }
  }, [metaInfo.volume]);

  const handleTimeUpdate = useCallback(
    (e: React.SyntheticEvent<HTMLVideoElement>) => {
      const video = e.currentTarget;
      const currentTime = video.currentTime;
      const duration = video.duration;
      const targetDuration = memoizedMetaInfo.duration;
      const isLooped = memoizedMetaInfo.isLooped;

      setVideoProgress(currentTime);

      // Проверка достижения конца (с запасом 0.1 сек)
      if (
        (!isLooped && duration - currentTime <= 0.1) ||
        (targetDuration && currentTime >= targetDuration - 0.1)
      ) {
        unmuteAll();
        unfreeze();
        callback();
      }
    },
    [memoizedMetaInfo, unmuteAll, unfreeze, callback]
  );

  const handleLoadedMetadata = useCallback(
    (event: React.SyntheticEvent<HTMLVideoElement>) => {
      const video = event.currentTarget;

      if (player.current) {
        muteAll();
        const fakeMediaInfo: MediaInfo = {
          id: "stub",
          fileInfo: {} as MediaFileInfo,
          metaInfo: {} as MediaMetaInfo,
          stylesInfo: {} as MediaStylesInfo,
          textInfo: {} as MediaTextInfo,
          positionInfo: memoizedPositionInfoForFunctions,
        };

        const newCords = getCoordinates(
          player.current,
          fakeMediaInfo,
          true,
          id
        );
        const randomRotation = getRandomRotation(fakeMediaInfo);

        if (memoizedPositionInfo.isUseOriginalWidthAndHeight) {
          setBaseStyles(previous => ({
            ...previous,
            width: video.videoWidth + "px",
            height: video.videoHeight + "px",
          }));
        }

        setBaseStyles(previous => ({
          ...previous,
          ...newCords,
          ...randomRotation,
        }));
      }

      // Автовоспроизведение с обработкой ошибок
      video.play().catch(() => {
        video.muted = true;
        video.play();
      });
    },
    [
      memoizedPositionInfo.isUseOriginalWidthAndHeight,
      memoizedPositionInfoForFunctions,
      muteAll,
    ]
  );

  useEffect(() => {
    if (!memoizedMetaInfo.duration || !player.current) return;

    const timer = setTimeout(
      () => {
        if (!(videoProgress >= memoizedMetaInfo.duration - 0.5)) {
          return;
        }

        unmuteAll();
        unfreeze();
        callback();
      },
      memoizedMetaInfo.duration * 1000 + 200
    ); // Длительность + 2 сек буфера

    setBackupTimer(timer);

    return () => {
      clearTimeout(timer);
    };
  }, [videoProgress, memoizedMetaInfo, callback, unmuteAll, unfreeze]);

  useEffect(() => {
    const timer = setTimeout(
      () => {
        unmuteAll();
        unfreeze();
        callback();
      },
      memoizedMetaInfo.duration * 1000 + 1000
    );

    return () => {
      clearTimeout(timer);
    };
  }, [callback, memoizedMetaInfo, unmuteAll, unfreeze]);

  return (
    <div
      ref={containerReference}
      id={memoizedId}
      key={id}
      className={styles.media}
      style={baseStyles}
      data-testid="pyro-alert-video"
    >
      <video
        ref={player}
        src={memoizedMediaInfo.filePath}
        controls={false}
        autoPlay
        loop={memoizedMetaInfo.isLooped}
        style={{
          maxWidth: baseStyles.maxWidth,
          maxHeight: baseStyles.maxHeight,
          width: baseStyles.width,
          height: baseStyles.height,
          objectFit: "fill",
          ...frameStyle,
        }}
        onCanPlay={e => {
          freeze();
          muteAll();

          // Настраиваем AudioContext и GainNode
          setupAudioContext();

          // Устанавливаем базовую громкость на 1.0, так как реальная громкость контролируется GainNode
          e.currentTarget.volume = 1;
        }}
        onError={e => {
          console.log(
            "%c" + e,
            "color: #7289DA; -webkit-text-stroke: 2px black; font-size: 72px; font-weight: bold;"
          );
          SignalRContext.invoke(
            "LogError",
            `RandomMem: failed to play VIDEO id=${memoizedId} path=${memoizedMediaInfo.filePath}`
          );
          unmuteAll();
          unfreeze();
          callback();
        }}
        onTimeUpdate={handleTimeUpdate}
        onLoadedMetadata={handleLoadedMetadata}
      />
      <Textfit
        className={`${common.textStrokeShadow} ${styles.alertText}`}
        mode="multi"
        min={30}
        style={{
          justifyContent: "center",
          display: "flex",
          width: "100%",
          maxWidth: memoizedPositionInfo.width + "px",
        }}
      >
        <KeyWordText
          keyWordColor={memoizedTextInfo.keyWordsColor}
          textColor={memoizedTextInfo.textColor}
          classNameForKeyWordedSpan={styles.key_word}
          keySymbol={memoizedTextInfo.keySymbol ?? "#"}
          isQuouted
          keyWordedString={memoizedTextInfo.text ?? ""}
        />
      </Textfit>
    </div>
  );
}
