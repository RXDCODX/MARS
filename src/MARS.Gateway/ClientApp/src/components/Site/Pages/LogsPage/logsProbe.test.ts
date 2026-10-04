import { describe, expect, it } from "vitest";

import { describeLogsProbe, isServiceResponse } from "./logsProbe";

/**
 * Эндпоинтов `/api/Logs/*` в репозитории нет: контроллера `Logs` не
 * существует, а маршрут YARP `admin-logs` выпилен вместе с Seq.
 *
 * Но catch-all клиента отвечает 200 с `index.html`, поэтому `response.ok` всегда
 * истинна и кнопка рапортовала об успехе, ничего не создав.
 */
const SPA_HEADERS = { contentType: "text/html; charset=utf-8" };
const SPA_BODY = "<!doctype html><html><head></head></html>";

describe("ответ на запрос логов", () => {
  it("HTML от catch-all не считается ответом сервиса", () => {
    expect(isServiceResponse(SPA_HEADERS.contentType, SPA_BODY)).toBe(false);
  });

  it("HTML с заголовком json всё равно не ответ сервиса", () => {
    // Заголовок задаёт прокси, а тело — то, что реально пришло. Проверять надо
    // оба: иначе достаточно одного заголовка, чтобы обмануть проверку.
    expect(isServiceResponse("application/json", SPA_BODY)).toBe(false);
  });

  it("JSON считается ответом сервиса", () => {
    expect(
      isServiceResponse("application/json", '{"success":true,"data":[]}')
    ).toBe(true);
  });

  it("называет причину, а не «ошибку»", () => {
    const probe = describeLogsProbe(SPA_HEADERS.contentType, SPA_BODY);

    expect(probe.kind).toBe("spa");
    expect(probe.kind === "spa" && probe.message).toMatch(/Grafana/);
  });

  it("на настоящий ответ сервиса не жалуется", () => {
    expect(describeLogsProbe("application/json", '{"success":true}').kind).toBe(
      "service"
    );
  });

  it("конвертный отказ не выглядит как успех", () => {
    // Сейчас недостижимо: эндпоинта нет. Но если сервис журналов заведут,
    // честный отказ должен быть виден как отказ, а проверка стоит ровно там,
    // где её пропустили бы.
    const probe = describeLogsProbe(
      "application/json",
      '{"success":false,"errorMessage":"нет доступа"}'
    );

    expect(probe.kind).toBe("spa");
  });
});
