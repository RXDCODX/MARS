import { CSSProperties, useCallback, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";

import {
  MediaDto,
  TelegramusHubSignalRContext as SignalRContext,
} from "@/shared/api";
import { KeyWordText } from "@/shared/components/KeyWordText";
import { getCoordinates, getRandomRotation } from "@/shared/Utils";

import common from "../../OBSCommon.module.scss";
import peelStyles from "../Animations/StickerPeel/StickerPeel.module.scss";
import styles from "./Media.module.scss";
import { getMediaFrameStyle } from "./mediaFrameStyle";
import { useAlertLifecycle } from "./useAlertLifecycle";

interface Properties {
  callBack: () => void;
  mediaInfo: MediaDto;
}

export function Image({ mediaInfo: MediaInfo, callBack }: Properties) {
  const mediaInfo = MediaInfo.mediaInfo;
  const fileInfo = mediaInfo.fileInfo;
  const id = mediaInfo.id;
  const metaInfo = mediaInfo.metaInfo;
  const textInfo = mediaInfo.textInfo;
  const positionInfo = mediaInfo.positionInfo;

  const [isDisappearing, setIsDisappearing] = useState(false);

  const handleDisappear = useCallback(() => {
    setIsDisappearing(true);
  }, []);

  useEffect(() => {
    const peelDuration = 700;
    const displayDuration = metaInfo.duration * 1000 - peelDuration;
    const timer = setTimeout(
      () => {
        handleDisappear();
      },
      Math.max(0, displayDuration)
    );

    return () => clearTimeout(timer);
  }, [handleDisappear, metaInfo.duration]);

  const isFreezeRequired = mediaInfo.metaInfo.isFreezeRequired;

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

  useEffect(() => {
    if (!isDisappearing) {
      return;
    }
    const timer = setTimeout(() => {
      unfreeze();
      callBack();
    }, 700);
    return () => clearTimeout(timer);
  }, [isDisappearing, callBack, unfreeze]);

  const reference = useRef<HTMLDivElement>(null);

  useAlertLifecycle({
    mediaInfo: MediaInfo,
    containerRef: reference,
    isEnabled: positionInfo.randomCoordinates,
  });

  const [containerStyle, setContainerStyle] = useState<CSSProperties>({
    display: "inline-flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "flex-start",
    gap: "8px",
    position: "absolute",
    visibility: "hidden",
  });

  const [imageStyle, setImageStyle] = useState<CSSProperties>({
    maxWidth: positionInfo.width + "px",
    maxHeight: positionInfo.height + "px",
    visibility: "hidden",
  });

  const frameStyle = getMediaFrameStyle(MediaInfo);
  const mergeTransformStyles = (
    coordinates: CSSProperties,
    rotation: CSSProperties
  ): CSSProperties => {
    const coordinatesTransform = coordinates.transform?.toString() ?? "";
    const rotationTransform = rotation.transform?.toString() ?? "";
    const transform = [coordinatesTransform, rotationTransform]
      .filter(Boolean)
      .join(" ");

    return {
      ...coordinates,
      ...rotation,
      ...(transform && { transform }),
    };
  };

  return (
    <div
      ref={reference}
      className={`${styles["imageContainer"]} ${isDisappearing ? peelStyles.peelOff : ""}`}
      style={containerStyle}
      data-testid="pyro-alert-image"
    >
      {positionInfo.isProportion ? (
        <img
          id={id}
          src={fileInfo.filePath}
          key={id}
          alt={"IMAGE ERROR"}
          className={styles.media}
          style={{ ...imageStyle, ...frameStyle }}
          onLoad={event => {
            const cords = getCoordinates(
              event.currentTarget,
              mediaInfo,
              true,
              id
            );
            const rotation = getRandomRotation(mediaInfo);
            const size = { ...imageStyle };
            setContainerStyle(() => ({
              ...mergeTransformStyles(cords, rotation),
              visibility: "visible",
            }));
            setImageStyle(() => ({
              ...size,
              visibility: "visible",
            }));
            freeze();
          }}
          onError={e => {
            console.log(e);
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to load IMAGE id=${id} path=${fileInfo.filePath}`
            );
          }}
          onErrorCapture={e => {
            console.log(e);
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to load IMAGE id=${id} path=${fileInfo.filePath}`
            );
          }}
        />
      ) : (
        <img
          id={id}
          src={fileInfo.filePath}
          key={id}
          alt={"IMAGE ERROR"}
          className={styles.media}
          style={{ ...imageStyle, ...frameStyle }}
          onError={e => {
            console.log(
              "%c" + e,
              "color: #7289DA; -webkit-text-stroke: 2px black; font-size: 72px; font-weight: bold;"
            );
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to load IMAGE id=${id} path=${fileInfo.filePath}`
            );
          }}
          onErrorCapture={e => {
            console.log(e);
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to load IMAGE id=${id} path=${fileInfo.filePath}`
            );
          }}
          onLoad={event => {
            const cords = getCoordinates(
              event.currentTarget,
              mediaInfo,
              true,
              id
            );
            const rotation = getRandomRotation(mediaInfo);
            const size = { ...imageStyle };
            setContainerStyle(() => ({
              ...mergeTransformStyles(cords, rotation),
              visibility: "visible",
            }));
            setImageStyle(() => ({
              ...size,
              visibility: "visible",
            }));
            freeze();
          }}
        />
      )}
      {textInfo.text !== "" && (
        <Textfit
          className={`${common.textStrokeShadow} ${styles.alertText}`}
          mode="multi"
          min={30}
          style={{
            justifyContent: "center",
            display: "flex",
            width: "100%",
            maxWidth: positionInfo.width + "px",
          }}
        >
          <KeyWordText
            keyWordColor={textInfo.keyWordsColor}
            textColor={textInfo.textColor}
            classNameForKeyWordedSpan={styles.key_word}
            keySymbol={textInfo.keyWordSybmolDelimiter ?? "#"}
            isQuouted
            keyWordedString={textInfo.text ?? ""}
          />
        </Textfit>
      )}
    </div>
  );
}
