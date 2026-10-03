import { useCallback, useEffect, useMemo, useReducer, useRef } from "react";

import {
  TunaHubSignalRContext,
  TunaHubSignalRHubWrapper,
  TunaMusicData,
  TunaMusicDTO,
} from "@/shared/api";

import CurrentTrack from "./CurrentTrack";

const defaultValue: TunaMusicData & { isDefaultValue: boolean } = {
  id: "00000000-0000-0000-0000-000000000000",
  album_url: "",
  artists: [],
  cover: "",
  duration: 0,
  progress: 0,
  status: "",
  title: "Not Playing",
  isDefaultValue: true,
};

// NOTE: legacy interface left for reference was removed to avoid unused warnings

interface State {
  currentTrack: (TunaMusicData & { isDefaultValue?: boolean }) | null;
  incomingTrack: (TunaMusicData & { isDefaultValue?: boolean }) | null;
  isAnimating: boolean;
  swap: boolean;
  counter: number;
}

type Action =
  | { type: "RECEIVE"; data: TunaMusicData }
  | { type: "SWAP" }
  | { type: "ANIMATION_END" };

export function CurrentTrackManager() {
  const [state, dispatch] = useReducer(
    (s: State, a: Action): State => {
      const key = (t: TunaMusicData | null | undefined) =>
        t ? `${t.artists.join(",")}-${t.title}` : "";

      switch (a.type) {
        case "RECEIVE": {
          // Первый трек — просто устанавливаем без анимации
          if (!s.currentTrack) {
            return {
              ...s,
              currentTrack: { ...a.data, isDefaultValue: false },
              incomingTrack: null,
              isAnimating: false,
              swap: false,
            };
          }

          // Если плеер остановлен и приходит новый трек — просто меняем без анимации
          if (
            s.currentTrack.status === "stopped" &&
            key(a.data) !== key(s.currentTrack)
          ) {
            return {
              ...s,
              currentTrack: { ...a.data, isDefaultValue: false },
              incomingTrack: null,
              isAnimating: false,
              swap: false,
            };
          }

          // Если уже идёт анимация — только обновляем incomingTrack
          if (s.isAnimating) {
            return {
              ...s,
              incomingTrack: { ...a.data, isDefaultValue: false },
            };
          }

          // Если трек изменился — запускаем анимацию
          if (key(a.data) !== key(s.currentTrack)) {
            return {
              ...s,
              incomingTrack: { ...a.data, isDefaultValue: false },
              isAnimating: true,
              swap: false,
              counter: s.counter + 1,
            };
          }

          // Трек тот же — просто обновляем текущий
          return {
            ...s,
            currentTrack: { ...a.data, isDefaultValue: false },
          };
        }
        case "SWAP": {
          if (!s.incomingTrack) return s;
          return {
            ...s,
            currentTrack: s.incomingTrack,
            swap: true,
          };
        }
        case "ANIMATION_END": {
          return {
            ...s,
            isAnimating: false,
            swap: false,
            incomingTrack: null,
          };
        }
        default: {
          return s;
        }
      }
    },
    {
      currentTrack: null,
      incomingTrack: null,
      isAnimating: false,
      swap: false,
      counter: 0,
    }
  );

  // Общая длительность ступенчатой анимации (сжатие/разжатие → NOW PLAYING → сжатие/разжатие → показ контента)
  const ANIMATION_TOTAL_MS = 8000;

  // Таймер завершения анимации
  const animationTimer = useRef<number | null>(null);
  useEffect(() => {
    if (state.isAnimating) {
      if (animationTimer.current)
        globalThis.clearTimeout(animationTimer.current);
      animationTimer.current = globalThis.setTimeout(() => {
        dispatch({ type: "ANIMATION_END" });
      }, ANIMATION_TOTAL_MS);
    }
    return () => {
      if (!animationTimer.current) {
        return;
      }

      globalThis.clearTimeout(animationTimer.current);
      animationTimer.current = null;
    };
  }, [state.isAnimating]);

  const trackKey = useMemo(() => `${state.counter}`, [state.counter]);

  const isDifferentTrack = useCallback((a: TunaMusicData, b: TunaMusicData) => {
    const mk = (t: TunaMusicData) => `${t.artists.join(",")}-${t.title}`;
    return mk(a) !== mk(b);
  }, []);

  TunaHubSignalRContext.useSignalREffect(
    "TunaMusicInfo",
    (data: TunaMusicDTO) => {
      // Если первый приходящий трек
      if (!state.currentTrack) {
        dispatch({ type: "RECEIVE", data: data.data });
        return;
      }

      const isNewTrack = isDifferentTrack(data.data, state.currentTrack);

      if (isNewTrack || state.isAnimating) {
        dispatch({ type: "RECEIVE", data: data.data });
      } else {
        dispatch({ type: "RECEIVE", data: data.data });
      }
    },
    [state.currentTrack?.artists, state.currentTrack?.title, state.isAnimating]
  );

  useEffect(() => {
    // no-op; keep for potential debug
    console.warn(trackKey);
  }, [trackKey]);

  useEffect(() => {
    console.warn(state.currentTrack?.title);
  }, [state.currentTrack?.title]);

  const displayed = state.currentTrack ?? defaultValue;
  return (
    !displayed.isDefaultValue &&
    displayed.status !== "stopped" && (
      <CurrentTrack
        oldTrack={state.currentTrack ?? defaultValue}
        track={state.incomingTrack ?? state.currentTrack ?? defaultValue}
        swap={state.swap}
        swapChange={() => dispatch({ type: "SWAP" })}
        key={trackKey}
        shouldAnimate={state.isAnimating}
      />
    )
  );
}

const CurrentTrackInfo = () => (
  <>
    <TunaHubSignalRHubWrapper>
      <CurrentTrackManager />
    </TunaHubSignalRHubWrapper>
  </>
);

export default CurrentTrackInfo;
