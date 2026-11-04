/**
 * Приведение `textInfo` медиа-алерта к форме, которой живут экраны.
 *
 * На проводе едет не доменный `MediaTextInfo`, а proto-сообщение
 * `MediaTextPayload`: его имена другие, а разделитель ключевых слов приходит
 * кодом символа вместе с отдельным признаком наличия. Экраны же читают имена
 * REST-контракта и ждут символ.
 *
 * Из-за этого настроенный цвет ключевых слов всегда был `undefined` —
 * `KeyWordedText` подставлял случайный цвет при каждом рендере, — а
 * разделитель всегда равнялся `#`, и алерты, настроенные на другой символ,
 * теряли подсветку целиком.
 *
 * Приведение повторяет `MediaGrpcMapper.ToDto` на сервере: там ровно эта
 * обратная пара полей и то же правило про признак наличия. Место выбрано
 * одно на всех, потому что форму разбора обязаны знать четыре примитива
 * `PyroAlerts`, а не каждый из них.
 */
export interface NormalizedTextInfo {
  /** Цвет ключевых слов: proto-поле `keywords_color`. */
  keyWordsColor?: string;
  /** Символ-разделитель: `keyword_symbol_delimiter` плюс признак наличия. */
  keyWordSybmolDelimiter?: string;
  [key: string]: unknown;
}

const asRecord = (value: unknown): Record<string, unknown> | null =>
  value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;

/**
 * `textInfo` в именах, которые читают экраны.
 *
 * Отсутствие `textInfo` возвращается как есть: медиа без текста — обычное дело,
 * и вызывающий сам решит, что рисовать.
 */
export const normalizeTextInfo = (value: unknown): unknown => {
  const wire = asRecord(value);

  if (wire === null) {
    return value;
  }

  // Разделитель приходит кодом символа, и code 0 вместе с признаком наличия
  // означает «разделитель не задан». Признак обязателен: без него `undefined`
  // и «нет разделителя» не различить, а в proto их оба даёт `\0`.
  const delimiterCode = wire.keywordSymbolDelimiter;
  const hasDelimiter = wire.hasKeywordSymbolDelimiter === true;

  const delimiter =
    hasDelimiter && typeof delimiterCode === "number" && delimiterCode !== 0
      ? String.fromCharCode(delimiterCode)
      : undefined;

  const color =
    typeof wire.keywordsColor === "string" ? wire.keywordsColor : undefined;

  // Остальные поля (`text`, `textColor`, `triggerWord`) на проводе и в
  // контракте называются одинаково, поэтому переносятся как есть.
  const rest: Record<string, unknown> = { ...wire };

  delete rest.keywordsColor;
  delete rest.keywordSymbolDelimiter;
  delete rest.hasKeywordSymbolDelimiter;
  delete rest.keyWordsColor;
  delete rest.keyWordSybmolDelimiter;

  const result: NormalizedTextInfo = { ...rest };

  // Поля без значения не добавляются: экран различает «не задано» по
  // отсутствию ключа и подставляет свой дефолт, а `null` сломал бы проверку.
  if (color !== undefined) {
    result.keyWordsColor = color;
  }

  if (delimiter !== undefined) {
    result.keyWordSybmolDelimiter = delimiter;
  }

  return result;
};

/**
 * Медиа-объект с приведённым `textInfo`.
 *
 * Ветка `AlertEvent` — это `{ media: MediaPayload }`, то есть объект с одним
 * полем `media`, а не сам `MediaPayload`. Разворачивание и приведение идут в
 * одном месте, чтобы экраны не помнили, что на проводе два слоя.
 */
export const normalizeMedia = (
  value: unknown
): Record<string, unknown> | null => {
  const wire = asRecord(value);

  if (wire === null) {
    return null;
  }

  if (wire.textInfo === undefined) {
    return wire;
  }

  return { ...wire, textInfo: normalizeTextInfo(wire.textInfo) };
};
