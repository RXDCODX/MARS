import { useEffect, useRef } from "react";

import { useVideoScreenStore } from "../../store/useVideoScreenStore";
import { parseDurationToSeconds } from "../../utils/parseDuration";
import styles from "./ProgressPercentLabel.module.scss";

interface Properties {
  trackDuration?: string;
}

/**
 * Компонент локального отображения прогресса в реальном времени с 60FPS.
 * Получает точное время воспроизведения из ReactPlayer через стор.
 */
export function ProgressPercentLabel({ trackDuration }: Properties) {
  const labelReference = useRef<HTMLDivElement>(null);
  const durationSeconds = parseDurationToSeconds(trackDuration);

  useEffect(() => {
    if (!durationSeconds || durationSeconds <= 0) return;

    let frameId: number;

    const updateProgress = () => {
      const getTime = useVideoScreenStore.getState().getCurrentTimeRef;

      if (getTime && labelReference.current) {
        const currentProgress = getTime();
        const calculatedProgressPercent =
          (currentProgress / durationSeconds) * 100;
        const sanitizedProgress = Math.min(
          Math.max(calculatedProgressPercent, 0),
          100
        );

        labelReference.current.textContent = `${sanitizedProgress.toFixed(1)}%`;
      }

      frameId = requestAnimationFrame(updateProgress);
    };

    frameId = requestAnimationFrame(updateProgress);

    return () => {
      cancelAnimationFrame(frameId);
    };
  }, [durationSeconds]);

  return (
    <div className={styles.progressPercentOverlay}>
      <div ref={labelReference} className={styles.progressPercentLabel}>
        0.0%
      </div>
    </div>
  );
}
