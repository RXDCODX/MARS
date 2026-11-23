import { useEffect } from "react";

import { signalrDebugEnabled } from "./signalrDebug";
import { useTelegramusHubStore } from "@/shared/stores/telegramusHubStore";

/**
 * Запуск и остановка оверлейного хаба с проглоченными отказами.
 *
 * `start` в сторе отказ **перебрасывает** намеренно: вызывающий обязан знать, что
 * подписки не появятся. Раньше пять мест звали его без обработки, и при любом
 * недоступном Gateway каждое OBS-экрана давало своё `Unhandled promise
 * rejection`, а `status: "error"` не показывался ни в одном UI — экран выглядел
 * просто пустым. `MikuMondayController` отказ обрабатывал, остальные четыре нет.
 *
 * Отказ логируется, а не прячется: состояние «error» в сторе остаётся
 * единственным источником правды, и подписчик, который его читает, увидит то же
 * самое.
 */
export const useOverlayHubLifecycle = (): void => {
  const start = useTelegramusHubStore(state => state.start);
  const stop = useTelegramusHubStore(state => state.stop);

  useEffect(() => {
    if (signalrDebugEnabled()) {
      // eslint-disable-next-line no-console
      console.info("[OverlayHub] starting connection");
    }

    start().catch((error: unknown) => {
      // Статус стора — единственный источник правды о том, была ли это реальная
      // ошибка подключения. Стор выставляет "error" только тогда, когда
      // отказавший адаптер всё ещё текущий. Иначе это устаревшее рукопожатие,
      // прерванное stop() (StrictMode: effect → cleanup → effect, или уход
      // со страницы): сообщать о нём как о сбое нельзя — следующая попытка
      // уже поднимается, и экран в итоге подключается. В dev-сборке это окно
      // возникает на каждой загрузке OBS-экрана.
      if (useTelegramusHubStore.getState().status !== "error") {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.debug(
            "[OverlayHub] handshake aborted (StrictMode/unmount) — expected, next attempt will connect",
            error
          );
        }
        return;
      }

      // eslint-disable-next-line no-console
      console.error("[OverlayHub] failed to connect", error);
    });

    return () => {
      if (signalrDebugEnabled()) {
        // eslint-disable-next-line no-console
        console.debug("[OverlayHub] stopping (StrictMode/unmount)");
      }
      // Остановка тоже может отражаться AbortError'ом прерванного рукопожатия,
      // но это шум устаревшей попытки, а не сбой текущего соединения.
      stop().catch(() => void 0);
    };
  }, [start, stop]);
};
