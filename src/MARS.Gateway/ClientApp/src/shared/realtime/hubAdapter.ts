import type { OverlayEventName, OverlayHandlers } from "./overlayEvents";

/** Состояние соединения с хабом. */
export type HubStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting";

/**
 * Вызовы, которые оверлей шлёт серверу.
 *
 * Раньше это был `invoke(methodName: string, ...args: unknown[])`: имя не
 * проверялось, и опечатка в строке проходила компиляцию и падала в рантайме на
 * живом стенде.
 */
export type HubInvocationMap = {
  TwitchMsg: [message: string];
  LogError: [message: string];
  ObsFreeze: [];
  ObsUnfreeze: [];
};

/**
 * Единственное, что приложение знает о транспорте.
 *
 * Всё, что раньше протекало в сторы, — `HubConnection` из
 * `@microsoft/signalr`, строковые имена событий и жизненный цикл соединения —
 * спрятано за этим интерфейсом. Настоящая реализация строит соединение, а
 * `FakeHubAdapter` в тестах позволяет вызвать обработчик вообще без сети.
 */
export interface HubAdapter {
  /** Текущее состояние соединения. Читается стором для индикации в UI. */
  readonly status: HubStatus;

  /** Подписка на события хаба. Повторный вызов без `disconnect` запрещён. */
  connect(handlers: OverlayHandlers): Promise<void>;

  /** Вызов клиентского метода хаба. Имя проверяется типами. */
  invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void>;

  /** Отключение. Идемпотентно: повторный вызов не бросает. */
  disconnect(): Promise<void>;
}

/** Имя события вместе с аргументами — внутренняя форма для диспетчера. */
export type OverlayInvocation = {
  [K in OverlayEventName]: [K, ...OverlayArgsOf<K>];
}[OverlayEventName];

type OverlayArgsOf<K extends OverlayEventName> = Parameters<OverlayHandlers[K]>;
