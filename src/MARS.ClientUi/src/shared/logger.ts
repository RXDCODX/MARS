import { ILogger } from "@microsoft/signalr";

const LogLevelNames = {
  [0]: "Trace",
  [1]: "Debug",
  [2]: "Information",
  [3]: "Warning",
  [4]: "Error",
  [5]: "Critical",
  [6]: "None",
} as const;

type LogLevel = keyof typeof LogLevelNames;

export const logger: ILogger = {
  log: (level: number, message: any) => {
    if (!(level in LogLevelNames)) {
      console.log("%cUnknown level:", "color: red;", message);
      return;
    }

    const levelName = LogLevelNames[level as LogLevel];
    const levelColor = getLevelColor(level);

    // Попробуем аккуратно обработать разные типы сообщения
    const parsed = tryParseJson(message);

    if (parsed !== null && typeof parsed === "object") {
      // Если это объект — передаём его отдельным аргументом, чтобы в консоли можно было развернуть
      console.log(
        `%c${levelName}:`,
        `color: ${levelColor}; font-weight: bold;`,
        parsed
      );
    } else {
      // Обычное сообщение — оставляем форматирование с двумя стилями
      console.log(
        `%c${levelName}:%c`,
        `color: ${levelColor}; font-weight: bold;`,
        "color: inherit;",
        typeof message === "string"
          ? message
          : (message?.toString?.() ?? String(message))
      );
    }
  },
};

// Вспомогательные функции
function tryParseJson(input: any) {
  try {
    if (input === null || input === undefined) return null;

    // Если это уже объект — возвращаем как есть
    if (typeof input === "object") return input;

    if (typeof input !== "string") return null;

    const string_ = input.trim();

    // Пробуем парсить как есть
    try {
      return JSON.parse(string_);
    } catch {
      // Игнорируем и пробуем искать JSON внутри строки
    }

    // Ищем ближайший JSON-объект или массив внутри строки
    const firstBrace = string_.indexOf("{");
    const firstBracket = string_.indexOf("[");
    const start =
      firstBrace !== -1 && (firstBrace < firstBracket || firstBracket === -1)
        ? firstBrace
        : firstBracket;
    if (start >= 0) {
      const lastBrace = string_.lastIndexOf("}");
      const lastBracket = string_.lastIndexOf("]");
      const end = Math.max(lastBrace, lastBracket);
      if (end > start) {
        const candidate = string_.substring(start, end + 1);
        try {
          return JSON.parse(candidate);
        } catch {
          // fallthrough
        }
      }
    }

    return null;
  } catch {
    return null;
  }
}

function getLevelColor(level: number): string {
  switch (level) {
    case 0: {
      return "#9E9E9E"; // Trace - серый
    }
    case 1: {
      return "#4CAF50"; // Debug - зеленый
    }
    case 2: {
      return "#2196F3"; // Info - синий
    }
    case 3: {
      return "#FFC107"; // Warning - желтый
    }
    case 4: {
      return "#F44336"; // Error - красный
    }
    case 5: {
      return "#9C27B0"; // Critical - фиолетовый
    }
    default: {
      return "#34aeeb";
    }
  }
}
