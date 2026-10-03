import type {
  OverlayEventArgs,
  OverlayEventName,
  OverlayHandlers,
} from "./overlayEvents";

/**
 * Состояние соединения с хабом.
 *
 * `idle` означает «ещё не начинали» и отличается от `disconnected`, который
 * значит «были подключены, разъединились». Оверлей показывает по ним разные
 * состояния, и сводить их к одному — значит потерять это различие.
 */
export type HubStatus =
  | "idle"
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
 *
 * Про `MuteAll`, `UnmuteSessions`, `ExplosionGo`, `MikuMikuDeleteTwitchMessages`,
 * `ObsFreeze` и `ObsUnfreeze`: это вызовы, которые перенесённый из монолита
 * клиент шлёт в хаб, но на сервере их не реализует — ни `OverlayHub`, ни
 * `TelegramusGrpcService` таких методов не имеют. Список собран из всех
 * `invoke(...)` компонентов оверлея, а не из контракта, поэтому они здесь
 * названы явно: иначе опечатка и несуществующий метод выглядели бы одинаково —
 * и тихо ломали бы поведение на стенде.
 */
export type HubInvocationMap = {
  TwitchMsg: [message: string];
  LogError: [message: string];
  ObsFreeze: [];
  ObsUnfreeze: [];
  MuteAll: [];
  UnmuteSessions: [];
  ExplosionGo: [];
  MikuMikuDeleteTwitchMessages: [];
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

  /**
   * Вызов без проверки имени, с ответом.
   *
   * Нужен там, где карты вызовов нет: стор MikuMonday зовёт MikuMondayTracks и
   * DecrementAvailableMikuTrack, которых нет ни в `HubInvocationMap`, ни в
   * контракте хаба. Отдельный метод, а не ослабление `invoke`, чтобы типизованные
   * вызовы оверлея не потеряли проверку вместе с этими двумя.
   */
  send<T = unknown>(method: string, ...args: unknown[]): Promise<T>;

  /** Отключение. Идемпотентно: повторный вызов не бросает. */
  disconnect(): Promise<void>;

  /**
   * Подписка одного компонента на одно событие. Возвращает отписку.
   *
   * Отдельный метод, а не поле в состоянии стора, потому что подписчики у
   * компонентов свои и с замыканиями: чат вертикальный и горизонтальный
   * слушают один `NewMessage` по-разному. Раньше это означало либо
   * `useSignalREffect` из react-signalr, либо собственное соединение на каждый
   * стор — четыре лишних соединения к одному хабу.
   *
   * Регистрация не требует соединения: обработчик привязывается сразу, а
   * событие придёт, когда `connect` откроет канал. Иначе компонент, смонтированный
   * раньше подключения, молча пропустил бы первые события.
   */
  on<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): () => void;

  /** Снятие подписки. Идентичен отписке, возвращённой `on`. */
  off<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): void;
}

/** Имя события вместе с аргументами — внутренняя форма для диспетчера. */
export type OverlayInvocation = {
  [K in OverlayEventName]: [K, ...OverlayArgsOf<K>];
}[OverlayEventName];

type OverlayArgsOf<K extends OverlayEventName> = Parameters<OverlayHandlers[K]>;
