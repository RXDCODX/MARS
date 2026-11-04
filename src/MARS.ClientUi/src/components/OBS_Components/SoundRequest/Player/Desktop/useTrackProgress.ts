import { useEffect, useRef, useState } from "react";

interface UseTrackProgressProperties {
  durationSec: number;
  isPlaying: boolean;
  trackId?: string;
  initialProgress?: string; // ISO 8601 duration строка (например, PT30S)
}

/**
 * Парсит ISO 8601 duration строку (PT30S) в секунды
 */
function parseDurationToSeconds(duration?: string): number {
  if (!duration) return 0;
  try {
    // Парсим ISO 8601 duration формат: PT1H2M3S
    const match = duration.match(
      /PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/
    );
    if (!match) return 0;

    const hours = Number.parseFloat(match[1] || "0");
    const minutes = Number.parseFloat(match[2] || "0");
    const seconds = Number.parseFloat(match[3] || "0");

    return hours * 3600 + minutes * 60 + seconds;
  } catch {
    return 0;
  }
}

export function useTrackProgress({
  durationSec,
  isPlaying,
  trackId,
  initialProgress,
}: UseTrackProgressProperties) {
  const [progress, setProgress] = useState(0);
  const startTimeReference = useRef<number>(0);
  const pausedAtReference = useRef<number>(0);
  const wasPausedReference = useRef<boolean>(false);
  const rafReference = useRef<number | null>(null);
  const lastExternalProgressReference = useRef<number>(0);
  const lastTrackKeyReference = useRef<string>("");

  // Reset/start progress when track changes
  useEffect(() => {
    if (!trackId || !durationSec) {
      setProgress(0);
      lastExternalProgressReference.current = 0;
      return;
    }

    // Если это новый трек, сбрасываем все состояния
    if (lastTrackKeyReference.current !== trackId) {
      console.log("🟠 [useTrackProgress] NEW TRACK DETECTED:", {
        oldTrackKey: lastTrackKeyReference.current,
        newTrackKey: trackId,
        initialProgress,
        durationSec,
      });

      lastTrackKeyReference.current = trackId;

      // Парсим начальный прогресс (если есть)
      const initialProgressSec = parseDurationToSeconds(initialProgress);
      const initialProgressRatio =
        durationSec > 0 ? initialProgressSec / durationSec : 0;

      setProgress(initialProgressRatio);
      lastExternalProgressReference.current = initialProgressRatio;

      // Устанавливаем startTime с учетом начального прогресса
      startTimeReference.current = Date.now() - initialProgressSec * 1000;

      wasPausedReference.current = false;
      pausedAtReference.current = 0;

      if (initialProgressSec > 0) {
        console.log(
          `[useTrackProgress] Восстановлен прогресс: ${initialProgressSec}s (${(initialProgressRatio * 100).toFixed(1)}%)`
        );
      }
    }
  }, [trackId, durationSec, initialProgress]);

  // Умная синхронизация с внешним прогрессом (как в ProgressBar.tsx)
  useEffect(() => {
    if (!durationSec || durationSec === 0 || !trackId) return;

    // Конвертируем внешний прогресс из ISO 8601 в проценты
    const externalProgressSeconds = parseDurationToSeconds(initialProgress);
    const externalProgress =
      durationSec > 0 ? externalProgressSeconds / durationSec : 0;
    const lastProgress = lastExternalProgressReference.current;
    const progressDiff = externalProgress - lastProgress;

    // Проверяем значительные изменения вперед (перемотка вперед на 3+ секунд)
    const isSignificantForwardJump = progressDiff > 3 / durationSec;

    // Проверяем значительные изменения назад (перемотка назад на 3+ секунд)
    const isSignificantBackwardJump = progressDiff < -(3 / durationSec);

    // Проверяем резкие изменения в любом направлении (более 1 секунды)
    const isRapidChange = Math.abs(progressDiff) > 1 / durationSec;

    // Дополнительная проверка для отмотки назад
    const isRewindBack =
      progressDiff < -(0.5 / durationSec) && externalProgress < lastProgress;

    // Проверяем, что изменение не слишком большое (не больше половины трека)
    const isReasonableChange = Math.abs(progressDiff) < 0.5;

    // Логируем все изменения прогресса для отладки
    if (Math.abs(progressDiff) > 0.01) {
      console.log("🟡 [useTrackProgress] PROGRESS CHANGE:", {
        externalProgressSeconds,
        externalProgress,
        lastProgress,
        progressDiff,
        isSignificantForwardJump,
        isSignificantBackwardJump,
        isRapidChange,
        isRewindBack,
        isReasonableChange,
        willSync:
          (isSignificantForwardJump ||
            isSignificantBackwardJump ||
            isRapidChange ||
            isRewindBack) &&
          isReasonableChange,
        isPlaying,
        currentInternalProgress: progress,
        durationSec,
      });
    }

    // Синхронизируемся при значительных перемотках, резких изменениях или отмотке назад
    if (
      (isSignificantForwardJump ||
        isSignificantBackwardJump ||
        isRapidChange ||
        isRewindBack) &&
      isReasonableChange
    ) {
      console.log("🟢 [useTrackProgress] SYNCING PROGRESS:", {
        from: progress,
        to: externalProgress,
        fromSeconds: progress * durationSec,
        toSeconds: externalProgressSeconds,
      });
      lastExternalProgressReference.current = externalProgress;
      setProgress(externalProgress);

      // Пересчитываем время начала для корректного продолжения анимации
      if (isPlaying) {
        startTimeReference.current =
          Date.now() - externalProgressSeconds * 1000;
      }
    } else if (Math.abs(progressDiff) > 0.01) {
      // Если изменение незначительное, просто обновляем отслеживаемое значение
      // но не синхронизируемся, чтобы избежать дрожания
      lastExternalProgressReference.current = externalProgress;
    }
  }, [initialProgress, durationSec, isPlaying, progress, trackId]);

  // Drive progress animation
  useEffect(() => {
    if (!trackId || !durationSec) return;

    const loop = () => {
      const currentTime = Date.now();
      const elapsedSec = (currentTime - startTimeReference.current) / 1000;
      const pr = Math.min(elapsedSec / durationSec, 1);

      // Логируем изменения прогресса в анимации (только значительные)
      if (Math.abs(pr - progress) > 0.01) {
        console.log("🔵 [useTrackProgress] ANIMATION PROGRESS:", {
          newProgress: pr,
          newProgressSeconds: pr * durationSec,
          currentProgress: progress,
          currentProgressSeconds: progress * durationSec,
          elapsed: elapsedSec,
          durationSec,
          isPlaying,
          startTime: startTimeReference.current,
          currentTime,
        });
      }

      setProgress(pr);

      if (pr < 1 && isPlaying) {
        rafReference.current = requestAnimationFrame(loop);
      }
    };

    if (isPlaying) {
      if (wasPausedReference.current) {
        const pauseDelta = Date.now() - pausedAtReference.current;
        console.log("▶️ [useTrackProgress] TRACK RESUMED after pause:", {
          pauseDuration: pauseDelta,
        });
        startTimeReference.current += pauseDelta;
        wasPausedReference.current = false;
      }
      rafReference.current = requestAnimationFrame(loop);
    } else {
      if (!wasPausedReference.current && progress > 0) {
        console.log("⏸️ [useTrackProgress] TRACK PAUSED");
        wasPausedReference.current = true;
        pausedAtReference.current = Date.now();
      }
      if (rafReference.current) cancelAnimationFrame(rafReference.current);
    }

    return () => {
      if (rafReference.current) cancelAnimationFrame(rafReference.current);
      rafReference.current = null;
    };
  }, [isPlaying, trackId, durationSec, progress]);

  // Очищаем анимацию при размонтировании
  useEffect(
    () => () => {
      if (rafReference.current) {
        cancelAnimationFrame(rafReference.current);
      }
    },
    []
  );

  // Логируем критические моменты (близко к завершению)
  useEffect(() => {
    const progressPercentage = progress * 100;
    if (progressPercentage >= 99) {
      console.log("🔴 [useTrackProgress] PROGRESS 99%+ reached:", {
        progressPercentage,
        progress,
        isPlaying,
        durationSec,
        externalProgressSeconds: parseDurationToSeconds(initialProgress),
        externalProgressPercent:
          durationSec > 0
            ? parseDurationToSeconds(initialProgress) / durationSec
            : 0,
        lastExternalProgress: lastExternalProgressReference.current,
        isPaused: wasPausedReference.current,
        startTime: startTimeReference.current,
        currentTime: Date.now(),
        elapsed: (Date.now() - startTimeReference.current) / 1000,
        trackId,
        lastTrackKey: lastTrackKeyReference.current,
      });
    }
  }, [progress, isPlaying, durationSec, initialProgress, trackId]);

  return progress;
}
