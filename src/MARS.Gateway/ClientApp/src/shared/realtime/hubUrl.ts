/**
 * Адрес хаба из базиса приложения.
 *
 * `VITE_BASE_PATH` — относительный корень, и это правильно для сборки: он
 * попадает в бандл и должен работать на любом origin за Gateway.
 * `withUrl` из @microsoft/signalr относительный адрес не принимает — требуется
 * абсолютный. В браузере он разрешается сам по `document.baseURI`, а в jsdom
 * этого не происходит, и падает не тест, а импорт модуля: `Cannot resolve
 * '/hubs/scoreboard'` при импорте стора, который строит соединение на верхнем
 * уровне.
 *
 * Поэтому адрес собирается здесь: корень приклеивается к origin текущего
 * документа. В браузере origin есть всегда, в vitest — тоже, а вот при
 * отсутствии адреса возвращается строка: падать должен вызов, а не импорт.
 */
export function resolveHubUrl(hubPath: string): string {
  const base = import.meta.env.VITE_BASE_PATH ?? "/";
  const path = `${base}${hubPath}`;

  const origin = globalThis.location?.origin;

  if (origin === undefined || origin === "null") {
    return path;
  }

  return `${origin}${path}`;
}
