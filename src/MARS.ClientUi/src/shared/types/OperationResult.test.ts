import { describe, expect, it } from "vitest";

import { createErrorResult, messageOf } from "./OperationResult";

/**
 * Текст ошибки из исключения.
 *
 * Транспорт превращает `HTTP 200 + success: false` в `throw new Error(текст
 * сервиса)`, и это единственный живой путь отказа для конвертных ответов.
 * `catch` без параметра текст выбрасывал, и вместо причины пользователь видел
 * «Ошибка сети» — хотя сервис отвечает внятно.
 */
describe("текст ошибки из исключения", () => {
  it("берёт сообщение из Error", () => {
    expect(messageOf(new Error("Вайфу уже существует"), "Ошибка сети")).toBe(
      "Вайфу уже существует"
    );
  });

  it("пустое сообщение не показывается пользователю", () => {
    // `new Error("")` — обычная форма упавшего HTTP-клиента без тела ответа.
    // Показывать пустую строку значит показать ничего: заглушка честнее.
    expect(messageOf(new Error(""), "Ошибка сети")).toBe("Ошибка сети");
  });

  it("берёт сообщение из объекта с полем message", () => {
    // Форма нужна не ради axios: `AxiosError extends Error`, и первая ветка
    // `messageOf` срабатывает для него сама. Объект с одним `message` приходит
    // из мест, где исключение подменяется данными, — и раньше такой попал бы в
    // запасной текст, потеряв причину.
    expect(messageOf({ message: "Таймаут" }, "Ошибка сети")).toBe("Таймаут");
  });

  it("axios-ошибка разбирается первой веткой", () => {
    // `AxiosError extends Error`, поэтому достаточно `instanceof`, а вторая
    // ветка для него недостижима. Проверяется явно, чтобы при смене axios на
    // другую библиотеку это не осталось незамеченным.
    class AxiosLikeError extends Error {}

    expect(
      messageOf(new AxiosLikeError("timeout of 5000ms"), "Ошибка сети")
    ).toBe("timeout of 5000ms");
  });

  it("на не-ошибку отвечает запасным текстом", () => {
    expect(messageOf("строка", "Ошибка сети")).toBe("Ошибка сети");
    expect(messageOf(null, "Ошибка сети")).toBe("Ошибка сети");
    expect(messageOf(undefined, "Ошибка сети")).toBe("Ошибка сети");
    expect(messageOf({ code: 500 }, "Ошибка сети")).toBe("Ошибка сети");
    expect(messageOf({ message: "" }, "Ошибка сети")).toBe("Ошибка сети");
    expect(messageOf({ message: 42 }, "Ошибка сети")).toBe("Ошибка сети");
  });

  it("подставляется прямо в результат", () => {
    const result = createErrorResult(
      messageOf(new Error("Отказано"), "Ошибка сети")
    );

    expect(result).toEqual({
      success: false,
      message: "Отказано",
      data: undefined,
    });
  });
});
