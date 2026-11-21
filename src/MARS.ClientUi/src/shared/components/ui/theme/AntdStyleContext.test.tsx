import { act, renderHook } from "@testing-library/react";
import { createElement } from "react";
import { describe, expect, it } from "vitest";

import { AntdStyleProvider, useAntdStyle } from "./AntdStyleContext";
import { useTheme } from "@/contexts/Theme";
import { ThemeProvider } from "@/contexts/ThemeContext";

const wrapper = ({ children }: { children: React.ReactNode }) =>
  createElement(
    ThemeProvider,
    null,
    createElement(AntdStyleProvider, null, children)
  );

describe("AntdStyleProvider theme sync", () => {
  it("does not reset a dark theme when switching between non-geek styles", () => {
    localStorage.clear();
    localStorage.setItem("theme", "dark");
    localStorage.setItem("antd-style", "illustration");

    const { result } = renderHook(
      () => ({ theme: useTheme(), style: useAntdStyle() }),
      { wrapper }
    );

    expect(result.current.theme.resolvedTheme).toBe("dark");

    act(() => result.current.style.setAntdStyle("mui"));

    expect(result.current.theme.resolvedTheme).toBe("dark");
    expect(document.body.dataset.theme).toBe("dark");
  });

  it("forces dark when selecting geek and restores light when leaving", () => {
    localStorage.clear();
    localStorage.setItem("theme", "light");
    localStorage.setItem("antd-style", "illustration");

    const { result } = renderHook(
      () => ({ theme: useTheme(), style: useAntdStyle() }),
      { wrapper }
    );

    act(() => result.current.style.setAntdStyle("geek"));
    expect(result.current.theme.resolvedTheme).toBe("dark");

    act(() => result.current.style.setAntdStyle("illustration"));
    expect(result.current.theme.resolvedTheme).toBe("light");
  });
});
