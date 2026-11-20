import { useEffect } from "react";

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
    start().catch((error: unknown) => {
      // Статус стора — единственный источник правды о том, была ли это реальная
      // ошибка подключения. Стор выставляет "error" только тогда, когда
      // отказавший адаптер всё ещё текущий. Иначе это устаревшее рукопожатие,
      // прерванное stop() (StrictMode: effect → cleanup → effect, или уход
      // со страницы): сообщать о нём как о сбое нельзя — следующая попытка
      // уже поднимается, и экран в итоге подключается. В dev-сборке это окно
      // возникает на каждой загрузке OBS-экрана.
      if (useTelegramusHubStore.getState().status !== "error") {
        // eslint-disable-next-line no-console
        console.debug("Рукопожатие хаба оверлея прервано (StrictMode/уход со страницы) — ожидаемо, следующая попытка подключится", error);
        return;
      }

      // eslint-disable-next-line no-console
      console.error("Не удалось подключиться к хабу оверлея", error);
    });

    return () => {
      // Остановка тоже может отражаться AbortError'ом прерванного рукопожатия,
      // но это шум устаревшей попытки, а не сбой текущего соединения.
      stop().catch(() => void 0);
    };
  }, [start, stop]);
};
