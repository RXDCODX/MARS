import { useEffect, useRef, useState } from "react";

import type { MikuTrackDto, TwitchUser } from "@/shared/api";
import animate from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";

import commonStyles from "../../../OBSCommon.module.scss";
import styles from "../../MikuMonday.module.scss";
import MarqueeTrackTitle from "../MarqueeTrackTitle";

interface ResultStageProperties {
  track: MikuTrackDto;
  twitchUser: TwitchUser;
  durationMs?: number;
  onComplete: () => void;
}

const DEFAULT_RESULT_DURATION = 7000;
const FADE_OUT_DURATION = 600;

export default function ResultStage({
  track,
  twitchUser,
  durationMs = DEFAULT_RESULT_DURATION,
  onComplete,
}: ResultStageProperties) {
  const [isFadingOut, setIsFadingOut] = useState(false);
  const messageSentReference = useRef(false);
  const sendMessage = useTwitchStore(state => state.sendMsgToPyrokxnezxz);

  useEffect(() => {
    // Отправляем сообщение о выпавшем треке только один раз
    if (messageSentReference.current) {
      return;
    }

    sendMessage(
      `@${twitchUser.displayName}, выпал трек: ${track.artist} - ${track.title} (#${track.number}) ${track.url}`
    );
    messageSentReference.current = true;
  }, [
    track.id,
    twitchUser.displayName,
    sendMessage,
    track.artist,
    track.title,
    track.number,
    track.url,
  ]);

  useEffect(() => {
    const fadeOutTimerId = globalThis.setTimeout(() => {
      setIsFadingOut(true);
    }, durationMs - FADE_OUT_DURATION);

    const completeTimerId = globalThis.setTimeout(onComplete, durationMs);

    return () => {
      globalThis.clearTimeout(fadeOutTimerId);
      globalThis.clearTimeout(completeTimerId);
    };
  }, [durationMs, onComplete]);

  const hasCover = Boolean(track.thumbnailUrl);
  const highlightColor = "rgb(57, 197, 187)";
  const userColor = twitchUser.chatColor ?? highlightColor;
  const backgroundStyle = hasCover
    ? {
        backgroundImage: `url(${track.thumbnailUrl})`,
      }
    : undefined;

  return (
    <article
      className={`${styles["result-stage"]} ${animate.animated} ${
        isFadingOut ? animate.fadeOut : animate.fadeIn
      }`}
      style={backgroundStyle}
    >
      <div className={styles["result-content"]}>
        <span
          className={`${styles["result-heading"]} ${commonStyles.textStrokeShadow}`}
          style={{ color: highlightColor }}
        >
          Мику решила сегодня благословить тебя
        </span>
        <span
          className={`${styles["result-user"]} ${commonStyles.textStrokeShadow}`}
          style={{ color: userColor }}
        >
          {twitchUser.displayName}
        </span>
        <div className={styles["result-track-info"]}>
          <MarqueeTrackTitle
            text={`${track.artist} - ${track.title}`}
            className={`${styles["result-track-title"]} ${commonStyles.textStrokeShadow}`}
          />
          <span className={styles["result-track-meta"]}>
            Трек #{track.number}
          </span>
        </div>
        <span className={styles["result-message"]}>
          Пусть этот трек наполнит эфир чистой магией.
        </span>
      </div>
    </article>
  );
}
