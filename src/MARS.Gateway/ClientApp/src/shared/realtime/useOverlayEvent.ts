import { useEffect } from "react";

import type { HubAdapter } from "./hubAdapter";
import { getOverlayAdapter } from "./overlayHub";
import type { OverlayEventArgs, OverlayEventName } from "./overlayEvents";

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
  handler: (...args: OverlayEventArgs[K]) => void,
  adapter?: HubAdapter | null
): void {
  const subscribed = adapter ?? getOverlayAdapter();

  useEffect(() => {
    // Компонент, смонтированный раньше подключения, не должен ни падать, ни
    // молча пропускать события. Подписка появится, когда стор вызовет start и
    // положит адаптер в реестр; сейчас её просто нет, и первый же обработчик
    // события придёт в никуда. Поэтому отсутствие адаптера — не ошибка, но
    // и не повод подписываться: событий не будет в любом случае.
    if (subscribed === null) {
      return undefined;
    }

    return subscribed.on(event, handler);
  }, [subscribed, event, handler]);
}
