import { useEffect, useSyncExternalStore } from "react";

import type { HubAdapter } from "./hubAdapter";
import { getOverlayAdapter, subscribeToOverlayAdapter } from "./overlayHub";
import { signalrDebugEnabled } from "./signalrDebug";

/**
 * Подписка компонента на событие оверлейного хаба.
 *
 * Заменяет `useSignalREffect` из react-signalr. Три отличия, из-за которых
 * замена и нужна:
 *
 * - имя события берётся из карты `OverlayEventArgs`, а не из строки. В проекте
 *   были подписки на `deletemessage`, `alerts` и `explosion`, которые разошлись
 *   с сервером и работали только потому, что резолвер SignalR
 *   регистронезависим;
 * - обработчик снимается при размонтировании, а не остаётся в соединении
 *   навсегда;
 * - адаптер берётся из реестра, который наполняет стор, а не из React-контекста,
 *   поэтому тест подставляет `FakeHubAdapter` и обходится без `vi.mock`
 *   библиотеки и без сети.
 *
 * @param event Имя события из контракта хаба.
 * @param handler Обработчик полезной нагрузки.
 * @param adapter Адаптер. Обычно не передаётся и берётся из реестра; явная
 *   передача нужна тесту.
 */
export function useOverlayEvent(
  event: string,
  handler: (...arguments_: unknown[]) => void,
  adapter?: HubAdapter | null
): void {
  // Адаптер читается через useSyncExternalStore, а не берётся из реестра прямо в
  // теле хука. Разница видна на гонке старта: стор вызывает start при подъёме
  // приложения, а компоненты монтируются независимо. Прямое чтение давало
  // компоненту null навсегда — перерисовки не было, смена адаптера никому не
  // сообщалась, и первый алерт после старта уходил в никуда.
  const subscribed = useSyncExternalStore(
    subscribeToOverlayAdapter,
    getOverlayAdapter,
    getOverlayAdapter
  );

  useEffect(() => {
    const wrappedHandler = signalrDebugEnabled()
      ? ((...args: unknown[]) => {
          // eslint-disable-next-line no-console
          console.info(
            `[OverlayEvent] received ${String(event)}`,
            ...args
          );
          return handler(...args);
        })
      : handler;

    if (adapter) {
      if (signalrDebugEnabled()) {
        // eslint-disable-next-line no-console
        console.debug(
          `[OverlayEvent] subscribe ${String(event)} (adapter: explicit)`
        );
      }
      return adapter.on(event, wrappedHandler);
    }

    // Адаптера ещё нет: компонент смонтировался раньше соединения. Подписка не
    // потеряется — как только стор положит адаптер в реестр, useSyncExternalStore
    // вернёт новое значение и эффект выполнится снова.
    if (subscribed === null) {
      if (signalrDebugEnabled()) {
        // eslint-disable-next-line no-console
        console.debug(
          `[OverlayEvent] subscribe ${String(event)} (waiting for adapter)`
        );
      }
      return;
    }

    if (signalrDebugEnabled()) {
      // eslint-disable-next-line no-console
      console.debug(
        `[OverlayEvent] subscribe ${String(event)} (adapter: registry)`
      );
    }
    return subscribed.on(event, wrappedHandler);
  }, [adapter, subscribed, event, handler]);
}
