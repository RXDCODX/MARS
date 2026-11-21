import { ReactNode, useCallback, useEffect, useMemo, useState } from "react";

import { Theme, ThemeContext } from "./Theme";

const THEME_STORAGE_KEY = "theme";

const isTheme = (value: string | null): value is Theme =>
  value === "light" || value === "dark" || value === "system";

const readStoredTheme = (): Theme => {
  try {
    const saved = localStorage.getItem(THEME_STORAGE_KEY);
    return isTheme(saved) ? saved : "light";
  } catch {
    return "light";
  }
};

const systemPrefersDark = (): boolean => {
  try {
    return globalThis.matchMedia("(prefers-color-scheme: dark)").matches;
  } catch {
    return false;
  }
};

interface ThemeProviderProperties {
  children: ReactNode;
}

export const ThemeProvider: React.FC<ThemeProviderProperties> = ({
  children,
}) => {
  const [theme, setThemeState] = useState<Theme>(() => readStoredTheme());
  const [systemDark, setSystemDark] = useState<boolean>(() =>
    systemPrefersDark()
  );

  // Следим за изменением системной темы, чтобы «Системная» тема
  // переключалась автоматически при смене настроек ОС.
  useEffect(() => {
    let mediaQuery: MediaQueryList;
    try {
      mediaQuery = globalThis.matchMedia("(prefers-color-scheme: dark)");
    } catch {
      return;
    }

    const handleChange = (event: MediaQueryListEvent) => {
      setSystemDark(event.matches);
    };

    setSystemDark(mediaQuery.matches);

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => mediaQuery.removeEventListener("change", handleChange);
    }

    // Устаревший API (Safari < 14)
    mediaQuery.addListener(handleChange);
    return () => mediaQuery.removeListener(handleChange);
  }, []);

  const resolvedTheme: "light" | "dark" = useMemo(() => {
    if (theme === "system") {
      return systemDark ? "dark" : "light";
    }
    return theme;
  }, [theme, systemDark]);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
  }, []);

  const toggleTheme = useCallback(() => {
    setThemeState(previous => {
      if (previous === "light") return "dark";
      if (previous === "dark") return "system";
      return "light";
    });
  }, []);

  useEffect(() => {
    // Применяем разрешённую тему к html/body для Bootstrap и прочего.
    document.documentElement.dataset.bsTheme = resolvedTheme;
    document.body.dataset.theme = resolvedTheme;
    // В localStorage кладём предпочтение пользователя (включая "system").
    try {
      localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch {
      // ignore
    }
  }, [theme, resolvedTheme]);

  const value = useMemo(
    () => ({ theme, resolvedTheme, setTheme, toggleTheme }),
    [theme, resolvedTheme, setTheme, toggleTheme]
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
};
