# API (`src/shared/api`)

Слой доступа frontend к backend MARS: HTTP-клиенты сервисов, подключения к SignalR-хабам, общие типы и их ручные дополнения.

## Структура

- `api-config.ts` — получение базового URL API (`getApiBaseUrl`, `createApiUrl`, `defaultApiConfig`) из `VITE_API_BASE_URL` / `VITE_BASE_PATH`.
- `index.ts` — главный баррель-файл: реэкспортирует конфигурацию, типы, ручные типы, HTTP-клиенты и SignalR-клиенты.
- `types/` — сгенерированные из OpenAPI data-contracts и enum'ы.
- `manual/` — ручные типы, которых нет в OpenAPI (payload'ы хабов, приходящие полями `bytes`).
- `http-clients/` — сгенерированные HTTP-клиенты по одному на сервис/контроллер плюс общий транспорт `http-client.ts`.
- `realtime/` — ручной слой работы с SignalR: адаптеры, жизненный цикл соединений, реестры, хуки, разбор payload'ов.
- `signalr-clients/` — React-обёртки (`react-signalr`) для каждого хаба.
- `tests/` — тесты транспортного слоя.

## Использование

```ts
import { Commands, getApiBaseUrl } from "@/shared/api";
import type { BaseCommand } from "@/shared/api";
```

Для конкретного клиента:

```ts
import { Commands } from "@/shared/api/http-clients/Commands";
```

## Генерация и ручной код

- `types/` и `http-clients/` (кроме `http-client.ts`) генерируются из OpenAPI-документов командой `yarn build-api`. Не редактируйте их вручную — изменения будут перезаписаны.
- `manual/`, `realtime/`, `api-config.ts`, `http-client.ts` и `tests/` поддерживаются руками.
- Перед правкой сгенерированных файлов меняйте источник (OpenAPI/контроллеры) и перегенерируйте.