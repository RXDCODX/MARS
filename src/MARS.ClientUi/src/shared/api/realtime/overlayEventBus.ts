import { OVERLAY_EVENT_NAMES } from "./overlayEvents";
import type {
  OverlayEventArgs,
  OverlayEventName,
  OverlayHandlers,
} from "./overlayEvents";
import { signalrDebugEnabled } from "./signalrDebug";

/**
 * Шина событий оверлейного хаба — единственная точка входа для событий,
 * приходящих с сервера.
 *
 * До её появления у каждого события было два независимых пути:
 *  - карта обработчиков, которую стор передавал в `adapter.connect(...)` —
 *    давала лог «[TelegramusHub event] …»;
 *  - подписка через `useOverlayEvent` → `adapter.on(...)` — давала лог
 *    «[OverlayEvent] received …».
 * SignalR допускает несколько обработчиков на одно событие, поэтому каждая
 * входящая рамка логировалась дважды, и «владелец» события было не определить.
 *
 * Теперь на соединении регистрируется ровно одна карта — результат
 * `buildOverlayBusHandlers()`. Каждый её обработчик делает две вещи: один раз
 * пишет лог (если включён отладочный режим) и рассылает событие всем
 * подписчикам шины. Стор (очереди алертов) и компоненты подписываются на шину
 * через `subscribeToOverlayEvent`, а не на адаптер: подписка не зависит от
 * жизненного цикла соединения, переживает смену адаптера и StrictMode.
 *
 * Контракт имён событий остаётся в `overlayEvents.ts` — шина лишь
 * диспетчеризует, не изобретая своих имён.
 */

type Listener = (...args: unknown[]) => void;

const listeners = new Map<OverlayEventName, Set<Listener>>();

/**
 * Подписка на событие хаба через шину. Возвращает функцию отписки.
 *
 * В отличие от `adapter.on(...)`, не требует соединения и не привязана к
 * конкретному адаптеру: обработчик будет вызван, когда *любой* подключённый
 * адаптер получит событие. Именно это делает подписку устойчивой к
 * реконнекту и к смене адаптера в реестре.
 */
export function subscribeToOverlayEvent<K extends OverlayEventName>(
  event: K,
  handler: (...args: OverlayEventArgs[K]) => void
): () => void {
  let set = listeners.get(event);

  if (set === undefined) {
    set = new Set();
    listeners.set(event, set);
  }

  const stored = handler as Listener;

  set.add(stored);

  return () => {
    const current = listeners.get(event);

    if (current === undefined) {
      return;
    }

    current.delete(stored);

    if (current.size === 0) {
      listeners.delete(event);
    }
  };
}

/**
 * Сброс шины. Нужен в тестах между кейсами, чтобы подписчики не утекали.
 * В продакшене не вызывается: подписки живут столько же, сколько документ.
 */
export function resetOverlayEventBus(): void {
  listeners.clear();
}

/**
 * Сколько слушателей подписано на событие через шину.
 *
 * Нужен тестам, чтобы заметить утечку подписок: по одному факту доставки
 * утечка не видна, а счётчик виден. В продакшене не используется.
 */
export function overlayEventSubscriberCount(event: OverlayEventName): number {
  return listeners.get(event)?.size ?? 0;
}

/**
 * Карта обработчиков, которую передают в `adapter.connect(...)`.
 *
 * Единственная регистрация на соединении: каждое событие имеет ровно один
 * обработчик, который логирует и рассылает. Тип `OverlayHandlers` (mapped)
 * заставляет перечислить все 24 события — пропуск не компилируется, как и
 * раньше для карты стора.
 */
export function buildOverlayBusHandlers(): OverlayHandlers {
  const handlers = {} as OverlayHandlers;

  for (const eventName of OVERLAY_EVENT_NAMES) {
    const event = eventName as OverlayEventName;

    (handlers as Record<string, unknown>)[event] = (...args: unknown[]) => {
      if (signalrDebugEnabled()) {
        // eslint-disable-next-line no-console
        console.info(`[OverlayHub] event ${String(event)}`, ...args);
      }

      const set = listeners.get(event);

      if (set !== undefined) {
        for (const listener of set) {
          (listener as Listener)(...args);
        }
      }
    };
  }

  return handlers;
}