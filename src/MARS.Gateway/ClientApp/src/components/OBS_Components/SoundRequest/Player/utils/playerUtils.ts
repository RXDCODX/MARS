/**
 * Утилитарные функции для плеера SoundRequest
 */

/**
 * Конвертирует длительность в секунды.
 *
 * Форматов два, и оба приходят на этот же экран:
 *
 * - `hh:mm:ss` — из хаба очереди звуковых запросов, там длительность строкой;
 * - ISO 8601 (`PT1M23S`) — из REST-контракта.
 *
 * Раньше понимался только ISO, и `PlayerToolbar` с `useTrackProgress`, читая
 * длительность из хаба, получали 0: полоса прогресса на `/player` не двигалась
 * никогда. Второй парсер с тем же именем в `VideoScreen/utils/parseDuration.ts`
 * оба формата понимал — починили видеоэкран, а десктоп остался.
 *
 * Мусор даёт 0, а не `NaN`: длительность попадает в делитель, и `NaN` ушёл бы в
 * отрисовку как «-1%».
 */
export const parseDurationToSeconds = (duration?: string): number => {
  if (!duration) {
    return 0;
  }

  const clockParts = duration.split(":");

  if (clockParts.length === 3 || clockParts.length === 2) {
    const numbers = clockParts.map(Number);

    if (numbers.every(value => Number.isFinite(value))) {
      const [hours, minutes, seconds] =
        numbers.length === 3 ? numbers : [0, ...numbers];

      return hours * 3600 + minutes * 60 + seconds;
    }

    return 0;
  }

  const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);

  if (!match) {
    return 0;
  }

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
