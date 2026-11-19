import { describe, expect, it } from "vitest";

import { readHighliteText } from "./highliteMessage";

/**
 * `message_json` в ветке `HighliteEvent`.
 *
 * Сервер кладёт туда строку — `notifier.Highlite(messageText, color, image)`
 * передаёт текст подсвеченного сообщения. Экран делал `decoded as ChatMessage` и
 * читал `.displayName`, `.message`, `.id`, то есть на строке всё было
 * `undefined`: пустая плашка и `id={undefined}`.
 */
describe("разбор подсвеченного сообщения", () => {
  it("читает текст из строки — фактической формы на проводе", () => {
    expect(readHighliteText("привет, чат")).toBe("привет, чат");
  });

  it("читает текст из объекта, если сервер начнёт класть сообщение", () => {
    // Форма объекта принимается, чтобы поломка не выглядела как пустая плашка:
    // сервер однажды может поменять форму, и экран покажет текст, а не ничего.
    expect(readHighliteText({ message: "привет" })).toBe("привет");
  });

  it("пустая строка отбрасывается", () => {
    // Плашка без текста — то же залипшее сообщение, от которого экран страдал:
    // удаление идёт по событию загрузки лица, а не по содержимому.
    expect(readHighliteText("")).toBeNull();
    expect(readHighliteText({ message: "" })).toBeNull();
  });

  it("мусор отбрасывается", () => {
    expect(readHighliteText(null)).toBeNull();
    expect(readHighliteText(undefined)).toBeNull();
    expect(readHighliteText(42)).toBeNull();
    expect(readHighliteText(["привет"])).toBeNull();
    expect(readHighliteText({})).toBeNull();
  });

  it("объект без поля message не превращается в «[object Object]»", () => {
    expect(readHighliteText({ displayName: "ник" })).toBeNull();
  });
});
