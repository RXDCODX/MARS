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

  /**
   * Вторая форма конверта — `MARS.Admin` и `MARS.Discord`.
   *
   * Форма `{ success, message, data }` вместо `{ success, result, errorMessage }`.
   * Разбор знал только первую, и на второй `data` становился `undefined`, а
   * текст ошибки терялся: страницы показывали пустое без сообщения — «списка
   * нет», «переменных нет», титры «—».
   */
  describe("конверт MARS.Admin и MARS.Discord", () => {
    it("кладёт data на место result", () => {
      const variables = [{ key: "TZ", value: "UTC" }];

      expect(
        unwrapOperationResult({ success: true, data: variables })
      ).toHaveProperty("result", variables);
    });

    it("не затирает data, если отданы оба поля", () => {
      // Показывать надо то, что реально пришло, а не угадывать по наличию
      // ключей: `result` у такой формы отсутствует, и подстановка `undefined`
      // молча убивала бы полезную нагрузку.
      const body = { success: true, data: [1], result: [2] };

      expect(unwrapOperationResult(body)).toHaveProperty("data", [1]);
    });

    it("текст ошибки берётся из message, а не из errorMessage", () => {
      expect(() =>
        unwrapOperationResult({ success: false, message: "нет доступа" })
      ).toThrow("нет доступа");
    });
  });
});
