import { create } from "zustand";
import { devtools } from "zustand/middleware";

import type { FrogAlertProps } from "@/components/OBS_Components/FrogAlerts/helper";
import type { FumoAlertProps } from "@/components/OBS_Components/FumoAlerts/helper";
import type { MikuAlertProps } from "@/components/OBS_Components/MikuAlerts/helper";
import type { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";
import type {
  HubAdapter,
  HubInvocationMap,
  HubStatus,
} from "@/shared/realtime/hubAdapter";
import type {
  OverlayHandlers,
  OverlayPayload,
} from "@/shared/realtime/overlayEvents";
import { setOverlayAdapter } from "@/shared/realtime/overlayHub";
import { createOverlayHubAdapter } from "@/shared/realtime/SignalRHubAdapter";
import useFrogPrizesStore from "@/shared/stores/frogPrizesStore";
import useFumoPrizesStore from "@/shared/stores/fumoPrizesStore";
import useMikuPrizesStore from "@/shared/stores/mikuPrizesStore";
import useWaifuPrizesStore from "@/shared/stores/waifuPrizesStore";

/**
 * Очередь алертов оверлея.
 *
 * Раньше стор сам строил `HubConnection` и сам же регистрировал обработчики
 * внутри `start()`, а `connection` лежал в состоянии классом. Из-за этого:
 *
 * - подделку в состояние положить было нельзя, и у 380 строк логики не было ни
 *   одного теста;
 * - узнать, какие события стор слушает, можно было только вызвав
 *   `connection.start()`, то есть открыв соединение;
 * - имена событий писались строкой в каждом `connection.on(...)`, и
 *   `updatewaifuprizes`/`UpdateWaifuPrizes` компилировались оба.
 *
 * Теперь состояние не знает про транспорт: в нём лежит `status` и карта
 * `handlers`, а соединение принадлежит `HubAdapter`. Из-за этого обработчик
 * вызывается напрямую — и в тесте, и при отладке.
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

  /** Обработчики хаба. Обычные функции: их можно вызвать без соединения. */
  handlers: OverlayHandlers;
}

interface TelegramusHubActions {
  /**
   * Подключение к хабу.
   *
   * Адаптер принимается аргументом, а не создаётся внутри: тест передаёт
   * подделку, компонент оставляет аргумент пустым и получает настоящий. Раньше
   * стор строил `HubConnection` сам, и выбора не оставалось.
   *
   * Адаптер хранится в замыкании, а не в состоянии: соединение — не данные
   * для отрисовки, и в состоянии ему не место (тогда-то `HubConnection` и
   * оказался в типе состояния, из-за чего подделку было некуда положить).
   */
  start: (adapter?: HubAdapter) => Promise<void>;

  /** Остановка и сброс соединения. Очереди не трогаются. */
  stop: () => Promise<void>;

  /**
   * Клиентский вызов хаба через подключённый адаптер.
   *
   * Тип взят у адаптера, а не задан здесь: раньше подпись была
   * `(method: "TwitchMsg", message: string)`, и все остальные вызовы —
   * MuteAll, UnmuteSessions, LogError — не компилировались, хотя сам хаб их
   * принимает.
   */
  invoke: <K extends keyof HubInvocationMap>(
    method: K,
    ...args: HubInvocationMap[K]
  ) => Promise<void>;

  /** Сброс в начальное состояние. Нужен между тестами. */
  reset: () => void;

  dequeueCurrent: () => void;
  dequeueFumoCurrent: () => void;
  dequeueFrogCurrent: () => void;
  dequeueMikuCurrent: () => void;
}

/**
 * Начальное состояние.
 *
 * Поля `current*` перечислены явно, хотя `undefined` и есть их значение по
 * умолчанию: `set` объединяет объект с текущим, и неупомянутый ключ сохранил
 * бы прежнего алерта. На этом спотыкался `reset()` — он чистил очереди и
 * оставлял на экране последний показанный алерт.
 */
const initialState: Omit<TelegramusHubState, "handlers"> = {
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

/**
 * Кладёт алерт в очередь или на экран.
 *
 * Общая логика для всех четырёх видов: если экран свободен, алерт показывается
 * сразу, иначе встаёт в очередь. Раньше эта логика копировалась четыре раза
 * внутри `start()` — правка в одной очереди не доезжала до остальных трёх.
 */
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

/** Показывает следующий алерт очереди, иначе очищает экран. */
function advance<TProps>(current: AlertQueue<TProps>): AlertQueue<TProps> {
  const [next, ...rest] = current.queue;

  if (next === undefined) {
    return { queue: [], current: undefined, showing: false };
  }

  return { queue: rest, current: next, showing: true };
}

/** Призы приходят отдельным списком; пустой список игнорируется. */
function nonEmptyPrizes(payload: OverlayPayload): OverlayPayload | null {
  return Array.isArray(payload) && payload.length > 0 ? payload : null;
}

/**
 * Форма события вайфу на проводе.
 *
 * Сервер шлёт одно сообщение из `oneof` telegramus.proto, а код монолита ждал
 * параметры по отдельности. Форма описана здесь явно, иначе распаковка была бы
 * размазана по четырём обработчикам и расхождение искалось бы вручную.
 */
interface WaifuRollPayload {
  waifu?: unknown;
  host?: { twitchUser?: { displayName?: string } } | null;
  twitchUser?: { displayName?: string } | null;
}

function unpackWaifuRoll(payload: OverlayPayload): {
  waifu: unknown;
  host: WaifuRollPayload["host"];
  twitchUser: WaifuRollPayload["twitchUser"];
} {
  const source = (payload ?? {}) as WaifuRollPayload;

  return {
    waifu: source.waifu,
    host: source.host ?? null,
    twitchUser: source.twitchUser ?? null,
  };
}

export const useTelegramusHubStore = create<
  TelegramusHubState & TelegramusHubActions
>()(
  devtools(
    (set, get) => {
      /** Подключённый адаптер. В состоянии его нет намеренно. */
      let connected: HubAdapter | null = null;

      /** Применяет очередь вида к состоянию по ключам из QUEUE_KEYS. */
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

      /** Текущая очередь вида из состояния. */
      const readQueue = <TProps>(kind: QueueKind): AlertQueue<TProps> => {
        const { queueKey, currentKey, showingKey } = QUEUE_KEYS[kind];
        const state = get() as unknown as Record<string, unknown>;

        return {
          queue: (state[queueKey] as TProps[]) ?? [],
          current: state[currentKey] as TProps | undefined,
          showing: Boolean(state[showingKey]),
        };
      };

      // Обработчики вынесены в отдельную переменную с явной аннотацией.
      // Внутри возвращаемого объекта литерал не получал контекстной
      // типизации, и параметры вроде (waifu, host) выводились как any —
      // карта типов переставала проверять подписи событий.
      const handlers: OverlayHandlers = {
        Alert: payload => {
          applyQueue<WaifuAlertProps>(
            "waifu",
            enqueue(readQueue("waifu"), payload as never)
          );
        },
        Alerts: payload => {
          applyQueue<WaifuAlertProps>(
            "waifu",
            enqueue(readQueue("waifu"), payload as never)
          );
        },
        // Событие приходит одним аргументом — сообщением из oneof telegramus.proto.
        // Клиент монолита получал параметры по отдельности (waifu, host), поэтому
        // распаковка здесь, а не в компонентах: иначе расхождение формы пришлось бы
        // искать в каждом обработчике.
        WaifuRoll: payload => {
          const { waifu, host } = unpackWaifuRoll(payload);
          const parsed: WaifuAlertProps = {
            waifu,
            displayName: host?.twitchUser?.displayName ?? "",
            waifuHusband: host,
          } as WaifuAlertProps;

          applyQueue(
            "waifu",
            enqueue(readQueue<WaifuAlertProps>("waifu"), parsed)
          );
        },
        AddNewWaifu: payload => {
          const { waifu, twitchUser } = unpackWaifuRoll(payload);
          const marked = { ...(waifu as object), isAdded: true };

          applyQueue<WaifuAlertProps>("waifu", {
            ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
              waifu: marked,
              displayName: twitchUser?.displayName ?? "",
            } as WaifuAlertProps),
          });
        },
        MergeWaifu: payload => {
          const { waifu, host } = unpackWaifuRoll(payload);
          const marked = { ...(waifu as object), isMerged: true };

          applyQueue<WaifuAlertProps>("waifu", {
            ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
              waifu: marked,
              displayName: host?.twitchUser?.displayName ?? "",
              waifuHusband: host,
            } as WaifuAlertProps),
          });
        },
        ShowCurrentWife: payload => {
          const { waifu, host } = unpackWaifuRoll(payload);

          applyQueue<WaifuAlertProps>("waifu", {
            ...enqueue(readQueue<WaifuAlertProps>("waifu"), {
              waifu,
              displayName: "",
              waifuHusband: host,
              isReminder: true,
            } as WaifuAlertProps),
          });
        },
        FumoRoll: fumo => {
          applyQueue<FumoAlertProps>(
            "fumo",
            enqueue(readQueue("fumo"), fumo as never)
          );
        },
        FrogRoll: frog => {
          applyQueue<FrogAlertProps>(
            "frog",
            enqueue(readQueue("frog"), frog as never)
          );
        },
        MikuRoll: miku => {
          applyQueue<MikuAlertProps>(
            "miku",
            enqueue(readQueue("miku"), miku as never)
          );
        },
        UpdateWaifuPrizes: payload => {
          const prizes = nonEmptyPrizes(payload);

          if (prizes !== null) {
            useWaifuPrizesStore.getState().addPrizes(prizes as never);
          }
        },
        UpdateFumoPrizes: payload => {
          const prizes = nonEmptyPrizes(payload);

          if (prizes !== null) {
            useFumoPrizesStore.getState().addPrizes(prizes as never);
          }
        },
        UpdateFrogPrizes: payload => {
          const prizes = nonEmptyPrizes(payload);

          if (prizes !== null) {
            useFrogPrizesStore.getState().addPrizes(prizes as never);
          }
        },
        UpdateMikuPrizes: payload => {
          const prizes = nonEmptyPrizes(payload);

          if (prizes !== null) {
            useMikuPrizesStore.getState().addPrizes(prizes as never);
          }
        },
        // События без полезной нагрузки: экран сам разбирается, что показать.
        Explosion: () => undefined,
        LeroyAlert: () => undefined,
        Credits: () => undefined,
        MichaelJackson: () => undefined,
        PhonkEdit: () => undefined,
        AudioQuizStop: () => undefined,
        // Остальные события оверлея разбираются компонентами напрямую через
        // собственные подписки. Заглушки обязательны: карта обработчиков
        // описана mapped-типом, и пропущенное событие не собирается.
        NewMessage: () => undefined,
        DeleteMessage: () => undefined,
        Highlite: () => undefined,
        PostTwitchInfo: () => undefined,
        MakeScreenParticles: () => undefined,
        MakeScreenEmojisParticles: () => undefined,
        RandomMem: () => undefined,
        AutoMessage: () => undefined,
        Adhd: () => undefined,
        GaoAlert: () => undefined,
        MikuMonday: () => undefined,
        MikuMikuBeam: () => undefined,
        TikTokEdit: () => undefined,
        AllRefund: () => undefined,
        AudioQuizStart: () => undefined,
        FumoFriday: () => undefined,
        AdhdConfig: () => undefined,
      };

      return {
        ...initialState,
        handlers,

        start: async (adapter?: HubAdapter) => {
          // Повторный start обязан переиспользовать подключённый адаптер, а не
          // создавать второй. Иначе на странице висело бы несколько соединений:
          // четыре компонента вызывают startHub() из useEffect, а StrictMode
          // вызывает эффекты дважды. Обработчики стора зарегистрированы на
          // каждом соединении, и одно событие легло бы в очередь дважды.
          const reused = connected;
          const active = adapter ?? reused ?? createOverlayHubAdapter();

          set({ status: "connecting" });
          connected = active;
          setOverlayAdapter(active);

          try {
            await active.connect(get().handlers);

            set({
              status: active.status,
              isConnected: active.status === "connected",
            });
          } catch (error) {
            connected = null;
            setOverlayAdapter(null);
            set({ status: "error", isConnected: false });

            throw error;
          }
        },

        stop: async () => {
          const adapter = connected;
          connected = null;
          setOverlayAdapter(null);

          if (adapter !== null) {
            await adapter.disconnect();
          }

          set({ status: "idle", isConnected: false });
        },

        invoke: async (method: "TwitchMsg", message: string) => {
          if (connected === null) {
            throw new Error(
              `invoke("${method}") до start(): соединения с хабом нет`
            );
          }

          await connected.invoke(method, message);
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
    { name: "TelegramusHubStore" }
  )
);

export default useTelegramusHubStore;
