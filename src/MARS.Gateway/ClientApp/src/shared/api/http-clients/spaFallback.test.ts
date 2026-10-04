import { describe, expect, it } from "vitest";

import { parseResponseBody } from "./http-client";

/**
 * HTML-заглушка вместо ответа API.
 *
 * Эндпоинтов `/api/BooruAutoPost/*` и `/api/Logs/*` в проекте нет, а catch-all
 * клиента отвечает на любой путь `index.html` с кодом **200**. Тело приходило
 * строкой, и вызов выглядел успешным: списки были пустыми, а тост показывал
 * исходный код страницы как «успешно».
 *
 * Отсекается в транспорте, а не в каждой странице: иначе каждая новая страница
 * обманывается отдельно.
 */
const SPA_HTML =
  '<!doctype html>\n<html lang="ru">\n<head><title>MARS</title></head><body><div id="root"></div></body>\n</html>';

describe("разбор тела ответа", () => {
  it("HTML-заглушка не проходит как данные", () => {
    // Раньше возвращалась строка, `response.ok` истинна, и вызов разбирался как
    // успешный: `Array.isArray` давал false, тост показывал исходник страницы.
    expect(() => parseResponseBody(SPA_HTML)).toThrow(/раздачу клиента/);
  });

  it("HTML без doctype тоже отсекается", () => {
    expect(() => parseResponseBody("<html><body>ok</body></html>")).toThrow();
  });

  it("название причины в отказе есть", () => {
    // Без текста пользователь видел бы «Ошибка запроса» и не понял бы, что
    // эндпоинта просто нет.
    expect(() => parseResponseBody(SPA_HTML)).toThrow(/эндпоинт отсутствует/);
  });

  it("обычный JSON разбирается как раньше", () => {
    expect(parseResponseBody('{"success":true,"result":[1,2]}')).toHaveProperty(
      "result",
      [1, 2]
    );
  });

  it("не-JSON строка без HTML остаётся строкой", () => {
    // Текстовые ответы и фрагменты разметки — легитимный случай: отсекать всё
    // не-JSON значило бы сломать их.
    expect(parseResponseBody("просто текст")).toBe("просто текст");
    expect(parseResponseBody("<p>фрагмент</p>")).toBe("<p>фрагмент</p>");
  });

  it("не-строки проходят как есть", () => {
    const blob = { size: 10 };

    expect(parseResponseBody(blob)).toBe(blob);
    expect(parseResponseBody(null)).toBeNull();
    expect(parseResponseBody([1])).toEqual([1]);
  });
});
