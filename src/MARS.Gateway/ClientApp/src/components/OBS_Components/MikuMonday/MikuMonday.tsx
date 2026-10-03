import "react-roulette-pro/dist/index.css";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactPlayer from "react-player";

import type { MikuTrackDto } from "@/shared/api";

import IntroStage from "./components/stages/IntroStage";
import ResultStage from "./components/stages/ResultStage";
import RouletteStage from "./components/stages/RouletteStage";
import WaitingStage from "./components/stages/WaitingStage";
import { useRouletteGroups } from "./hooks/useRouletteGroups";
import styles from "./MikuMonday.module.scss";
import { requestMuteOtherAudio } from "./mikuMondayAudio";
import useMikuMondayStore from "./store/mikuMondayStore";

type StageKey = "waiting" | "intro" | "roulette" | "result";
const QUEUE_PAUSE_MS = 2000;
const BACKGROUND_AUDIO_TARGET_VOLUME = 0.25;
const BACKGROUND_AUDIO_FADE_IN_MS = 5500;
const BACKGROUND_AUDIO_FADE_OUT_MS = 5500;
const BACKGROUND_AUDIO_FADE_TICK_MS = 100;

function MikuMondayContent() {
  const currentAlert = useMikuMondayStore(state => state.currentAlert);
  const decrementAvailableTrack = useMikuMondayStore(
    state => state.decrementAvailableTrack
  );
  const dequeueCurrent = useMikuMondayStore(state => state.dequeueCurrent);

  const rouletteGroups = useRouletteGroups(currentAlert);

  // Получаем выигрышный трек из рулетки
  const winnerTrack = useMemo(() => {
    if (!currentAlert) return;

    const winnerGroup = rouletteGroups.find(group => group.hasWinner);
    if (!winnerGroup) {
      console.log(
        "[MikuMonday] Нет группы с победителем, используем исходный трек"
      );
      return currentAlert.selectedTrack;
    }

    const winnerPrize = winnerGroup.prizes[winnerGroup.prizeIndex];
    if (!winnerPrize) {
      console.warn("[MikuMonday] Не найден приз для победителя");
      return currentAlert.selectedTrack;
    }

    // Ищем полный объект трека в availableTracks
    const fullTrack = currentAlert.availableTracks.find(
      track => track.id === winnerPrize.id
    );

    if (!fullTrack) {
      console.warn("[MikuMonday] Не найден полный объект трека", {
        prizeId: winnerPrize.id,
        prizeText: winnerPrize.text,
      });
      return currentAlert.selectedTrack;
    }

    console.log("[MikuMonday] Выигрышный трек определен", {
      originalTrackId: currentAlert.selectedTrack.id,
      originalTrackNumber: currentAlert.selectedTrack.number,
      winnerTrackId: fullTrack.id,
      winnerTrackNumber: fullTrack.number,
      winnerArtist: fullTrack.artist,
      winnerTitle: fullTrack.title,
    });

    return fullTrack;
  }, [currentAlert, rouletteGroups]);

  const [stage, setStage] = useState<StageKey>("waiting");
  const [isVideoVisible, setIsVideoVisible] = useState(false);
  const [backgroundTrack, setBackgroundTrack] = useState<MikuTrackDto>();
  const [backgroundVolume, setBackgroundVolume] = useState(0);
  const waitingTimeoutReference = useRef<number | null>(null);
  const stageSyncTimeoutReference = useRef<number | null>(null);
  const videoReference = useRef<HTMLVideoElement | null>(null);
  const backgroundAudioFadeIntervalReference = useRef<number | null>(null);
  const backgroundVolumeReference = useRef(0);

  const shouldPlayBackgroundAudio =
    Boolean(currentAlert) && stage !== "waiting";

  const excludedBackgroundTrackIds = useMemo(() => {
    const ids = new Set<string>();

    if (currentAlert?.selectedTrack?.id) {
      ids.add(currentAlert.selectedTrack.id);
    }

    if (winnerTrack?.id) {
      ids.add(winnerTrack.id);
    }

    return ids;
  }, [currentAlert, winnerTrack]);

  const backgroundTrackCandidates = useMemo(
    () =>
      currentAlert?.availableTracks?.filter(
        track =>
          Boolean(track.url) &&
          track.url.trim().length > 0 &&
          !excludedBackgroundTrackIds.has(track.id)
      ) ?? [],
    [currentAlert, excludedBackgroundTrackIds]
  );

  const clearBackgroundAudioFadeInterval = useCallback(() => {
    if (backgroundAudioFadeIntervalReference.current === null) {
      return;
    }

    globalThis.clearInterval(backgroundAudioFadeIntervalReference.current);
    backgroundAudioFadeIntervalReference.current = null;
  }, []);

  useEffect(() => {
    backgroundVolumeReference.current = backgroundVolume;
  }, [backgroundVolume]);

  const fadeBackgroundAudioTo = useCallback(
    (targetVolume: number, durationMs: number, onComplete?: () => void) => {
      clearBackgroundAudioFadeInterval();

      const startVolume = backgroundVolumeReference.current;
      const volumeDelta = targetVolume - startVolume;
      if (Math.abs(volumeDelta) < 0.001 || durationMs <= 0) {
        const nextVolume = Math.max(0, Math.min(1, targetVolume));
        backgroundVolumeReference.current = nextVolume;
        setBackgroundVolume(nextVolume);
        onComplete?.();
        return;
      }

      const steps = Math.max(
        1,
        Math.round(durationMs / BACKGROUND_AUDIO_FADE_TICK_MS)
      );
      let currentStep = 0;

      backgroundAudioFadeIntervalReference.current = globalThis.setInterval(
        () => {
          currentStep += 1;
          const progress = Math.min(1, currentStep / steps);
          const nextVolume = Math.max(
            0,
            Math.min(1, startVolume + volumeDelta * progress)
          );
          backgroundVolumeReference.current = nextVolume;
          setBackgroundVolume(nextVolume);

          if (progress >= 1) {
            clearBackgroundAudioFadeInterval();
            onComplete?.();
          }
        },
        BACKGROUND_AUDIO_FADE_TICK_MS
      );
    },
    [clearBackgroundAudioFadeInterval]
  );

  const startBackgroundAudio = useCallback(() => {
    if (!backgroundTrack?.url) {
      return;
    }

    void requestMuteOtherAudio();
    clearBackgroundAudioFadeInterval();
    backgroundVolumeReference.current = 0;
    setBackgroundVolume(0);
    fadeBackgroundAudioTo(
      BACKGROUND_AUDIO_TARGET_VOLUME,
      BACKGROUND_AUDIO_FADE_IN_MS
    );
  }, [
    backgroundTrack?.url,
    clearBackgroundAudioFadeInterval,
    fadeBackgroundAudioTo,
  ]);

  const stopBackgroundAudio = useCallback(() => {
    fadeBackgroundAudioTo(0, BACKGROUND_AUDIO_FADE_OUT_MS, () => {
      backgroundVolumeReference.current = 0;
      setBackgroundVolume(0);
    });
  }, [fadeBackgroundAudioTo]);

  // Предзагружаем видео при монтировании компонента
  useEffect(() => {
    const video = videoReference.current;
    if (video) {
      video.load(); // Начинаем загрузку видео
    }
  }, []);

  // Управление воспроизведением видео
  useEffect(() => {
    const video = videoReference.current;
    if (!video) return;

    if (isVideoVisible) {
      video.currentTime = 0; // Сбрасываем на начало
      video.play().catch(error => {
        console.warn("[MikuMonday] Не удалось воспроизвести видео:", error);
      });
    } else {
      video.pause();
    }
  }, [isVideoVisible]);

  useEffect(
    () => () => {
      if (waitingTimeoutReference.current !== null) {
        globalThis.clearTimeout(waitingTimeoutReference.current);
        waitingTimeoutReference.current = null;
      }
      if (stageSyncTimeoutReference.current !== null) {
        globalThis.clearTimeout(stageSyncTimeoutReference.current);
        stageSyncTimeoutReference.current = null;
      }
      clearBackgroundAudioFadeInterval();
    },
    [clearBackgroundAudioFadeInterval]
  );

  useEffect(() => {
    const updateBackgroundTrackId = globalThis.setTimeout(() => {
      if (!currentAlert) {
        if (backgroundTrack) {
          setBackgroundTrack(undefined);
        }
        return;
      }

      if (backgroundTrackCandidates.length === 0) {
        if (backgroundTrack) {
          setBackgroundTrack(undefined);
        }
        return;
      }

      const currentTrackIsValid =
        backgroundTrack &&
        backgroundTrackCandidates.some(
          track => track.id === backgroundTrack.id
        );

      if (stage !== "result" && currentTrackIsValid) {
        return;
      }

      const randomIndex = Math.floor(
        Math.random() * backgroundTrackCandidates.length
      );
      const nextBackgroundTrack = backgroundTrackCandidates[randomIndex];

      setBackgroundTrack(nextBackgroundTrack);

      console.log(
        stage === "result"
          ? "[MikuMonday] Выбран новый фоновый трек (result stage)"
          : "[MikuMonday] Выбран фоновый трек",
        {
          backgroundTrackId: nextBackgroundTrack.id,
          backgroundTrackNumber: nextBackgroundTrack.number,
          backgroundTrackArtist: nextBackgroundTrack.artist,
          backgroundTrackTitle: nextBackgroundTrack.title,
          excludedTrackIds: [...excludedBackgroundTrackIds],
        }
      );
    }, 0);

    return () => {
      globalThis.clearTimeout(updateBackgroundTrackId);
    };
  }, [
    currentAlert,
    stage,
    backgroundTrack,
    backgroundTrackCandidates,
    excludedBackgroundTrackIds,
  ]);

  useEffect(() => {
    if (stageSyncTimeoutReference.current !== null) {
      globalThis.clearTimeout(stageSyncTimeoutReference.current);
      stageSyncTimeoutReference.current = null;
    }

    if (currentAlert) {
      if (waitingTimeoutReference.current !== null) {
        globalThis.clearTimeout(waitingTimeoutReference.current);
        waitingTimeoutReference.current = null;
      }
      stageSyncTimeoutReference.current = globalThis.setTimeout(() => {
        setStage(previousStage =>
          previousStage === "waiting" ? "intro" : previousStage
        );
        setIsVideoVisible(true);
        stageSyncTimeoutReference.current = null;
      }, 0);
      return;
    }

    stageSyncTimeoutReference.current = globalThis.setTimeout(() => {
      setStage(previousStage =>
        previousStage === "waiting" ? previousStage : "waiting"
      );
      stageSyncTimeoutReference.current = null;
    }, 0);
  }, [currentAlert?.queueId, currentAlert]);

  const shouldSkipAvailableTracksUpdate =
    currentAlert?.skipAvailableTracksUpdate === true;

  const handleBackgroundPlayerPlay = useCallback(() => {
    startBackgroundAudio();
  }, [startBackgroundAudio]);

  const handleBackgroundPlayerPause = useCallback(() => {
    stopBackgroundAudio();
  }, [stopBackgroundAudio]);

  const handleBackgroundPlayerEnded = useCallback(() => {
    stopBackgroundAudio();
  }, [stopBackgroundAudio]);

  const handleBackgroundPlayerError = useCallback(() => {
    stopBackgroundAudio();
  }, [stopBackgroundAudio]);

  const handleIntroComplete = useCallback(() => {
    console.log("[MikuMonday] handleIntroComplete вызван", {
      rouletteGroupsLength: rouletteGroups.length,
      currentAlertId: currentAlert?.id,
      trackNumber: currentAlert?.selectedTrack.number,
    });
    if (rouletteGroups.length > 0) {
      console.log("[MikuMonday] Переходим на рулетку");
      setStage("roulette");
      return;
    }
    console.log("[MikuMonday] Нет рулеток, переходим на результат");
    setStage("result");
  }, [rouletteGroups.length, currentAlert]);

  const handleRouletteComplete = useCallback(() => {
    setStage("result");
  }, []);

  const handleResultComplete = useCallback(() => {
    console.log("[MikuMonday] handleResultComplete вызван", {
      currentAlertId: currentAlert?.id,
      queueId: currentAlert?.queueId,
      displayName: currentAlert?.twitchUser.displayName,
      trackNumber: currentAlert?.selectedTrack.number,
    });
    setIsVideoVisible(false);
    setStage("waiting");
    if (waitingTimeoutReference.current !== null) {
      globalThis.clearTimeout(waitingTimeoutReference.current);
    }

    waitingTimeoutReference.current = globalThis.setTimeout(() => {
      console.log("[MikuMonday] Вызываем dequeueCurrent после паузы", {
        pauseMs: QUEUE_PAUSE_MS,
      });
      waitingTimeoutReference.current = null;
      dequeueCurrent();
      const nextAlert = useMikuMondayStore.getState().currentAlert;
      console.log("[MikuMonday] Текущий статус после деквеу", {
        nextAlertId: nextAlert?.id,
        nextAlertDisplayName: nextAlert?.twitchUser.displayName,
        nextAlertTrackNumber: nextAlert?.selectedTrack.number,
      });
      setStage(nextAlert ? "intro" : "waiting");
    }, QUEUE_PAUSE_MS);
  }, [dequeueCurrent, currentAlert]);

  // Видео-фон существует постоянно, чтобы не перемонтироваться между стейджами
  const videoBackground = (
    <div
      className={`${styles.videoBackground} ${isVideoVisible ? styles.videoVisible : styles.videoHidden}`}
    >
      <video
        ref={videoReference}
        src="/static/miku.mp4"
        preload="auto"
        muted
        playsInline
        loop
        style={{
          position: "absolute",
          top: "50%",
          left: "50%",
          minWidth: "100%",
          minHeight: "100%",
          width: "auto",
          height: "auto",
          transform: "translate(-50%, -50%)",
          objectFit: "cover",
          pointerEvents: "none",
        }}
      />
    </div>
  );

  // Рендерим контент в зависимости от стейджа
  let stageContent;

  if (!currentAlert || stage === "waiting") {
    stageContent = (
      <div className={styles.layout}>
        <WaitingStage onComplete={() => {}} />
      </div>
    );
  } else if (stage === "intro") {
    stageContent = (
      <div className={styles.layout}>
        <IntroStage
          twitchUser={currentAlert.twitchUser}
          fallbackAvatar={currentAlert.selectedTrack.thumbnailUrl ?? undefined}
          onComplete={handleIntroComplete}
        />
      </div>
    );
  } else if (stage === "roulette") {
    stageContent = (
      <RouletteStage
        rouletteGroups={rouletteGroups}
        shouldSkipAvailableTracksUpdate={shouldSkipAvailableTracksUpdate}
        decrementAvailableTrack={decrementAvailableTrack}
        onComplete={handleRouletteComplete}
      />
    );
  } else {
    const trackToDisplay = winnerTrack ?? currentAlert.selectedTrack;
    console.log("[MikuMonday] 📺 Отправляем трек в ResultStage", {
      trackId: trackToDisplay.id,
      trackNumber: trackToDisplay.number,
      trackTitle: trackToDisplay.title,
      trackArtist: trackToDisplay.artist,
      isWinnerTrack: winnerTrack !== undefined,
      winnerGroupExists: rouletteGroups.some(g => g.hasWinner),
    });
    stageContent = (
      <div className={styles.layout}>
        <ResultStage
          track={trackToDisplay}
          twitchUser={currentAlert.twitchUser}
          onComplete={handleResultComplete}
        />
      </div>
    );
  }

  return (
    <>
      {backgroundTrack?.url ? (
        <div
          aria-hidden="true"
          style={{
            position: "fixed",
            inset: 0,
            width: 1,
            height: 1,
            opacity: 0,
            pointerEvents: "none",
            zIndex: -1,
            overflow: "hidden",
          }}
        >
          <ReactPlayer
            key={backgroundTrack.id}
            src={backgroundTrack.url}
            playing={shouldPlayBackgroundAudio}
            loop={true}
            volume={backgroundVolume}
            muted={false}
            width={1}
            height={1}
            controls={false}
            onPlay={handleBackgroundPlayerPlay}
            onPause={handleBackgroundPlayerPause}
            onEnded={handleBackgroundPlayerEnded}
            onError={handleBackgroundPlayerError}
          />
        </div>
      ) : null}
      {videoBackground}
      {stageContent}
    </>
  );
}

// Wrapper с key для автоматического сброса состояния при смене алерта
export default MikuMondayContent;
