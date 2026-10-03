import emoticons from "@mkody/twitch-emoticons";
import { ApiClient, HelixChatBadgeSet, HelixUser } from "@twurple/api";
import { AppTokenAuthProvider } from "@twurple/auth";
import { ChatMessage } from "@twurple/chat";
import { create } from "zustand";
import { devtools } from "zustand/middleware";

import { TelegramusHubSignalRConnectionBuilder } from "../api";

interface Actions {
  init: (clientId: string, clientSecret: string) => void;
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
    const connection = TelegramusHubSignalRConnectionBuilder.build();

    connection.on(
      "posttwitchinfo",
      (clientId: string, clientSecret: string) => {
        const { fetcher, parser } = get();
        if (!fetcher || !parser) {
          const { client, fetcher, parser, newParser } = initialization(
            clientId,
            clientSecret
          );
          getBadges(client)
            .then(badges => set({ badges }))
            .catch(error => {
              console.error(error);
              set({ badges: [] });
            });

          Promise.all([
            // Twitch global
            fetcher.fetchTwitchEmotes(),
            // Twitch channel
            fetcher.fetchTwitchEmotes(785_975_641),
            //BTTV global
            fetcher.fetchBTTVEmotes(),
            // 7TV global
            fetcher.fetchSevenTVEmotes(),
            // 7TV channel
            fetcher.fetchSevenTVEmotes(785_975_641),
            // FFZ global
            fetcher.fetchFFZEmotes(),
          ])
            .then(() => {
              console.log("Emotes loaded");
              set({
                fetcher,
                parser,
                twitchApiClient: client,
                parseToLink: newParser,
              });
            })
            .catch(error => {
              console.error("Error loading emotes...");
              console.error(error);
              set({
                fetcher,
                parser,
                twitchApiClient: client,
                parseToLink: newParser,
              });
            });
        }
      }
    );

    connection.start();

    return {
      ...initialState,
      setBadges: (badges: HelixChatBadgeSet[]) => set({ badges }),
      parse: (text: string, size?: number) => {
        const parser = get().parser ?? get().parseToLink;
        if (!parser) return text;
        return parser.parse(text, size);
      },
      sendMsgToPyrokxnezxz: async (message: string) => {
        await connection.invoke("TwitchMsg", message);
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

function initialization(clientId: string, clientSecret: string) {
  const provider = new AppTokenAuthProvider(clientId, clientSecret);
  const client = new ApiClient({ authProvider: provider });
  const fetcher = new emoticons.EmoteFetcher(undefined, undefined, {
    apiClient: client,
  });
  const parser = new emoticons.EmoteParser(fetcher, {
    template: '<img class="emote" alt="{name}" src="{link}" />',
    match: /(\w+)+?/g,
  });

  const newParser = new emoticons.EmoteParser(fetcher, {
    template: '<img class="emote" alt="{name}" src="{link}" />',
    match: /(?<!<[^>]*)(\w+)(?![^<]*>)/g,
  });

  return { fetcher, parser, client, newParser };
}

async function getBadges(apiClient: ApiClient) {
  const badges = await apiClient.chat.getChannelBadges(785_975_641);
  const globalBadges: HelixChatBadgeSet[] =
    await apiClient.chat.getGlobalBadges();

  const result = [...badges, ...globalBadges];
  return result;
}

export default useTwitchStore;
