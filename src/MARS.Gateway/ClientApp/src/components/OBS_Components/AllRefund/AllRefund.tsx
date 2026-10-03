import { AnimatePresence, motion } from "framer-motion";
import { useEffect, useRef, useState } from "react";

import { TelegramusHubSignalRContext, type TwitchUser } from "@/shared/api";
import { useInjectStyles } from "@/shared/hooks";

import militaryAlarmAudio from "./alarm.mp3";
import styles from "./AllRefund.module.scss";

const TOTAL_DURATION_SECONDS = 72;
const INTRO_DURATION_MS = 10_000;
const COMPACT_MODE_THRESHOLD_MS = 9000;
const TICK_INTERVAL_MS = 100;

interface AllRefundProperties {
  user: TwitchUser;
  onComplete: () => void;
}

function formatClock(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
    .toString()
    .padStart(2, "0");
  const seconds = Math.floor(totalSeconds % 60)
    .toString()
    .padStart(2, "0");

  return `${minutes}:${seconds}`;
}

function AllRefund({ user, onComplete }: AllRefundProperties) {
  const audioReference = useRef<HTMLAudioElement | null>(null);
  const audioStartedReference = useRef(false);
  const finishTriggeredReference = useRef(false);
  const startTimeReference = useRef<number>(0);

  const [remainingSeconds, setRemainingSeconds] = useState(
    TOTAL_DURATION_SECONDS
  );
  const [showIntro, setShowIntro] = useState(true);
  const [compactMode, setCompactMode] = useState(false);

  useInjectStyles(
    `
      body {
        overflow: hidden;
        background: transparent !important;
      }
    `,
    "all-refund-body-lock"
  );

  const displayName = user.displayName || user.userLogin || user.twitchId;
  const userColor = user.chatColor || "#ffffff";
  const timerValue = formatClock(remainingSeconds);
  const userInitial = displayName.trim().slice(0, 1).toUpperCase() || "?";

  useEffect(() => {
    const audio = audioReference.current;
    if (!audio || audioStartedReference.current) {
      return;
    }

    audioStartedReference.current = true;
    audio.currentTime = 0;
    audio.volume = 0.55;
    audio.loop = false;

    const playPromise = audio.play();
    if (playPromise) {
      playPromise.catch(() => {
        audio.muted = true;
        audio
          .play()
          .then(() => {
            globalThis.setTimeout(() => {
              if (audioReference.current) {
                audioReference.current.muted = false;
              }
            }, 200);
          })
          .catch(() => {
            // Автозапуск может быть заблокирован браузером.
          });
      });
    }

    return () => {
      audio.pause();
    };
  }, []);

  useEffect(() => {
    startTimeReference.current = Date.now();

    const updateState = () => {
      const elapsedMs = Date.now() - startTimeReference.current;
      const nextRemainingSeconds = Math.max(
        0,
        Math.ceil((TOTAL_DURATION_SECONDS * 1000 - elapsedMs) / 1000)
      );

      setRemainingSeconds(previousSeconds => {
        if (previousSeconds === nextRemainingSeconds) {
          return previousSeconds;
        }

        return nextRemainingSeconds;
      });

      setCompactMode(previousCompactMode => {
        const nextCompactMode =
          previousCompactMode || elapsedMs >= COMPACT_MODE_THRESHOLD_MS;

        return nextCompactMode;
      });

      if (elapsedMs >= INTRO_DURATION_MS) {
        setShowIntro(false);
      }

      if (nextRemainingSeconds <= 0 && !finishTriggeredReference.current) {
        finishTriggeredReference.current = true;
        onComplete();
      }
    };

    updateState();

    const intervalId = globalThis.setInterval(updateState, TICK_INTERVAL_MS);
    const introTimeoutId = globalThis.setTimeout(() => {
      setShowIntro(false);
    }, INTRO_DURATION_MS);
    const compactTimeoutId = globalThis.setTimeout(() => {
      setCompactMode(true);
    }, COMPACT_MODE_THRESHOLD_MS);

    return () => {
      globalThis.clearInterval(intervalId);
      globalThis.clearTimeout(introTimeoutId);
      globalThis.clearTimeout(compactTimeoutId);
    };
  }, [onComplete]);

  useEffect(() => {
    TelegramusHubSignalRContext.invoke("MuteAll", []);
    return () => {
      TelegramusHubSignalRContext.invoke("UnmuteSessions");
    };
  }, []);

  return (
    <div className={styles.root} data-testid="all-refund-content">
      <motion.div
        className={styles.backdrop}
        animate={{
          backgroundColor: compactMode
            ? "rgba(0, 0, 0, 0)"
            : "rgba(0, 0, 0, 0.98)",
        }}
        transition={{ duration: 0.9, ease: "easeInOut" }}
      />

      <motion.div
        layout
        className={`${styles.timerShell} ${
          compactMode ? styles.timerCompact : styles.timerIntro
        }`}
        data-testid="all-refund-timer"
        animate={{
          backgroundColor: compactMode
            ? "rgba(0, 0, 0, 0)"
            : "rgba(0, 0, 0, 0.82)",
          borderColor: compactMode
            ? "rgba(255, 0, 0, 0.18)"
            : "rgba(255, 0, 0, 0.45)",
          opacity: 1,
        }}
        transition={{
          layout: { duration: 1, ease: "easeInOut" },
          backgroundColor: { duration: 0.9, ease: "easeInOut" },
          borderColor: { duration: 0.9, ease: "easeInOut" },
          opacity: { duration: 0.2 },
        }}
      >
        <div className={styles.timerCaption} data-testid="text-timer-caption">
          Возврат баллов канала
        </div>
        <div className={styles.timerValue} data-testid="text-timer-value">
          {timerValue}
        </div>
      </motion.div>

      <AnimatePresence initial={false} mode="wait">
        {showIntro && (
          <motion.div
            key="intro"
            className={styles.introPanel}
            data-testid="all-refund-intro"
            initial={{ opacity: 0, y: 20, scale: 0.985 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{
              opacity: 0,
              y: -18,
              scale: 0.985,
              transition: { duration: 0.7, ease: "easeInOut" },
            }}
            transition={{ duration: 0.55, ease: "easeOut" }}
          >
            <div className={styles.heroLine} data-testid="text-hero-line">
              Алармагеддон
            </div>
            <div className={styles.rewardLine}>
              активировал награду, которая через 10 секунд начнёт в течение 1
              минуты возвращать всем зрителям все потраченные баллы канала
            </div>

            <div className={styles.userRow}>
              {user.profileImageUrl ? (
                <img
                  className={styles.avatar}
                  alt={displayName}
                  src={user.profileImageUrl}
                />
              ) : (
                <div className={styles.avatarFallback}>{userInitial}</div>
              )}

              <div className={styles.userMeta}>
                <div className={styles.userLabel}>Юзер</div>
                <div
                  className={styles.userName}
                  style={{ color: userColor }}
                  data-testid="text-user-name"
                >
                  {displayName}
                </div>
              </div>
            </div>

            <div className={styles.rewardName}>
              Награда <span>«Алармагеддон»</span>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      <audio ref={audioReference} src={militaryAlarmAudio} preload="auto" />
    </div>
  );
}

export default AllRefund;
