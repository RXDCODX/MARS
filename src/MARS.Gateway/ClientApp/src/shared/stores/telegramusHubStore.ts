import { HubConnection } from "@microsoft/signalr";
import { PrizeType } from "react-roulette-pro";
import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { FrogAlertProps } from "@/components/OBS_Components/FrogAlerts/helper";
import { FumoAlertProps } from "@/components/OBS_Components/FumoAlerts/helper";
import { MikuAlertProps } from "@/components/OBS_Components/MikuAlerts/helper";
import { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";
import {
  Frog,
  Fumo,
  Husband,
  MikuModule,
  TwitchUser,
  Waifu,
} from "@/shared/api";
import { TelegramusHubSignalRConnectionBuilder } from "@/shared/api/signalr-clients/TelegramusHub/SignalRContext";
import useFrogPrizesStore from "@/shared/stores/frogPrizesStore";
import useFumoPrizesStore from "@/shared/stores/fumoPrizesStore";
import useMikuPrizesStore from "@/shared/stores/mikuPrizesStore";
import useWaifuPrizesStore from "@/shared/stores/waifuPrizesStore";

interface TelegramusHubActions {
  start: () => Promise<void>;
  stop: () => Promise<void>;
  invoke: (methodName: string, ...arguments_: unknown[]) => Promise<unknown>;

  dequeueCurrent: () => void;
  dequeueFumoCurrent: () => void;
  dequeueFrogCurrent: () => void;
  dequeueMikuCurrent: () => void;
}

interface TelegramusHubState {
  connection?: HubConnection;
  isConnected: boolean;

  // Очередь алертов вайфу
  messages: WaifuAlertProps[];
  currentMessage?: WaifuAlertProps;
  isWaifuShowing: boolean;

  // Очередь алертов Fumo
  fumoMessages: FumoAlertProps[];
  currentFumoMessage?: FumoAlertProps;
  isFumoShowing: boolean;

  // Очередь алертов Frog
  frogMessages: FrogAlertProps[];
  currentFrogMessage?: FrogAlertProps;
  isFrogShowing: boolean;

  // Очередь алертов Miku
  mikuMessages: MikuAlertProps[];
  currentMikuMessage?: MikuAlertProps;
  isMikuShowing: boolean;
}

const initialState: TelegramusHubState = {
  isConnected: false,
  messages: [],
  isWaifuShowing: false,
  fumoMessages: [],
  isFumoShowing: false,
  frogMessages: [],
  isFrogShowing: false,
  mikuMessages: [],
  isMikuShowing: false,
};

export const useTelegramusHubStore = create<
  TelegramusHubState & TelegramusHubActions
>()(
  devtools(
    (set, get) => ({
      ...initialState,

      start: async () => {
        const state = get();
        if (state.connection && state.isConnected) {
          return;
        }

        const connection = TelegramusHubSignalRConnectionBuilder.build();

        // Регистрация обработчиков событий до старта
        connection.on("WaifuRoll", (message: Waifu, host: Husband) => {
          const parsed: WaifuAlertProps = {
            waifu: message,
            displayName: host.twitchUser?.displayName ?? "",
            waifuHusband: host,
          };
          const { messages, isWaifuShowing } = get();
          if (!isWaifuShowing) {
            set({
              messages: [...messages],
              currentMessage: parsed,
              isWaifuShowing: true,
            });
            return;
          }
          set({ messages: [...messages, parsed] });
        });

        connection.on(
          "addnewwaifu",
          (message: Waifu, twitchUser: TwitchUser) => {
            message.isAdded = true;
            const parsed: WaifuAlertProps = {
              waifu: message,
              displayName: twitchUser.displayName ?? "",
            };
            const { messages, isWaifuShowing } = get();
            if (!isWaifuShowing) {
              set({
                messages: [...messages],
                currentMessage: parsed,
                isWaifuShowing: true,
              });
              return;
            }
            set({ messages: [...messages, parsed] });
          }
        );

        connection.on("Mergewaifu", (message: Waifu, host: Husband) => {
          message.isMerged = true;
          const parsed: WaifuAlertProps = {
            waifu: message,
            displayName: host.twitchUser!.displayName!,
            waifuHusband: host,
          };
          const { messages, isWaifuShowing } = get();
          if (!isWaifuShowing) {
            set({
              messages: [...messages],
              currentMessage: parsed,
              isWaifuShowing: true,
            });
            return;
          }
          set({ messages: [...messages, parsed] });
        });

        connection.on("Showcurrentwife", (message: Waifu, host: Husband) => {
          const parsed: WaifuAlertProps = {
            waifu: message,
            displayName: host.twitchUser!.displayName!,
            waifuHusband: host,
            isReminder: true,
          };
          const { messages, isWaifuShowing } = get();
          if (!isWaifuShowing) {
            set({
              messages: [...messages],
              currentMessage: parsed,
              isWaifuShowing: true,
            });
            return;
          }
          set({ messages: [...messages, parsed] });
        });

        // FumoRoll обработчик
        connection.on(
          "FumoRoll",
          (
            fumo: Fumo,
            twitchUser: TwitchUser,
            collectedCount?: number,
            totalCount?: number
          ) => {
            const parsed: FumoAlertProps = {
              fumo,
              twitchUser,
              collectedCount,
              totalCount,
            };
            const { fumoMessages, isFumoShowing } = get();
            if (!isFumoShowing) {
              set({
                fumoMessages: [...fumoMessages],
                currentFumoMessage: parsed,
                isFumoShowing: true,
              });
              return;
            }
            set({ fumoMessages: [...fumoMessages, parsed] });
          }
        );

        // UpdateWaifuPrizes обработчик
        const handlePrizesUpdate = (prizes: PrizeType[]) => {
          if (!prizes || prizes.length === 0) {
            return;
          }
          useWaifuPrizesStore.getState().addPrizes(prizes);
        };

        connection.on("updatewaifuprizes", handlePrizesUpdate);
        connection.on("UpdateWaifuPrizes", handlePrizesUpdate);

        // UpdateFumoPrizes обработчик
        const handleFumoPrizesUpdate = (prizes: PrizeType[]) => {
          if (!prizes || prizes.length === 0) {
            return;
          }
          useFumoPrizesStore.getState().addPrizes(prizes);
        };

        connection.on("updatefumoprizes", handleFumoPrizesUpdate);
        connection.on("UpdateFumoPrizes", handleFumoPrizesUpdate);

        // FrogRoll обработчик
        connection.on("FrogRoll", (frog: Frog, twitchUser: TwitchUser) => {
          const parsed: FrogAlertProps = {
            frog,
            twitchUser,
          };
          const { frogMessages, isFrogShowing } = get();
          if (!isFrogShowing) {
            set({
              frogMessages: [...frogMessages],
              currentFrogMessage: parsed,
              isFrogShowing: true,
            });
            return;
          }
          set({ frogMessages: [...frogMessages, parsed] });
        });

        // UpdateFrogPrizes обработчик
        const handleFrogPrizesUpdate = (prizes: PrizeType[]) => {
          if (!prizes || prizes.length === 0) {
            return;
          }
          useFrogPrizesStore.getState().addPrizes(prizes);
        };

        connection.on("updatefrogprizes", handleFrogPrizesUpdate);
        connection.on("UpdateFrogPrizes", handleFrogPrizesUpdate);

        // MikuRoll обработчик
        connection.on(
          "MikuRoll",
          (
            mikuModule: MikuModule,
            twitchUser: TwitchUser,
            collectedCount?: number,
            totalCount?: number
          ) => {
            const parsed: MikuAlertProps = {
              mikuModule,
              twitchUser,
              collectedCount,
              totalCount,
            };
            const { mikuMessages, isMikuShowing } = get();
            if (!isMikuShowing) {
              set({
                mikuMessages: [...mikuMessages],
                currentMikuMessage: parsed,
                isMikuShowing: true,
              });
              return;
            }
            set({ mikuMessages: [...mikuMessages, parsed] });
          }
        );

        // UpdateMikuPrizes обработчик
        const handleMikuPrizesUpdate = (prizes: PrizeType[]) => {
          if (!prizes || prizes.length === 0) {
            return;
          }
          useMikuPrizesStore.getState().addPrizes(prizes);
        };

        connection.on("updatemikuprizes", handleMikuPrizesUpdate);
        connection.on("UpdateMikuPrizes", handleMikuPrizesUpdate);

        await connection.start();
        set({ connection, isConnected: true });
      },

      stop: async () => {
        const { connection } = get();
        if (!connection) {
          return;
        }
        try {
          await connection.stop();
        } finally {
          set({ isConnected: false, connection: undefined });
        }
      },

      invoke: async (methodName: string, ...arguments_: unknown[]) => {
        const { connection, isConnected } = get();
        if (!connection || !isConnected) {
          await get().start();
        }
        return await get().connection!.invoke(methodName, ...arguments_);
      },

      dequeueCurrent: () => {
        const { messages, currentMessage } = get();
        if (messages.length > 0 && currentMessage) {
          const newArray = messages.filter(
            m => m.waifu.shikiId !== currentMessage.waifu.shikiId
          );
          if (newArray.length > 0) {
            const next = newArray[0];
            set({
              messages: newArray,
              currentMessage: next,
              isWaifuShowing: true,
            });
            return;
          }
          set({
            messages: [],
            currentMessage: undefined,
            isWaifuShowing: false,
          });
          return;
        }
        set({ messages: [], currentMessage: undefined, isWaifuShowing: false });
      },

      dequeueFumoCurrent: () => {
        const { fumoMessages, currentFumoMessage } = get();
        if (fumoMessages.length > 0 && currentFumoMessage) {
          const newArray = fumoMessages.filter(
            m => m.fumo.mfcId !== currentFumoMessage.fumo.mfcId
          );
          if (newArray.length > 0) {
            const next = newArray[0];
            set({
              fumoMessages: newArray,
              currentFumoMessage: next,
              isFumoShowing: true,
            });
            return;
          }
          set({
            fumoMessages: [],
            currentFumoMessage: undefined,
            isFumoShowing: false,
          });
          return;
        }
        set({
          fumoMessages: [],
          currentFumoMessage: undefined,
          isFumoShowing: false,
        });
      },

      dequeueFrogCurrent: () => {
        const { frogMessages, currentFrogMessage } = get();
        if (frogMessages.length > 0 && currentFrogMessage) {
          const newArray = frogMessages.filter(
            m => m.frog.pid !== currentFrogMessage.frog.pid
          );
          if (newArray.length > 0) {
            const next = newArray[0];
            set({
              frogMessages: newArray,
              currentFrogMessage: next,
              isFrogShowing: true,
            });
            return;
          }
          set({
            frogMessages: [],
            currentFrogMessage: undefined,
            isFrogShowing: false,
          });
          return;
        }
        set({
          frogMessages: [],
          currentFrogMessage: undefined,
          isFrogShowing: false,
        });
      },

      dequeueMikuCurrent: () => {
        const { mikuMessages, currentMikuMessage } = get();
        if (mikuMessages.length > 0 && currentMikuMessage) {
          const newArray = mikuMessages.filter(
            m => m.mikuModule.pageId !== currentMikuMessage.mikuModule.pageId
          );
          if (newArray.length > 0) {
            const next = newArray[0];
            set({
              mikuMessages: newArray,
              currentMikuMessage: next,
              isMikuShowing: true,
            });
            return;
          }
          set({
            mikuMessages: [],
            currentMikuMessage: undefined,
            isMikuShowing: false,
          });
          return;
        }
        set({
          mikuMessages: [],
          currentMikuMessage: undefined,
          isMikuShowing: false,
        });
      },
    }),
    { name: "TelegramusHubStore" }
  )
);

export default useTelegramusHubStore;
