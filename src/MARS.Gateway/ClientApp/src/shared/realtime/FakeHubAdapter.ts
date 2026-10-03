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
  }

  async invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void> {
    this.sent.push({ method, args });
  }

  async disconnect(): Promise<void> {
    this.handlers = null;
    this.currentStatus = "disconnected";
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

  /** Снимает обработчик, не трогая состояние: удобно для проверки отписки. */
  unsubscribe(): void {
    this.handlers = null;
  }
}
