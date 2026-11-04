// Интерфейс для конфигурации маршрута
export interface RouteConfig {
  path: string;
  element: React.ReactNode;
  name?: string; // Название маршрута для отображения
  type: "site" | "obs" | "control panel" | "special"; // Тип маршрута
}
