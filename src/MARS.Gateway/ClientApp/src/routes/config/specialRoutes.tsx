import { lazy, Suspense } from "react";

import ScoreboardAdminPanel from "@/components/OBS_Components/Scoreboard/AdminPanel/AdminPanel";
import { OBSLazyLoader } from "@/components/shared/LazyLoader";
import TelegramClipboardCopyPage from "@/components/Site/Pages/TelegramClipboardCopyPage";

import { RouteConfig } from "./RouteConfig";

const adhdAdminPanelLoader = () =>
  import("@/components/OBS_Components/ADHDLayout").then(m => ({
    default: m.ADHDAdminPanel,
  }));
const ADHDAdminPanel = lazy(adhdAdminPanelLoader);

// Массив специальных маршрутов
export const specialRoutes: RouteConfig[] = [
  {
    path: "/telegram-copy",
    name: "Telegram copy",
    type: "special",
    element: <TelegramClipboardCopyPage />,
  },
  {
    path: "/scoreboard-admin",
    name: "Админ панель скорборда",
    type: "special",
    element: <ScoreboardAdminPanel />,
  },
  {
    path: "/adhd-admin",
    name: "Админ панель ADHD",
    type: "special",
    element: (
      <Suspense fallback={<OBSLazyLoader />}>
        <ADHDAdminPanel />
      </Suspense>
    ),
  },
];
