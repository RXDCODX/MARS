/**
 * Типы для глобальных цветовых переменных сайта
 */

export type SiteTextColor =
  | "primary"
  | "secondary"
  | "muted"
  | "light"
  | "dark"
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "info";

export type SiteBackgroundColor =
  | "primary"
  | "secondary"
  | "tertiary"
  | "card"
  | "overlay"
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "info";

export type SiteBorderColor =
  | "primary"
  | "secondary"
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "info";

export type SiteShadowType = "light" | "medium" | "heavy" | "inset";

export type SiteButtonVariant =
  | "primary"
  | "secondary"
  | "success"
  | "warning"
  | "danger"
  | "info";

export interface SiteColors {
  text: Record<SiteTextColor, string>;
  background: Record<SiteBackgroundColor, string>;
  border: Record<SiteBorderColor, string>;
  shadow: Record<SiteShadowType, string>;
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
  theme: "light" | "dark";
  utils: {
    getTextStyle: (type: SiteTextColor) => React.CSSProperties;
    getBackgroundStyle: (type: SiteBackgroundColor) => React.CSSProperties;
    getBorderStyle: (type: SiteBorderColor) => React.CSSProperties;
    getShadowStyle: (type: SiteShadowType) => React.CSSProperties;
    getCardStyle: () => React.CSSProperties;
    getButtonStyle: (variant?: SiteButtonVariant) => React.CSSProperties;
  };
}

export interface SiteColorStyles {
  // Текстовые стили
  textPrimary: React.CSSProperties;
  textSecondary: React.CSSProperties;
  textMuted: React.CSSProperties;
  textLight: React.CSSProperties;
  textDark: React.CSSProperties;
  textAccent: React.CSSProperties;
  textSuccess: React.CSSProperties;
  textWarning: React.CSSProperties;
  textDanger: React.CSSProperties;
  textInfo: React.CSSProperties;

  // Фоновые стили
  bgPrimary: React.CSSProperties;
  bgSecondary: React.CSSProperties;
  bgTertiary: React.CSSProperties;
  bgCard: React.CSSProperties;
  bgOverlay: React.CSSProperties;
  bgAccent: React.CSSProperties;
  bgSuccess: React.CSSProperties;
  bgWarning: React.CSSProperties;
  bgDanger: React.CSSProperties;
  bgInfo: React.CSSProperties;

  // Стили границ
  borderPrimary: React.CSSProperties;
  borderSecondary: React.CSSProperties;
  borderAccent: React.CSSProperties;
  borderSuccess: React.CSSProperties;
  borderWarning: React.CSSProperties;
  borderDanger: React.CSSProperties;
  borderInfo: React.CSSProperties;

  // Стили теней
  shadowLight: React.CSSProperties;
  shadowMedium: React.CSSProperties;
  shadowHeavy: React.CSSProperties;
  shadowInset: React.CSSProperties;

  // Комбинированные стили
  card: React.CSSProperties;
  button: React.CSSProperties;
  buttonPrimary: React.CSSProperties;
  buttonSecondary: React.CSSProperties;
  buttonSuccess: React.CSSProperties;
  buttonWarning: React.CSSProperties;
  buttonDanger: React.CSSProperties;
  buttonInfo: React.CSSProperties;
}
