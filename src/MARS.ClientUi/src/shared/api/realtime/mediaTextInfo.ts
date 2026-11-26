/**
 * `textInfo` на проводе — это доменный `MediaTextInfo` (REST-контракт),
 * сериализованный System.Text.Json в camelCase: на проводе уже лежат имена
 * REST-контракта (`keyWordsColor`, `keyWordSybmolDelimiter`), а разделитель
 * ключевых слов — строка, а не код символа с отдельным признаком наличия.
 *
 * Proto-слоя больше нет (phase E), поэтому функция остаётся для совместимости
 * с вызовами и тестами.
 */
export const normalizeTextInfo = (value: unknown): unknown => value;

const asRecord = (value: unknown): Record<string, unknown> | null =>
  value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;

/**
 * Приводит обе формы медиа к `MediaDto`, которую читают PyroAlerts/RandomMem.
 *
 * Текущий `AlertEvent` сервера присылает `MediaInfo` напрямую
 * (`{ id, textInfo, fileInfo, ... }`), а легаси-фикстуры — `MediaDto`
 * (`{ mediaInfo, uploadStartTime }`). Раньше нормализатор молча возвращал
 * сырой `MediaInfo`, и экран падал на `message.mediaInfo.id`.
 */
export const normalizeMedia = (
  value: unknown
): Record<string, unknown> | null => {
  const wire = asRecord(value);

  if (wire === null) {
    return null;
  }

  const mediaInfo = asRecord(wire.mediaInfo);

  if (mediaInfo !== null) {
    return {
      ...wire,
      mediaInfo: {
        ...mediaInfo,
        textInfo: normalizeTextInfo(mediaInfo.textInfo),
      },
    };
  }

  if (wire.textInfo !== undefined || wire.fileInfo !== undefined) {
    return {
      mediaInfo: {
        ...wire,
        textInfo: normalizeTextInfo(wire.textInfo),
      },
      uploadStartTime: new Date().toISOString(),
    };
  }

  return wire;
};
