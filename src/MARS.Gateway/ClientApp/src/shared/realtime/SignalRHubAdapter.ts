import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  LogLevel,
} from "@microsoft/signalr";

import type { HubAdapter, HubInvocationMap, HubStatus } from "./hubAdapter";
import type { OverlayEventName, OverlayHandlers } from "./overlayEvents";

/**
 * Настоящий транспорт: `@microsoft/signalr`.
 *
 * Единственный модуль проекта, который знает про `HubConnection`. Всё
 * остальное работает с `HubAdapter`, поэтому `FakeHubAdapter` в тестах не
 * требует ни библиотеки, ни сети, ни моков.
 *
 * Раньше `HubConnection` жил прямо в состоянии `telegramusHubStore` и был
 * классом в типе состояния. Из-за этого подставить подделку было нельзя, а
 * билдер соединения собирался на уровне модуля: `import.meta.env.VITE_BASE_PATH`
 * захватывался при импорте и в vitest давал `"undefinedhubs/telegramus"` —
 * строку, которую тест `OBSComponentsSmokeCoverage` был обязан шить в ожидаемые
 * значения, то есть проверял сам себя.
 */

/** Задержка перед повтором после обрыва. */
const RECONNECT_DELAY_MS = 5000;

const retryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: () => RECONNECT_DELAY_MS,
};

const STATE_TO_STATUS: Record<HubConnectionState, HubStatus> = {
  [HubConnectionState.Disconnected]: "disconnected",
  [HubConnectionState.Connecting]: "connecting",
  [HubConnectionState.Connected]: "connected",
  [HubConnectionState.Reconnecting]: "reconnecting",
  [HubConnectionState.Disconnecting]: "disconnected",
};

export function createSignalRHubAdapter(hubUrl: string): HubAdapter {
  return new SignalRHubAdapter(hubUrl);
}

/**
 * Настоящий адаптер оверлея.
 *
 * Адрес хаба читается здесь, в момент вызова, а не на уровне модуля. Раньше
 * `VITE_BASE_PATH` захватывался при импорте, и в vitest давал строку
 * `"undefinedhubs/telegramus"` — тест `OBSComponentsSmokeCoverage` был обязан
 * держать её в списке ожидаемых значений, то есть проверял сам себя.
 *
 * Путь хаба совпадает с маршрутом `overlay-hub` в `src/MARS.Gateway/appsettings.json`.
 */
export function createOverlayHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(
    `${import.meta.env.VITE_BASE_PATH}hubs/overlay`
  );
}

class SignalRHubAdapter implements HubAdapter {
  private readonly connection: HubConnection;
  private started = false;

  constructor(hubUrl: string) {
    // Соединение строится сразу, а не в connect(): компоненты подписываются
    // через on() при монтировании, то есть до того, как кто-то откроет канал.
    // Построение HubConnection сеть не трогает — подключает его start().
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(LogLevel.Warning)
      .build();
  }

  get status(): HubStatus {
    return STATE_TO_STATUS[this.connection.state];
  }

  async connect(handlers: OverlayHandlers): Promise<void> {
    if (this.started) {
      throw new Error(
        "SignalRHubAdapter уже подключён: повторный connect закрыл бы прежнее соединение"
      );
    }

    // Обработчики стора. Имена берутся из ключей карты, а не пишутся строкой:
    // разойтись с сервером они могли молча, updatewaifuprizes и
    // UpdateWaifuPrizes компилировались оба.
    (Object.keys(handlers) as OverlayEventName[]).forEach(event => {
      const handler = handlers[event] as unknown as (
        ...payload: unknown[]
      ) => void;

      this.connection.on(event, handler);
    });

    await this.connection.start();

    this.started = true;
  }

  async invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void> {
    await this.connection.invoke(method, ...args);
  }

  async disconnect(): Promise<void> {
    if (this.started) {
      await this.connection.stop();
    }
  }

  on<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): () => void {
    // Имя приходит из карты типов, поэтому опечатка в регистре не собирается.
    this.connection.on(event, handler as (...payload: unknown[]) => void);

    return () => {
      this.off(event, handler);
    };
  }

  off<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): void {
    this.connection.off(event, handler as (...payload: unknown[]) => void);
  }
}
