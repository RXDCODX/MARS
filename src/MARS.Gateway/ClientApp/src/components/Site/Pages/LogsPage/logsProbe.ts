/**
 * Ответ ли сервиса на запрос логов — или это индексная страница SPA.
 *
 * Эндпоинтов `/api/Logs/*` в репозитории нет: контроллера `Logs` не существует,
 * а маршрут YARP `admin-logs` выпилен вместе с Seq. Но catch-all клиента
 * (`spa`, `Order: 1000`) отвечает на любой путь, а `try_files` в nginx отдаёт
 * `index.html` — то есть приходит **200 с HTML**.
 *
 * Проверка `response.ok` на этом запросе всегда истинна, и кнопка «Создать
 * тестовые логи» рапортовала об успехе, хотя запрос не дошёл ни до одного
 * сервиса и ничего не создал. Отличить HTML от ответа сервиса можно только по
 * заголовку `Content-Type` и по первому символу тела.
 */

/** Что пришло в ответ на запрос к несуществующему эндпоинту. */
export type LogsProbe = { kind: "service" } | { kind: "spa"; message: string };

const HTML_HINT = "<!doctype html";

export const isServiceResponse = (
  contentType: string,
  body: string
): boolean => {
  if (!contentType.toLowerCase().includes("json")) {
    return false;
  }

  // `Content-Type: application/json` с телом `index.html` — тоже не ответ
  // сервиса: тело проверяется, потому что заголовок задаёт прокси.
  return !body.trimStart().toLowerCase().startsWith(HTML_HINT);
};

/**
 * Что показать пользователю по ответу на запрос логов.
 *
 * Отдельный тип ответа «эндпоинта нет» вводится намеренно: сообщение должно
 * называть причину, а не «ошибка», иначе следующий запрос к тому же адресу будет
 * выглядеть как загадочный сбой.
 */
export const describeLogsProbe = (
  contentType: string,
  body: string
): LogsProbe => {
  if (!isServiceResponse(contentType, body)) {
    return {
      kind: "spa",
      message:
        "Эндпоинт логов недоступен: сервис журналов в проекте нет, а запрос попал в раздачу клиента. Смотреть логи нужно в Grafana (Loki).",
    };
  }

  // Конвертный отказ не должен выглядеть как успех. Сейчас достижимости нет —
  // эндпоинта не существует, — но проверка стоит ровно там, где её пропустили бы,
  // если бы сервис завёл.
  if (/"success"\s*:\s*false/.test(body)) {
    return {
      kind: "spa",
      message: "Сервис журналов отклонил запрос.",
    };
  }

  return { kind: "service" };
};
