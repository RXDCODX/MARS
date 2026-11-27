import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { MikuMondayDto, MikuTrackDto } from "@/shared/api";
import type { HubAdapter } from "@/shared/api/realtime/hubAdapter";
import { readBranch } from "@/shared/api/realtime/overlayPayload";
import { subscribeToOverlayEvent } from "@/shared/api/realtime/overlayEventBus";
import { useOverlayHubStore } from "@/shared/stores/overlayHubStore";

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
let unsubscribeFromStatus: (() => void) | null = null;

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
        const { status } = get();
        if (status === "connecting" || status === "connected") {
          return;
        }

        set({ status: "connecting", error: undefined });

        if (unsubscribeFromHub === null) {
          unsubscribeFromHub = subscribeToOverlayEvent("MikuMonday", payload => {
            const branch = readBranch(payload);
            const decoded = (branch ?? undefined) as MikuMondayDto | undefined;

            if (
              decoded !== undefined &&
              decoded.selectedTrack !== undefined &&
              typeof decoded.selectedTrack.id === "string" &&
              typeof decoded.twitchUser?.twitchId === "string"
            ) {
              get().handleIncomingAlert(decoded);
            } else if (decoded !== undefined) {
              console.warn(
                "[MikuMonday] Некорректная форма payload, алерт пропущен",
                decoded
              );
            }
          });
        }

        unsubscribeFromStatus?.();
        unsubscribeFromStatus = useOverlayHubStore.subscribe(state => {
          if (state.status === "connected") {
            set({ status: "connected", isConnected: true });
            void get().fetchAvailableTracks();
          } else if (state.status === "error") {
            set({
              status: "error",
              error: "Не удалось подключиться",
              isConnected: false,
            });
          } else if (state.status === "disconnected") {
            set({ status: "idle", isConnected: false });
          }
        });

        const current = useOverlayHubStore.getState();
        if (current.status === "connected") {
          set({ status: "connected", isConnected: true });
          await get().fetchAvailableTracks();
        } else if (current.status === "error") {
          set({
            status: "error",
            error: "Не удалось подключиться",
            isConnected: false,
          });
        }
      },

      stop: async () => {
        unsubscribeFromHub?.();
        unsubscribeFromHub = null;

        unsubscribeFromStatus?.();
        unsubscribeFromStatus = null;

        set({ ...initialState, adapter: undefined, status: "idle" });
      },

      // Имена методов остаются строками: карты вызовов для них нет, а
      // проверка строкой здесь ровно то же, что была: метод, которого
      // сервер не знает, вернёт ошибку при вызове, а не молча пропадёт.
      invoke: async (methodName: string, ...arguments_: unknown[]) => {
        const overlay = useOverlayHubStore.getState();
        if (overlay.status !== "connected") {
          await get().start();
        }

        return await useOverlayHubStore
          .getState()
          .invoke(methodName as any, ...arguments_);
      },

      // Запрос списка треков не должен ронять подписку.
      //
      // Метод `MikuMondayTracks` теперь реализован на хабе (AlertsHub) и проксирует
      // IWaifuGachaClient.GetMikuMondayTracksAsync. Раньше его не было — вызов
      // отказывался, и раньше это уносило с собой подписку на `MikuMonday`.
      //
      // Отказ здесь всё ещё безобиден: список треков — это то, что можно
      // долить позже, а подписка на алерты от него не зависит.
      fetchAvailableTracks: async () => {
        const overlay = useOverlayHubStore.getState();
        if (overlay.status !== "connected") {
          return;
        }
        try {
          const tracks = (await useOverlayHubStore
            .getState()
            .invoke("MikuMondayTracks")) as unknown as MikuTrackDto[];

          set({
            availableTracks: tracks ?? [],
            availableTracksCount: (tracks ?? []).length,
          });
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "Ошибка получения треков";

          set({ error: message });
          console.warn(
            "[MikuMonday] Список доступных треков получить не удалось, хаб такого метода не знает.",
            error
          );
        }
      },

      decrementAvailableTrack: async () => {
        const overlay = useOverlayHubStore.getState();
        if (overlay.status !== "connected") {
          return;
        }
        const { availableTracksCount } = get();
        if (availableTracksCount <= 0) {
          return;
        }
        try {
          await useOverlayHubStore.getState().invoke("DecrementAvailableMikuTrack");
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

      reset: () => {
        unsubscribeFromHub?.();
        unsubscribeFromHub = null;
        unsubscribeFromStatus?.();
        unsubscribeFromStatus = null;
        set({ ...initialState });
      },
    }),
    { name: "MikuMondayStore" }
  )
);

export default useMikuMondayStore;
