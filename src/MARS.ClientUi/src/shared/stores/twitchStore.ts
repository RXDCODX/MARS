import emoticons from "@mkody/twitch-emoticons";
import { ApiClient, HelixChatBadgeSet, HelixUser } from "@twurple/api";
import { ChatMessage } from "@twurple/chat";
import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { useOverlayHubStore } from "@/shared/stores/overlayHubStore";

interface Actions {
  setBadges: (badges: HelixChatBadgeSet[]) => void;
  parse: (text: string, size?: number) => string;
  sendMsgToPyrokxnezxz: (message: string) => Promise<void>;
  getStreamerInfo: (userId: string) => Promise<HelixUser | null>;
  getStreamerChatColor: (userId: string) => Promise<string | null>;
}

interface State {
  fetcher?: emoticons.EmoteFetcher;
  parser?: emoticons.EmoteParser;
  parseToLink?: emoticons.EmoteParser;
  badges: HelixChatBadgeSet[];
  twitchApiClient?: ApiClient;
  twitchMessages: ChatMessage[];
}

const initialState: State = {
  badges: [],
  twitchMessages: [],
};

export const useTwitchStore = create<State & Actions>()(
  devtools((set, get) => {


    return {
      ...initialState,
      setBadges: (badges: HelixChatBadgeSet[]) => set({ badges }),
      parse: (text: string, size?: number) => {
        const parser = get().parser ?? get().parseToLink;
        if (!parser) return text;
        return parser.parse(text, size);
      },
      sendMsgToPyrokxnezxz: async (message: string) => {
        // Пока хаб не подключён, сообщение некуда отправить: раньше здесь стоял
        // вызов по собственному соединению, и он падал бы на отсутствии
        // канала — вместе с компонентом, который шлёт сообщение.
        const overlay = useOverlayHubStore.getState();

        if (overlay.status === "connected") {
          await useOverlayHubStore.getState().invoke("TwitchMsg", message);
        }
      },
      getStreamerInfo: async (userId: string) => {
        const client = get().twitchApiClient;
        if (!client) return null;

        try {
          const user = await client.users.getUserById(userId);
          return user;
        } catch (error) {
          console.error("Ошибка получения информации о стримере:", error);
          return null;
        }
      },
      getStreamerChatColor: async (userId: string) => {
        const client = get().twitchApiClient;
        if (!client) return null;

        try {
          const color = await client.chat.getColorForUser(userId);
          return color || null;
        } catch (error) {
          console.error("Ошибка получения цвета чата стримера:", error);
          return null;
        }
      },
    };
  })
);

export default useTwitchStore;
