# Матрица «модуль монолита × микросервис»

Производное представление от [SERVICE_MIGRATION_CHECKLIST.md](SERVICE_MIGRATION_CHECKLIST.md) —
не источник истины. Правки вносятся в чеклист, матрица перегенерируется.

Пунктов: **226**, модулей монолита: **33**, микросервисов-столбцов: **16**.

## Как читать

- Ячейка `x/n` — из `n` пунктов модуля, приписанных этому сервису, `x` помечены `[x]`.
  `x/n` с `n = 0` не рисуется; пустая ячейка — сервис модуля не касается.
- Основание атрибуции:
  - **коду** — сервис определён по реально существующим путям `src/…` в тексте пункта;
  - **по объявлению модуля** — код не упомянут (обычно пункт `[ ]`), адресат взят из
    заголовка модуля `### X. \`mod/\` → \`MARS.Y\``;
  - **нет адресата** — модуль помечен в монолите `OBSOLETE`/`UNUSED` или код заменён;
    таким пунктам сервис назначать нечего, см. раздел «Пункты без сервиса».
- Имена файлов, встречающиеся в 4+ сервисах (`Program.cs`, `RootState.cs`, `TwitchUser.cs`
  и подобные), при атрибуции **не учитываются**: они не несут сигнала о владельце.
  Такие случаи перечислены в Приложении.

## Матрица

| Модуль монолита | Пунктов | `[x]` | `[ ]` | GW | ADM | ALT | CIN | CMD | DSC | MST | OBS | SCB | SHR | SND | TEL | TTS | TWC | V36 | WGC | — |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A. Корень Services/ | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| B. 365Genius/ → MARS.Videos365 | 5 | 5 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  | 5/5 |  ·  |  ·  |
| C. Adhd/ | 3 | 3 | 0 |  ·  |  ·  | 3/3 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| D. AppStateService_OBSOLETE/ | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |
| E. AudioControllerHub/ | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  1/1 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |
| F. AutoArts_OBSOLETE/ | 1 | 1 | 0 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| G. BooruAutoPost/ | 6 | 1 | 5 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/6 |  ·  |  ·  |  ·  |  ·  |  ·  |
| H. BooruShared/ | 6 | 1 | 5 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/6 |  ·  |  ·  |  ·  |  ·  |  ·  |
| I. CinemaQueue/ → MARS.CinemaQueue | 8 | 8 | 0 |  ·  |  ·  |  ·  |  8/8 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| J. CommandExecutor/ → MARS.Commands | 8 | 7 | 1 |  ·  |  ·  |  ·  |  ·  |  7/7 |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  1/2 |  ·  |  ·  |  ·  |
| K. Configuration/ | 1 | 1 | 0 |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| L. Discord/ → MARS.Discord | 8 | 6 | 2 |  ·  |  ·  |  ·  |  ·  |  ·  |  6/8 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| M. EnvironmentVariable/ | 1 | 1 | 0 |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| N. KeyboardHook_UNUSED/ | 2 | 2 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  2/2 |
| O. Logs/ | 1 | 0 | 1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  0/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| P. Media/ → MARS.MediaStorage | 3 | 3 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  3/3 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| Q. MemoryStorageService/ | 1 | 1 | 0 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |
| R. Obs/ → MARS.OBS | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| S. PyroAlerts/ | 2 | 2 | 0 |  ·  |  ·  |  2/2 |  ·  |  ·  |  ·  |  2/2 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| T. Scoreboard/ → MARS.Scoreboard | 2 | 2 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  2/2 |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| U. ServiceManager/ → MARS.Admin | 2 | 2 | 0 |  ·  |  2/2 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| V. SevenTv/ | 1 | 1 | 0 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  1/1 |  ·  |  ·  |  ·  |
| W. Shikimori/ → MARS.WaifuGacha | 3 | 2 | 1 |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  2/3 |  ·  |
| X. SoundBarService/ → MARS.SoundRequest | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| Y. SoundRequest/ → MARS.SoundRequest | 13 | 13 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  13/13 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| Z. StreamAcrhive_UNUSED/ → MARS.Admin | 5 | 1 | 4 |  ·  |  1/5 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |
| AA. TabletopGames_OBSOLETE/ | 2 | 2 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  2/2 |
| AB. Telegram/ → MARS.Telegram | 12 | 11 | 1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  10/11 |  ·  |  ·  |  ·  |  ·  |  ·  |
| AC. Twitch/Rewards/ — инфраструктура наград | 20 | 17 | 3 |  ·  |  ·  |  2/4 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  12/15 |  ·  |  2/2 |  1/1 |
| AC.R — отдельные награды | 72 | 58 | 14 |  ·  |  ·  |  44/58 |  1/1 |  ·  |  ·  |  4/4 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  3/3 |  ·  |  8/8 |  ·  |
| AD. Twitch/ — вне Rewards/ | 24 | 19 | 5 |  ·  |  2/2 |  1/1 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  2/2 |  ·  |  ·  |  2/2 |  18/23 |  ·  |  3/3 |  ·  |
| AE. WaifuRoll/ → MARS.WaifuGacha | 8 | 8 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  8/8 |  ·  |
| AF. YouTube/ | 1 | 1 | 0 |  ·  |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |  ·  |  1/1 |  ·  |  ·  |  1/1 |  ·  |  ·  |  ·  |
| **Итого** | **226** | **184** | **42** |  ·  |  8/12 | 55/71 |  9/9 |  7/7 |  7/9 |  10/10 |  2/2 |  2/2 |  10/11 |  16/16 |  13/24 |  3/3 |  38/47 | 5/5 |  23/24 |  6/6 |

Столбец «Итого» — сумма по столбцам и больше 226: пункт, приписанный нескольким сервисам,
учитывается в каждом из них. Сумма графы «Пунктов» по столбцам = **252** при **226** пунктах;
**23** пунктов приписаны 2+ сервисам (см. «Модули, разбросанные по нескольким сервисам»).

Расшифровка столбцов: `GW` = `MARS.Gateway` · `ADM` = `MARS.Admin` · `ALT` = `MARS.Alerts` · `CIN` = `MARS.CinemaQueue` · `CMD` = `MARS.Commands` · `DSC` = `MARS.Discord` · `MST` = `MARS.MediaStorage` · `OBS` = `MARS.OBS` · `SCB` = `MARS.Scoreboard` · `SHR` = `MARS.Shared` · `SND` = `MARS.SoundRequest` · `TEL` = `MARS.Telegram` · `TTS` = `MARS.TTS` · `TWC` = `MARS.TwitchCore` · `V36` = `MARS.Videos365` · `WGC` = `MARS.WaifuGacha` · `—` = нет адресата.

## Сводка по столбцам

| Микросервис | Пунктов | `[x]` | `[ ]` | Доля | Из них «по объявлению» | Модули-источники |
|---|---|---|---|---|---|---|
| `MARS.Admin` | 12 | 8 | 4 | 5.3% | 4 | 6 |
| `MARS.Alerts` | 71 | 54 | 17 | 31.4% | 19 | 8 |
| `MARS.CinemaQueue` | 9 | 9 | 0 | 4% | 0 | 2 |
| `MARS.Alerts` | 71 | 55 | 16 | 31.4% | 19 | 8 |
| `MARS.Discord` | 9 | 7 | 2 | 4% | 2 | 2 |
| `MARS.MediaStorage` | 10 | 10 | 0 | 4.4% | 0 | 4 |
| `MARS.OBS` | 2 | 2 | 0 | 0.9% | 0 | 2 |
| `MARS.Scoreboard` | 2 | 2 | 0 | 0.9% | 0 | 1 |
| `MARS.Shared` | 11 | 10 | 1 | 4.9% | 0 | 10 |
| `MARS.SoundRequest` | 16 | 16 | 0 | 7.1% | 0 | 4 |
| `MARS.Telegram` | 24 | 13 | 11 | 10.6% | 6 | 4 |
| `MARS.TTS` | 3 | 3 | 0 | 1.3% | 0 | 2 |
| `MARS.TwitchCore` | 47 | 38 | 9 | 20.8% | 3 | 8 |
| `MARS.Videos365` | 5 | 5 | 0 | 2.2% | 3 | 1 |
| `MARS.WaifuGacha` | 24 | 23 | 1 | 10.6% | 1 | 5 |
| **нет адресата** | 6 | 6 | 0 | 2.7% | 0 | 4 |

## Микросервисы репозитория без единого пункта

- `MARS.Gateway` — ни одного пункта. В монолите ему не соответствует ничего: `MARS.Server` был единым приложением с одной HTTP-точкой входа, поэтому `MARS.Gateway` — результат разделения на сервисы, а не переноса. См. Приложение C чеклиста.

## Модули, разбросанные по нескольким сервисам

Модулей с 2+ сервисами: **15** из 33.

| Модуль монолита | Сервисов | Пунктов | Куда разошлось |
|---|---|---|---|
| AC.R — отдельные награды | 6 | 72 | `MARS.Alerts`, `MARS.CinemaQueue`, `MARS.MediaStorage`, `MARS.Shared`, `MARS.TwitchCore`, `MARS.WaifuGacha` |
| AD. Twitch/ — вне Rewards/ | 6 | 24 | `MARS.Admin`, `MARS.Alerts`, `MARS.Shared`, `MARS.TTS`, `MARS.TwitchCore`, `MARS.WaifuGacha` |
| AC. Twitch/Rewards/ — инфраструктура наград | 3 | 20 | `MARS.Alerts`, `MARS.TwitchCore`, `MARS.WaifuGacha` |
| AF. YouTube/ | 3 | 1 | `MARS.Discord`, `MARS.SoundRequest`, `MARS.TwitchCore` |
| E. AudioControllerHub/ | 3 | 1 | `MARS.OBS`, `MARS.SoundRequest`, `MARS.TwitchCore` |
| J. CommandExecutor/ → MARS.Commands | 3 | 8 | `MARS.Commands`, `MARS.Shared`, `MARS.TwitchCore` |
| Q. MemoryStorageService/ | 3 | 1 | `MARS.Alerts`, `MARS.MediaStorage`, `MARS.Telegram` |
| S. PyroAlerts/ | 3 | 2 | `MARS.Alerts`, `MARS.MediaStorage`, `MARS.Shared` |
| V. SevenTv/ | 3 | 1 | `MARS.Alerts`, `MARS.TTS`, `MARS.TwitchCore` |
| AB. Telegram/ → MARS.Telegram | 2 | 12 | `MARS.Shared`, `MARS.Telegram` |
| AE. WaifuRoll/ → MARS.WaifuGacha | 2 | 8 | `MARS.TwitchCore`, `MARS.WaifuGacha` |
| M. EnvironmentVariable/ | 2 | 1 | `MARS.Admin`, `MARS.Shared` |
| T. Scoreboard/ → MARS.Scoreboard | 2 | 2 | `MARS.Scoreboard`, `MARS.Shared` |
| W. Shikimori/ → MARS.WaifuGacha | 2 | 3 | `MARS.Admin`, `MARS.WaifuGacha` |
| Y. SoundRequest/ → MARS.SoundRequest | 2 | 13 | `MARS.Shared`, `MARS.SoundRequest` |

## Пункты без определённого сервиса

| Пункт | Название | Модуль | Причина |
|---|---|---|---|
| `AA1` | `CheckersGame`, `CheckersGameManager`, `CheckersQueue` | AA. TabletopGames_OBSOLETE/ | нет ни кода, ни объявленного адресата |
| `AA2` | Модели настольных игр (6: `Board`, `Cell`, `Checker`, `Color`, `Figure`, `GameStatus`) | AA. TabletopGames_OBSOLETE/ | нет ни кода, ни объявленного адресата |
| `AC.C03` | `ChannelRewardsServiceCollectionExtensions` | AC. Twitch/Rewards/ — инфраструктура наград | нет ни кода, ни объявленного адресата |
| `D1` | `AppStateService` | D. AppStateService_OBSOLETE/ | нет ни кода, ни объявленного адресата |
| `N1` | `IKeyboardHookService`, `KeyboardHookService`, `NullKeyboardHookService`, `KeyboardHookFactory`, `KeyboardHookServiceCollectionExtensions` | N. KeyboardHook_UNUSED/ | нет ни кода, ни объявленного адресата |
| `N2` | `KeyboardHookController` | N. KeyboardHook_UNUSED/ | нет ни кода, ни объявленного адресата |

## Приложение. Пункт → микросервисы

| Пункт | Название | Отметка | Вердикт | Микросервисы | Основание |
|---|---|---|---|---|---|
| `A1` | `OperationResult` | [x] | заменено | `MARS.Shared` | коду |
| `B1` | `Worker365` | [x] | частично | `MARS.Videos365` | коду |
| `B2` | `Video365` (сущность) | [x] | полностью | `MARS.Videos365` | коду |
| `B3` | `IDnsResolver` / `SystemDnsResolver` | [x] | полностью | `MARS.Videos365` | коду |
| `B4` | `SiteAvailabilityChecker` | [x] | полностью | `MARS.Videos365` | коду |
| `B5` | `SiteUnavailableNotifier` | [x] | полностью | `MARS.Videos365` | коду |
| `C1` | `AdhdLayoutService` | [x] | полностью | `MARS.Alerts` | коду |
| `C2` | `AdhdLayoutConfig` (сущность) | [x] | полностью | `MARS.Alerts` | по объявлению модуля |
| `C3` | `AdhdLayoutConfigDto` | [x] | полностью | `MARS.Alerts` | коду |
| `D1` | `AppStateService` | [x] | исключено | — | нет адресата |
| `E1` | `SignalRAudioControllerService` | [x] | заменено | `MARS.OBS` + `MARS.SoundRequest` + `MARS.TwitchCore` | коду |
| `F1` | `AutoArtImage` (сущность) | [x] | полностью | `MARS.Alerts` | коду |
| `G1` | `IBooruAutoPostService` / `BooruAutoPostService` | [ ] | — | `MARS.Telegram` | коду |
| `G2` | `IBooruDiscordPoster` / `BooruDiscordPoster` | [ ] | — | `MARS.Telegram` | коду |
| `G3` | `IBooruTelegramPoster` / `BooruTelegramPoster` | [ ] | — | `MARS.Telegram` | коду |
| `G4` | `Rule34RandomPostService` | [ ] | — | `MARS.Telegram` | коду |
| `G5` | `TelegramScheduleMatcher` | [ ] | — | `MARS.Telegram` | коду |
| `G6` | Модели/схема BooruAutoPost (9 entity) | [x] | полностью | `MARS.Telegram` | коду |
| `H1` | `BooruMessageTemplateResolver` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `H2` | `BooruValidationHelper` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `H3` | `IDeduplicationService` / `DeduplicationService` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `H4` | `TagValidator` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `H5` | `PostedImageRecord` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `H6` | `BooruAutoPostCreateRequestBase`, `BooruAutoPostUpdateRequestBase`, `TelegramParseMode` | [x] | заменено | `MARS.Telegram` | коду |
| `I1` | `CinemaQueueServiceCollectionExtensions` | [x] | заменено | `MARS.CinemaQueue` | коду |
| `I2` | `ICinemaQueueService` / `CinemaQueueService` | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I3` | `ICinemaQueueRepository` / `CinemaQueueRepository` | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I4` | `KinopoiskService` + 6 моделей Kinopoisk | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I5` | `MediaMetadataService` | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I6` | `TwitchCinemaQueueService` | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I7` | `CinemaQueueNotificationService` | [x] | полностью | `MARS.CinemaQueue` | коду |
| `I8` | Модели CinemaQueue (6 сущностей/DTO) | [x] | полностью | `MARS.CinemaQueue` | коду |
| `J1` | Платформенные адаптеры команд | [x] | полностью | `MARS.Commands` + `MARS.TwitchCore` | коду |
| `J2` | `CommandExecutorService` | [x] | полностью | `MARS.Commands` + `MARS.Shared` | коду |
| `J3` | `CommandFactory` | [x] | полностью | `MARS.Commands` | коду |
| `J4` | `CommandExecutorServiceCollectionExtensions` | [x] | полностью | `MARS.Commands` | коду |
| `J5` | `BaseCommand` | [x] | полностью | `MARS.Commands` | коду |
| `J6` | `Platform`, `CommandVisibility`, `CommandParameterInfo` | [x] | полностью | `MARS.Commands` | коду |
| `J7` | Команды, перенесённые в `MARS.Commands` (61 файл, 59 уникальных `CommandName`) | [x] | полностью | `MARS.Commands` | по объявлению модуля |
| `J8` | Команды, отсутствующие в `MARS.Commands` (7) | [ ] | — | `MARS.TwitchCore` | коду |
| `K1` | `ConfigurationKeysBootstrapHostedService` | [x] | полностью | `MARS.Admin` | коду |
| `L1` | `IDiscordGatewayService` / `DiscordGatewayService` | [x] | полностью | `MARS.Discord` | коду |
| `L2` | `IMediaCompressor` / `MediaCompressor` | [ ] | — | `MARS.Discord` | по объявлению модуля |
| `L3` | `VideoExtensions` (`VideoCompressionProfile`, `ColorExtensions`) | [ ] | — | `MARS.Discord` | по объявлению модуля |
| `L4` | `DiscordPlayRequestService` | [x] | полностью | `MARS.Discord` | коду |
| `L5` | `DiscordPlayAudioCacheService` | [x] | полностью | `MARS.Discord` | коду |
| `L6` | `DiscordPlaySelectionSession` | [x] | полностью | `MARS.Discord` | коду |
| `L7` | `DiscordPreparedAudioFile` | [x] | полностью | `MARS.Discord` | коду |
| `L8` | `IDiscordTtsVoiceRelayService` / `DiscordTtsVoiceRelayService` | [x] | полностью | `MARS.Discord` | коду |
| `M1` | `EnvironmentVariable` (сущность) | [x] | полностью | `MARS.Admin` + `MARS.Shared` | коду |
| `N1` | `IKeyboardHookService`, `KeyboardHookService`, `NullKeyboardHookService`, `KeyboardHookFactory`, `KeyboardHookServiceCollectionExtensions` | [x] | исключено | — | нет адресата |
| `N2` | `KeyboardHookController` | [x] | исключено | — | нет адресата |
| `O1` | `ILogsService` / `LogsService` | [ ] | — | `MARS.Shared` | коду |
| `P1` | `IMediaInspector` / `FfprobeMediaInspector` | [x] | полностью | `MARS.MediaStorage` | коду |
| `P2` | `IMediaTranscoder` / `MediaTranscoder` | [x] | полностью | `MARS.MediaStorage` | коду |
| `P3` | `IMediaFileStorageService` / `WebRootMediaFileStorageService` | [x] | полностью | `MARS.MediaStorage` | коду |
| `Q1` | `MemoryStorage` + `MemoryFile` | [x] | полностью | `MARS.Alerts` + `MARS.MediaStorage` + `MARS.Telegram` | коду |
| `R1` | `IObsService` + `ObsConfiguration` | [x] | полностью | `MARS.OBS` | коду |
| `S1` | `PyroAlertsHandler` + `PyroAlertsHelper` | [x] | полностью | `MARS.Alerts` + `MARS.MediaStorage` | коду |
| `S2` | Модели PyroAlerts (10 сущностей/DTO) | [x] | полностью | `MARS.Alerts` + `MARS.MediaStorage` + `MARS.Shared` | коду |
| `T1` | `ScoreboardService` | [x] | полностью | `MARS.Scoreboard` + `MARS.Shared` | коду |
| `T2` | Модели Scoreboard (4) | [x] | полностью | `MARS.Scoreboard` | коду |
| `U1` | `IServiceManager` / `ServiceManager` / `ManagedServiceBase` | [x] | полностью | `MARS.Admin` | коду |
| `U2` | Модели ServiceManager (5) | [x] | полностью | `MARS.Admin` | коду |
| `V1` | `ISevenTvApiService` / `SevenTvApiService` | [x] | заменено | `MARS.Alerts` + `MARS.TTS` + `MARS.TwitchCore` | коду |
| `W1` | `IShikimoriApiClient` / `ShikimoriApiClient` / `ShikimoriService` | [x] | заменено | `MARS.WaifuGacha` | коду |
| `W2` | `IShikimoriRateLimiter` / `ShikimoriShikimoriRateLimiter` / `RateLimiterInfo` | [x] | заменено | `MARS.Admin` + `MARS.WaifuGacha` | коду |
| `W3` | GraphQL-модели Shikimori (16 node/DTO) | [ ] | — | `MARS.WaifuGacha` | по объявлению модуля |
| `X1` | `ISoundBar` + `SoundMuteCoordinator` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y1` | Модели SoundRequest (5) | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y2` | `IPlayerController` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y3` | `MainPlayer` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y4` | `StateManager` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y5` | `SoundRequestUserQueue` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y6` | `SoundRequestCommandsService` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y7` | `InSignalRHubService` | [x] | заменено | `MARS.Shared` + `MARS.SoundRequest` | коду |
| `Y8` | `OutSignalRHubService` | [x] | заменено | `MARS.SoundRequest` | коду |
| `Y9` | `SoundCloudResolver` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y10` | `SpotifyApiClient` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y11` | `SpotifyAuthService` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y12` | `SpotifyPlaybackService` + `SpotifyPlaybackSnapshot` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Y13` | `SpotifyResolver` | [x] | полностью | `MARS.SoundRequest` | коду |
| `Z1` | Модели StreamArchive (6) | [x] | полностью | `MARS.Admin` | коду |
| `Z2` | `IStreamArchiveService` / `StreamArchiveService` | [ ] | — | `MARS.Admin` | по объявлению модуля |
| `Z3` | `StreamArchiveWorker` | [ ] | — | `MARS.Admin` | по объявлению модуля |
| `Z4` | `IFFmpegService` / `FFmpegService` | [ ] | — | `MARS.Admin` | по объявлению модуля |
| `Z5` | Модели FFprobe (4: `FFprobeFormat`, `FFprobeOutput`, `FFprobeStream`, `VideoInfo`) | [ ] | — | `MARS.Admin` | по объявлению модуля |
| `AA1` | `CheckersGame`, `CheckersGameManager`, `CheckersQueue` | [x] | исключено | — | нет адресата |
| `AA2` | Модели настольных игр (6: `Board`, `Cell`, `Checker`, `Color`, `Figure`, `GameStatus`) | [x] | исключено | — | нет адресата |
| `AB1` | `IReceiverService`, `PollingServiceBase`, `ReceiverServiceBase` | [x] | полностью | `MARS.Telegram` | коду |
| `AB2` | `PollingService`, `ReceiverService` | [x] | полностью | `MARS.Telegram` | коду |
| `AB3` | `UpdateHandler` | [x] | полностью | `MARS.Telegram` | коду |
| `AB4` | Модели Telegram BotService (5) | [x] | полностью | `MARS.Telegram` | коду |
| `AB5` | `TelegramProxyHelper` | [ ] | — | `MARS.Telegram` | по объявлению модуля |
| `AB6` | Буфер обмена (4 файла) | [x] | полностью | `MARS.Telegram` | коду |
| `AB7` | `ITelegramDiscordBridgeService` / `TelegramDiscordBridgeService` | [x] | полностью | `MARS.Telegram` | коду |
| `AB8` | Модели DiscordBridge (7) | [x] | полностью | `MARS.Telegram` | коду |
| `AB9` | Google Photos (3) | [x] | полностью | `MARS.Telegram` | коду |
| `AB10` | `ITelegramusService` | [x] | заменено | `MARS.Shared` | коду |
| `AB11` | `TelegramChannelsResenderService` + `ChannelProcessingState` | [x] | полностью | `MARS.Telegram` | коду |
| `AB12` | WTelegram (3) | [x] | полностью | `MARS.Telegram` | коду |
| `AC.C01` | `ChannelRewardsManager` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C02` | `ChannelRewardsService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C03` | `ChannelRewardsServiceCollectionExtensions` | [x] | заменено | — | нет адресата |
| `AC.C04` | `ChannelRewardsSyncService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C05` | `ChannelRewardRecord` (сущность) | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C06` | `IRewardsCacheService` / `RewardsCacheService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C07` | `ChannelRewardDefinition` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.C08` | `PyroAlertRewardDefinition` | [x] | заменено | `MARS.TwitchCore` | коду |
| `AC.C09` | `UpdateCustomRewardDto` | [x] | заменено | `MARS.TwitchCore` | коду |
| `AC.C10` | `TwitchAlertsInitializationService` | [ ] | — | `MARS.Alerts` + `MARS.TwitchCore` | коду |
| `AC.C11` | `TwitchRewardsOptions` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.S01` | `AnswersForTwitchRewards` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.S02` | `Command` | [x] | заменено | `MARS.TwitchCore` | коду |
| `AC.S03` | `HighlitedMessage` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.S04` | `MiniGamesManager` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.S05` | `RickRollerService` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.S06` | `RollCooldownNotificationService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.S07` | `RollCooldownService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.S08` | `TwitchEventSubAlertsAwaker` | [ ] | — | `MARS.Alerts` + `MARS.TwitchCore` | коду |
| `AC.S09` | `TwitchMessagesHubAwaker` | [ ] | — | `MARS.TwitchCore` | коду |
| `AC.R01` | `1_RandomReward/RandomReward_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R02` | `2_WaifuMarriage/MergeWaifu` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R03` | `2_WaifuMarriage/WaifuMarriage_TwitchReward` | [x] | частично | `MARS.Shared` | коду |
| `AC.R04` | `4_FrogRoll/FrogRollService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R05` | `4_FrogRoll/FrogRoll_TwitchReward` | [x] | частично | `MARS.Alerts` | по объявлению модуля |
| `AC.R06` | `4_FumoRoll/FumoRollService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R07` | `4_FumoRoll/FumoCollectionService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R08` | `4_FumoRoll/FumoFridayRoll_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R09` | `4_MikuRoll/MikuRollService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R10` | `4_MikuRoll/MikuCollectionService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R11` | `4_MikuRoll/MikuRoll_TwitchReward` | [x] | частично | `MARS.Alerts` | по объявлению модуля |
| `AC.R12` | `4_SearchWife/SearchWife_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R13` | `5_AddWife/AddNewWaifu` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AC.R14` | `5_AddWife/AddWife_TwitchReward` | [x] | частично | `MARS.Alerts` | по объявлению модуля |
| `AC.R15` | `6_RussianRoulette/RussianRoulette_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R16` | `6_RussianRoulette/TwitchRussianRoulete` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R17` | `7_Quiz/Quiz_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R18` | `7_Quiz/TwitchTrivia` | [x] | частично | `MARS.TwitchCore` | коду |
| `AC.R19` | `9_AudioQuiz/AudioQuiz_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R20` | `9_AudioQuiz/AudioTriviaMiniGame` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R21` | `10_RandomSound/RandomSound_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R22` | `11_RandomMemReward/RandomMem_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R23` | `11_RandomMemReward/Service/RandomMemeWorker` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R24` | `11_RandomMemReward/Service/RandomMemOnline` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R25` | `11_RandomMemReward/Service/IRandomMemeService` | [x] | полностью | `MARS.MediaStorage` | коду |
| `AC.R26` | `11_RandomMemReward/Service/RandomMemeService` | [x] | полностью | `MARS.MediaStorage` | коду |
| `AC.R27` | `11_RandomMemReward/Service/Entity/MemeOrder` + `MemeType` | [x] | полностью | `MARS.Alerts` + `MARS.MediaStorage` | коду |
| `AC.R28` | `11_RandomMemReward/Service/DTOs/MemeOrderDto` + `MemeTypeDto` | [x] | полностью | `MARS.MediaStorage` | коду |
| `AC.R29` | `11_RandomMemReward/Service/Entity/WTelegramAlloweedChannel` | [x] | заменено | `MARS.Alerts` | коду |
| `AC.R30` | `13_FumoFriday/FumoFriday_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R31` | `13_FumoFriday/Entitys/FumoUser` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AC.R32` | `18_GaoAlert/GaoAlert_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R33` | `27_RandomArt/RandomArt_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R34` | `27_RandomArt/RandomArt` | [x] | заменено | `MARS.Alerts` | коду |
| `AC.R35` | `27_RandomArt/DanbooruRandomPostService` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R36` | `38_WednsdayFrog/WednsdayFrog_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R37` | `39_MikuMonday/MikuMondayTracksService` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R38` | `39_MikuMonday/TwitchMikuMondayRewardService` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R39` | `61_What/What_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R40` | `134_Pedro/Pedro_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R41` | `150_TyazheloReward/Tyazhelo_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R42` | `1510_StatusQuestion/StatusQuestion_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R43` | `155_MichaelTime/MichaelTime_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R44` | `1580_MikuBeam/MikuBeam_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R45` | `1580_MikuBeam/TwitchMikuBeamRewardService` | [x] | заменено | `MARS.TwitchCore` + `MARS.WaifuGacha` | коду |
| `AC.R46` | `160_LegBum/LegBum_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R47` | `160_LegBum/LegBumRefundService` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R48` | `1602_CinemaRequest/CinemaRequest_TwitchReward` | [x] | полностью | `MARS.Alerts` + `MARS.CinemaQueue` | коду |
| `AC.R49` | `170_FumoFridayNightReward/FumoFridayNight_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R50` | `170_MikuMondayAlert/MikuMondayAlert_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R51` | `1700_Confetti/Confetti_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R52` | `1701_Fireworks/Fireworks_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R53` | `1702_EmojisReward/Emojis_TwitchReward` | [ ] | — | `MARS.Alerts` | по объявлению модуля |
| `AC.R54` | `182_Stone/Stone_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R55` | `195_Cringe/Cringe_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R56` | `2002_AdhdSuperpower/AdhdSuperpower_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R57` | `210_Hello/Hello_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R58` | `215_Bye/Bye_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R59` | `317_Intelligence/Intelligence_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R60` | `320_MikuScreamer/MikuScreamer_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R61` | `333_Skibidibop/Skibidibop_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R62` | `337_PhonkEdit/PhonkEdit_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R63` | `341_Aga/Aga_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R64` | `342_BadToBone/BadToBone_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R65` | `353_TikTokEdit/TikTokEdit_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R66` | `375_DanceDance/DanceDance_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R67` | `666_Edge0100Alert/Edge0100Alert_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R68` | `1333_SkibidibopLong/SkibidibopLong_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R69` | `6666_CloseGame/CloseGame_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R70` | `75000_SelectGame/SelectGame_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R71` | `8005_Credits/Credits_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AC.R72` | `99999_AllRefundService/AllRefund_TwitchReward` | [x] | полностью | `MARS.Alerts` | коду |
| `AD1` | `AutoRewardInfoFetcher` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD2` | `TwitchBlackListService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD3` | `TwitchApiRateLimiter` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD4` | `TwitchClientProxy` | [ ] | — | `MARS.TwitchCore` | коду |
| `AD5` | `TwitchConnectionManager` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD6` | AutoMessages (7 файлов) | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD7` | `AutoHello` + `AutoVideoHello` | [x] | заменено | `MARS.TwitchCore` + `MARS.WaifuGacha` | коду |
| `AD8` | Модели Twitch/Entitys (27) | [x] | полностью | `MARS.Admin` + `MARS.Alerts` + `MARS.Shared` + `MARS.TTS` + `MARS.TwitchCore` + `MARS.WaifuGacha` | коду |
| `AD9` | `ITwitchUserEnsureService` / `TwitchUserEnsureService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD10` | `EventSubService` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD11` | `TelegramTokenNotification` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD12` | `TokenService` + `TokenInfo` + `ITwitchReward` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD13` | `ITwitchMediaPreparationService` / `TwitchMediaPreparationService` / `TwitchMediaTranscodeWorker` | [ ] | — | `MARS.TwitchCore` | по объявлению модуля |
| `AD14` | `ILeaderboardService` / `LeaderboardService` | [ ] | — | `MARS.TwitchCore` | коду |
| `AD15` | PuntoSwitcher (3) | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD16` | `TwitchStreamStartupNotifications` | [x] | полностью | `MARS.TwitchCore` | коду |
| `AD17` | `TwitchStreamManagementService` + `TwitchTitleChangeCommand` | [x] | частично | `MARS.TwitchCore` | коду |
| `AD18` | Synthesizer/TTS (7) | [x] | частично | `MARS.Shared` + `MARS.TTS` | коду |
| `AD19` | `TekkenStreamsDiscordForwarderService` | [ ] | — | `MARS.TwitchCore` | по объявлению модуля |
| `AD20` | TwitchFollowers (8) | [x] | частично | `MARS.Admin` + `MARS.TwitchCore` | коду |
| `AD21` | Validation (8) | [x] | частично | `MARS.TwitchCore` | коду |
| `AD22` | `WaifuChatTwitchReward` | [ ] | — | `MARS.TwitchCore` | по объявлению модуля |
| `AD23` | `WeddingAnniversaryService` | [x] | полностью | `MARS.TwitchCore` + `MARS.WaifuGacha` | коду |
| `AD24` | HelloVideos (2) | [x] | полностью | `MARS.TwitchCore` | коду |
| `AE1` | Гарантия ролла (3) | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AE2` | `PrizeTypeAbstract` | [x] | заменено | `MARS.WaifuGacha` | коду |
| `AE3` | Модели WaifuRoll (7) | [x] | полностью | `MARS.TwitchCore` + `MARS.WaifuGacha` | коду |
| `AE4` | `WaifuRollException` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AE5` | `WaifuRollEnsurenceService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AE6` | `IWaifuPrizesService` / `WaifuPrizesService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AE7` | `IWaifuRollService` / `WaifuRollService` | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AE8` | Модели ответа (4) | [x] | полностью | `MARS.WaifuGacha` | коду |
| `AF1` | `YouTubeResolver` | [x] | полностью | `MARS.Discord` + `MARS.SoundRequest` + `MARS.TwitchCore` | коду |

## Приложение. Проигнорированные при атрибуции имена файлов

Имена, встречающиеся в 4+ сервисах: сигнала о владельце не дают.

| Пункт | Имя файла |
|---|---|
| `AD8` | `RootState.cs -> 5 сервисов` |
| `AD8` | `TwitchUser.cs -> 4 сервисов` |

## Не распределено — к распределению

Сухой список. Группа A закрыта решением владельца и распределения не требует;
группы B, C и D — к решению. Обоснований нет, только пункты.
Колонка `→` пустая: впишите сервис, если распределение нужно утвердить или исправить.

### A. Исключено решением владельца — 5

Решение принято, перенос не планируется. Адресат не назначается.

| Пункт | Название | Что это | Решение |
|---|---|---|---|
| `AA1` | `CheckersGame`, `CheckersGameManager`, `CheckersQueue` | настольные игры, в монолите OBSOLETE | лишнее |
| `AA2` | Модели настольных игр (6: `Board`, `Cell`, `Checker`, `Color`, `Figure`, `GameStatus`) | настольные игры, в монолите OBSOLETE | лишнее |
| `D1` | `AppStateService` | заглушка из одного блока комментариев | не переносим |
| `N1` | `IKeyboardHookService`, `KeyboardHookService`, `NullKeyboardHookService`, `KeyboardHookFactory`, `KeyboardHookServiceCollectionExtensions` | хук клавиатуры, в монолите UNUSED | не нужен нигде |
| `N2` | `KeyboardHookController` | хук клавиатуры, в монолите UNUSED | не нужен нигде |

### B. Нет адресата — 1

| Пункт | Название | Модуль | Причина | → |
|---|---|---|---|---|
| `AC.C03` | `ChannelRewardsServiceCollectionExtensions` | AC. Twitch/Rewards/ — инфраструктура наград | нет кода и нет объявленного адресата | |

### C. Распределено не по коду — 35

Сервис взят из заголовка модуля, потому что код в пункте не упомянут. Подтвердите или переопределите.

| Пункт | Название | Сейчас | → |
|---|---|---|---|
| `AB5` | `TelegramProxyHelper` | `MARS.Telegram` | |
| `AC.R01` | `1_RandomReward/RandomReward_TwitchReward` | `MARS.Alerts` | |
| `AC.R05` | `4_FrogRoll/FrogRoll_TwitchReward` | `MARS.Alerts` | |
| `AC.R08` | `4_FumoRoll/FumoFridayRoll_TwitchReward` | `MARS.Alerts` | |
| `AC.R11` | `4_MikuRoll/MikuRoll_TwitchReward` | `MARS.Alerts` | |
| `AC.R12` | `4_SearchWife/SearchWife_TwitchReward` | `MARS.Alerts` | |
| `AC.R14` | `5_AddWife/AddWife_TwitchReward` | `MARS.Alerts` | |
| `AC.R15` | `6_RussianRoulette/RussianRoulette_TwitchReward` | `MARS.Alerts` | |
| `AC.R16` | `6_RussianRoulette/TwitchRussianRoulete` | `MARS.Alerts` | |
| `AC.R17` | `7_Quiz/Quiz_TwitchReward` | `MARS.Alerts` | |
| `AC.R19` | `9_AudioQuiz/AudioQuiz_TwitchReward` | `MARS.Alerts` | |
| `AC.R20` | `9_AudioQuiz/AudioTriviaMiniGame` | `MARS.Alerts` | |
| `AC.R21` | `10_RandomSound/RandomSound_TwitchReward` | `MARS.Alerts` | |
| `AC.R30` | `13_FumoFriday/FumoFriday_TwitchReward` | `MARS.Alerts` | |
| `AC.R38` | `39_MikuMonday/TwitchMikuMondayRewardService` | `MARS.Alerts` | |
| `AC.R47` | `160_LegBum/LegBumRefundService` | `MARS.Alerts` | |
| `AC.R50` | `170_MikuMondayAlert/MikuMondayAlert_TwitchReward` | `MARS.Alerts` | |
| `AC.R53` | `1702_EmojisReward/Emojis_TwitchReward` | `MARS.Alerts` | |
| `AD13` | `ITwitchMediaPreparationService` / `TwitchMediaPreparationService` / `TwitchMediaTranscodeWorker` | `MARS.TwitchCore` | |
| `AD19` | `TekkenStreamsDiscordForwarderService` | `MARS.TwitchCore` | |
| `AD22` | `WaifuChatTwitchReward` | `MARS.TwitchCore` | |
| `C2` | `AdhdLayoutConfig` (сущность) | `MARS.Alerts` | |
| `H1` | `BooruMessageTemplateResolver` | `MARS.Telegram` | |
| `H2` | `BooruValidationHelper` | `MARS.Telegram` | |
| `H3` | `IDeduplicationService` / `DeduplicationService` | `MARS.Telegram` | |
| `H4` | `TagValidator` | `MARS.Telegram` | |
| `H5` | `PostedImageRecord` | `MARS.Telegram` | |
| `J7` | Команды, перенесённые в `MARS.Commands` (61 файл, 59 уникальных `CommandName`) | `MARS.Commands` | |
| `L2` | `IMediaCompressor` / `MediaCompressor` | `MARS.Discord` | |
| `L3` | `VideoExtensions` (`VideoCompressionProfile`, `ColorExtensions`) | `MARS.Discord` | |
| `W3` | GraphQL-модели Shikimori (16 node/DTO) | `MARS.WaifuGacha` | |
| `Z2` | `IStreamArchiveService` / `StreamArchiveService` | `MARS.Admin` | |
| `Z3` | `StreamArchiveWorker` | `MARS.Admin` | |
| `Z4` | `IFFmpegService` / `FFmpegService` | `MARS.Admin` | |
| `Z5` | Модели FFprobe (4: `FFprobeFormat`, `FFprobeOutput`, `FFprobeStream`, `VideoInfo`) | `MARS.Admin` | |

### D. Несколько сервисов — нужен основной — 23

Пункт стоит сразу в нескольких сервисах. Отметьте, какой основной; остальные останутся вторичными.

| Пункт | Название | Сейчас | Основной → |
|---|---|---|---|
| `AC.C10` | `TwitchAlertsInitializationService` | `MARS.Alerts` + `MARS.TwitchCore` | |
| `AC.R27` | `11_RandomMemReward/Service/Entity/MemeOrder` + `MemeType` | `MARS.Alerts` + `MARS.MediaStorage` | |
| `AC.R45` | `1580_MikuBeam/TwitchMikuBeamRewardService` | `MARS.TwitchCore` + `MARS.WaifuGacha` | |
| `AC.R48` | `1602_CinemaRequest/CinemaRequest_TwitchReward` | `MARS.Alerts` + `MARS.CinemaQueue` | |
| `AC.S08` | `TwitchEventSubAlertsAwaker` | `MARS.Alerts` + `MARS.TwitchCore` | |
| `AD18` | Synthesizer/TTS (7) | `MARS.Shared` + `MARS.TTS` | |
| `AD20` | TwitchFollowers (8) | `MARS.Admin` + `MARS.TwitchCore` | |
| `AD23` | `WeddingAnniversaryService` | `MARS.TwitchCore` + `MARS.WaifuGacha` | |
| `AD7` | `AutoHello` + `AutoVideoHello` | `MARS.TwitchCore` + `MARS.WaifuGacha` | |
| `AD8` | Модели Twitch/Entitys (27) | `MARS.Admin` + `MARS.Alerts` + `MARS.Shared` + `MARS.TTS` + `MARS.TwitchCore` + `MARS.WaifuGacha` | |
| `AE3` | Модели WaifuRoll (7) | `MARS.TwitchCore` + `MARS.WaifuGacha` | |
| `AF1` | `YouTubeResolver` | `MARS.Discord` + `MARS.SoundRequest` + `MARS.TwitchCore` | |
| `E1` | `SignalRAudioControllerService` | `MARS.OBS` + `MARS.SoundRequest` + `MARS.TwitchCore` | |
| `J1` | Платформенные адаптеры команд | `MARS.Commands` + `MARS.TwitchCore` | |
| `J2` | `CommandExecutorService` | `MARS.Commands` + `MARS.Shared` | |
| `M1` | `EnvironmentVariable` (сущность) | `MARS.Admin` + `MARS.Shared` | |
| `Q1` | `MemoryStorage` + `MemoryFile` | `MARS.Alerts` + `MARS.MediaStorage` + `MARS.Telegram` | |
| `S1` | `PyroAlertsHandler` + `PyroAlertsHelper` | `MARS.Alerts` + `MARS.MediaStorage` | |
| `S2` | Модели PyroAlerts (10 сущностей/DTO) | `MARS.Alerts` + `MARS.MediaStorage` + `MARS.Shared` | |
| `T1` | `ScoreboardService` | `MARS.Scoreboard` + `MARS.Shared` | |
| `V1` | `ISevenTvApiService` / `SevenTvApiService` | `MARS.Alerts` + `MARS.TTS` + `MARS.TwitchCore` | |
| `W2` | `IShikimoriRateLimiter` / `ShikimoriShikimoriRateLimiter` / `RateLimiterInfo` | `MARS.Admin` + `MARS.WaifuGacha` | |
| `Y7` | `InSignalRHubService` | `MARS.Shared` + `MARS.SoundRequest` | |

Итого к решению: **59** пунктов из 226. Ещё 5 закрыты решением владельца (группа A).

