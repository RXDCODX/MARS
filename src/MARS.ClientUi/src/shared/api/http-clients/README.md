# HTTP clients (`src/shared/api/http-clients`)

Сгенерированные HTTP-клиенты для сервисов MARS. По одному классу на сервис/контроллер (`Commands`, `Scoreboard`, `MediaStorage` и т.д.).

## Файлы

- `http-client.ts` — общий транспорт на `axios`: класс `HttpClient`, типы запросов, разбор тела ответа.
- `*.ts` (кроме `http-client.ts`) — сгенерированные клиенты, импортирующие типы из `../types/data-contracts`.

## Особенности транспорта

- Все клиенты используют общий базовый URL из `getApiBaseUrl()` (`../api-config`).
- `unwrapOperationResult` приводит ответы сервисов к форме `{ data, result }`: сервисы отвечают `{ success, result, errorMessage }` или `{ success, message, data }` (`MARS.Admin`, `MARS.Discord`).
- При `success === false` выбрасывается `Error` с текстом сервиса.
- `parseResponseBody` отсекает HTML-заглушку SPA (ответ nginx с `index.html`) и не JSON-ответы.
- Функции чистые и покрыты тестами в `../tests`.

## Использование

```ts
import { Commands } from "@/shared/api/http-clients/Commands";
// или через баррель:
import { Commands } from "@/shared/api";

const client = new Commands();
const response = await client.getAllCommands();
```

## Генерация

Файлы клиентов генерируются командой `yarn build-api` из OpenAPI-документов. Ручные правки в `*.ts` (кроме `http-client.ts`) будут перезаписаны. Транспорт `http-client.ts` поддерживается руками.