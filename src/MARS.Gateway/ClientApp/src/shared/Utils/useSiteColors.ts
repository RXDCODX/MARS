import { useTheme } from "@/contexts/Theme";

// Определяем тип возвращаемого значения
type SiteColors = {
  text: {
    primary: string;
    secondary: string;
    muted: string;
    light: string;
    dark: string;
    accent: string;
    success: string;
    warning: string;
    danger: string;
    info: string;
  };
  background: {
    primary: string;
    secondary: string;
    tertiary: string;
    card: string;
    overlay: string;
    accent: string;
    success: string;
    warning: string;
    danger: string;
    info: string;
  };
  border: {
    primary: string;
    secondary: string;
    accent: string;
    success: string;
    warning: string;
    danger: string;
    info: string;
  };
  shadow: {
    light: string;
    medium: string;
    heavy: string;
    inset: string;
  };
  hover: {
    background: string;
    text: string;
    border: string;
  };
  focus: {
    ring: string;
    border: string;
  };
  scrollbar: {
    track: string;
    thumb: string;
    thumbHover: string;
  };
  theme: string;
  utils: {
    getTextStyle: (type: string) => { color: string };
    getBackgroundStyle: (type: string) => { backgroundColor: string };
    getBorderStyle: (type: string) => { borderColor: string };
    getShadowStyle: (type: string) => { boxShadow: string };
    getCardStyle: () => {
      backgroundColor: string;
      borderColor: string;
      boxShadow: string;
    };
    getButtonStyle: (
      variant?:
        | "primary"
        | "secondary"
        | "success"
        | "warning"
        | "danger"
        | "info"
    ) => {
      backgroundColor: string;
      color: string;
      borderColor: string;
      boxShadow: string;
    };
  };
};

/**
 * Хук для получения глобальных цветовых переменных сайта
 * Автоматически адаптируется к текущей теме
 */
export const useSiteColors = (): SiteColors => {
  const { theme } = useTheme();

  return {
    // Цвета текста
    text: {
      primary: "var(--site-text-primary)",
      secondary: "var(--site-text-secondary)",
      muted: "var(--site-text-muted)",
      light: "var(--site-text-light)",
      dark: "var(--site-text-dark)",
      accent: "var(--site-text-accent)",
      success: "var(--site-text-success)",
      warning: "var(--site-text-warning)",
      danger: "var(--site-text-danger)",
      info: "var(--site-text-info)",
    },

    // Цвета фонов
    background: {
      primary: "var(--site-bg-primary)",
      secondary: "var(--site-bg-secondary)",
      tertiary: "var(--site-bg-tertiary)",
      card: "var(--site-bg-card)",
      overlay: "var(--site-bg-overlay)",
      accent: "var(--site-bg-accent)",
      success: "var(--site-bg-success)",
      warning: "var(--site-bg-warning)",
      danger: "var(--site-bg-danger)",
      info: "var(--site-bg-info)",
    },

    // Цвета границ
    border: {
      primary: "var(--site-border-primary)",
      secondary: "var(--site-border-secondary)",
      accent: "var(--site-border-accent)",
      success: "var(--site-border-success)",
      warning: "var(--site-border-warning)",
      danger: "var(--site-border-danger)",
      info: "var(--site-border-info)",
    },

    // Цвета теней
    shadow: {
      light: "var(--site-shadow-light)",
      medium: "var(--site-shadow-medium)",
      heavy: "var(--site-shadow-heavy)",
      inset: "var(--site-shadow-inset)",
    },

    // Цвета для ховер эффектов
    hover: {
      background: "var(--site-hover-bg)",
      text: "var(--site-hover-text)",
      border: "var(--site-hover-border)",
    },

    // Цвета для фокуса
    focus: {
      ring: "var(--site-focus-ring)",
      border: "var(--site-focus-border)",
    },

    // Цвета для скроллбаров
    scrollbar: {
      track: "var(--site-scrollbar-track)",
      thumb: "var(--site-scrollbar-thumb)",
      thumbHover: "var(--site-scrollbar-thumb-hover)",
    },

    // Текущая тема
    theme,

    // Утилитарные функции
    utils: {
      // Получить стили для текста
      getTextStyle: (type: string) => ({
        color: `var(--site-text-${type})`,
      }),

      // Получить стили для фона
      getBackgroundStyle: (type: string) => ({
        backgroundColor: `var(--site-bg-${type})`,
      }),

      // Получить стили для границы
      getBorderStyle: (type: string) => ({
        borderColor: `var(--site-border-${type})`,
      }),

      // Получить стили для тени
      getShadowStyle: (type: string) => ({
        boxShadow: `var(--site-shadow-${type})`,
      }),

      // Получить стили для карточки
      getCardStyle: () => ({
        backgroundColor: "var(--site-bg-card)",
        borderColor: "var(--site-border-primary)",
        boxShadow: "var(--site-shadow-light)",
      }),

      // Получить стили для кнопки
      getButtonStyle: (
        variant:
          | "primary"
          | "secondary"
          | "success"
          | "warning"
          | "danger"
          | "info" = "primary"
      ) => ({
        backgroundColor: `var(--site-bg-${variant})`,
        color: `var(--site-text-light)`,
        borderColor: `var(--site-border-${variant})`,
        boxShadow: "var(--site-shadow-light)",
      }),
    },
  };
};

export default useSiteColors;
