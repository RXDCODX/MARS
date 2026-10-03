import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  LogLevel,
} from "@microsoft/signalr";

import type { HubAdapter, HubInvocationMap, HubStatus } from "./hubAdapter";
import { resolveHubUrl } from "./hubUrl";
import type {
  OverlayEventArgs,
  OverlayEventName,
  OverlayHandlers,
} from "./overlayEvents";

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
 * Настоящий адаптер хаба.
 *
 * Адрес хаба читается здесь, в момент вызова, а не на уровне модуля. Раньше
 * `VITE_BASE_PATH` захватывался при импорте, и в vitest давал строку
 * `"undefinedhubs/telegramus"` — тест `OBSComponentsSmokeCoverage` был обязан
 * держать её в списке ожидаемых значений, то есть проверял сам себя.
 *
 * Адрес собирается через `resolveHubUrl`, а не склейкой шаблонной строки:
 * шаблон `${import.meta.env.VITE_BASE_PATH}hubs/overlay` при незаданной
 * переменной даёт `undefinedhubs/overlay`, и та же ошибка уехала бы в боевое
 * окружение, где переменную тоже могут не задать.
 *
 * Пути совпадают с маршрутами `overlay-hub`, `tuna-hub`, `scoreboard-hub` и
 * `soundrequest-hub` в `src/MARS.Gateway/appsettings.json`.
 */
export function createOverlayHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/overlay"));
}

export function createTunaHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/tuna"));
}

export function createScoreboardHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/scoreboard"));
}

export function createSoundRequestHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/soundrequest"));
}

class SignalRHubAdapter implements HubAdapter<OverlayHandlers> {
  private readonly connection: HubConnection;

  /**
   * Обещание текущего подключения, а не флаг.
   *
   * Идемпотентность обязательна: ребенок и родитель на маршруте `/waifu`
   * смонтированы в одной пачке эффектов, и оба звали `start`. Второй
   * `HubConnection.start()` отвергается с «Cannot start a HubConnection that is
   * not in the 'Disconnected' state», а catch в сторе обнулял адаптер — все
   * подписки документа оставались без хаба при зелёном индикаторе.
   *
   * Флаг `started`, взводимый после `await`, отставал: второй вызов успевал
   * пройти мимо него и начать второе подключение к тому же сокету.
   */
  private starting: Promise<void> | null = null;

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

  /**
   * Подключение. Повторный вызов ничего не делает, а не бросает.
   *
   * Идемпотентность обязательна: `start()` зовут несколько компонентов из
   * useEffect, StrictMode вызывает эффекты дважды, а на маршруте `/waifu` ребенок и
   * родитель попадают в одну пачку пассивных эффектов. Бросать здесь означало бы
   * уронить монтирование компонента из-за того, что его обернули в StrictMode.
   */
  connect(handlers?: OverlayHandlers): Promise<void> {
    // Уже идёт подключение или канал открыт: отдаём то же обещание. Начать
    // второе нельзя — сокет один, и SignalR такой вызов отвергает.
    if (this.starting !== null) {
      return this.starting;
    }

    // Карта обработчиков необязательна, и это не формальность: потребитель
    // забирает свои подписки через `acquire(handlers)`, а `start` зовётся без
    // карты. Раньше `connect` всегда получал объект, и `Object.keys(undefined)`
    // уронил подключение хаба табло — юнит-тесты этого не видели, потому что
    // работают на подделке, а поймал только E2E на живом стенде.
    for (const [event, handler] of Object.entries(handlers ?? {})) {
      (
        this.connection.on.bind(this.connection) as (
          event: string,
          handler: (...payload: unknown[]) => void
        ) => void
      )(event, handler as (...payload: unknown[]) => void);
    }

    this.starting = this.connection.start().finally(() => {
      // Гвард живёт только на время подключения. После него признаком открытого
      // канала остаётся состояние самого `HubConnection`, а ошибочно оставленный
      // гвард не дал бы подключиться снова после отключения.
      this.starting = null;
    });

    return this.starting;
  }

  async invoke<K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ): Promise<void> {
    await this.connection.invoke(method, ...args);
  }

  async send<T = unknown>(method: string, ...args: unknown[]): Promise<T> {
    return (await this.connection.invoke(method, ...args)) as T;
  }

  /**
   * Отключение. Идемпотентно: повторный вызов не бросает.
   *
   * Проверяется состояние самого соединения, а не флаг: после `stop` тот же
   * адаптер должен подняться снова, и оставленный взведённый гвард не дал бы.
   * `stop()` у `HubConnection` на уже закрытом сокете безвреден.
   */
  async disconnect(): Promise<void> {
    this.starting = null;

    await this.connection.stop();
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
