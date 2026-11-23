import { useEffect } from "react";

import type {
  OverlayEventArgs as OverlayEventArguments,
  OverlayEventName,
} from "./overlayEvents";
import { subscribeToOverlayEvent } from "./overlayEventBus";
import { signalrDebugEnabled } from "./signalrDebug";

/**
 * Подписка компонента на событие оверлейного хаба.
 *
 * Единая точка входа для событий — шина `overlayEventBus`. На соединении
 * регистрируется одна карта обработчиков, а компоненты подписываются на шину,
 * поэтому подписка не зависит от адаптера и от его жизненного цикла: не
 * требует соединения, переживает смену адаптера и StrictMode, а размонтирование
 * снимает подписку со шины.
 *
 * Имя события по-прежнему берётся из карты `OverlayEventArgs`, а не из строки.
 */
export function useOverlayEvent<K extends OverlayEventName>(
  event: K,
  handler: (...arguments_: OverlayEventArguments[K]) => void
): void {
  useEffect(() => {
    if (signalrDebugEnabled()) {
      // eslint-disable-next-line no-console
      console.debug(`[OverlayEvent] subscribe ${String(event)}`);
    }

    const unsubscribe = subscribeToOverlayEvent(event, handler);

    return () => {
      if (signalrDebugEnabled()) {
        // eslint-disable-next-line no-console
        console.debug(`[OverlayEvent] unsubscribe ${String(event)}`);
      }

      unsubscribe();
    };
  }, [event, handler]);
}