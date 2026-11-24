/**
 * `textInfo` на проводе — это доменный `MediaTextInfo` (REST-контракт),
 * сериализованный System.Text.Json в camelCase: на проводе уже лежат имена
 * REST-контракта (`keyWordsColor`, `keyWordSybmolDelimiter`), а разделитель
 * ключевых слов — строка, а не код символа с отдельным признаком наличия.
 *
 * Никакого proto-слоя больше нет (phase E: «Media*Payload family removed,
 * MediaInfo on wire»), поэтому приведение сводится к сохранению полей как есть.
 * Функция остаётся для совместимости с вызовами и тестами.
 */
export const normalizeTextInfo = (value: unknown): unknown => value;

const asRecord = (value: unknown): Record<string, unknown> | null =>
  value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;

/**
 * Медиа-объект с приведённым `textInfo`.
 *
 * На проводе `AlertEvent.media` — это `MediaDto` (`{ mediaInfo, uploadStartTime }`),
 * а `textInfo` живёт внутри `mediaInfo`. Приведение идёт только до вложенного
 * `mediaInfo`, чтобы экраны читали поля REST-контракта напрямую.
 */
export const normalizeMedia = (
  value: unknown
): Record<string, unknown> | null => {
  const wire = asRecord(value);

  if (wire === null) {
    return null;
  }

  const mediaInfo = asRecord(wire.mediaInfo);

  if (mediaInfo === null) {
    return wire;
  }

  return {
    ...wire,
    mediaInfo: {
      ...mediaInfo,
      textInfo: normalizeTextInfo(mediaInfo.textInfo),
    },
  };
};
