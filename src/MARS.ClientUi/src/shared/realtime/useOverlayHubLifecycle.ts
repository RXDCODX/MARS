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
      // eslint-disable-next-line no-console
      console.error("Не удалось подключиться к хабу оверлея", error);
    });

    return () => {
      // Остановка тоже может отказать, если соединение не дошло до конца.
      // Экран уже размонтирован, сообщить об этом некому.
      stop().catch(() => void 0);
    };
  }, [start, stop]);
};
