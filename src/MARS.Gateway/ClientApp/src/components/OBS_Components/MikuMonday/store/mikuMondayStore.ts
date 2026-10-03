import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { MikuMondayDto, MikuTrackDto } from "@/shared/api";
import type { HubAdapter } from "@/shared/realtime/hubAdapter";
import { decodeJsonBranch } from "@/shared/realtime/overlayPayload";
import {
  subscribeToOverlayEvent,
  whenOverlayAdapterReady,
} from "@/shared/realtime/overlaySubscription";

import type { QueuedMikuMondayAlert } from "../types";

/**
 * Отписка от события хаба.
 *
 * В замыкании рядом со стором, а не в состоянии: это служебная величина, а не
 * данные для отрисовки. В состоянии ей самое место разве что для индикации.
 *
 * Признака «создал ли стор соединение» больше нет и не нужно: соединение
 * принадлежит оверлею, стор его не строит и не закрывает.
 */
let unsubscribeFromHub: (() => void) | null = null;

type ConnectionStatus = "idle" | "connecting" | "connected" | "error";

interface MikuMondayState {
  adapter?: HubAdapter;
  isConnected: boolean;
  status: ConnectionStatus;
  error?: string;

  // Свободные треки
  availableTracks: MikuTrackDto[];
  availableTracksCount: number;

  // Очередь алертов
  alerts: QueuedMikuMondayAlert[];
  currentAlert?: QueuedMikuMondayAlert;
  isAlertShowing: boolean;
}

interface MikuMondayActions {
  start: () => Promise<void>;
  stop: () => Promise<void>;
  invoke: (methodName: string, ...arguments_: unknown[]) => Promise<unknown>;

  fetchAvailableTracks: () => Promise<void>;
  decrementAvailableTrack: () => Promise<void>;

  handleIncomingAlert: (dto: MikuMondayDto) => void;
  dequeueCurrent: () => void;
  clearQueue: () => void;
  reset: () => void;
}

const initialState: MikuMondayState = {
  isConnected: false,
  status: "idle",
  availableTracks: [],
  availableTracksCount: 0,
  alerts: [],
  isAlertShowing: false,
};

export const useMikuMondayStore = create<MikuMondayState & MikuMondayActions>()(
  devtools(
    (set, get) => ({
      ...initialState,

      start: async () => {
        const { adapter, isConnected, status } = get();
        if (adapter && (isConnected || status === "connecting")) {
          return;
        }

        set({ status: "connecting", error: undefined });

        // Оверлейный адаптер переиспользуется, а не строится свой.
        //
        // Раньше здесь был `createSignalRHubAdapter(`${VITE_BASE_PATH}hubs/overlay`)`
        // — пятое соединение к тому же хабу плюс склейка адреса, которая при
        // незаданной переменной давала `undefinedhubs/overlay`. Событие MikuMonday
        // приезжало в отдельный сокет, а оверлейные компоненты слушали другой.
        //
        // Реестр не читается один раз: React выполняет эффекты детей раньше
        // родительских, и этот компонент смонтировался раньше владельца
        // соединения. И ожидание, и отложенная подписка убирают обе ветки — и
        // второе соединение, и молчание после переподключения.

        // Очередь: приход нового алерта.
        //
        // Событие едет содержимым ветки { mikuMondayJson }, где полезная
        // нагрузка объявлена полем bytes — приходит массивом байт.
        // Раньше резолвер SignalR разбирал это и отдавал готовый DTO.
        //
        // Подписка отложенная, а не на текущем адаптере: компонент внутри
        // обёртки оверлея смонтировался раньше, чем соединение поднялось, и
        // чтение реестра один раз оставляло стор без событий навсегда.
        unsubscribeFromHub = subscribeToOverlayEvent("MikuMonday", payload => {
          const decoded = decodeJsonBranch(payload, "mikuMondayJson") as
            | MikuMondayDto
            | undefined;

          if (decoded !== undefined) {
            get().handleIncomingAlert(decoded);
          }
        });

        try {
          // Подключать общий адаптер повторно нельзя: он уже открыт, и второй
          // connect на том же сокете закрыл бы его у всех остальных. Если
          // адаптера ещё нет, ждём его, а не строим второй.
          const ready = await whenOverlayAdapterReady();

          if (ready === null) {
            // Оверлейное соединение не поднялось. Подписка уже стоит и заработает
            // сама, когда адаптер появится, поэтому состояние не «сломанное», а
            // «ждём канал».
            set({ status: "idle", isConnected: false });

            return;
          }

          set({ adapter: ready, isConnected: true, status: "connected" });
          await get().fetchAvailableTracks();
        } catch (error) {
          unsubscribeFromHub?.();
          unsubscribeFromHub = null;

          const message =
            error instanceof Error ? error.message : "Не удалось подключиться";
          set({ status: "error", error: message, isConnected: false });
          throw error;
        }
      },

      stop: async () => {
        // Подписка снимается всегда: оставленная на адаптере подписка держала бы
        // стор живым и кормила его событиями после остановки.
        unsubscribeFromHub?.();
        unsubscribeFromHub = null;

        // Соединение не закрывается: его создал и владеет оверлей, а не этот
        // стор. Разрыв здесь уводил бы соединение у всех компонентов страницы
        // разом.
        //
        // `adapter` в initialState нет: он необязательный, и без явного сброса
        // стор продолжал считать себя подключённым после остановки.
        set({ ...initialState, adapter: undefined, status: "idle" });
      },

      // Имена методов остаются строками: карты вызовов для них нет, а
      // проверка строкой здесь ровно то же, что была: метод, которого
      // сервер не знает, вернёт ошибку при вызове, а не молча пропадёт.
      invoke: async (methodName: string, ...arguments_: unknown[]) => {
        const { adapter, isConnected } = get();
        if (!adapter || !isConnected) {
          await get().start();
        }
        return await get().adapter!.send(methodName, ...arguments_);
      },

      fetchAvailableTracks: async () => {
        const { adapter, isConnected } = get();
        if (!adapter || !isConnected) {
          return;
        }
        try {
          const tracks = (await adapter.send(
            "MikuMondayTracks"
          )) as MikuTrackDto[];

          set({
            availableTracks: tracks ?? [],
            availableTracksCount: (tracks ?? []).length,
          });
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "Ошибка получения треков";
          set({ error: message });
          throw error;
        }
      },

      decrementAvailableTrack: async () => {
        const { adapter, isConnected, availableTracksCount } = get();
        if (!adapter || !isConnected) {
          return;
        }
        if (availableTracksCount <= 0) {
          return;
        }
        try {
          await adapter.send("DecrementAvailableMikuTrack");
          const newCount = Math.max(0, availableTracksCount - 1);
          set({ availableTracksCount: newCount });
          if (newCount === 0) {
            await get().fetchAvailableTracks();
          }
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "Ошибка списания трека";
          set({ error: message });
          throw error;
        }
      },

      handleIncomingAlert: (dto: MikuMondayDto) => {
        const alert: QueuedMikuMondayAlert = {
          ...dto,
          queueId:
            dto.id ||
            (typeof crypto !== "undefined" && "randomUUID" in crypto
              ? crypto.randomUUID()
              : `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`),
        };

        set(state => {
          // Проверяем дублирование
          const allAlerts = [
            ...(state.currentAlert ? [state.currentAlert] : []),
            ...state.alerts,
          ];
          const isDuplicate = allAlerts.some(
            existingAlert =>
              existingAlert.id === alert.id ||
              (existingAlert.selectedTrack.id === alert.selectedTrack.id &&
                existingAlert.twitchUser.twitchId === alert.twitchUser.twitchId)
          );

          if (isDuplicate) {
            console.warn("[MikuMonday] ВНИМАНИЕ: Обнаружен дубликат алерта!", {
              incomingAlert: {
                id: alert.id,
                trackId: alert.selectedTrack.id,
                trackNumber: alert.selectedTrack.number,
                twitchId: alert.twitchUser.twitchId,
                displayName: alert.twitchUser.displayName,
              },
              currentAlert: state.currentAlert
                ? {
                    id: state.currentAlert.id,
                    trackId: state.currentAlert.selectedTrack.id,
                    trackNumber: state.currentAlert.selectedTrack.number,
                    twitchId: state.currentAlert.twitchUser.twitchId,
                  }
                : null,
              queueLength: state.alerts.length,
            });
            return state; // Не добавляем дубликат
          }

          if (!state.isAlertShowing) {
            console.log("[MikuMonday] Новый алерт, очередь пуста", {
              alertId: alert.id,
              displayName: alert.twitchUser.displayName,
              selectedTrack: alert.selectedTrack.number,
            });
            return {
              currentAlert: alert,
              isAlertShowing: true,
              alerts: [...state.alerts],
            };
          }

          const updatedAlerts = [...state.alerts, alert];
          console.debug("[MikuMonday] Алерт добавлен в очередь", {
            alertId: alert.id,
            queueLength: updatedAlerts.length,
            displayName: alert.twitchUser.displayName,
            selectedTrack: alert.selectedTrack.number,
          });
          return { alerts: updatedAlerts };
        });
      },

      dequeueCurrent: () => {
        set(state => {
          if (state.alerts.length > 0) {
            const [nextAlert, ...rest] = state.alerts;
            console.debug("[MikuMonday] Показ следующего алерта", {
              alertId: nextAlert.id,
              queueLength: rest.length,
              displayName: nextAlert.twitchUser.displayName,
              selectedTrack: nextAlert.selectedTrack.number,
            });
            return {
              alerts: rest,
              currentAlert: nextAlert,
              isAlertShowing: true,
            };
          }
          console.debug("[MikuMonday] Очередь опустела");
          return {
            alerts: [],
            currentAlert: undefined,
            isAlertShowing: false,
          };
        });
      },

      clearQueue: () => {
        set({ alerts: [], currentAlert: undefined, isAlertShowing: false });
      },

      reset: () => set({ ...initialState }),
    }),
    { name: "MikuMondayStore" }
  )
);

export default useMikuMondayStore;
