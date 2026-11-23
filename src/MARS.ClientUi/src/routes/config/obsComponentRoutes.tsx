import { lazy, Suspense } from "react";

import { OBSComponentWrapper } from "@/overlays/wrapper";
import { OBSLazyLoader } from "@/components/LazyLoader";

import { registerPrefetchComponents } from "../utils/prefetchRoutes";
import { RouteConfig } from "./RouteConfig";

// Все OBS компоненты - lazy loading для оптимизации производительности
const afkScreenLoader = () => import("@/overlays/AFKScreen/AFKScreen");
const AFKScreen = lazy(afkScreenLoader);

const matrixScreenLoader = () =>
  import("@/overlays/MatrixScreen/MatrixScreen").then(m => ({
    default: m.default,
  }));
const MatrixScreen = lazy(matrixScreenLoader);

const matrixConfiguratorLoader = () =>
  import("@/overlays/MatrixScreen/MatrixConfigurator").then(m => ({
    default: m.default,
  }));
const MatrixConfigurator = lazy(matrixConfiguratorLoader);

const chatHorizontalLoader = () =>
  import("@/overlays/ChatHorizontal/ChatHorizontal");
const ChatHorizontal = lazy(chatHorizontalLoader);

const chatVerticalLoader = () => import("@/overlays/ChatVertical/ChatVertical");
const ChatVertical = lazy(chatVerticalLoader);

const highliteMessageLoader = () =>
  import("@/overlays/HighliteMessage/HighliteMessage");
const HighliteMessage = lazy(highliteMessageLoader);

const choosePathLoader = () =>
  import("@/overlays/SoundRequest/ChoosePath").then(m => ({
    default: m.ChoosePath,
  }));
const ChoosePath = lazy(choosePathLoader);

// Тяжелые компоненты - lazy loading
const creditsLoader = () => import("@/overlays/Credits/Credits");
const Credits = lazy(creditsLoader);

const adhdControllerLoader = () =>
  import("@/overlays/ADHDLayout").then(m => ({
    default: m.ADHDController,
  }));
const ADHDController = lazy(adhdControllerLoader);

const explosionVideoLoader = () =>
  import("@/overlays/ADHDLayout/ExplosionVideo").then(m => ({
    default: m.ExplosionVideo,
  }));
const ExplosionVideo = lazy(explosionVideoLoader);

const autoMessageBillboardLoader = () =>
  import("@/overlays/AutoMessageBillboard/AutoMessageBillboard");
const AutoMessageBillboard = lazy(autoMessageBillboardLoader);


const fumoFridayLoader = () =>
  import("@/overlays/FumoFriday").then(m => ({
    default: m.FumoFriday,
  }));
const FumoFriday = lazy(fumoFridayLoader);

const gaoAlertControllerLoader = () =>
  import("@/overlays/GaoAlert/GaoAlertController");
const GaoAlertController = lazy(gaoAlertControllerLoader);

const michaelJacksonLoader = () => import("@/overlays/MichaelJackson");
const MichaelJackson = lazy(michaelJacksonLoader);

const mikuMondayLoader = () =>
  import("@/overlays/MikuMonday/MikuMondayController");
const MikuMonday = lazy(mikuMondayLoader);

const mikuMikuBeamLoader = () =>
  import("@/overlays/MikuMikuBeam").then(m => ({
    default: m.MikuMikuBeamComponent,
  }));
const MikuMikuBeam = lazy(mikuMikuBeamLoader);

const pngTuberLoader = () =>
  import("@/overlays/PNGTuber").then(m => ({
    default: m.PNGTuber,
  }));
const PNGTuber = lazy(pngTuberLoader);

const avatarWithFireLoader = () =>
  import("@/overlays/PNGTuber").then(m => ({
    default: m.AvatarWithFire,
  }));
const AvatarWithFire = lazy(avatarWithFireLoader);

const avatarWithFireSvgLoader = () =>
  import("@/overlays/PNGTuber").then(m => ({
    default: m.AvatarWithFireSvg,
  }));
const AvatarWithFireSvg = lazy(avatarWithFireSvgLoader);

const pyroAlertsLoader = () => import("@/overlays/PyroAlerts/PyroAlerts");
const PyroAlerts = lazy(pyroAlertsLoader);

const randomMemLoader = () => import("@/overlays/RandomMem/RandomMem");
const RandomMem = lazy(randomMemLoader);

const scoreboardLoader = () => import("@/overlays/Scoreboard");
const Scoreboard = lazy(scoreboardLoader);

const managerLoader = () => import("@/overlays/ScreenParticles/Manager");
const Manager = lazy(managerLoader);

const waifuAlertsLoader = () => import("@/overlays/WaifuAlerts/WaifuAlerts");
const WaifuAlerts = lazy(waifuAlertsLoader);

const fumoAlertsLoader = () => import("@/overlays/FumoAlerts/FumoAlerts");
const FumoAlerts = lazy(fumoAlertsLoader);

const frogAlertsLoader = () => import("@/overlays/FrogAlerts/FrogAlerts");
const FrogAlerts = lazy(frogAlertsLoader);

const mikuAlertsLoader = () => import("@/overlays/MikuAlerts/MikuAlerts");
const MikuAlerts = lazy(mikuAlertsLoader);

const phonkEditLoader = () =>
  import("@/overlays/PhonkLayout/PhonkLayoutManager");
const PhonkLayoutManager = lazy(phonkEditLoader);

const tikTokEditLoader = () =>
  import("@/overlays/TikTokBigEdit/TikTokLayoutManager");
const TikTokLayoutManager = lazy(tikTokEditLoader);

import { AllRefundManager } from "./AllRefundLoader";

// Регистрируем OBS компоненты для фоновой загрузки
// Эти компоненты загружаются редко, поэтому у них низкий приоритет
registerPrefetchComponents([
  // Легкие компоненты
  afkScreenLoader,
  chatHorizontalLoader,
  chatVerticalLoader,
  highliteMessageLoader,
  matrixScreenLoader,
  matrixConfiguratorLoader,
  // Тяжелые компоненты
  // choosePathLoader, - убрано из предзагрузки, чтобы CSS загружался только при рендере компонента
  creditsLoader,
  adhdControllerLoader,
  explosionVideoLoader,
  autoMessageBillboardLoader,
  fumoFridayLoader,
  gaoAlertControllerLoader,
  michaelJacksonLoader,
  mikuMondayLoader,
  mikuMikuBeamLoader,
  pngTuberLoader,
  avatarWithFireLoader,
  avatarWithFireSvgLoader,
  pyroAlertsLoader,
  randomMemLoader,
  scoreboardLoader,
  managerLoader,
  waifuAlertsLoader,
  fumoAlertsLoader,
  frogAlertsLoader,
  mikuAlertsLoader,
]);

// Массив OBS компонентов (без Layout для интеграции в OBS)
export const obsComponentRoutes: RouteConfig[] = [
  {
    path: "/all-refund",
    name: "All Refund",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <AllRefundManager />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/tik-tok-manager",
    name: "TikTok Manager",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <TikTokLayoutManager />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/phonk-manager",
    name: "Phonk Manager",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <PhonkLayoutManager />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },

  {
    path: "/MikuMonday",
    name: "Miku Monday",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MikuMonday />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/mikumikubeam",
    name: "Miku Miku Beam",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MikuMikuBeam />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/sr/*",
    name: "SR: SoundRequest",
    type: "obs",
    element: (
      <Suspense fallback={<OBSLazyLoader />}>
        <ChoosePath />
      </Suspense>
    ),
  },
  {
    path: "/MichaelJackson",
    name: "Michael Jackson",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MichaelJackson />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/credits",
    name: "Титры (RXDCODX)",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <Credits />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/gaoalert",
    name: "Гао алертс",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <GaoAlertController />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/adhd",
    name: "ADHD Layout",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <ADHDController />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/automessage",
    name: "Автосообщения",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <AutoMessageBillboard />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/pyroalerts",
    name: "Pyro Алерты",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <PyroAlerts />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/randommem",
    name: "Случайные мемы",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <RandomMem />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/waifu",
    name: "Waifu Алерты",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <WaifuAlerts />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/fumo",
    name: "Fumo Алерты",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <FumoAlerts />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/frogs",
    name: "Frog Алерты",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <FrogAlerts />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/miku-roll",
    name: "Miku Алерты",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MikuAlerts />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/fumofriday",
    name: "Fumo Friday",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <FumoFriday />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/highlite",
    name: "Подсветка сообщений",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <HighliteMessage />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/confetti",
    name: "Конфетти",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <Manager />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/scoreboard",
    name: "Скорборд",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <Scoreboard />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/chath",
    name: "Чат горизонтальный",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <ChatHorizontal />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/chatv",
    name: "Чат вертикальный",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <ChatVertical />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/afkscreen",
    name: "AFK экран",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <AFKScreen />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/avatarka",
    name: "PNG Tuber",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <PNGTuber />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/avatarka-fire",
    name: "Аватарка с огнем",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <AvatarWithFire />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/avatarka-fire-svg",
    name: "Аватарка с огнем (SVG)",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <AvatarWithFireSvg />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/explosion",
    name: "Explosion",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <ExplosionVideo />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/matrix",
    name: "Matrix Screen",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MatrixScreen />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
  {
    path: "/matrix-configurator",
    name: "Matrix Конфигуратор",
    type: "obs",
    element: (
      <OBSComponentWrapper>
        <Suspense fallback={<OBSLazyLoader />}>
          <MatrixConfigurator />
        </Suspense>
      </OBSComponentWrapper>
    ),
  },
];
