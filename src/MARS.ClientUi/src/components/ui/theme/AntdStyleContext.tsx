import {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import { useTheme } from "@/contexts/Theme";

export const ANTD_STYLES = [
  "illustration",
  "mui",
  "cartoon",
  "geek",
  "nord",
  "dracula",
  "solarized",
  "cyberpunk",
  "glass",
  "brutalist",
  "aurora",
  "midnight",
  "editorial",
  "sakura",
] as const;

export type AntdStyle = (typeof ANTD_STYLES)[number];

interface AntdStyleContextType {
  antdStyle: AntdStyle;
  setAntdStyle: (style: AntdStyle) => void;
}

const AntdStyleContext = createContext<AntdStyleContextType | undefined>(
  undefined
);

export const useAntdStyle = () => {
  const context = useContext(AntdStyleContext);
  if (!context) {
    throw new Error("useAntdStyle must be used within AntdStyleProvider");
  }
  return context;
};

interface AntdStyleProviderProperties {
  children: ReactNode;
}

export const AntdStyleProvider: React.FC<AntdStyleProviderProperties> = ({
  children,
}) => {
  const { setTheme } = useTheme();

  const [antdStyle, setAntdStyleState] = useState<AntdStyle>(() => {
    const saved = localStorage.getItem("antd-style");
    return saved && ANTD_STYLES.includes(saved as AntdStyle)
      ? (saved as AntdStyle)
      : "illustration";
  });

  const previousStyleReference = useRef<AntdStyle>(antdStyle);

  const setAntdStyle = useCallback((style: AntdStyle) => {
    setAntdStyleState(style);
    localStorage.setItem("antd-style", style);
  }, []);

  // Стиль «geek» всегда тёмный: сохраняем прежнее поведение — при выборе
  // geek переводим тему в dark, при уходе с geek возвращаем light. Никакой
  // другой стиль тему не трогает: раньше здесь принудительно сбрасывался DOM
  // в light на каждом переключении стиля, пока React-состояние оставалось
  // dark — и страница расходилась с antd-токенами («не ререндерятся блоки»).
  useEffect(() => {
    const previous = previousStyleReference.current;

    if (antdStyle === "geek" && previous !== "geek") {
      setTheme("dark");
    } else if (antdStyle !== "geek" && previous === "geek") {
      setTheme("light");
    }

    // Первый монтирование: geek сохранён в localStorage — синхронизируем тему.
    if (previous === antdStyle && antdStyle === "geek") {
      setTheme("dark");
    }

    previousStyleReference.current = antdStyle;
  }, [antdStyle, setTheme]);

  const value = useMemo(
    () => ({ antdStyle, setAntdStyle }),
    [antdStyle, setAntdStyle]
  );

  return (
    <AntdStyleContext.Provider value={value}>
      {children}
    </AntdStyleContext.Provider>
  );
};
