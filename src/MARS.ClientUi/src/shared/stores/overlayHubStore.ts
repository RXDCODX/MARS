import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type { FrogAlertProps } from "@/overlays/FrogAlerts/helper";
import type { FumoAlertProps } from "@/overlays/FumoAlerts/helper";
import type { MikuAlertProps } from "@/overlays/MikuAlerts/helper";
import type { WaifuAlertProps } from "@/overlays/WaifuAlerts/helper";
import type {
  HubAdapter,
  HubInvocationMap,
  HubStatus,
} from "@/shared/api/realtime/hubAdapter";
import {
  buildOverlayBusHandlers,
  subscribeToOverlayEvent,
} from "@/shared/api/realtime/overlayEventBus";

import { setOverlayAdapter } from "@/shared/api/realtime/overlayHub";
import { createOverlayHubAdapter } from "@/shared/api/realtime/SignalRHubAdapter";
import { signalrDebugEnabled } from "@/shared/api/realtime/signalrDebug";

import { unpackWaifuHusband, unpackWaifuRoll } from "./telegramusHubUnpack";

/**
 * Очередь алертов оверлея.
 *
 * Состояние не знает про транспорт: в нём лежит `status` и очередь алертов,
 * а соединение принадлежит `HubAdapter`. Обработчики событий регистрируются
 * не здесь, а в шине `overlayEventBus` — стор подписывается на неё в `start`
 * и отписывается в `stop`. Из-за этого обработчик вызывается напрямую — и в
 * тесте, и при отладке — и событие имеет ровно одного владельца.
 */

/** Очередь алертов одного вида. */
interface AlertQueue<TProps> {
  /** Отложенные алерты: всё, что не поместилось на экран. */
  queue: TProps[];
  /** Алерт на экране. */
  current?: TProps;
  /** Показывается ли алерт. Различается от `current !== undefined`. */
  showing: boolean;
}

type QueueKind = "waifu" | "fumo" | "frog" | "miku";

const QUEUE_KEYS = {
  waifu: {
    queueKey: "messages",
    currentKey: "currentMessage",
    showingKey: "isWaifuShowing",
  },
  fumo: {
    queueKey: "fumoMessages",
    currentKey: "currentFumoMessage",
    showingKey: "isFumoShowing",
  },
  frog: {
    queueKey: "frogMessages",
    currentKey: "currentFrogMessage",
    showingKey: "isFrogShowing",
  },
  miku: {
    queueKey: "mikuMessages",
    currentKey: "currentMikuMessage",
    showingKey: "isMikuShowing",
  },
} as const satisfies Record<QueueKind, Record<string, string>>;

interface TelegramusHubState {
  status: HubStatus | "error";
  isConnected: boolean;

  messages: WaifuAlertProps[];
  currentMessage?: WaifuAlertProps;
  isWaifuShowing: boolean;

  fumoMessages: FumoAlertProps[];
  currentFumoMessage?: FumoAlertProps;
  isFumoShowing: boolean;

  frogMessages: FrogAlertProps[];
  currentFrogMessage?: FrogAlertProps;
  isFrogShowing: boolean;

  mikuMessages: MikuAlertProps[];
  currentMikuMessage?: MikuAlertProps;
  isMikuShowing: boolean;
}

interface TelegramusHubActions {
  start: (adapter?: HubAdapter) => Promise<void>;

  stop: () => Promise<void>;

  invoke: <K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ) => Promise<void>;

  reset: () => void;

  dequeueCurrent: () => void;
  dequeueFumoCurrent: () => void;
  dequeueFrogCurrent: () => void;
  dequeueMikuCurrent: () => void;
}

/**
 * Начальное состояние.
 */
const initialState: TelegramusHubState = {
  status: "idle",
  isConnected: false,
  messages: [],
  currentMessage: undefined,
  isWaifuShowing: false,
  fumoMessages: [],
  currentFumoMessage: undefined,
  isFumoShowing: false,
  frogMessages: [],
  currentFrogMessage: undefined,
  isFrogShowing: false,
  mikuMessages: [],
  currentMikuMessage: undefined,
  isMikuShowing: false,
};

function enqueue<TProps>(
  current: AlertQueue<TProps>,
  next: TProps
): AlertQueue<TProps> {
  if (!current.showing) {
    return { queue: [...current.queue], current: next, showing: true };
  }

  return {
    queue: [...current.queue, next],
    current: current.current,
    showing: true,
  };
}

function advance<TProps>(current: AlertQueue<TProps>): AlertQueue<TProps> {
  const [next, ...rest] = current.queue;

  if (next === undefined) {
    return { queue: [], current: undefined, showing: false };
  }

  return { queue: rest, current: next, showing: true };
}

export const useOverlayHubStore = create<
  TelegramusHubState & TelegramusHubActions
>()(
  devtools(
    (set, get) => {
      let connected: HubAdapter | null = null;
      let starting: Promise<void> | null = null;
      let queueUnsubs: Array<() => void> = [];

      const applyQueue = <TProps>(
        kind: QueueKind,
        next: AlertQueue<TProps>
      ) => {
        const { queueKey, currentKey, showingKey } = QUEUE_KEYS[kind];

        set({
          [queueKey]: next.queue,
          [currentKey]: next.current,
          [showingKey]: next.showing,
        } as never);
      };

      const readQueue = <TProps>(kind: QueueKind): AlertQueue<TProps> => {
        const { queueKey, currentKey, showingKey } = QUEUE_KEYS[kind];
        const state = get() as unknown as Record<string, unknown>;

        return {
          queue: (state[queueKey] as TProps[]) ?? [],
          current: state[currentKey] as TProps | undefined,
          showing: Boolean(state[showingKey]),
        };
      };

      // Обработчики событий очередей. Регистрируются на шине в start().
      const handleWaifuRoll = (payload: unknown) => {
        const { waifu, displayName, color } = unpackWaifuRoll(payload);
        const parsed: WaifuAlertProps = {
          waifu,
          displayName,
          color,
          waifuHusband: null,
        } as unknown as WaifuAlertProps;

        applyQueue(
          "waifu",
          enqueue(readQueue<WaifuAlertProps>("waifu"), parsed)
        );
      };

      const handleAddNewWaifu = (payload: unknown) => {
        const { waifu, displayName } = unpackWaifuRoll(payload);
        const marked = { ...(waifu as object), isAdded: true };

        applyQueue<WaifuAlertProps>("waifu", {
          ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
            waifu: marked,
            displayName,
          } as WaifuAlertProps),
        });
      };

      const handleMergeWaifu = (payload: unknown) => {
        const { waifu, husband } = unpackWaifuHusband(payload);
        const marked = { ...(waifu as object), isMerged: true };

        applyQueue<WaifuAlertProps>("waifu", {
          ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
            waifu: marked,
            displayName: husband?.displayName ?? "",
            waifuHusband: husband,
          } as WaifuAlertProps),
        });
      };

      const handleShowCurrentWife = (payload: unknown) => {
        const { waifu, husband } = unpackWaifuHusband(payload);

        applyQueue<WaifuAlertProps>("waifu", {
          ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
            waifu,
            displayName: "",
            waifuHusband: husband,
            isReminder: true,
          } as WaifuAlertProps),
        });
      };

      const handleFumoRoll = (payload: unknown) => {
        const branch = (payload ?? {}) as Record<string, unknown>;

        applyQueue<FumoAlertProps>(
          "fumo",
          enqueue(readQueue("fumo"), {
            ...branch,
            fumo: branch.fumo,
            twitchUser: branch.user,
          } as FumoAlertProps)
        );
      };

      const handleFrogRoll = (payload: unknown) => {
        const branch = (payload ?? {}) as Record<string, unknown>;

        applyQueue<FrogAlertProps>(
          "frog",
          enqueue(readQueue("frog"), {
            ...branch,
            frog: branch.frog,
            twitchUser: branch.user,
          } as FrogAlertProps)
        );
      };

      const handleMikuRoll = (payload: unknown) => {
        const branch = (payload ?? {}) as Record<string, unknown>;

        applyQueue<MikuAlertProps>(
          "miku",
          enqueue(readQueue("miku"), {
            ...branch,
            mikuModule: branch.module,
            twitchUser: branch.user,
          } as unknown as MikuAlertProps)
        );
      };

      return {
        ...initialState,

        start: async (adapter?: HubAdapter) => {
          if (starting !== null) {
            return starting;
          }

          const active = adapter ?? connected ?? createOverlayHubAdapter();

          set({ status: "connecting" });
          if (signalrDebugEnabled()) {
            // eslint-disable-next-line no-console
            console.info("[TelegramusHub] status → connecting");
          }
          connected = active;
          setOverlayAdapter(active);

          // Очереди подписываются на шину ДО открытия канала: обработчики
          // должны быть на месте к моменту, когда адаптер получит событие.
          queueUnsubs = [
            subscribeToOverlayEvent("WaifuRoll", handleWaifuRoll),
            subscribeToOverlayEvent("AddNewWaifu", handleAddNewWaifu),
            subscribeToOverlayEvent("MergeWaifu", handleMergeWaifu),
            subscribeToOverlayEvent("ShowCurrentWife", handleShowCurrentWife),
            subscribeToOverlayEvent("FumoRoll", handleFumoRoll),
            subscribeToOverlayEvent("FrogRoll", handleFrogRoll),
            subscribeToOverlayEvent("MikuRoll", handleMikuRoll),
          ];

          let attempt: Promise<void>;

          // Единственная карта обработчиков на соединении — карта шины.
          const handlersForConnect = buildOverlayBusHandlers();

          attempt = active
            .connect(handlersForConnect)
            .then(() => {
              if (connected !== active) {
                return;
              }

              set({
                status: active.status,
                isConnected: active.status === "connected",
              });
              if (signalrDebugEnabled()) {
                // eslint-disable-next-line no-console
                console.info(`[TelegramusHub] status → ${active.status}`);
              }
            })
            .catch((error: unknown) => {
              if (connected === active) {
                for (const un of queueUnsubs) {
                  un();
                }
                queueUnsubs = [];
                connected = null;
                setOverlayAdapter(null);
                set({ status: "error", isConnected: false });
                if (signalrDebugEnabled()) {
                  // eslint-disable-next-line no-console
                  console.error("[TelegramusHub] status → error", error);
                }
              }

              throw error;
            })
            .finally(() => {
              if (starting === attempt) {
                starting = null;
              }
            });

          starting = attempt;

          return starting;
        },

        stop: async () => {
          const adapter = connected;
          connected = null;
          setOverlayAdapter(null);

          starting = null;

          for (const un of queueUnsubs) {
            un();
          }
          queueUnsubs = [];

          if (adapter !== null) {
            await adapter.disconnect();
          }

          if (connected === null) {
            set({ status: "idle", isConnected: false });
            if (signalrDebugEnabled()) {
              // eslint-disable-next-line no-console
              console.debug("[TelegramusHub] status → idle (stopped)");
            }
          }
        },

        invoke: async <K extends keyof HubInvocationMap>(
          method: K,
          ...args: HubInvocationMap[K]
        ) => {
          if (connected === null) {
            throw new Error(
              `invoke("${method}") до start(): соединения с хабом нет`
            );
          }

          await connected.invoke(method, ...args);
        },

        reset: () => {
          set({ ...initialState });
        },

        dequeueCurrent: () => applyQueue("waifu", advance(readQueue("waifu"))),
        dequeueFumoCurrent: () =>
          applyQueue("fumo", advance(readQueue("fumo"))),
        dequeueFrogCurrent: () =>
          applyQueue("frog", advance(readQueue("frog"))),
        dequeueMikuCurrent: () =>
          applyQueue("miku", advance(readQueue("miku"))),
      };
    },
    { name: "OverlayHubStore" }
  )
);

export default useOverlayHubStore;