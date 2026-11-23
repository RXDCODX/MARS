import { useEffect, useMemo, useState } from "react";

import type { TwitchUser } from "@/shared/api";
import animate from "@/shared/styles/animate.module.scss";

import commonStyles from "../../../OBSCommon.module.scss";
import styles from "../../MikuMonday.module.scss";

interface IntroStageProperties {
  twitchUser: TwitchUser;
  fallbackAvatar?: string;
  durationMs?: number;
  onComplete: () => void;
}

const DEFAULT_INTRO_DURATION = 7000;
const FADE_OUT_DURATION = 600;

export default function IntroStage({
  twitchUser,
  fallbackAvatar,
  durationMs = DEFAULT_INTRO_DURATION,
  onComplete,
}: IntroStageProperties) {
  const [isFadingOut, setIsFadingOut] = useState(false);

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

  const displayName = twitchUser.displayName ?? twitchUser.userLogin;
  const avatarUrl = twitchUser.profileImageUrl ?? fallbackAvatar ?? "";

  const fallbackSymbol = useMemo(() => {
    if (displayName && displayName.length > 0) {
      return displayName.charAt(0).toUpperCase();
    }
    return "M";
  }, [displayName]);

  const hasAvatar = avatarUrl.length > 0;

  return (
    <article
      className={`${styles["intro-stage"]} ${animate.animated} ${
        isFadingOut ? animate.fadeOut : animate.fadeIn
      }`}
    >
      <div className={styles["intro-text-block"]}>
        <span
          className={`${styles["intro-heading"]} ${commonStyles.textStrokeShadow}`}
        >
          Мику обратила свой взор на
        </span>
        <div className={styles["intro-name-with-avatar"]}>
          <div className={styles["intro-small-avatar"]}>
            {hasAvatar ? (
              <img src={avatarUrl} alt={displayName} />
            ) : (
              <div className={styles["intro-small-avatar-placeholder"]}>
                {fallbackSymbol}
              </div>
            )}
          </div>
          <span
            className={`${styles["intro-name"]} ${commonStyles.textStrokeShadow}`}
            style={{ color: twitchUser.chatColor ?? "#fff" }}
          >
            {displayName}
          </span>
        </div>
        <span className={styles["intro-subtitle"]}>
          Всем приготовься к волшебству — новый трек уже на подходе!
        </span>
      </div>
    </article>
  );
}
