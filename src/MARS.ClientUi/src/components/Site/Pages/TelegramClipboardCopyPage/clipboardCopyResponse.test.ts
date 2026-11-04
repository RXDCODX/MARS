import { describe, expect, it } from "vitest";

import { readClipboardUrls } from "./clipboardCopyResponse";

/**
 * Разбор ответа `/api/TelegramClipboardCopy/{requestId}`.
 *
 * Контроллер отвечает общим `MARS.Shared.Models.OperationResult<string[]>`, то
 * есть `{ success, result, errorMessage }`. Поле `data` появляется только в
 * транспорте HTTP-клиента, а страница звала `fetch` напрямую и читала
 * `operation.data` — то есть всегда получала `undefined`.
 *
 * Итог был не «ошибка при разборе», а постоянное «Не удалось получить файлы»
 * при живом сервисе, который отдавал ссылки. Текст сервиса терялся по той же
 * причине: `message` на проводе нет.
 */
describe("разбор ответа буфера обмена Telegram", () => {
  it("берёт ссылки из result", () => {
    const urls = ["/telegram-copy/req-1"];

    expect(readClipboardUrls({ success: true, result: urls })).toEqual({
      ok: true,
      urls,
    });
  });

  it("пустой список — это «файлов нет», а не «ответ не тот»", () => {
    expect(readClipboardUrls({ success: true, result: [] })).toEqual({
      ok: true,
      urls: [],
    });
  });

  it("отказ отдаёт текст сервиса", () => {
    const read = readClipboardUrls({
      success: false,
      errorMessage: "Запрос истёк",
    });

    expect(read.ok).toBe(false);
    expect(read.ok === false && read.message).toBe("Запрос истёк");
  });

  it("успех без списка не выдаётся за пустой список", () => {
    // На экране «файлов нет» и «ответ не тот» выглядят одинаково, а значат
    // разное: первое — сервис нечего отдать, второе — клиент ждёт не то.
    expect(readClipboardUrls({ success: true }).ok).toBe(false);
  });

  it("мусор вместо конверта не выдаётся за успех", () => {
    expect(readClipboardUrls("привет").ok).toBe(false);
    expect(readClipboardUrls(null).ok).toBe(false);
    expect(readClipboardUrls([]).ok).toBe(false);
  });

  it("ссылки фильтруются: не-строки в разметку попасть не должны", () => {
    const read = readClipboardUrls({
      success: true,
      result: ["/telegram-copy/a", null, 42, "/telegram-copy/b"],
    });

    expect(read.ok && read.urls).toEqual([
      "/telegram-copy/a",
      "/telegram-copy/b",
    ]);
  });
});
