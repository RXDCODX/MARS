import { CSSProperties, useEffect } from "react";

interface ShadowConfig {
  /** Цвет тени текста (по умолчанию: rgba(0, 0, 0, 0.3)) */
  color?: string;
  /** Размытие тени в пикселях (по умолчанию: 4) */
  blur?: number;
  /** Горизонтальное смещение тени в пикселях (по умолчанию: 2) */
  offsetX?: number;
  /** Вертикальное смещение тени в пикселях (по умолчанию: 2) */
  offsetY?: number;
  /** Включить/выключить тени (по умолчанию: false) */
  enabled?: boolean;
}

type SpeedPreset = "very-slow" | "slow" | "normal" | "fast" | "very-fast";

interface GradientTextProperties {
  text?: string; // теперь опционально
  children?: React.ReactNode; // новый способ
  colors?: string[];
  speed?: SpeedPreset;
  fontSize?: string;
  fontWeight?: CSSProperties["fontWeight"];
  className?: string;
  shadow?: ShadowConfig;
}

const GradientText: React.FC<GradientTextProperties> = ({
  text,
  children,
  colors = [
    "#ff00cc",
    "#cc00ff",
    "#9900ff",
    "#6600ff",
    "#3333ff",
    "#0066ff",
    "#00ccff",
    "#00ffff",
    "#00ffcc",
    "#00ff99",
    "#00ff66",
    "#00ff33",
    "#ccff00",
    "#ffff00",
    "#ffcc00",
    "#ff9900",
    "#ff00cc",
  ],
  speed = "normal",
  fontSize = "inherit",
  fontWeight = "normal",
  className = "",
  shadow = {
    color: "rgba(0, 0, 0, 0.3)",
    blur: 4,
    offsetX: 2,
    offsetY: 2,
    enabled: false,
  },
}) => {
  // Функция для получения значения скорости из пресета
  const getSpeedValue = (preset: SpeedPreset): number => {
    const presets = {
      "very-slow": 40,
      slow: 25,
      normal: 15,
      fast: 8,
      "very-fast": 3,
    };
    return presets[preset];
  };

  const speedValue = getSpeedValue(speed);

  useEffect(() => {
    const style = document.createElement("style");
    style.textContent = `
      @keyframes gradientShift {
        0% { background-position: 0% 50%; }
        100% { background-position: 100% 50%; }
      }
    `;
    document.head.append(style);

    return () => style.remove();
  }, []);

  const gradientStyle: CSSProperties = {
    fontSize,
    fontWeight,
    backgroundImage: `linear-gradient(90deg, ${colors.join(", ")})`,
    backgroundSize: `${colors.length * 200}% 100%`,
    animation: `gradientShift ${speedValue}s linear infinite`,
    WebkitBackgroundClip: "text",
    backgroundClip: "text",
    MozBackgroundClip: "text",
    WebkitTextFillColor: "transparent",
    color: "transparent",
    display: "inline-flex",
    alignItems: "center",
    textShadow: shadow.enabled
      ? `${shadow.offsetX}px ${shadow.offsetY}px ${shadow.blur}px ${shadow.color}`
      : "none",
  };

  // Старый способ: если передан text (старый способ) — рендерим как раньше
  if (typeof text === "string" && !children) {
    return (
      <span
        className={`gradient-text ${className}`}
        style={gradientStyle}
        aria-label={text}
      >
        {text}
      </span>
    );
  }

  // Новый способ: children — рендерим как есть, чтобы emoji (img) были частью текста
  return (
    <span className={`gradient-text ${className}`} style={gradientStyle}>
      {children}
    </span>
  );
};

export default GradientText;
