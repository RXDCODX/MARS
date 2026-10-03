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
  private connection: HubConnection | null = null;
  private readonly hubUrl: string;

  constructor(hubUrl: string) {
    this.hubUrl = hubUrl;
  }

  get status(): HubStatus {
    if (this.connection === null) {
      return "disconnected";
    }

    return STATE_TO_STATUS[this.connection.state];
  }

  async connect(handlers: OverlayHandlers): Promise<void> {
    if (this.connection !== null) {
      throw new Error(
        "SignalRHubAdapter уже подключён: повторный connect закрыл бы старое соединение"
      );
    }

    const connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(LogLevel.Warning)
      .build();

    // Имена событий берутся из ключей карты обработчиков. Раньше они писались
    // строкой в каждом `connection.on(...)`, и разойтись с сервером могли
    // молча: `updatewaifuprizes` и `UpdateWaifuPrizes` компилировались оба.
    (Object.keys(handlers) as OverlayEventName[]).forEach(event => {
      const args = handlers[event] as unknown as (
        ...payload: unknown[]
      ) => void;

      connection.on(event, args);
    });

    connection.onclose(() => {
      this.connection = null;
    });

    this.connection = connection;

    try {
      await connection.start();
    } catch (error) {
      this.connection = null;
      throw error;
    }
  }

  async invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void> {
    if (this.connection === null) {
      throw new Error(
        `SignalRHubAdapter.invoke("${String(method)}") до connect()`
      );
    }

    await this.connection.invoke(method, ...args);
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;

    if (connection !== null) {
      await connection.stop();
    }
  }
}
