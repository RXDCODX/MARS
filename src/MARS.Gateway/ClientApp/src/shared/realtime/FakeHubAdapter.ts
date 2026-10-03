import type { HubAdapter, HubInvocationMap, HubStatus } from "./hubAdapter";
import type {
  OverlayEventArgs,
  OverlayEventName,
  OverlayHandlers,
} from "./overlayEvents";

/**
 * Подделка транспорта для тестов.
 *
 * Единственное назначение — дать тесту вызвать обработчик события и увидеть
 * состояние стора, без сети, без `@microsoft/signalr` и без `vi.mock`
 * чужой библиотеки. Раньше это было невозможно: `HubConnection` лежал в
 * состоянии стор-а классом, и подставить фейк в состояние не позволяли типы.
 */
export class FakeHubAdapter implements HubAdapter {
  /** Вызовы клиента, сделанные через `invoke`. Для ассертов. */
  readonly sent: { method: string; args: readonly unknown[] }[] = [];

  /**
   * Сколько раз звали `connect` и `disconnect`.
   *
   * Нужно, чтобы отличить «соединение одно» от «соединение переоткрыли»: сам
   * адаптер на это не смотрит, а проверка идемпотентности старта без счётчика
   * проходила бы на любой реализации, потому что подделка ничего не запрещает.
   */
  connectCalls = 0;
  disconnectCalls = 0;

  /** Обработчики, переданные в `connect`. `null`, пока не подключены. */
  private handlers: OverlayHandlers | null = null;

  /**
   * Подписки компонентов поверх основной карты: событие → набор обработчиков.
   * Набор, а не один обработчик: чаты вертикальный и горизонтальный слушают
   * один `NewMessage` по-разному и живут одновременно.
   */
  private readonly subscriptions = new Map<
    string,
    Set<(...args: never[]) => void>
  >();

  private currentStatus: HubStatus = "disconnected";

  get status(): HubStatus {
    return this.currentStatus;
  }

  async connect(handlers: OverlayHandlers): Promise<void> {
    this.handlers = handlers;
    this.currentStatus = "connected";
    this.connectCalls += 1;
  }

  async invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void> {
    this.sent.push({ method, args });
  }

  /**
   * Вызов без проверки имени. Ответа нет: подделка не изображает сервер, её
   * дело — зафиксировать, что метод вызван, что и проверяют тесты.
   */
  async send<T = unknown>(method: string, ...args: unknown[]): Promise<T> {
    this.sent.push({ method, args });

    return undefined as T;
  }

  async disconnect(): Promise<void> {
    this.handlers = null;
    this.currentStatus = "disconnected";
    this.disconnectCalls += 1;
  }

  /**
   * Выдаёт событие с именем вне карты оверлея: `ReceiveState`, `SkipTrack` и
   * прочие методы хабов табло и очереди звуковых запросов.
   *
   * Отдельный метод, а не ослабление `emit`: имя события оверлея проверяется
   * типами, и ослабление погасило бы ту проверку ради нескольких хабов.
   * Обработчик ищется и в основной карте `connect`, и в подписках `on` — так же,
   * как в `emit`.
   */
  emitEvent(event: string, payload: unknown): void {
    // Карта набрана именами оверлейных событий, а здесь имя приходит строкой:
    // индекс по строке на ней невозможен, поэтому доступ идёт через приведение.
    const handlers = this.handlers as Record<
      string,
      ((payload: unknown) => void) | undefined
    > | null;
    const fromConnect = handlers?.[event];
    const fromOn = this.subscriptions.get(event);

    if (fromConnect === undefined && fromOn === undefined) {
      throw new Error(
        `FakeHubAdapter.emitEvent("${event}"): подписчиков нет, событие ушло бы в никуда. ` +
          "Сначала connect() или on()."
      );
    }

    this.deliveries.set(event, (this.deliveries.get(event) ?? 0) + 1);

    if (fromConnect !== undefined) {
      (fromConnect as (payload: unknown) => void)(payload);
    }

    if (fromOn !== undefined) {
      for (const handler of fromOn) {
        (handler as unknown as (payload: unknown) => void)(payload);
      }
    }
  }

  /**
   * Подписка компонента. В отличие от `emit`, не требует `connect`: настоящий
   * транспорт тоже принимает обработчики до открытия канала, иначе компонент,
   * смонтированный раньше подключения, пропустил бы первые события.
   */
  on<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): () => void {
    const existing = this.subscriptions.get(event) ?? new Set();
    existing.add(handler as (...args: never[]) => void);
    this.subscriptions.set(event, existing);

    return () => {
      this.off(event, handler);
    };
  }

  off<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): void {
    const existing = this.subscriptions.get(event);

    existing?.delete(handler as (...args: never[]) => void);

    if (existing !== undefined && existing.size === 0) {
      this.subscriptions.delete(event);
    }
  }

  /**
   * Выдаёт событие подписчикам: сначала основной карте (если подключена),
   * затем всем, кто подписался через `on`.
   *
   * Имя проверяется типами по карте `OverlayEventArgs`: `emit("credits")` —
   * ошибка компиляции. Раньше регистр не проверялся ничем, и клиент
   * подписывался на `deletemessage`, тогда как сервер слал `DeleteMessage`.
   *
   * Если подписчиков нет ни одного, бросается исключение. Молчать нельзя:
   * тест, который решил, что событие ушло, а на деле отправил его в пустоту,
   * проходит зелёным и проверяет ничего. Подписка через `on` при этом
   * разрешена до `connect`, поэтому подписчик есть — и исключения нет.
   */
  emit<K extends OverlayEventName>(
    event: K,
    ...args: OverlayEventArgs[K]
  ): void {
    const subscribed = this.subscriptions.get(event);

    if (this.handlers === null && subscribed === undefined) {
      throw new Error(
        `FakeHubAdapter.emit("${event}"): подписчиков нет, событие ушло бы в никуда. ` +
          "Сначала connect() или on()."
      );
    }

    this.deliveries.set(event, (this.deliveries.get(event) ?? 0) + 1);

    if (this.handlers !== null) {
      const handler = this.handlers[event] as (
        ...handlerArgs: OverlayEventArgs[K]
      ) => void;

      handler(...args);
    }

    if (subscribed !== undefined) {
      for (const handler of subscribed) {
        (handler as unknown as (...eventArgs: OverlayEventArgs[K]) => void)(
          ...args
        );
      }
    }
  }

  /**
   * Сколько обработчиков подписано на событие.
   *
   * Нужен, чтобы заметить утечку подписок: по одному «событие дошло» это
   * не видно — оно доходит и один раз, и десять. Счётчик виден.
   */
  subscriberCount(event: string): number {
    return this.subscriptions.get(event)?.size ?? 0;
  }

  /**
   * Сколько раз событие было выдано подписчикам — из карты `connect` и из `on`.
   *
   * Нужно, чтобы отличить «событие дошло один раз» от «дошло десять раз»: по
   * одному факту доставки утечка подписок не видна, а она и была причиной того,
   * что каждое перерисовывание экрана добавляло обработчик.
   */
  delivered(event: string): number {
    return this.deliveries.get(event) ?? 0;
  }

  /** Счётчик выдач по каждому событию. */
  private readonly deliveries = new Map<string, number>();
}
