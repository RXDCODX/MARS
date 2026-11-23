import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  IRetryPolicy,
  LogLevel,
} from "@microsoft/signalr";

import { logger } from "@/shared/lib/logger";

import type { HubAdapter, HubInvocationMap, HubStatus } from "./hubAdapter";
import { resolveHubUrl } from "./hubUrl";
import type {
  OverlayEventArgs as OverlayEventArguments,
  OverlayEventName,
  OverlayHandlers,
} from "./overlayEvents";
import { signalrDebugEnabled } from "./signalrDebug";

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
  private readonly hubUrl: string;
  private readonly connection: HubConnection;

  private starting: Promise<void> | null = null;

  /** Отмена отложенного повтора (disconnect во время ожидания). */
  private delayResolve: (() => void) | null = null;
  private delayTimer: ReturnType<typeof setTimeout> | null = null;

  /** Флаг остановки: ретраи не должны продолжаться после disconnect. */
  private stopped = false;

  constructor(hubUrl: string) {
    this.hubUrl = hubUrl;
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect(retryPolicy)
      .configureLogging(signalrDebugEnabled() ? logger : LogLevel.Warning)
      .build();

    if (signalrDebugEnabled()) {
      console.info(`[SignalR] adapter created → ${hubUrl}`);
    }
  }

  get status(): HubStatus {
    return STATE_TO_STATUS[this.connection.state];
  }

  connect(handlers?: OverlayHandlers): Promise<void> {
    if (this.starting !== null) {
      if (signalrDebugEnabled()) {
        console.debug(
          `[SignalR] connect() reused in-flight handshake → ${this.hubUrl}`
        );
      }
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

    if (signalrDebugEnabled()) {
      console.info(`[SignalR] connect() → ${this.hubUrl}`);
    }

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

      attempt += 1;

      if (signalrDebugEnabled()) {
        console.info(
          `[SignalR] negotiate → ${this.hubUrl} (attempt ${attempt}/${INITIAL_CONNECT_MAX_ATTEMPTS})`
        );
      }

      try {
        await this.connection.start();
        if (signalrDebugEnabled()) {
          console.info(
            `[SignalR] connected → ${this.hubUrl} (attempt ${attempt})`
          );

          console.info(`[SignalR] endpoint OK → ${this.hubUrl}`);
        }
        return;
      } catch (error) {
        const status = extractHttpStatus(error);
        if (signalrDebugEnabled()) {
          console.error(
            `[SignalR] attempt ${attempt} failed → ${this.hubUrl}:`,
            error
          );

          console.error(
            `[SignalR] endpoint FAIL → ${this.hubUrl}${
              status === null ? "" : ` (HTTP ${status})`
            }`
          );
        }

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
    ...arguments_: HubInvocationMap[K]
  ): Promise<void> {
    await this.connection.invoke(method, ...arguments_);
  }

  async send<T = unknown>(
    method: string,
    ...arguments_: unknown[]
  ): Promise<T> {
    return (await this.connection.invoke(method, ...arguments_)) as T;
  }

  async disconnect(): Promise<void> {
    if (signalrDebugEnabled()) {
      console.info(`[SignalR] disconnect() → ${this.hubUrl}`);
    }

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

    if (signalrDebugEnabled()) {
      console.info(`[SignalR] disconnected → ${this.hubUrl}`);
    }
  }

  on<K extends OverlayEventName>(
    event: K,
    handler: (...arguments_: OverlayEventArguments[K]) => void
  ): () => void {
    this.connection.on(event, handler as (...payload: unknown[]) => void);

    return () => {
      this.off(event, handler);
    };
  }

  off<K extends OverlayEventName>(
    event: K,
    handler: (...arguments_: OverlayEventArguments[K]) => void
  ): void {
    this.connection.off(event, handler as (...payload: unknown[]) => void);
  }
}

/**
 * Пытается вытащить HTTP-статус из ошибки SignalR (502/503/404/...).
 * SignalR кладёт статус в разные поля в зависимости от транспорта, поэтому
 * проверяем несколько известных мест.
 */
function extractHttpStatus(error: unknown): number | null {
  if (error === null || typeof error !== "object") {
    return null;
  }

  const record = error as Record<string, unknown>;

  const direct = record.statusCode ?? record.status ?? record.httpStatusCode;
  if (typeof direct === "number") {
    return direct;
  }

  const message = typeof record.message === "string" ? record.message : "";
  const match = message.match(/Status code '(\d{3})'/);
  if (match !== null) {
    return Number(match[1]);
  }

  const match2 = message.match(
    /(\d{3})\s*(Bad Gateway|Service Unavailable|Not Found)/i
  );
  if (match2 !== null) {
    return Number(match2[1]);
  }

  return null;
}
