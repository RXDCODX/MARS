import { screen, waitFor } from "@testing-library/react";
import { createElement } from "react";
import { describe, expect, it, vi } from "vitest";

import {
  renderToStaticMarkupWithProviders,
  renderWithProviders,
} from "@/tests/renderWithProviders";

vi.mock("react-router-dom", () => ({
  Link: ({ children, to, ...properties }: any) =>
    createElement("a", { href: to, ...properties }, children),
}));

vi.mock("@/shared/components/ReactBitsBackgroundsLegacy/registry", () => ({
  reactBitsBackgroundComponentRegistry: {
    Particles: () => null,
  },
}));

describe("WelcomePage", () => {
  it("renders without crashing", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkupWithProviders(
      createElement(WelcomePage)
    );
    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });

  it("contains dashboard title", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkupWithProviders(
      createElement(WelcomePage)
    );
    expect(markup).toContain("MARS Server Dashboard");
  });

  it("shows quick links once stats arrive", async () => {
    // Быстрые ссылки лежат за состоянием, которое приходит из
    // fetch("/api/ServerStats"). При статическом рендере эффект не выполняется,
    // страница остаётся на спиннере, и проверять ссылки нечем. Поэтому здесь
    // клиентский рендер с подставленным ответом — единственный способ увидеть
    // их содержимое.
    //
    // Форма ответа — та, что реально отдаёт `ServerStatsController`:
    // `{ success, result }`. Раньше подставлялся `{ success, data }` — поле
    // `data` добавляет транспорт, а страница зовет `fetch` напрямую. При
    // настоящем ответе `stats` становился `undefined`, `error` оставался
    // `null`, и страница показывала зелёный «Онлайн» с пустым телом.
    const { default: WelcomePage } = await import("./WelcomePage");
    const payload = {
      activeServicesCount: 1,
      totalServicesCount: 2,
      cpuUsagePercent: 0,
      memoryWorkingSetBytes: 0,
      memoryPrivateBytes: 0,
      memoryGcHeapBytes: 0,
      memoryTotalBytes: 0,
      uptimeSeconds: 0,
      threadCount: 0,
      osVersion: "",
      runtimeVersion: "",
      machineName: "",
      processorCount: 0,
      isEventSubConnected: false,
    };
    const stats = { success: true, result: payload };
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValue(new Response(JSON.stringify(stats), { status: 200 }));

    try {
      renderWithProviders(createElement(WelcomePage));

      await waitFor(() => {
        expect(screen.getByTestId("link-панель-управления")).toBeTruthy();
      });

      expect(screen.getByTestId("link-логи")).toBeTruthy();
      expect(screen.getByTestId("link-сервисы")).toBeTruthy();
      expect(screen.getByTestId("link-маршруты")).toBeTruthy();
    } finally {
      fetchMock.mockRestore();
    }
  });

  it("has data-testid attributes", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkupWithProviders(
      createElement(WelcomePage)
    );
    expect(markup).toContain('data-testid="page-welcome"');
  });

  it("shows loading state initially", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkupWithProviders(
      createElement(WelcomePage)
    );
    expect(markup).toContain("Загрузка статистики...");
  });
});
