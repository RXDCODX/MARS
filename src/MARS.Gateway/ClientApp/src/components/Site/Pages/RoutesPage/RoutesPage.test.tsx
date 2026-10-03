import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("react-router-dom", () => ({
  Link: ({ children, to, ...properties }: any) =>
    createElement("a", { href: to, ...properties }, children),
  useLocation: () => ({ pathname: "/" }),
}));

vi.mock("@/routes/config", () => ({
  allRoutes: [
    { path: "/", name: "Главная", type: "site" },
    { path: "/about", name: "О проекте", type: "site" },
    { path: "/admin", name: "Панель управления", type: "control panel" },
    { path: "/logs", name: "Логи", type: "control panel" },
    { path: "/pyroalerts", name: "Pyro Alerts", type: "obs" },
    { path: "/chath", name: "Горизонтальный чат", type: "obs" },
  ],
  RouteConfig: {},
}));

vi.mock("@/shared/components/ReactBitsBackgroundsLegacy/registry", () => ({
  reactBitsBackgroundComponentRegistry: {
    GridMotion: () => null,
  },
}));

describe("RoutesPage", () => {
  it("renders without crashing", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });

  it("contains page title", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain("Все маршруты");
  });

  it("has data-testid for routes page", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain('data-testid="routes-page"');
  });

  it("has filter tabs", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain('data-testid="filter-tabs"');
    expect(markup).toContain('data-testid="filter-tab-all"');
  });

  it("has search input", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain('data-testid="search-input"');
  });

  it("displays route cards", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain("Главная");
    expect(markup).toContain("Панель управления");
    expect(markup).toContain("Pyro Alerts");
  });

  it("shows type labels", async () => {
    const { default: RoutesPage } = await import("./RoutesPage");
    const markup = renderToStaticMarkup(createElement(RoutesPage));
    expect(markup).toContain("Страницы сайта");
    expect(markup).toContain("OBS Компоненты");
    expect(markup).toContain("Панель управления");
  });
});
