import { CSSProperties, useEffect } from "react";

interface StrokeConfig {
  /** Цвет обводки текста (по умолчанию: #fff) */
  color?: string;
  /** Толщина обводки в px (по умолчанию: 4) */
  width?: number;
  /** Промежуток между текстом и обводкой (по умолчанию: 2) */
  gap?: number;
  /** Включить/выключить обводку (по умолчанию: true) */
  enabled?: boolean;
}

type SpeedPreset = "very-slow" | "slow" | "normal" | "fast" | "very-fast";

interface GradientTextProperties {
  text: string;
  colors?: string[];
  speed?: SpeedPreset;
  fontSize?: string;
  fontWeight?: CSSProperties["fontWeight"];
  className?: string;
  stroke?: StrokeConfig;
}

const GradientTextSVG: React.FC<GradientTextProperties> = ({
  text,
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
  stroke = {
    color: "#fff",
    width: 1,
    gap: 2,
    enabled: true,
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
  const strokeColor = stroke.color || "#fff";
  const strokeWidth = stroke.width ?? 4;
  const gap = stroke.gap ?? 2;
  const outerStroke = strokeWidth;
  const innerStroke = Math.max(outerStroke - gap, 1);
  const fontId = `gradient-${Math.random().toString(36).slice(2, 10)}`;

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

  if (stroke.enabled) {
    return (
      <svg
        style={{ display: "inline-block", fontSize, fontWeight }}
        className={className}
        aria-label={text}
      >
        <defs>
          <linearGradient id={fontId} x1="0%" y1="0%" x2="100%" y2="0%">
            {colors.map((color, index) => (
              <stop
                key={index}
                offset={`${(index / (colors.length - 1)) * 100}%`}
                stopColor={color}
              />
            ))}
          </linearGradient>
        </defs>
        {/* Внешняя обводка */}
        <text
          x="0"
          y="0"
          dy="1em"
          stroke={strokeColor}
          strokeWidth={outerStroke}
          fill="none"
          strokeLinejoin="round"
          style={{
            fontSize: "inherit",
            fontWeight: "inherit",
            paintOrder: "stroke",
          }}
        >
          {text}
        </text>
        {/* Прозрачный gap (внутренняя "обводка" цветом фона, если нужно) */}
        <text
          x="0"
          y="0"
          dy="1em"
          stroke="#fff"
          strokeWidth={innerStroke}
          fill="none"
          strokeLinejoin="round"
          style={{
            fontSize: "inherit",
            fontWeight: "inherit",
            paintOrder: "stroke",
            opacity: 1,
          }}
        >
          {text}
        </text>
        {/* Градиентный текст */}
        <text
          x="0"
          y="0"
          dy="1em"
          fill={`url(#${fontId})`}
          style={{
            fontSize: "inherit",
            fontWeight: "inherit",
          }}
        >
          {text}
        </text>
      </svg>
    );
  }

  // Обычный градиентный текст без обводки
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
    display: "inline-block",
  };

  return (
    <span
      className={`gradient-text ${className}`}
      style={gradientStyle}
      aria-label={text}
    >
      {text}
    </span>
  );
};

export default GradientTextSVG;
