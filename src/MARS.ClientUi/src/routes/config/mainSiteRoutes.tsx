import { lazy, Suspense } from "react";

import { PageLoader } from "@/components/shared/LazyLoader";
import Layout from "@/components/Site/Pages/Layout/Layout";

import {
  registerCriticalComponents,
  registerPrefetchComponents,
} from "../utils/prefetchRoutes";
import { RouteConfig } from "./RouteConfig";

// КРИТИЧНЫЕ компоненты - загружаются сразу для главной страницы
const welcomePageLoader = () => import("@/components/Site/Pages/WelcomePage");
const WelcomePage = lazy(welcomePageLoader);

// Регистрируем критичный компонент
registerCriticalComponents([welcomePageLoader]);

// Остальные страницы - lazy loading для оптимизации производительности
const aboutPageLoader = () => import("@/components/Site/Pages/AboutPage");
const AboutPage = lazy(aboutPageLoader);

const docsPageLoader = () => import("@/components/Site/Pages/DocsPage");
const DocsPage = lazy(docsPageLoader);

const contactsPageLoader = () => import("@/components/Site/Pages/ContactsPage");
const ContactsPage = lazy(contactsPageLoader);

const routesPageLoader = () =>
  import("@/components/Site/Pages/RoutesPage/RoutesPage");
const RoutesPage = lazy(routesPageLoader);

// Тяжелые страницы - lazy loading для оптимизации производительности
const randomMemePageLoader = () =>
  import("@/components/Site/Pages/RandomMemePage/RandomMemePage");
const RandomMemePage = lazy(randomMemePageLoader);

const randomMemeDetailsPageLoader = () =>
  import("@/components/Site/Pages/RandomMemePage/pages").then(m => ({
    default: m.RandomMemeDetailsPage,
  }));
const RandomMemeDetailsPage = lazy(randomMemeDetailsPageLoader);

const randomMemeEditPageLoader = () =>
  import("@/components/Site/Pages/RandomMemePage/pages").then(m => ({
    default: m.RandomMemeEditPage,
  }));
const RandomMemeEditPage = lazy(randomMemeEditPageLoader);

const cinemaQueuePageLoader = () =>
  import("@/components/Site/Pages/CinemaQueuePage/CinemaQueuePage");
const CinemaQueuePage = lazy(cinemaQueuePageLoader);

const commandsPageLoader = () =>
  import("@/components/Site/Pages/CommandsPage/CommandsPage");
const CommandsPage = lazy(commandsPageLoader);

const mediaInfoListPageLoader = () =>
  import("@/components/Site/Pages/MediaInfoPage/MediaInfoListPage").then(m => ({
    default: m.MediaInfoListPage,
  }));
const MediaInfoListPage = lazy(mediaInfoListPageLoader);

const mediaInfoCreatePageLoader = () =>
  import("@/components/Site/Pages/MediaInfoPage/MediaInfoCreatePage").then(
    m => ({
      default: m.MediaInfoCreatePage,
    })
  );
const MediaInfoCreatePage = lazy(mediaInfoCreatePageLoader);

const mediaInfoEditPageLoader = () =>
  import("@/components/Site/Pages/MediaInfoPage/MediaInfoEditPage").then(m => ({
    default: m.MediaInfoEditPage,
  }));
const MediaInfoEditPage = lazy(mediaInfoEditPageLoader);

const environmentVariablesPageLoader = () =>
  import("@/components/Site/Pages/EnvironmentVariablesPage/EnvironmentVariablesPage").then(
    m => ({ default: m.default })
  );

const EnvironmentVariablesPage = lazy(environmentVariablesPageLoader);

const waifuRollPageLoader = () =>
  import("@/components/Site/Pages/WaifuRollPage/WaifuRollPage").then(m => ({
    default: m.default,
  }));

const WaifuRollPage = lazy(waifuRollPageLoader);

// Регистрируем все некритичные компоненты для фоновой загрузки
registerPrefetchComponents([
  // Часто используемые страницы
  aboutPageLoader,
  docsPageLoader,
  contactsPageLoader,
  commandsPageLoader,
  routesPageLoader,
  // Тяжелые страницы
  randomMemePageLoader,
  randomMemeDetailsPageLoader,
  randomMemeEditPageLoader,
  cinemaQueuePageLoader,
  mediaInfoListPageLoader,
  mediaInfoCreatePageLoader,
  mediaInfoEditPageLoader,
  environmentVariablesPageLoader,
  waifuRollPageLoader,
]);

// Массив основных страниц сайта с Layout
export const mainSiteRoutes: RouteConfig[] = [
  {
    path: "/environment-variables",
    name: "Переменные окружения",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <EnvironmentVariablesPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/waifu-roll",
    name: "WaifuRoll Manager",
    type: "control panel",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <WaifuRollPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/",
    name: "Главная страница",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <WelcomePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/about",
    name: "О проекте",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <AboutPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/docs",
    name: "Документация",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <DocsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/contacts",
    name: "Контакты",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <ContactsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/commands",
    name: "Команды",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <CommandsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/routes",
    name: "Все маршруты",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <RoutesPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/media-info",
    name: "Информация о медиа",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <MediaInfoListPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/media-info/create",
    name: "Создание медиа",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <MediaInfoCreatePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/media-info/edit/:id",
    name: "Редактирование медиа",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <MediaInfoEditPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/cinema-queue",
    name: "Очередь кинотеатра",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <CinemaQueuePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/random-meme",
    name: "Random Meme Manager",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <RandomMemePage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/random-meme/:id",
    name: "Random Meme Details",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <RandomMemeDetailsPage />
        </Suspense>
      </Layout>
    ),
  },
  {
    path: "/random-meme/edit/:id",
    name: "Edit Random Meme",
    type: "site",
    element: (
      <Layout>
        <Suspense fallback={<PageLoader />}>
          <RandomMemeEditPage />
        </Suspense>
      </Layout>
    ),
  },
];
