import { useCallback, useEffect, useReducer, useRef, useState } from "react";

import { GaoAlertDto } from "@/shared/api";
import { decodeJsonBranch } from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
import Announce from "@/shared/Utils/Announce/Announce";

import { GaoAlert } from "./GaoAlert";
import { gaoAlertReducer, initialState } from "./GaoAlertReducer";

export default function GaoAlertController() {
  const [state, dispatch] = useReducer(gaoAlertReducer, initialState);
  const [isAnounced, setAnounced] = useState(false);
  const timeoutReference = useRef<NodeJS.Timeout | null>(null);

  const onGaoAlert = useCallback((payload: unknown) => {
    // Событие едет веткой { gaoAlert: { gaoAlertJson } }, а полезная нагрузка
    // внутри объявлена полем bytes — то есть приходит массивом байт. Раньше
    // обработчик получал готовый объект: форму задавал резолвер SignalR, и
    // форма proto до клиента не доходила.
    const decoded = decodeJsonBranch(payload, "gaoAlertJson");

    if (decoded === undefined) {
      return;
    }

    dispatch({ type: 0, payload: decoded as GaoAlertDto }); // StateStatus.add
  }, []);

  const handleComplete = useCallback(() => {
    if (state.current) {
      dispatch({ type: 1, payload: state.current }); // StateStatus.remove
    }

    if (timeoutReference.current) {
      clearTimeout(timeoutReference.current);
      timeoutReference.current = null;
    }

    // Автоматически обрабатываем следующий алерт через 1.2 секунды
    timeoutReference.current = setTimeout(() => {
      if (state.queue.length === 0 || state.isProcessing) {
        return;
      }

      // Логика обработки следующего алерта встроена в reducer
      const nextAlert = state.queue[0];
      dispatch({ type: 0, payload: nextAlert }); // StateStatus.add для следующего
    }, 1200);
  }, [state]);

  // Обработка очереди
  useEffect(() => {
    if (state.current || state.queue.length === 0 || state.isProcessing) {
      return;
    }

    // Логика обработки следующего алерта встроена в reducer
    const nextAlert = state.queue[0];
    dispatch({ type: 0, payload: nextAlert }); // StateStatus.add для следующего
  }, [state, dispatch]);

  // Подписка на события хаба оверлея
  useOverlayEvent("GaoAlert", onGaoAlert);

  // Очистка таймаутов при размонтировании
  useEffect(
    () => () => {
      if (timeoutReference.current) {
        clearTimeout(timeoutReference.current);
      }
    },
    []
  );

  return (
    <>
      {!isAnounced && (
        <Announce title={"GaoAlert"} callback={() => setAnounced(true)} />
      )}
      <div
        style={{
          position: "fixed",
          inset: 0,
          width: "100vw",
          height: "100vh",
          pointerEvents: "none",
          zIndex: 1000,
        }}
        data-testid="obs-gao-alert"
      >
        {state.current && (
          <GaoAlert dto={state.current} onComplete={handleComplete} />
        )}
      </div>
    </>
  );
}
