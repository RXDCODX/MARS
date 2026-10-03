import { describe, expect, it } from "vitest";

import { unwrapOperationResult } from "./http-client";

/**
 * Разбор конверта ответа сервиса.
 *
 * Сервисы отвечают `{ success, result, errorMessage }`, а клиент написан под
 * `{ data }`. Разница закрывается один раз, в транспортном слое: 52 места
 * вызова читают `result.data.data`, и знание о форме ответа не должно
 * размазываться по компонентам.
 *
 * Функция чистая, поэтому проверяется напрямую: поднимать ради неё axios или
 * мок сетевого слоя значило бы проверять мок.
 */
describe("разбор конверта ответа", () => {
  it("полезная нагрузка доступна и как result, и как data", () => {
    const parsed = unwrapOperationResult({
      success: true,
      result: [{ commandName: "help" }],
    }) as { result: unknown; data: unknown };

    expect(parsed.data).toEqual([{ commandName: "help" }]);
    expect(parsed.result).toEqual([{ commandName: "help" }]);
  });

  it("неуспешный конверт превращается в исключение с текстом сервиса", () => {
    // Исключение обязано нести текст сервиса: иначе на экране будет
    // «undefined is not iterable» вместо причины, и искать пришлось бы в консоли
    // браузера.
    expect(() =>
      unwrapOperationResult({
        success: false,
        result: null,
        errorMessage: "Команда не найдена.",
      })
    ).toThrow("Команда не найдена.");
  });

  it("неуспешный конверт без текста даёт внятное сообщение", () => {
    expect(() => unwrapOperationResult({ success: false })).toThrow(/ошибк/i);
  });

  it("тело без конверта проходит без изменений", () => {
    const body = [{ id: 1 }];

    expect(unwrapOperationResult(body)).toBe(body);
  });

  it("объект с не-булевым success конвертом не считается", () => {
    // Поле `success` может встретиться в данных: нельзя разворачивать такой
    // объект и ронять вызов, который отработал бы нормально.
    const body = { success: "yes", data: [1] };

    expect(unwrapOperationResult(body)).toBe(body);
  });

  it("null и числа проходят без изменений", () => {
    expect(unwrapOperationResult(null)).toBeNull();
    expect(unwrapOperationResult(42)).toBe(42);
  });
});
