import {
  CSSProperties,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import type { OperationResult } from "@/shared/types/OperationResult";
import { createErrorResult } from "@/shared/types/OperationResult";

import type { RouletteGroup } from "../types";

type ShowToast = (result: OperationResult<unknown>) => void;

interface UseRouletteAnimationParameters {
  rouletteGroups: RouletteGroup[];
  shouldSkipAvailableTracksUpdate: boolean;
  decrementAvailableTrack: () => Promise<void>;
  showToast: ShowToast;
  onComplete: () => void;
}

const ROULETTE_FADE_TIMEOUT = 15_000;
const WINNER_FADE_DELAY = 1200;
const ROULETTE_START_DELAY = 0;

export function useRouletteAnimation({
  rouletteGroups,
  shouldSkipAvailableTracksUpdate,
  decrementAvailableTrack,
  showToast,
  onComplete,
}: UseRouletteAnimationParameters) {
  const containerReference = useRef<HTMLDivElement>(null);
  const groupsReference = useRef<HTMLDivElement>(null);
  const completionNotifiedReference = useRef(false);
  const othersFadedReference = useRef(false);
  const fadeTimersReference = useRef<{
    fadeOthers?: number;
  }>({});
  const startTimerReference = useRef<number | undefined>(undefined);

  const [baseStyle] = useState<CSSProperties>({
    width: "100%",
    height: "100%",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "flex-end",
    animationDuration: "2.2s",
  });
  const [rouletteStart, setRouletteStart] = useState(true);
  const [pointerVisible, setPointerVisible] = useState(false);
  const [pointerHeight, setPointerHeight] = useState(0);
  const [fadedOpacities, setFadedOpacities] = useState<number[]>([]);
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [_, setCompletedRoulettes] = useState(0);

  const initialOpacities = useMemo(
    () => Array.from({ length: rouletteGroups.length }).fill(1),
    [rouletteGroups.length]
  );

  const rouletteOpacities =
    fadedOpacities.length > 0 ? fadedOpacities : initialOpacities;

  useEffect(() => {
    if (!rouletteStart || rouletteGroups.length <= 1) {
      return;
    }

    const fadeOthersTimer = globalThis.setTimeout(() => {
      const nextOpacities = rouletteGroups.map(group =>
        group.hasWinner ? 1 : 0
      );
      setFadedOpacities(nextOpacities);
    }, ROULETTE_FADE_TIMEOUT);

    fadeTimersReference.current.fadeOthers = fadeOthersTimer;

    return () => {
      const { fadeOthers } = fadeTimersReference.current;
      if (fadeOthers) {
        globalThis.clearTimeout(fadeOthers);
      }
      fadeTimersReference.current = {};
      setFadedOpacities([]);
    };
  }, [rouletteStart, rouletteGroups]);

  useEffect(() => {
    const groupsElement = groupsReference.current;
    if (!groupsElement) {
      return;
    }

    const updatePointer = () => {
      const height = groupsElement.offsetHeight;
      if (height > 0) {
        setPointerHeight(height);
        setPointerVisible(true);
      }
    };

    updatePointer();

    if (typeof ResizeObserver !== "undefined") {
      const observer = new ResizeObserver(() => {
        updatePointer();
      });
      observer.observe(groupsElement);

      return () => {
        observer.disconnect();
      };
    }

    const fallbackInterval = globalThis.setInterval(updatePointer, 250);
    return () => {
      globalThis.clearInterval(fallbackInterval);
    };
  }, [rouletteGroups.length]);

  const handleContainerAnimationEnd = useCallback(() => {
    startTimerReference.current = globalThis.setTimeout(() => {
      setRouletteStart(true);
    }, ROULETTE_START_DELAY);
  }, []);

  useEffect(
    () => () => {
      if (startTimerReference.current) {
        globalThis.clearTimeout(startTimerReference.current);
      }
    },
    []
  );

  useEffect(
    () => () => {
      const { fadeOthers } = fadeTimersReference.current;
      if (fadeOthers) {
        globalThis.clearTimeout(fadeOthers);
      }
    },
    []
  );

  const handleOthersFaded = useCallback(() => {
    if (othersFadedReference.current) return;
    othersFadedReference.current = true;

    console.log(
      "[useRouletteAnimation] Другие рулетки исчезли, определяем победителя",
      {
        rouletteGroups: rouletteGroups.map((g, index) => ({
          index: index,
          hasWinner: g.hasWinner,
          prizeIndex: g.prizeIndex,
          winningPrizeId: g.prizes[g.prizeIndex]?.id,
          winningPrizeText: g.prizes[g.prizeIndex]?.text,
        })),
      }
    );

    // Теперь затухаем выигрышную рулетку
    setTimeout(() => {
      setFadedOpacities(rouletteGroups.map(() => 0));
      setPointerVisible(false);
    }, WINNER_FADE_DELAY);
  }, [rouletteGroups]);

  const handleWinnerFaded = useCallback(async () => {
    if (completionNotifiedReference.current) return;
    completionNotifiedReference.current = true;

    console.log(
      "[useRouletteAnimation] Рулетка завершена, уведомляем родителя",
      {
        winner: rouletteGroups.find(g => g.hasWinner)?.prizes[
          rouletteGroups.find(g => g.hasWinner)?.prizeIndex || 0
        ],
      }
    );

    if (!shouldSkipAvailableTracksUpdate) {
      try {
        await decrementAvailableTrack();
      } catch {
        showToast(createErrorResult("Ошибка обновления свободных треков"));
      }
    }
    onComplete();
  }, [
    rouletteGroups,
    shouldSkipAvailableTracksUpdate,
    onComplete,
    decrementAvailableTrack,
    showToast,
  ]);

  const handleSingleRouletteComplete = useCallback(() => {
    setCompletedRoulettes(previous => {
      const newCount = previous + 1;
      console.log(
        `✅ Рулетка завершена. Всего: ${newCount}/${rouletteGroups.length}`
      );

      // Логика перехода теперь в fadePointer таймере
      return newCount;
    });
  }, [rouletteGroups.length]);

  return {
    containerRef: containerReference,
    groupsRef: groupsReference,
    baseStyle,
    rouletteStart,
    visible: pointerVisible,
    pointerHeight,
    rouletteOpacities,
    handleContainerAnimationEnd,
    handleSingleRouletteComplete,
    handleOthersFaded,
    handleWinnerFaded,
  };
}
