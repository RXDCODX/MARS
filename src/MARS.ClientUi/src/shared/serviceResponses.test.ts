import { describe, expect, it } from "vitest";

import {
  readServicesList,
  readLogsList,
  readActionResult,
} from "./serviceResponses";

/**
 * Разбор ответов `/api/ServiceManager`.
 *
 * Эндпоинты отвечают конвертом `MARS.Admin` — `{ success, message, data }`, то
 * есть второй формой, а не `{ success, result, errorMessage }` у общих
 * контроллеров. Стор звал `axios` напрямую, минуя транспорт, который разворачивает
 * обе формы, и отсюда три поломки:
 *
 * - список сервисов всегда был пустым, а ошибки не было: `res.data` — конверт,
 *   а не массив;
 * - логи клались конвертом, и `logs.filter` в просмотрщике ронял страницу;
 * - отказ управления приходил кодом 200, axios резолвился, и интерфейс считал
 *   действие выполненным.
 */
describe("разбор ответов ServiceManager", () => {
  it("список сервисов берётся из data", () => {
    const services = [{ name: "twitch-core", isEnabled: true }];

    expect(readServicesList({ success: true, data: services })).toEqual({
      ok: true,
      services,
    });
  });

  it("пустой список — это «сервисов нет», а не «ответ непонятен»", () => {
    expect(readServicesList({ success: true, data: [] })).toEqual({
      ok: true,
      services: [],
    });
  });

  it("конверт без списка не выдаётся за пустой список", () => {
    // Пустой список и отсутствие данных выглядят на экране одинаково, а значат
    // разное: первое — сервисов нет, второе — ответ не тот. Второе обязано быть
    // отказом с текстом, иначе страница молча показывает «нет сервисов».
    const read = readServicesList({ success: true });

    expect(read.ok).toBe(false);
  });

  it("отказ списка отдаёт текст сервиса", () => {
    const read = readServicesList({ success: false, message: "нет доступа" });

    expect(read.ok).toBe(false);
    expect(read.ok === false && read.message).toBe("нет доступа");
  });

  it("логи берутся из data, а не кладутся конвертом", () => {
    // Именно из-за этого `logs.filter` падал: в `logs` лежал объект.
    const logs = [
      { timestamp: "2026-10-04T00:00:00Z", level: "Info", message: "старт" },
    ];

    expect(readLogsList({ success: true, data: logs })).toEqual({
      ok: true,
      logs,
    });
  });

  it("мусор вместо логов не роняет просмотрщик", () => {
    const read = readLogsList({ success: true, data: null });

    expect(read.ok).toBe(false);
    expect(readLogsList("привет").ok).toBe(false);
    expect(readLogsList(null).ok).toBe(false);
  });

  it("отказ управления не выглядит успехом", () => {
    // HTTP-код здесь 200 даже при отказе, поэтому решать только по телу.
    const read = readActionResult({
      success: false,
      message: "Не удалось запустить service",
    });

    expect(read.ok).toBe(false);
    expect(read.ok === false && read.message).toBe(
      "Не удалось запустить service"
    );
  });

  it("успешное действие подтверждается, а не угадывается", () => {
    expect(readActionResult({ success: true, message: "started" }).ok).toBe(
      true
    );
    expect(readActionResult({ success: true }).ok).toBe(true);
  });

  it("тело без конверта не считается успехом", () => {
    expect(readActionResult("started").ok).toBe(false);
    expect(readActionResult(undefined).ok).toBe(false);
  });
});
