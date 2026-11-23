# Manual types (`src/shared/api/manual`)

Ручные типы, которых нет в OpenAPI-документах сервисов.

## Зачем

Часть полезных нагрузок хабов объявлена в proto полями `bytes` (`messageJson`, `fumoJson`, `twitchUserJson` и подобными). В OpenAPI они выглядят как массивы байт, а на проводе приходит JSON внутри этих байт. Такой тип сгенерировать нельзя, поэтому он описан здесь руками.

## Файлы

- `index.ts` — ручные интерфейсы и enum'ы: `MediaInfo`, `MediaDto`, `ChatMessage`, `Frog`, `Fumo`, `Husband`, `MikuModule`, `Waifu`, `GaoAlertDto`, `MikuMondayDto`, `PrizeType`, `User`, и др.

## Правила

- Генератор (`build-api.js`) не трогает этот каталог — файлы не удаляются и не перезаписываются.
- Типы должны совпадать с тем, что реально кладёт на провод сервер. См. тесты `tests/MARS.Alerts.Tests/Hubs/OverlayPayloadWireFormatTests.cs`.
- Импорты типов из `../types/data-contracts` (например, `ApiMediaInfo`, `TwitchUser`) допустимы.

## Использование

```ts
import type { ChatMessage, MediaDto } from "@/shared/api/manual";
// или через баррель:
import type { ChatMessage } from "@/shared/api";
```