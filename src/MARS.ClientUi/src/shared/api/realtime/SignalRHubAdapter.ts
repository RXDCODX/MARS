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

/** Задержка перед повтором после обрыва. */
const RECONNECT_DELAY_MS = 5000;

/** Сколько раз пытаться первое рукопожатие, прежде чем сдаться. */
const INITIAL_CONNECT_MAX_ATTEMPTS = 5;

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

export function createOverlayHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/overlay"));
}

export function createScoreboardHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/scoreboard"));
}

export function createSoundRequestHubAdapter(): HubAdapter {
  return createSignalRHubAdapter(resolveHubUrl("hubs/soundrequest"));
}

class SignalRHubAdapter implements HubAdapter<OverlayHandlers> {
  private readonly connection: HubConnection;

  private starting: Promise<void> | null = null;

  /** Отмена отложенного повтора (disconnect во время ожидания). */
  private delayResolve: (() => void) | null = null;
  private delayTimer: ReturnType<typeof setTimeout> | null = null;

  /** Флаг остановки: ретраи не должны продолжаться после disconnect. */
  private stopped = false;

  constructor(hubUrl: string) {
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(LogLevel.None)
      .build();
  }

  get status(): HubStatus {
    return STATE_TO_STATUS[this.connection.state];
  }

  connect(handlers?: OverlayHandlers): Promise<void> {
    if (this.starting !== null) {
      return this.starting;
    }

    for (const [event, handler] of Object.entries(handlers ?? {})) {
      (
        this.connection.on.bind(this.connection) as (
          event: string,
          handler: (...payload: unknown[]) => void
        ) => void
      )(event, handler as (...payload: unknown[]) => void);
    }

    this.stopped = false;

    this.starting = this.startWithRetry().finally(() => {
      this.starting = null;
    });

    return this.starting;
  }

  /**
   * Первое рукопожатие с повторами. `withAutomaticReconnect` покрывает только
   * обрыв уже установленного соединения; неудачный первый negotiate (холодный
   * старт Gateway/сервиса) иначе навсегда оставлял бы хаб в error до F5.
   *
   * Повторы отменяются флагом `stopped`, который взводит `disconnect()` — иначе
   * остановка во время ожидания приводила бы к лишним попыткам.
   */
  private async startWithRetry(): Promise<void> {
    let attempt = 0;

    while (true) {
      if (this.stopped) {
        throw new Error("Connection stopped");
      }

      try {
        await this.connection.start();
        return;
      } catch (error) {
        attempt += 1;

        if (this.stopped || attempt >= INITIAL_CONNECT_MAX_ATTEMPTS) {
          throw error;
        }

        await this.delay(RECONNECT_DELAY_MS);
      }
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => {
      this.delayResolve = resolve;
      this.delayTimer = setTimeout(() => {
        this.delayResolve = null;
        this.delayTimer = null;
        resolve();
      }, ms);
    });
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

  async disconnect(): Promise<void> {
    this.stopped = true;
    this.starting = null;

    if (this.delayTimer !== null) {
      clearTimeout(this.delayTimer);
      this.delayTimer = null;
    }
    if (this.delayResolve !== null) {
      const resolve = this.delayResolve;
      this.delayResolve = null;
      resolve();
    }

    await this.connection.stop();
  }

  on<K extends OverlayEventName>(
    event: K,
    handler: (...args: OverlayEventArgs[K]) => void
  ): () => void {
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
