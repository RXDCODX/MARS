import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";

import {
  adminRoutes,
  mainSiteRoutes,
  obsComponentRoutes,
  specialRoutes,
} from "./config";

const PrivateRoutes = () => (
  <BrowserRouter>
    <Routes>
      {/* Основные страницы сайта с Layout */}
      {mainSiteRoutes.map(route => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}

      {/* OBS Компоненты (без Layout для интеграции в OBS) */}
      {obsComponentRoutes.map(route => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}

      {/* Панель управления (с Layout) */}
      {adminRoutes.map(route => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}

      {/* Специальные маршруты */}
      {specialRoutes.map(route => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}

      {/* Старые маршруты для совместимости */}
      <Route path="/ui/index.html" />
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  </BrowserRouter>
);

PrivateRoutes.displayName = "PrivateRoutes";

export default PrivateRoutes;
