import { useEffect, useReducer, useRef, useState } from "react";

import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api/signalr-clients/TelegramusHub/SignalRHubWrapper";
import Announce from "@/shared/Utils/Announce/Announce";

import styles from "./ADHDLayout.module.scss";
import { ADHDPage } from "./ADHDPage";
import { useAdhdConfig } from "./store/adhdLayoutStore";

interface ADHDState {
  isVisible: boolean;
  duration: number;
  elapsedTime: number;
  isPermanent: boolean;
}

type ADHDAction =
  | { type: "SHOW"; payload: { duration: number } }
  | { type: "SHOW_PERMANENT" }
  | { type: "HIDE" }
  | { type: "TICK" };

const adhdReducer = (state: ADHDState, action: ADHDAction): ADHDState => {
  switch (action.type) {
    case "SHOW": {
      return {
        isVisible: true,
        duration: action.payload.duration,
        elapsedTime: 0,
        isPermanent: false,
      };
    }
    case "SHOW_PERMANENT": {
      // Повторный вызов без параметра — выключение перманентного режима
      if (state.isVisible && state.isPermanent) {
        return initialState;
      }

      return {
        isVisible: true,
        duration: 0,
        elapsedTime: 0,
        isPermanent: true,
      };
    }
    case "HIDE": {
      return initialState;
    }
    case "TICK": {
      const nextElapsed = state.elapsedTime + 1;

      return {
        ...state,
        elapsedTime: state.isPermanent
          ? nextElapsed
          : Math.min(nextElapsed, state.duration),
      };
    }
    default: {
      return state;
    }
  }
};

const initialState: ADHDState = {
  isVisible: false,
  duration: 0,
  elapsedTime: 0,
  isPermanent: false,
};

// Функция форматирования времени в формат MM:SS
const formatTime = (seconds: number): string => {
  const safeSeconds = Math.max(0, seconds);
  const minutes = Math.floor(safeSeconds / 60);
  const remainingSeconds = safeSeconds % 60;
  return `${minutes.toString().padStart(2, "0")}:${remainingSeconds
    .toString()
    .padStart(2, "0")}`;
};

export function ADHDController() {
  const [announced, setAnnounced] = useState<boolean>(false);
  const [state, dispatch] = useReducer(adhdReducer, initialState);
  const intervalReference = useRef<NodeJS.Timeout | undefined>(undefined);
  const config = useAdhdConfig();

  const handleMessage = (seconds?: number) => {
    if (typeof seconds === "number" && seconds > 0) {
      const duration = import.meta.env.DEV ? 10 : seconds;
      dispatch({ type: "SHOW", payload: { duration } });
    } else {
      // Без параметра — перманентный toggle оверлея
      dispatch({ type: "SHOW_PERMANENT" });
    }
  };

  // Подписка на SignalR события
  SignalRContext.useSignalREffect("adhd", handleMessage, [handleMessage]);

  // Тикаем каждую секунду, пока оверлей видим
  useEffect(() => {
    if (!state.isVisible) {
      return;
    }

    intervalReference.current = setInterval(() => {
      dispatch({ type: "TICK" });
    }, 1000);

    return () => {
      if (!intervalReference.current) {
        return;
      }

      clearInterval(intervalReference.current);
      intervalReference.current = undefined;
    };
  }, [state.isVisible]);

  // Обработка окончания времени в timed режиме — взрыв и скрытие
  useEffect(() => {
    if (
      !(
        state.isVisible &&
        !state.isPermanent &&
        state.elapsedTime >= state.duration
      )
    ) {
      return;
    }

    // Когда время истекло, вызываем взрыв через SignalR
    SignalRContext.invoke("ExplosionGo");

    // Не скрываем сразу, ждем 2 секунды
    const hideTimer = setTimeout(() => {
      dispatch({ type: "HIDE" });
    }, 2000);

    return () => {
      clearTimeout(hideTimer);
    };
  }, [state.isVisible, state.isPermanent, state.elapsedTime, state.duration]);

  return (
    <>
      {!announced && (
        <Announce title={"ADHD"} callback={() => setAnnounced(true)} />
      )}
      {state.isVisible && (
        <div className={styles.adhdControllerContainer} data-testid="obs-adhd">
          <ADHDPage />
          {config.showTimer && (
            <div className={styles.timerOverlay} data-testid="adhd-timer">
              <div className={styles.timerContent}>
                <span
                  className={`${styles.timerValue} ${
                    state.isPermanent
                      ? styles.timerStopwatch
                      : styles.timerCountdown
                  }`}
                  data-testid="text-adhd-timer-value"
                >
                  {formatTime(
                    state.isPermanent
                      ? state.elapsedTime
                      : state.duration - state.elapsedTime
                  )}
                </span>
              </div>
            </div>
          )}
        </div>
      )}
    </>
  );
}
