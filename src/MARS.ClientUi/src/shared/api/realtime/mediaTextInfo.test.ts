import { describe, expect, it } from "vitest";

import { normalizeMedia, normalizeTextInfo } from "./mediaTextInfo";

/**
 * `textInfo` на проводе — это доменный `MediaTextInfo`, сериализованный в
 * camelCase: на проводе уже лежат имена REST-контракта
 * (`keyWordsColor`, `keyWordSybmolDelimiter`), и разделитель — строка.
 */
const wireTextInfo = {
  text: "привет {user.name}",
  textColor: "#FFFFFF",
  triggerWord: "привет",
  keyWordsColor: "#00FF00",
  keyWordSybmolDelimiter: "#",
};

describe("приведение textInfo медиа-алерта", () => {
  it("сохраняет цвет ключевых слов как есть", () => {
    expect(normalizeTextInfo(wireTextInfo)).toHaveProperty(
      "keyWordsColor",
      "#00FF00"
    );
  });

  it("сохраняет разделитель как есть", () => {
    expect(normalizeTextInfo(wireTextInfo)).toHaveProperty(
      "keyWordSybmolDelimiter",
      "#"
    );
  });

  it("переносит поля, у которых имя совпадает на обоих слоях", () => {
    const result = normalizeTextInfo(wireTextInfo) as Record<string, unknown>;

    expect(result.text).toBe("привет {user.name}");
    expect(result.textColor).toBe("#FFFFFF");
    expect(result.triggerWord).toBe("привет");
  });

  it("медиа без textInfo остаётся как есть", () => {
    const media = { mediaInfo: { id: "1" } };

    expect(normalizeMedia(media)).toEqual(media);
  });

  it("приводит textInfo внутри mediaInfo", () => {
    const media = normalizeMedia({
      mediaInfo: { id: "1", textInfo: wireTextInfo },
      uploadStartTime: "2026-10-03T00:00:00Z",
    });

    expect(media).toHaveProperty(
      "mediaInfo.textInfo.keyWordsColor",
      "#00FF00"
    );
    expect(media).toHaveProperty(
      "mediaInfo.textInfo.keyWordSybmolDelimiter",
      "#"
    );
  });

  it("оборачивает сырой MediaInfo в MediaDto", () => {
    const media = normalizeMedia({
      id: "11111111-2222-3333-4444-555555555555",
      textInfo: wireTextInfo,
      fileInfo: { type: "Image", filePath: "/Alerts/test.png" },
    });

    expect(media).toHaveProperty(
      "mediaInfo.id",
      "11111111-2222-3333-4444-555555555555"
    );
    expect(media).toHaveProperty("mediaInfo.textInfo.text", "привет {user.name}");
    expect(media).toHaveProperty("mediaInfo.fileInfo.filePath", "/Alerts/test.png");
    expect(media).toHaveProperty("uploadStartTime");
  });

  it("не падает на мусоре вместо textInfo", () => {
    expect(normalizeTextInfo(null)).toBeNull();
    expect(normalizeTextInfo("привет")).toBe("привет");
    expect(normalizeMedia(null)).toBeNull();
    expect(normalizeMedia("медиа")).toBeNull();
  });
});