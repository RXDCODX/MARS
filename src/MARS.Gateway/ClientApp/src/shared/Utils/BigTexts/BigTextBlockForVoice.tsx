import { useCallback, useState } from "react";
import { Textfit } from "react-textfit";

import { MediaDto } from "@/shared/api";

import styles from "./aa.module.scss";

interface Properties {
  mediaInfo: MediaDto;
  callback: () => void;
}

export function BigTextBlockForVoice({ mediaInfo, callback }: Properties) {
  const { metaInfo, fileInfo, textInfo } = mediaInfo.mediaInfo;
  const bellSource = import.meta.env.VITE_BASE_PATH + "Alerts/bell.wav";
  const voiceSource = import.meta.env.VITE_BASE_PATH + fileInfo.filePath;

  const [isBellPlayed, setIsBellPlayed] = useState(false);

  const error = useCallback(() => {
    throw new Error("Failed to play audio");
    callback();
  }, [callback]);

  return (
    <>
      {!isBellPlayed && (
        <audio
          src={bellSource}
          onEnded={() => setIsBellPlayed(true)}
          onError={() => error()}
          onErrorCapture={() => error()}
        />
      )}
      {isBellPlayed && (
        <audio
          src={voiceSource}
          onEnded={() => callback()}
          onError={() => error()}
          onErrorCapture={() => error()}
        />
      )}
      {
        <div className={styles.container}>
          <div className={styles.block}>
            <Textfit
              forceSingleModeWidth
              style={{
                width: "100%",
                height: "100%",
                marginLeft: "auto",
                marginRight: "auto",
                textAlign: "center",
                alignContent: "center",
                alignItems: "center",
                display: "flex",
                justifyContent: "center",
              }}
              max={2000}
            >
              Стример, заткнись
            </Textfit>
          </div>
          <div className={styles.block_image}>
            <img src={import.meta.env.VITE_BASE_PATH + "Alerts/mute.png"} />
          </div>
          <div className={styles.block}>
            <Textfit
              forceSingleModeWidth
              style={{
                width: "100%",
                height: "100%",
                marginLeft: "auto",
                marginRight: "auto",
                textAlign: "center",
                alignContent: "center",
                alignItems: "center",
                display: "flex",
                justifyContent: "center",
              }}
            >
              Сейчас говорит <img className="emote" src={textInfo.text} />
              {metaInfo.displayName}
            </Textfit>
          </div>
        </div>
      }
    </>
  );
}
