# Types (`src/shared/api/types`)

Сгенерированные из OpenAPI data-contracts и enum'ы MARS.

## Файлы

- `data-contracts.ts` — интерфейсы DTO (`AutoMessageDto`, `TwitchUser`, `ScoreboardDto`, `MediaStorageEntryDto`, …) и enum'ы (`MediaType`, `ServiceStatus`, `Platform`, …). Файл большой (генерируется) и помечен `@ts-nocheck`.

## Особенности

- Содержит как REST-контракты, так и SignalR-специфичные типы (секция «SignalR-специфичные типы»: `AlertEvent`, `MediaPayload`, `ChatMessage`-payloads и т.п.).
- `OperationResult<TData>` — обобщённый результат операции; транспорт кладёт полезную нагрузку и в `data`, и в `result`.
- Генерируется командой `yarn build-api`. Не редактируйте вручную.

## Использование

```ts
import type { TwitchUser, MediaType } from "@/shared/api/types/data-contracts";
// или через баррель:
import type { TwitchUser } from "@/shared/api";
```