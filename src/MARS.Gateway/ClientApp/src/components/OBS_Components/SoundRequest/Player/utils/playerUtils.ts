/**
 * Утилитарные функции для плеера SoundRequest
 */

/**
 * Конвертирует длительность из формата ISO 8601 в секунды
 * @param duration - длительность в формате ISO 8601 (например, "PT1M23S")
 * @returns количество секунд
 */
export const parseDurationToSeconds = (duration?: string): number => {
  if (!duration) return 0;
  const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
  if (!match) return 0;
  const h = parseInt(match[1] || "0");
  const m = parseInt(match[2] || "0");
  const s = parseInt(match[3] || "0");
  return h * 3600 + m * 60 + s;
};

/**
 * Форматирует длительность трека из формата ISO 8601 (PT1M23S) в читаемый формат (MM:SS или HH:MM:SS)
 * @param duration - длительность в формате ISO 8601 (например, "PT1M23S" или "PT2H3M45S")
 * @returns отформатированная строка времени
 */
export const formatDuration = (duration: string): string => {
  if (!duration) return "00:00";

  // Парсим формат PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?
  const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
  if (!match) return duration;

  const hours = parseInt(match[1] || "0");
  const minutes = parseInt(match[2] || "0");
  const seconds = parseInt(match[3] || "0");

  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, "0")}:${seconds
      .toString()
      .padStart(2, "0")}`;
  }

  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
};

/**
 * Получает список авторов трека в виде строки
 * @param authors - массив авторов
 * @returns строка с авторами, разделенными запятыми
 */
export const getAuthorsString = (authors?: string[]): string =>
  authors?.join(", ") || "Неизвестный исполнитель";

/**
 * Получает имя пользователя, запросившего трек
 * @param displayName - отображаемое имя пользователя
 * @returns строка с именем пользователя
 */
export const getRequestedByString = (displayName?: string): string =>
  displayName || "Неизвестный пользователь";
