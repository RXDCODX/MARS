/**
 * Интерфейс параметров для настройки Matrix iframe
 * Основан на параметрах URL из https://github.com/rezmason/matrix
 */
export interface MatrixOptions {
  /**
   * Версия симуляции Матрицы
   * @default "classic"
   */
  version?:
    | "classic"
    | "3d"
    | "megacity"
    | "operator"
    | "nightmare"
    | "paradise"
    | "resurrections"
    | "palimpsest";

  /**
   * Начинать ли с пустого экрана
   * @default true
   */
  skipIntro?: boolean;

  /**
   * Набор глифов для отрисовки
   * @default "matrixcode"
   */
  font?:
    | "matrixcode"
    | "resurrections"
    | "gothic"
    | "coptic"
    | "huberfishA"
    | "huberfishD";

  /**
   * Количество колонок (и строк) для отрисовки
   * @default 80
   */
  numColumns?: number;

  /**
   * Переворачивать ли глифы
   * @default false
   */
  glyphFlip?: boolean;

  /**
   * Угол поворота глифов в градусах
   * @default 0
   */
  glyphRotation?: number;

  /**
   * Отрисовывать ли глифы с глубиной
   * @default false
   */
  volumetric?: boolean;

  /**
   * Количество 3D дождевых капель
   * @default 1.0
   */
  density?: number;

  /**
   * Скорость приближения 3D капель
   * @default 1.0
   */
  forwardSpeed?: number;

  /**
   * Угол падения 2D капель в градусах
   * @default 0
   */
  slant?: number;

  /**
   * Качество свечения, от 0 до 1
   * @default 0.4
   */
  bloomSize?: number;

  /**
   * Интенсивность свечения, от 0 до 1
   * @default 0.7
   */
  bloomStrength?: number;

  /**
   * Величина затенения пикселей для скрытия полос
   * @default 0.05
   */
  ditherMagnitude?: number;

  /**
   * Размер изображения относительно размера окна
   * @default 1
   */
  resolution?: number;

  /**
   * Вертикальный масштаб "капель" в колонках
   * @default 1
   */
  raindropLength?: number;

  /**
   * Общая скорость анимации
   * @default 1
   */
  animationSpeed?: number;

  /**
   * Скорость падения дождя
   * @default 1
   */
  fallSpeed?: number;

  /**
   * Скорость изменения символа глифами
   * @default 1
   */
  cycleSpeed?: number;

  /**
   * Альтернативные пост-эффекты
   * @default "default"
   */
  effect?: "plain" | "pride" | "stripes" | "none" | "image" | "mirror";

  /**
   * Включать ли веб-камеру для эффектов
   * @default false
   */
  camera?: boolean;

  /**
   * Цвета полос (для effect="stripes") - чередующиеся R,G,B значения
   */
  stripeColors?: string;

  /**
   * Палитра цветов (чередующиеся R,G,B,% значения)
   */
  palette?: string;

  /**
   * Цвет фона (R,G,B)
   */
  backgroundColor?: string;

  /**
   * Цвет курсора (R,G,B)
   */
  cursorColor?: string;

  /**
   * Цвет блика (R,G,B)
   */
  glintColor?: string;

  /**
   * Палитра цветов в HSL (H,S,L)
   */
  paletteHSL?: string;

  /**
   * Палитра полос в HSL
   */
  stripeHSL?: string;

  /**
   * Фоновый цвет в HSL
   */
  backgroundHSL?: string;

  /**
   * Цвет курсора в HSL
   */
  cursorHSL?: string;

  /**
   * Цвет блика в HSL
   */
  glintHSL?: string;

  /**
   * Яркость свечения курсора
   * @default 2.0
   */
  cursorIntensity?: number;

  /**
   * Яркость свечения блика
   * @default 1.0
   */
  glintIntensity?: number;

  /**
   * URL изображения для эффекта (для effect="image")
   */
  url?: string;

  /**
   * Зацикливать ли эффект
   */
  loops?: boolean;

  /**
   * Частота кадров от 0 до 60
   * @default 60
   */
  fps?: number;

  /**
   * Подавлять ли предупреждения
   */
  suppressWarnings?: boolean;
}

export interface MatrixScreenProps {
  /**
   * Дополнительные параметры для настройки Matrix
   */
  options?: MatrixOptions;

  /**
   * Дополнительные классы для стилизации
   */
  className?: string;

  /**
   * Стили компонента
   */
  style?: React.CSSProperties;
}

const baseUrl = "https://rezmason.github.io/matrix";

export default function MatrixScreen({
  options,
  className,
  style,
}: MatrixScreenProps) {
  // Формируем URL с параметрами

  const parameters = new URLSearchParams();

  if (options) {
    // Обрабатываем boolean параметры
    if (options.skipIntro !== undefined)
      parameters.set("skipIntro", String(options.skipIntro));
    if (options.glyphFlip !== undefined)
      parameters.set("glyphFlip", String(options.glyphFlip));
    if (options.volumetric !== undefined)
      parameters.set("volumetric", String(options.volumetric));
    if (options.camera !== undefined)
      parameters.set("camera", String(options.camera));
    if (options.loops !== undefined)
      parameters.set("loops", String(options.loops));
    if (options.suppressWarnings !== undefined)
      parameters.set("suppressWarnings", String(options.suppressWarnings));

    // Обрабатываем строковые параметры
    if (options.version) parameters.set("version", options.version);
    if (options.font) parameters.set("font", options.font);
    if (options.effect) parameters.set("effect", options.effect);
    if (options.stripeColors)
      parameters.set("stripeColors", options.stripeColors);
    if (options.palette) parameters.set("palette", options.palette);
    if (options.backgroundColor)
      parameters.set("backgroundColor", options.backgroundColor);
    if (options.cursorColor) parameters.set("cursorColor", options.cursorColor);
    if (options.glintColor) parameters.set("glintColor", options.glintColor);
    if (options.paletteHSL) parameters.set("paletteHSL", options.paletteHSL);
    if (options.stripeHSL) parameters.set("stripeHSL", options.stripeHSL);
    if (options.backgroundHSL)
      parameters.set("backgroundHSL", options.backgroundHSL);
    if (options.cursorHSL) parameters.set("cursorHSL", options.cursorHSL);
    if (options.glintHSL) parameters.set("glintHSL", options.glintHSL);
    if (options.url) parameters.set("url", options.url);

    // Обрабатываем числовые параметры
    if (options.numColumns !== undefined)
      parameters.set("numColumns", String(options.numColumns));
    if (options.glyphRotation !== undefined)
      parameters.set("glyphRotation", String(options.glyphRotation));
    if (options.density !== undefined)
      parameters.set("density", String(options.density));
    if (options.forwardSpeed !== undefined)
      parameters.set("forwardSpeed", String(options.forwardSpeed));
    if (options.slant !== undefined)
      parameters.set("slant", String(options.slant));
    if (options.bloomSize !== undefined)
      parameters.set("bloomSize", String(options.bloomSize));
    if (options.bloomStrength !== undefined)
      parameters.set("bloomStrength", String(options.bloomStrength));
    if (options.ditherMagnitude !== undefined)
      parameters.set("ditherMagnitude", String(options.ditherMagnitude));
    if (options.resolution !== undefined)
      parameters.set("resolution", String(options.resolution));
    if (options.raindropLength !== undefined)
      parameters.set("raindropLength", String(options.raindropLength));
    if (options.animationSpeed !== undefined)
      parameters.set("animationSpeed", String(options.animationSpeed));
    if (options.fallSpeed !== undefined)
      parameters.set("fallSpeed", String(options.fallSpeed));
    if (options.cycleSpeed !== undefined)
      parameters.set("cycleSpeed", String(options.cycleSpeed));
    if (options.cursorIntensity !== undefined)
      parameters.set("cursorIntensity", String(options.cursorIntensity));
    if (options.glintIntensity !== undefined)
      parameters.set("glintIntensity", String(options.glintIntensity));
    if (options.fps !== undefined) parameters.set("fps", String(options.fps));
  }

  const source = `${baseUrl}?${parameters.toString()}`;

  return (
    <div
      className={className}
      style={{ width: "100%", height: "100%", ...style }}
      data-testid="obs-matrix-screen"
    >
      <iframe
        src={source}
        title="Matrix"
        style={{ width: "100%", height: "100%", border: "none" }}
        allow="autoplay; clipboard-write"
        sandbox="allow-scripts allow-same-origin"
        data-testid="matrix-iframe"
      />
    </div>
  );
}
