import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { ThemeProvider } from "./ThemeContext";
import { useTheme } from "./Theme";

const STORAGE_KEY = "theme";

const createMatchMediaMock = (initialMatches: boolean) => {
  let matches = initialMatches;
  const listeners: Array<(event: MediaQueryListEvent) => void> = [];
  const mediaQueryList = {
    matches,
    media: "(prefers-color-scheme: dark)",
    onchange: null,
    addEventListener: (
      _type: string,
      listener: (e: MediaQueryListEvent) => void
    ) => {
      if (_type === "change") listeners.push(listener);
    },
    removeEventListener: (
      _type: string,
      listener: (e: MediaQueryListEvent) => void
    ) => {
      const index = listeners.indexOf(listener);
      if (index >= 0) listeners.splice(index, 1);
    },
    addListener: (listener: (e: MediaQueryListEvent) => void) => {
      listeners.push(listener);
    },
    removeListener: (listener: (e: MediaQueryListEvent) => void) => {
      const index = listeners.indexOf(listener);
      if (index >= 0) listeners.splice(index, 1);
    },
    dispatchEvent: () => true,
  } as unknown as MediaQueryList;

  const trigger = (nextMatches: boolean) => {
    matches = nextMatches;
    (mediaQueryList as { matches: boolean }).matches = nextMatches;
    const event = { matches: nextMatches } as MediaQueryListEvent;
    listeners.forEach(listener => listener(event));
  };

  return { mediaQueryList, trigger };
};

describe("ThemeProvider", () => {
  let matchMediaMock: ReturnType<typeof createMatchMediaMock>;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute("data-bs-theme");
    document.body.removeAttribute("data-theme");
    matchMediaMock = createMatchMediaMock(false);
    vi.stubGlobal("matchMedia", () => matchMediaMock.mediaQueryList);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
  });

  it("reads stored preference and resolves system against OS", () => {
    localStorage.setItem(STORAGE_KEY, "system");
    matchMediaMock = createMatchMediaMock(true);
    vi.stubGlobal("matchMedia", () => matchMediaMock.mediaQueryList);

    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => createElement(ThemeProvider, null, children),
    });

    expect(result.current.theme).toBe("system");
    expect(result.current.resolvedTheme).toBe("dark");
  });

  it("cycles light -> dark -> system -> light", () => {
    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => createElement(ThemeProvider, null, children),
    });

    expect(result.current.theme).toBe("light");

    act(() => result.current.toggleTheme());
    expect(result.current.theme).toBe("dark");

    act(() => result.current.toggleTheme());
    expect(result.current.theme).toBe("system");

    act(() => result.current.toggleTheme());
    expect(result.current.theme).toBe("light");
  });

  it("writes resolved theme to DOM and preference to localStorage", () => {
    localStorage.setItem(STORAGE_KEY, "dark");

    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => createElement(ThemeProvider, null, children),
    });

    expect(document.documentElement.dataset.bsTheme).toBe("dark");
    expect(document.body.dataset.theme).toBe("dark");
    expect(localStorage.getItem(STORAGE_KEY)).toBe("dark");

    act(() => result.current.setTheme("system"));
    // OS reports light in this mock
    expect(document.documentElement.dataset.bsTheme).toBe("light");
    expect(localStorage.getItem(STORAGE_KEY)).toBe("system");
  });

  it("updates resolvedTheme when OS preference changes in system mode", async () => {
    localStorage.setItem(STORAGE_KEY, "system");

    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => createElement(ThemeProvider, null, children),
    });

    expect(result.current.resolvedTheme).toBe("light");

    act(() => matchMediaMock.trigger(true));

    await waitFor(() => {
      expect(result.current.resolvedTheme).toBe("dark");
    });
    expect(document.documentElement.dataset.bsTheme).toBe("dark");
  });
});
