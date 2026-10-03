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
   * Выдаёт событие подписчикам.
   *
   * Имя проверяется типами по карте `OverlayEventArgs`: `emit("credits")` —
   * ошибка компиляции. Раньше регистр не проверялся ничем, и клиент подписывался
   * на `updatewaifuprizes`, тогда как сервер слал `UpdateWaifuPrizes`.
   */
  emit<K extends OverlayEventName>(
    event: K,
    ...args: OverlayEventArgs[K]
  ): void {
    if (this.handlers === null) {
      throw new Error(
        `FakeHubAdapter.emit("${event}") до connect(): подписчиков нет, событие ушло бы в никуда`
      );
    }

    const handler = this.handlers[event] as (
      ...handlerArgs: OverlayEventArgs[K]
    ) => void;

    handler(...args);
  }

  /** Снимает обработчик, не трогая состояние: удобно для проверки отписки. */
  unsubscribe(): void {
    this.handlers = null;
  }
}
