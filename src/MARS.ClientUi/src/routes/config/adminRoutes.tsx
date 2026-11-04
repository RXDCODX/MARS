import { lazy, Suspense } from "react";

import { PageLoader } from "@/components/shared/LazyLoader";
import Layout from "@/components/Site/Pages/Layout/Layout";

import { registerPrefetchComponents } from "../utils/prefetchRoutes";
import { RouteConfig } from "./RouteConfig";

// Тяжелые админ страницы - lazy loading
const logsPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({ default: m.LogsPage }));
const LogsPage = lazy(logsPageLoader);

const serverViewerLoader = () =>
  import("@/components/Site/Pages/ServerViewer/ServerViewer");
const ServerViewer = lazy(serverViewerLoader);

const twitchRewardsPageLoader = () =>
  import("@/components/Site/Pages/TwitchRewardsPage");
const TwitchRewardsPage = lazy(twitchRewardsPageLoader);

const autoMessagesPageLoader = () =>
  import("@/components/Site/Pages/AutoMessagesPage");
const AutoMessagesPage = lazy(autoMessagesPageLoader);

const telegramDiscordBridgePageLoader = () =>
  import("@/components/Site/Pages/TelegramDiscordBridgePage");
const TelegramDiscordBridgePage = lazy(telegramDiscordBridgePageLoader);

const serviceDetailsPageLoader = () => import("./ServiceDetailsPage");
const ServiceDetailsPage = lazy(serviceDetailsPageLoader);

const rootStatePageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.RootStatePage,
  }));
const RootStatePage = lazy(rootStatePageLoader);

// Загрузчики ниже остались в registerPrefetchComponents: страница нужна не
// только как маршрут, но и для фоновой подгрузки. После удаления дублей
// маршрутов осиротели лишь обёртки lazy() — сам загрузчик нужен.
const environmentVariablesPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.EnvironmentVariablesPage,
  }));

const commandsPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.CommandsPage,
  }));

const cinemaQueuePageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.CinemaQueuePage,
  }));

const spotifyAuthManagerLoader = () =>
  import("@/components/Site/Pages/AdminPanel/sections/SpotifyAuthManager");
const SpotifyAuthManager = lazy(spotifyAuthManagerLoader);

const twitchUsersPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.TwitchUsersPage,
  }));
const TwitchUsersPage = lazy(twitchUsersPageLoader);

const twitchUserCreatePageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.TwitchUserCreatePage,
  }));
const TwitchUserCreatePage = lazy(twitchUserCreatePageLoader);

const twitchUserEditPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.TwitchUserEditPage,
  }));
const TwitchUserEditPage = lazy(twitchUserEditPageLoader);

const wTelegramPageLoader = () =>
  import("@/components/Site/Pages").then(m => ({
    default: m.WTelegramPage,
  }));
const WTelegramPage = lazy(wTelegramPageLoader);

const booruAutoPostPageLoader = () =>
  import("@/components/Site/Pages/BooruAutoPostPage/BooruAutoPostPage");
const BooruAutoPostPage = lazy(booruAutoPostPageLoader);

// Регистрируем админ компоненты для фоновой загрузки
registerPrefetchComponents([
  logsPageLoader,
  serverViewerLoader,
  twitchRewardsPageLoader,
  autoMessagesPageLoader,
  telegramDiscordBridgePageLoader,
  serviceDetailsPageLoader,
  environmentVariablesPageLoader,
  rootStatePageLoader,
  commandsPageLoader,
  cinemaQueuePageLoader,
  spotifyAuthManagerLoader,
  twitchUsersPageLoader,
  twitchUserCreatePageLoader,
  twitchUserEditPageLoader,
  wTelegramPageLoader,
  booruAutoPostPageLoader,
]);

// Массив панелей управления (с Layout)
export const adminRoutes: RouteConfig[] = [
  {
    path: "/logs",
    name: "Панель логов",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <LogsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/admin",
    name: "Админ панель",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ServerViewer />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/dashboard",
    name: "Дашборд",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ServerViewer />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/main",
    name: "Главная панель",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ServerViewer />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/services",
    name: "Сервисы",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ServerViewer />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/services/details",
    name: "Детали сервиса",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ServiceDetailsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/root-state",
    name: "RootState",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <RootStatePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/twitch-rewards",
    name: "Twitch награды",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <TwitchRewardsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/auto-messages",
    name: "Автосообщения",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <AutoMessagesPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/telegram-discord-bridge",
    name: "Telegram ↔ Discord Bridge",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <TelegramDiscordBridgePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/twitch-users",
    name: "Пользователи Twitch",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <TwitchUsersPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/twitch-users/create",
    name: "Создание пользователя Twitch",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <TwitchUserCreatePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/twitch-users/:id/edit",
    name: "Редактирование пользователя Twitch",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <TwitchUserEditPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/spotify",
    name: "Spotify",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <SpotifyAuthManager />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/wtelegram",
    name: "WTelegram",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <WTelegramPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/booru-auto-post",
    name: "Booru автопостинг",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <BooruAutoPostPage />
        </Suspense>
      </Layout>
    ),
  },
];
