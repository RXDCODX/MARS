import { describe, expect, it } from "vitest";

import { readCreateResult } from "./mediaInfoPageHelpers";

/**
 * Разбор ответа записи медиа.
 *
 * Сервер отвечает конвертом `{ success, result, errorMessage }`. Раньше страницы
 * создания и правки звали `fetch` напрямую и читали `result.data` и
 * `result.message` — полей с такими именами на проводе нет. Запись при этом
 * проходила, а пользователю показывалось «Не удалось создать медиа»: ошибка
 * ложная, данные сохранены.
 */
describe("разбор ответа записи медиа", () => {
  it("берёт созданный объект из result", () => {
    const created = { id: "media-1", albumName: "альбом" };

    expect(readCreateResult({ success: true, result: created })).toEqual({
      ok: true,
      id: created.id,
    });
  });

  it("отказ сервира даёт его собственный текст", () => {
    const read = readCreateResult({
      success: false,
      errorMessage: "Файл уже существует",
    });

    expect(read.ok).toBe(false);
    expect(read.ok === false && read.message).toBe("Файл уже существует");
  });

  it("успех без объекта — это не успех", () => {
    // `success: true` без `result` означает, что сохранять было нечего.
    // Считать это успехом значит увести пользователя на страницу правки
    // несуществующей записи.
    const read = readCreateResult({ success: true });

    expect(read.ok).toBe(false);
  });

  it("мусор вместо конверта не выдаётся за успех", () => {
    expect(readCreateResult("привет").ok).toBe(false);
    expect(readCreateResult(null).ok).toBe(false);
    expect(readCreateResult({}).ok).toBe(false);
  });

  it("в успехе id обязан быть строкой", () => {
    // Идентификатор подставляется в адрес и в запрос PUT. Число прошло бы в
    // `navigate`, а сервер вернул бы 400 уже после показа «успеха».
    expect(readCreateResult({ success: true, result: { id: 7 } })).toEqual({
      ok: false,
      message: "Сервер вернул запись без идентификатора",
    });
  });
});
