import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/components/ThemeToggle", () => ({
  default: () => null,
}));

vi.mock("react-router-dom", () => ({
  Link: ({ children, to, ...properties }: any) =>
    createElement("a", { href: to, ...properties }, children),
  useLocation: () => ({ pathname: "/" }),
}));

vi.mock("lucide-react", () => ({
  ChevronDown: ({ size, className, ...properties }: any) =>
    createElement("svg", { width: size, className, ...properties }),
  Monitor: ({ size, className, ...properties }: any) =>
    createElement("svg", { width: size, className, ...properties }),
  FolderOpen: ({ size, className, ...properties }: any) =>
    createElement("svg", { width: size, className, ...properties }),
}));

describe("Header", () => {
  it("renders without crashing", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });

  it("contains MARS logo text", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain("MARS");
  });

  it("has navbar data-testid", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain('data-testid="navbar"');
  });

  it("has logo data-testid", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain('data-testid="nav-logo"');
  });

  it("has dropdown containers with data-testid", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain('data-testid="nav-dropdown-site"');
    expect(markup).toContain('data-testid="nav-dropdown-obs"');
  });

  it("has external link buttons", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain('data-testid="nav-link-ui"');
    expect(markup).toContain('data-testid="nav-link-static"');
  });

  it("has mobile toggle button", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain('data-testid="nav-mobile-toggle"');
  });

  it("includes animate-dropdown-open class in style block", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain("animate-dropdown-open");
    expect(markup).toContain("dropdown-open");
  });

  it("dropdown animation uses scaleY (not translateX centering)", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain("scaleY(0.95)");
    expect(markup).toContain("scaleY(1)");
    expect(markup).not.toContain("translateX(-50%)");
  });

  it("dropdown uses scale+fade animation (not translateX centering)", async () => {
    const { default: Header } = await import("./Header");
    const markup = renderToStaticMarkup(createElement(Header));
    expect(markup).toContain("dropdown-open");
    expect(markup).not.toContain("translateX(-50%)");
  });
});
