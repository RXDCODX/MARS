/**
 * Разбор ответа `/api/TelegramClipboardCopy/{requestId}`.
 *
 * Контроллер отвечает общим `MARS.Shared.Models.OperationResult<string[]>`, то
 * есть `{ success, result, errorMessage }`. Поле `data` появляется только в
 * транспорте HTTP-клиента, а страница звала `fetch` напрямую и читала
 * `operation.data` — то есть всегда получала `undefined`.
 *
 * Итог был не «ошибка при разборе», а постоянное «Не удалось получить файлы»
 * при живом сервисе, который отдавал ссылки: страница не показывала ни одного
 * файла и не показывала почему, потому что `message` на проводе тоже нет.
 */

/** Ответ со ссылками: либо список, либо причина отказа. */
export type ClipboardUrlsRead =
  | { ok: true; urls: string[] }
  | { ok: false; message: string };

export const readClipboardUrls = (body: unknown): ClipboardUrlsRead => {
  const fallback = "Не удалось получить файлы";

  if (typeof body !== "object" || body === null) {
    return { ok: false, message: fallback };
  }

  const envelope = body as {
    success?: boolean;
    result?: unknown;
    data?: unknown;
    errorMessage?: string | null;
    message?: string | null;
  };

  if (envelope.success !== true) {
    return {
      ok: false,
      message: envelope.errorMessage ?? envelope.message ?? fallback,
    };
  }

  const urls = envelope.result ?? envelope.data;

  if (!Array.isArray(urls)) {
    return { ok: false, message: fallback };
  }

  // Ссылки идут в `src` превью, и не-строка там становится битым URL. Фильтр
  // обязателен именно потому, что отдаёт их сервис, а не клиент.
  return {
    ok: true,
    urls: urls.filter(
      (url): url is string => typeof url === "string" && url.length > 0
    ),
  };
};
