import { useEffect, useState } from "react";

import MatrixScreen, { MatrixOptions } from "./MatrixScreen";

// Функция для получения параметров из URL
function getOptionsFromUrl(): MatrixOptions {
  const parameters = new URLSearchParams(location.search);
  const options: MatrixOptions = {};

  const parameterMap: { [key: string]: string | number | boolean | null } = {};

  // Строковые параметры
  const stringParameters = [
    "version",
    "font",
    "effect",
    "palette",
    "stripeColors",
    "url",
  ];
  for (const parameter of stringParameters) {
    const value = parameters.get(parameter);
    if (value) parameterMap[parameter] = value;
  }

  // Булевы параметры
  const booleanParameters = [
    "skipIntro",
    "glyphFlip",
    "volumetric",
    "camera",
    "loops",
    "suppressWarnings",
  ];
  for (const parameter of booleanParameters) {
    const value = parameters.get(parameter);
    if (value !== null) parameterMap[parameter] = value === "true";
  }

  // Числовые параметры
  const numberParameters = [
    "numColumns",
    "glyphRotation",
    "slant",
    "bloomSize",
    "bloomStrength",
    "ditherMagnitude",
    "resolution",
    "raindropLength",
    "animationSpeed",
    "fallSpeed",
    "cycleSpeed",
    "density",
    "forwardSpeed",
    "fps",
    "cursorIntensity",
    "glintIntensity",
  ];
  for (const parameter of numberParameters) {
    const value = parameters.get(parameter);
    if (value) {
      const numberValue = Number.parseFloat(value);
      if (!isNaN(numberValue)) parameterMap[parameter] = numberValue;
    }
  }

  return parameterMap as MatrixOptions;
}

// Функция для обновления URL с текущими параметрами
function updateUrlWithOptions(options: MatrixOptions) {
  const parameters = new URLSearchParams();
  const colorParameters: { key: string; value: string }[] = [];

  Object.entries(options).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }

    const stringValue = String(value);

    // Цветовые параметры не должны кодироваться
    if (
      key === "backgroundColor" ||
      key === "cursorColor" ||
      key === "glintColor" ||
      key === "stripeColors" ||
      key === "palette"
    ) {
      colorParameters.push({ key, value: stringValue });
    } else if (typeof value === "boolean") {
      parameters.set(key, stringValue);
    } else {
      parameters.set(key, stringValue);
    }
  });

  // Собираем URL без кодирования цветов
  const parts: string[] = [];
  parameters.forEach((value, key) => {
    parts.push(`${key}=${value}`);
  });

  // Добавляем цветовые параметры без кодирования
  for (const { key, value } of colorParameters) {
    parts.push(`${key}=${value}`);
  }

  // Обновляем URL без перезагрузки страницы
  const newUrl = `${location.pathname}${parts.length > 0 ? "?" + parts.join("&") : ""}`;
  history.replaceState({}, "", newUrl);
}

// Функция для удаления параметров из URL
function clearUrlOptions() {
  history.replaceState({}, "", location.pathname);
}

// Типы для UI элементов
type ControlType = "select" | "number" | "checkbox" | "color" | "text";

interface ControlDefinition {
  key: string;
  label: string;
  type: ControlType;
  options?: { value: string; label: string }[];
  min?: number;
  max?: number;
  step?: number;
  placeholder?: string;
}

// Группы параметров для организации UI
const controlGroups = {
  // Группа 1: Основные настройки версии и отображения
  basic: [
    {
      key: "version",
      label: "Версия Матрицы",
      type: "select",
      options: [
        { value: "classic", label: "Классическая" },
        { value: "3d", label: "3D режим" },
        { value: "megacity", label: "Мегасити" },
        { value: "operator", label: "Оператор" },
        { value: "nightmare", label: "Кошмар" },
        { value: "paradise", label: "Рай" },
        { value: "resurrections", label: "Воскрешение" },
        { value: "palimpsest", label: "Палimpseст" },
      ],
    },
    {
      key: "font",
      label: "Шрифт",
      type: "select",
      options: [
        { value: "matrixcode", label: "Matrix Code" },
        { value: "resurrections", label: "Resurrections" },
        { value: "gothic", label: "Готический" },
        { value: "coptic", label: "Коптский" },
        { value: "huberfishA", label: "Huberfish A" },
        { value: "huberfishD", label: "Huberfish D" },
      ],
    },
    {
      key: "effect",
      label: "Эффект",
      type: "select",
      options: [
        { value: "plain", label: "Обычный" },
        { value: "pride", label: "Геи" },
        { value: "stripes", label: "Полосы" },
        { value: "none", label: "Отключен" },
        { value: "image", label: "Изображение" },
        { value: "mirror", label: "Зеркало" },
      ],
    },
    {
      key: "numColumns",
      label: "Кол-во колонок",
      type: "number",
      min: 10,
      max: 300,
      step: 10,
    },
  ] as ControlDefinition[],

  // Группа 2: Скорости и анимация
  animation: [
    {
      key: "animationSpeed",
      label: "Скорость анимации",
      type: "number",
      min: 0.1,
      max: 10,
      step: 0.1,
    },
    {
      key: "fallSpeed",
      label: "Скорость падения",
      type: "number",
      min: -5,
      max: 5,
      step: 0.1,
    },
    {
      key: "cycleSpeed",
      label: "Скорость смены символов",
      type: "number",
      min: 0.1,
      max: 10,
      step: 0.1,
    },
    {
      key: "fps",
      label: "Кадры в секунду",
      type: "number",
      min: 10,
      max: 60,
      step: 5,
    },
  ] as ControlDefinition[],

  // Группа 3: 3D и визуальные эффекты
  visual: [
    {
      key: "volumetric",
      label: "Объемный режим",
      type: "checkbox",
    },
    {
      key: "slant",
      label: "Наклон дождя (градусы)",
      type: "number",
      min: -180,
      max: 180,
      step: 5,
    },
    {
      key: "glyphRotation",
      label: "Поворот глифов (градусы)",
      type: "number",
      min: 0,
      max: 360,
      step: 45,
    },
    {
      key: "bloomSize",
      label: "Размер свечения",
      type: "number",
      min: 0,
      max: 1,
      step: 0.05,
    },
    {
      key: "bloomStrength",
      label: "Интенсивность свечения",
      type: "number",
      min: 0,
      max: 1,
      step: 0.05,
    },
  ] as ControlDefinition[],

  // Группа 4: Цвета и палитра
  colors: [
    {
      key: "backgroundColor",
      label: "Цвет фона",
      type: "color",
    },
    {
      key: "cursorColor",
      label: "Цвет курсора",
      type: "color",
    },
    {
      key: "glintColor",
      label: "Цвет блика",
      type: "color",
    },
    {
      key: "palette",
      label: "Палитра (R,G,B,%...)",
      type: "text",
      placeholder: "0.1,0,0.2,0,0.2,0.5,0,0.5,1,0.7,0,1",
    },
    {
      key: "stripeColors",
      label: "Цвета полос (R,G,B...)",
      type: "text",
      placeholder: "1,0,0,1,1,0,0,1,0",
    },
  ] as ControlDefinition[],

  // Группа 5: Дополнительные настройки
  advanced: [
    {
      key: "density",
      label: "Плотность капель",
      type: "number",
      min: 0.1,
      max: 5,
      step: 0.1,
    },
    {
      key: "forwardSpeed",
      label: "Скорость вперед (3D)",
      type: "number",
      min: 0.1,
      max: 5,
      step: 0.1,
    },
    {
      key: "resolution",
      label: "Разрешение",
      type: "number",
      min: 0.25,
      max: 1,
      step: 0.1,
    },
    {
      key: "raindropLength",
      label: "Длина капли",
      type: "number",
      min: 0.1,
      max: 10,
      step: 0.5,
    },
    {
      key: "glyphFlip",
      label: "Переворот глифов",
      type: "checkbox",
    },
    {
      key: "skipIntro",
      label: "Пропустить вступление",
      type: "checkbox",
    },
    {
      key: "camera",
      label: "Камера (для mirror)",
      type: "checkbox",
    },
    {
      key: "loops",
      label: "Зациклить",
      type: "checkbox",
    },
    {
      key: "suppressWarnings",
      label: "Без предупреждений",
      type: "checkbox",
    },
  ] as ControlDefinition[],
};

// Вспомогательная функция для обработки изменений
function parseValue(
  value: string,
  type: ControlType
): string | number | boolean {
  if (type === "checkbox") {
    return value === "true";
  }
  if (type === "number") {
    return Number.parseFloat(value) || 0;
  }
  return value;
}

export interface MatrixConfiguratorProps {
  initialOptions?: MatrixOptions;
  onOptionsChange?: (options: MatrixOptions) => void;
  className?: string;
  style?: React.CSSProperties;
}

export default function MatrixConfigurator({
  initialOptions = {},
  className,
  style,
}: MatrixConfiguratorProps) {
  const [options, setOptions] = useState<MatrixOptions>(initialOptions);
  const [isInitialized, setIsInitialized] = useState(false);

  // Загрузка параметров из URL при монтировании (если нет initialOptions)
  useEffect(() => {
    if (isInitialized || Object.keys(initialOptions).length > 0) {
      return;
    }

    const urlOptions = getOptionsFromUrl();
    if (Object.keys(urlOptions).length > 0) {
      setOptions(urlOptions);
    }
    setIsInitialized(true);
  }, []);

  // Обновление URL при изменении параметров
  useEffect(() => {
    if (isInitialized) {
      updateUrlWithOptions(options);
    }
  }, [options, isInitialized]);

  const handleValueChange = (key: string, value: string, type: ControlType) => {
    const parsedValue = parseValue(value, type);

    setOptions(previous => {
      const newOptions = { ...previous, [key]: parsedValue };
      return newOptions;
    });
  };

  // Генерация UI элемента по определению
  const renderControl = (control: ControlDefinition) => {
    const value = options[control.key as keyof MatrixOptions];
    const stringValue =
      typeof value === "boolean" ? String(value) : String(value ?? "");

    return (
      <div key={control.key} style={styles.controlGroup}>
        <label style={styles.label}>
          <span style={styles.labelText}>{control.label}</span>

          {control.type === "select" && control.options && (
            <select
              style={styles.select}
              value={stringValue}
              onChange={e =>
                handleValueChange(control.key, e.target.value, "select")
              }
            >
              {control.options.map(opt => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          )}

          {control.type === "number" && (
            <input
              type="number"
              style={styles.number}
              value={stringValue}
              onChange={e =>
                handleValueChange(control.key, e.target.value, "number")
              }
              min={control.min}
              max={control.max}
              step={control.step}
            />
          )}

          {control.type === "checkbox" && (
            <input
              type="checkbox"
              style={styles.checkbox}
              checked={Boolean(value)}
              onChange={e =>
                handleValueChange(
                  control.key,
                  String(e.target.checked),
                  "checkbox"
                )
              }
            />
          )}

          {control.type === "color" && (
            <div style={styles.colorWrapper}>
              <input
                type="color"
                style={styles.colorInput}
                value={
                  stringValue === "undefined" || stringValue === "null"
                    ? "#000000"
                    : stringValue
                }
                onChange={e =>
                  handleValueChange(control.key, e.target.value, "color")
                }
              />
              <input
                type="text"
                style={styles.colorText}
                value={stringValue}
                onChange={e =>
                  handleValueChange(control.key, e.target.value, "color")
                }
                placeholder="#RRGGBB"
              />
            </div>
          )}

          {control.type === "text" && (
            <input
              type="text"
              style={styles.text}
              value={stringValue}
              onChange={e =>
                handleValueChange(control.key, e.target.value, "text")
              }
              placeholder={control.placeholder}
            />
          )}
        </label>
      </div>
    );
  };

  return (
    <div className={className} style={{ ...styles.container, ...style }}>
      {/* Левая колонка - настройки */}
      <div style={styles.settingsColumn}>
        <h2 style={styles.columnTitle}>Настройки</h2>

        <div style={styles.content}>
          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Основное</h3>
            {controlGroups.basic.map(renderControl)}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Анимация</h3>
            {controlGroups.animation.map(renderControl)}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Визуальные эффекты</h3>
            {controlGroups.visual.map(renderControl)}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Цвета</h3>
            {controlGroups.colors.map(renderControl)}
          </section>

          <section style={styles.section}>
            <h3 style={styles.sectionTitle}>Дополнительно</h3>
            {controlGroups.advanced.map(renderControl)}
          </section>

          <section style={styles.section}>
            <button style={styles.resetButton} onClick={() => setOptions({})}>
              Сбросить все настройки
            </button>
          </section>
        </div>
      </div>

      {/* Правая колонка - результат */}
      <div style={styles.resultColumn}>
        <h2 style={styles.columnTitle}>Результат</h2>
        <div style={styles.iframeContainer}>
          <MatrixScreen options={options} />
        </div>
      </div>
    </div>
  );
}

// Стили для двухколоночной верстки
const styles: { [key: string]: React.CSSProperties } = {
  container: {
    display: "flex",
    height: "100%",
    width: "100%",
    overflow: "hidden",
  },
  settingsColumn: {
    width: "380px",
    flexShrink: 0,
    backgroundColor: "#1a1a1a",
    borderRight: "1px solid #333",
    display: "flex",
    flexDirection: "column",
    overflow: "hidden",
  },
  resultColumn: {
    flex: 1,
    display: "flex",
    flexDirection: "column",
    backgroundColor: "#000",
  },
  columnTitle: {
    margin: 0,
    padding: "16px",
    fontSize: "16px",
    fontWeight: "600",
    color: "#fff",
    borderBottom: "1px solid #333",
    backgroundColor: "#252525",
  },
  content: {
    display: "flex",
    flexDirection: "column",
    gap: "0",
    overflowY: "auto",
    flex: 1,
  },
  section: {
    display: "flex",
    flexDirection: "column",
    gap: "12px",
    padding: "16px",
    borderBottom: "1px solid #2a2a2a",
  },
  sectionTitle: {
    margin: 0,
    marginBottom: "8px",
    fontSize: "14px",
    fontWeight: "600",
    color: "#aaa",
    textTransform: "uppercase",
    letterSpacing: "1px",
  },
  controlGroup: {
    display: "flex",
    flexDirection: "column",
    gap: "4px",
  },
  label: {
    display: "flex",
    flexDirection: "column",
    gap: "4px",
  },
  labelText: {
    fontSize: "12px",
    fontWeight: "500",
    color: "#ccc",
  },
  select: {
    padding: "8px 12px",
    backgroundColor: "#2a2a2a",
    color: "#fff",
    border: "1px solid #444",
    borderRadius: "4px",
    fontSize: "13px",
    cursor: "pointer",
  },
  number: {
    padding: "8px 12px",
    backgroundColor: "#2a2a2a",
    color: "#fff",
    border: "1px solid #444",
    borderRadius: "4px",
    fontSize: "13px",
  },
  checkbox: {
    width: "18px",
    height: "18px",
    cursor: "pointer",
  },
  colorWrapper: {
    display: "flex",
    gap: "8px",
    alignItems: "center",
  },
  colorInput: {
    width: "40px",
    height: "32px",
    padding: "2px",
    border: "1px solid #444",
    borderRadius: "4px",
    cursor: "pointer",
  },
  colorText: {
    flex: 1,
    padding: "8px 12px",
    backgroundColor: "#2a2a2a",
    color: "#fff",
    border: "1px solid #444",
    borderRadius: "4px",
    fontSize: "13px",
  },
  text: {
    padding: "8px 12px",
    backgroundColor: "#2a2a2a",
    color: "#fff",
    border: "1px solid #444",
    borderRadius: "4px",
    fontSize: "13px",
  },
  resetButton: {
    padding: "12px 24px",
    backgroundColor: "#d32f2f",
    color: "#fff",
    border: "none",
    borderRadius: "6px",
    fontSize: "14px",
    fontWeight: "600",
    cursor: "pointer",
    transition: "background-color 0.2s",
    width: "100%",
  },
  iframeContainer: {
    flex: 1,
    width: "100%",
    overflow: "hidden",
  },
};
