import { describe, expect, it } from "vitest";

import { normalizeMedia, normalizeTextInfo } from "./mediaTextInfo";

/**
 * `textInfo` на проводе.
 *
 * Форма снята из `MediaGrpcMapper.ToProto`, а не из `data-contracts.ts`: сервер
 * кладёт в proto-сообщение `MediaTextPayload`, где имена другие, а разделитель
 * ключевых слов — код символа с отдельным признаком наличия. Экраны же читали
 * имена REST-контракта и ждали символ, поэтому цвет всегда был `undefined`, а
 * разделитель всегда `#`.
 */
const wireTextInfo = {
  text: "привет {user.name}",
  textColor: "#FFFFFF",
  triggerWord: "привет",
  keywordsColor: "#00FF00",
  keywordSymbolDelimiter: 35,
  hasKeywordSymbolDelimiter: true,
};

describe("приведение textInfo медиа-алерта", () => {
  it("читает цвет из keywordsColor, а не из keyWordsColor", () => {
    // Раньше здесь был `undefined`, и KeyWordedText подставлял случайный цвет
    // при каждом рендере.
    expect(normalizeTextInfo(wireTextInfo)).toHaveProperty(
      "keyWordsColor",
      "#00FF00"
    );
  });

  it("превращает код символа в символ", () => {
    expect(normalizeTextInfo(wireTextInfo)).toHaveProperty(
      "keyWordSybmolDelimiter",
      "#"
    );
  });

  it("учитывает нестандартный разделитель, а не всегда «#»", () => {
    const star = { ...wireTextInfo, keywordSymbolDelimiter: "*".charCodeAt(0) };

    expect(normalizeTextInfo(star)).toHaveProperty(
      "keyWordSybmolDelimiter",
      "*"
    );
  });

  it("без признака наличия разделителя не придумывает его", () => {
    // В proto разделителя может не быть, и тогда приходит код 0. Признак
    // обязателен: без него «не задан» и «задан пробел» неразличимы.
    const absent = {
      ...wireTextInfo,
      keywordSymbolDelimiter: 0,
      hasKeywordSymbolDelimiter: false,
    };

    expect(normalizeTextInfo(absent)).not.toHaveProperty(
      "keyWordSybmolDelimiter"
    );
  });

  it("переносит поля, у которых имя совпадает на обоих слоях", () => {
    const result = normalizeTextInfo(wireTextInfo) as Record<string, unknown>;

    expect(result.text).toBe("привет {user.name}");
    expect(result.textColor).toBe("#FFFFFF");
    expect(result.triggerWord).toBe("привет");
  });

  it("не оставляет proto-имена в результате", () => {
    const result = normalizeTextInfo(wireTextInfo) as Record<string, unknown>;

    expect(result).not.toHaveProperty("keywordsColor");
    expect(result).not.toHaveProperty("keywordSymbolDelimiter");
    expect(result).not.toHaveProperty("hasKeywordSymbolDelimiter");
  });

  it("медиа без textInfo остаётся как есть", () => {
    const media = { mediaInfo: { id: "1" } };

    expect(normalizeMedia(media)).toEqual(media);
  });

  it("приводит textInfo внутри медиа", () => {
    const media = normalizeMedia({
      mediaInfo: { id: "1" },
      textInfo: wireTextInfo,
    });

    expect(media).toHaveProperty("textInfo.keyWordsColor", "#00FF00");
    expect(media).toHaveProperty("textInfo.keyWordSybmolDelimiter", "#");
  });

  it("не падает на мусоре вместо textInfo", () => {
    expect(normalizeTextInfo(null)).toBeNull();
    expect(normalizeTextInfo("привет")).toBe("привет");
    expect(normalizeMedia(null)).toBeNull();
    expect(normalizeMedia("медиа")).toBeNull();
  });
});
