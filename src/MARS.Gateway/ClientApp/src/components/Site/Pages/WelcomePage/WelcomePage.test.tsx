import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

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
    const markup = renderToStaticMarkup(createElement(WelcomePage));
    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });

  it("contains dashboard title", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkup(createElement(WelcomePage));
    expect(markup).toContain("MARS Server Dashboard");
  });

  it("contains quick links", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkup(createElement(WelcomePage));
    expect(markup).toContain("/admin");
    expect(markup).toContain("/logs");
    expect(markup).toContain("/services");
    expect(markup).toContain("/routes");
  });

  it("has data-testid attributes", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkup(createElement(WelcomePage));
    expect(markup).toContain('data-testid="page-welcome"');
  });

  it("shows loading state initially", async () => {
    const { default: WelcomePage } = await import("./WelcomePage");
    const markup = renderToStaticMarkup(createElement(WelcomePage));
    expect(markup).toContain("Загрузка статистики...");
  });
});
