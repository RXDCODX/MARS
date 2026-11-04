import { useEffect, useSyncExternalStore } from "react";

import type { HubAdapter } from "./hubAdapter";
import type {
  OverlayEventArgs as OverlayEventArguments,
  OverlayEventName,
} from "./overlayEvents";
import { getOverlayAdapter, subscribeToOverlayAdapter } from "./overlayHub";

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
export function useOverlayEvent<K extends OverlayEventName>(
  event: K,
  handler: (...arguments_: OverlayEventArguments[K]) => void,
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
    if (adapter) {
      return adapter.on(event, handler);
    }

    // Адаптера ещё нет: компонент смонтировался раньше соединения. Подписка не
    // потеряется — как только стор положит адаптер в реестр, useSyncExternalStore
    // вернёт новое значение и эффект выполнится снова.
    if (subscribed === null) {
      return;
    }

    return subscribed.on(event, handler);
  }, [adapter, subscribed, event, handler]);
}
