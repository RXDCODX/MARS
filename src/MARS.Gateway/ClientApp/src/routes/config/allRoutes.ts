import { adminRoutes } from "./adminRoutes";
import { mainSiteRoutes } from "./mainSiteRoutes";
import { obsComponentRoutes } from "./obsComponentRoutes";
import { RouteConfig } from "./RouteConfig";
import { specialRoutes } from "./specialRoutes";

// Объединяем все маршруты в один массив
export const allRoutes: RouteConfig[] = [
  ...mainSiteRoutes,
  ...obsComponentRoutes,
  ...adminRoutes,
  ...specialRoutes,
];
