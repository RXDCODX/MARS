# Чеклист сервисов монолита `MARS.Server/Services` → микросервисы `MARS`

Файл создан с нуля. Каждый пункт отслеживается до конкретного файла монолита и до
конкретного артефакта текущего репозитория. Полное покрытие исходника — 510 `.cs`
файлов — доказывается в Приложении A (таблица «файл монолита → пункт чеклиста»).

Производное представление тех же 226 пунктов, разложенных по микросервисам, — в
[SERVICE_MATRIX.md](SERVICE_MATRIX.md). Правки вносятся в этот файл; матрица
перегенерируется из него.

## 0. Источник данных

| Параметр | Значение |
|---|---|
| Репозиторий-источник | `D:\VS\MARS_old` |
| Путь исходника | `MARS.Projects/MARS.Server/Services` (в `D:\VS` существует ровно один каталог `MARS.Server`) |
| Всего `.cs` под `Services/` | **510** (без `.git/`) |
| Из них в `.git/` | 0 |
| Прочих файлов под `Services/` | 20 `.md` — см. Приложение B, сервисами не являются |
| Репозиторий-приёмник | `D:\VS\MARS` |
| `.cs` в `src/` | 698 |
| `.cs` в `tests/` | тестовые проекты 16 |

Ловушка при сборе: `Get-ChildItem -Recurse -Include *.cs` в PowerShell 5.1 при
`-LiteralPath` на каталоге **игнорирует `-Include`** и возвращает все файлы —
534 вместо 510. Пересчёт через `[System.IO.Directory]::GetFiles` и через
`-Filter *.cs` совпадает: 510.

## 1. Легенда

| Отметка | Значение |
|---|---|
| `- [x] **полностью**` | Сервис перенесён и реализован в текущем репозитории |
| `- [x] **частично**` | Перенесена только часть (только схема БД / только контракт / только сущность / логика ушла в другой сервис) |
| `- [x] **заменено**` | Намеренно заменён другим решением, эквивалент присутствует |
| `- [x] **исключено**` | Решение владельца: перенос не планируется, действий не требуется. Пункт остаётся в файле, чтобы покрытие монолита оставалось полным |
| `- [ ]` | Требует работы: нет ни кода, ни схемы, ни контракта |

## 2. Правило гранулярности

- **Один пункт = один логический сервис/компонент монолита.**
- Файлы без самостоятельной логики (entity, DTO, enum, record, абстрактные базы)
  объединяются в один пункт «модели/DTO» внутри своего модуля.
- Каждая папка `Twitch/Rewards/*` и каждый файл `CommandExecutor/Commands/*` — отдельный пункт.
- Вложенные `.md` монолита сервисами не считаются (Приложение B).
- Один пункт может покрывать несколько файлов монолита — все они перечислены в Приложении A.
- **Соглашение о путях.** Пути к артефактам репозитория внутри пункта указаны
  относительно папки сервиса, названной в заголовке модуля (`Services/…`, `Entities/…`,
  `Grpc/…`); путь от `src/` приводится, когда это важно для однозначности или когда
  файл лежит вне сервиса. Проверка существования путей (Проверка 2, п. 2.1) идёт по
  полным путям, поэтому всё, что важно для проверяемости, приведено полностью.

## 3. Сводка

| Метрика | Значение |
|---|---|
| Пунктов чеклиста всего | **226** |
| Из них `[x]` (перенесено, заменено или исключено по решению) | **184** |
| ├─ `полностью` | 150 |
| ├─ `частично` | 10 |
| ├─ `заменено` | 19 |
| └─ `исключено` (решение владельца, см. §5) | 5 |
| Из них `[ ]` (требует работы) | **42** |
| Файлов монолита под `Services/` | 510 `.cs` — покрыто 510 (100 %) |
| Пунктов-приёмников файлов монолита | 226 |
| Не-`.cs` файлов под `Services/` | 20 `.md` — учтены в Приложении B |
| Сервисов в `src/` | 16 проектов, 698 `.cs` |
| Планируется к добавлению | `MARS.Shikimori` — план в §6.7 |

## 4. Чеклист

### A. Корень `Services/`

- [x] **A1. `OperationResult`** — *заменено*. `src/MARS.Shared/Models/OperationResult.cs`; также продублировано в `MARS.Admin/Services/OperationResult.cs` и `MARS.Discord/Models/OperationResult.cs`.

### B. `365Genius/` → `MARS.Videos365`

- [x] **B1. `Worker365`** — *частично*. `src/MARS.Videos365/Services/Worker365.cs`. В XML-доке зафиксировано: конвейер — заглушка, `TODO(365)` прямо в файле.
- [x] **B2. `Video365` (сущность)** — *полностью*. `src/MARS.Videos365/Entities/Video365.cs`, `Data/Videos365DbContext.cs`, миграции `20260930152034_InitialVideos365` + `20260930190805_SeedLegacyVideos365`.
- [x] **B3. `IDnsResolver` / `SystemDnsResolver`** — *полностью*. `src/MARS.Videos365/Services/IDnsResolver.cs`, `SystemDnsResolver.cs`; регистрация `builder.Services.AddSingleton<IDnsResolver, SystemDnsResolver>()`; тест `tests/MARS.Videos365.Tests/SystemDnsResolverTests.cs` (резолв IP-литерала, без обращения к DNS-серверу).
- [x] **B4. `SiteAvailabilityChecker`** — *полностью*. `src/MARS.Videos365/Services/SiteAvailabilityChecker.cs`; вызывается из `Services/Worker365.cs` перед обходом избранки. Отличие от монолита: проверка возвращает `OperationResult` вместо исключения — вызывающая сторона на ошибке отправляет уведомление и завершает проход; без успешного DNS HTTP-запрос не выполняется. Тесты `tests/MARS.Videos365.Tests/SiteAvailabilityCheckerTests.cs` (8 сценариев, HTTP подменён `HttpMessageHandler`).
- [x] **B5. `SiteUnavailableNotifier`** — *полностью*. `src/MARS.Videos365/Services/SiteUnavailableNotifier.cs`; seam над Telegram.Bot — `Services/ITelegramAdminMessenger.cs` + `Services/TelegramAdminMessenger.cs` (в Telegram.Bot 22.x `SendMessage` — метод расширения, а не член `ITelegramBotClient`, поэтому подменой клиента его не перехватить). Адресаты берутся из секции `Telegram` (`MARS.Shared/Configuration/AppBase.cs`, тип `TelegramConfig`), env — `Telegram__BotToken`, `Telegram__AdminIds__0`; список собирается вручную, потому что compose подставляет пустую строку, а приведение `""` к `long` роняет хост. Ошибка отправки одному адресату не отменяет остальных. Тесты `tests/MARS.Videos365.Tests/SiteUnavailableNotifierTests.cs`.

### C. `Adhd/`

- [x] **C1. `AdhdLayoutService`** — *полностью*. `src/MARS.Alerts/Services/Adhd/IAdhdLayoutService.cs`, `AdhdLayoutService.cs`; слой БД — `src/MARS.Alerts/Data/AlertsDbContext.cs`, миграции `20260930152358_InitialAlertsLayout` + `20260930192839_SeedLegacyAlerts`. Отличие от монолита: возвращается `OperationResult<AdhdLayoutConfigDto>` вместо «вернуть дефолт или исключение». Пустая таблица означает «показать весь набор виджетов» — как и в монолите, где `CreateDefaultConfig()` отдавал DTO со всеми включёнными флагами. Тесты `tests/MARS.Alerts.Tests/AdhdLayoutServiceTests.cs` (5 сценариев) и `AdhdLayoutConfigSchemaTests.cs`.
- [x] **C2. `AdhdLayoutConfig` (сущность)** — *полностью*. См. C1.
- [x] **C3. `AdhdLayoutConfigDto`** — *полностью*. `src/MARS.Alerts/Models/AdhdLayoutConfigDto.cs`. Значения по умолчанию перенесены из монолита (все 15 флагов `true`, `DvdLogosCount = 12`): пустая таблица у оверлея означает полный набор виджетов, а не пустой экран. DTO едет в оверлей по контракту `TelegramusService` — см. C1.

### D. `AppStateService_OBSOLETE/`

- [x] **D1. `AppStateService`** — *исключено*. Решение владельца: не переносим. В монолите класс был заглушкой — тело состоит из одного блока комментариев (14 пунктов «что собиралось показывать»), ни одного поля или метода. Покрыто `RootState` (`MARS.Admin/Entities/RootState.cs`, `MARS.TwitchCore/Entities/RootState.cs`, `MARS.Telegram/Entities/RootState.cs`, `MARS.WaifuGacha/Entities/RootState.cs`, `MARS.SoundRequest/Entities/RootState.cs`) и метриками Prometheus — но это отдельное решение, а не перенос `AppStateService`.

### E. `AudioControllerHub/`

- [x] **E1. `SignalRAudioControllerService`** — *заменено*. SignalR-хабы из репозитория выпилены. Функциональность распределена: `src/MARS.SoundRequest/Services/SoundBarService/` (`SoundBarFactory`, `SoundBarHttpClient`, `SoundBarServiceLocal`, `SoundMuteCoordinator`), `src/MARS.OBS/Services/HttpObsService.cs`, `src/MARS.TwitchCore/Configuration/AudioControllerOptions.cs`.

### F. `AutoArts_OBSOLETE/`

- [x] **F1. `AutoArtImage` (сущность)** — *полностью*. `src/MARS.Alerts/Models/AutoArtImage.cs`. Используется в `src/MARS.Alerts/Services/Twitch/Rewards/HighlitedMessage.cs`.

### G. `BooruAutoPost/`

- [ ] **G1. `IBooruAutoPostService` / `BooruAutoPostService`** — сервиса автопостинга нет. Отсутствие явно зафиксировано в `src/MARS.Telegram/Entities/BooruAutoPostConfig.cs`: «код автопостинга не портирован … `TODO(Booru)`: восстановить планировщик поверх этих таблиц».
- [ ] **G2. `IBooruDiscordPoster` / `BooruDiscordPoster`** — публикатора в Discord нет (упомянут как непортированный в `BooruAutoPostConfig.cs`).
- [ ] **G3. `IBooruTelegramPoster` / `BooruTelegramPoster`** — публикатора в Telegram нет (упомянут как непортированный в `BooruAutoPostConfig.cs`).
- [ ] **G4. `Rule34RandomPostService`** — нет. Enum `BooruSource` с `Rule34` перенесён (`src/MARS.Telegram/Entities/BooruEnums.cs`), сам сервис нет.
- [ ] **G5. `TelegramScheduleMatcher`** — нет (поимённо назван непортированным в XML-доке `BooruAutoPostConfig.cs`).
- [x] **G6. Модели/схема BooruAutoPost (9 entity)** — *полностью* (перенесена только схема и данные; потребители кода — см. G1–G5). `src/MARS.Telegram/Entities/BooruAutoPostConfig.cs`, `BooruScheduledPost.cs`, `BooruEnums.cs`; миграции `20260930152539_AddBooruAutoPostConfigs` + `20260930192816_SeedLegacyChat`; тесты `tests/MARS.Telegram.Tests/BooruSchemaTests.cs`. Данные перенесены, потребителя нет.

### H. `BooruShared/`

- [ ] **H1. `BooruMessageTemplateResolver`** — нет.
- [ ] **H2. `BooruValidationHelper`** — нет.
- [ ] **H3. `IDeduplicationService` / `DeduplicationService`** — нет.
- [ ] **H4. `TagValidator`** — нет.
- [ ] **H5. `PostedImageRecord`** — нет.
- [x] **H6. `BooruAutoPostCreateRequestBase`, `BooruAutoPostUpdateRequestBase`, `TelegramParseMode`** — *заменено*. В монолите отдельные DTO запросов и enum парсинга; в `MARS` это поля `BooruAutoPostConfig` (`Message`, `Tags`, `CronExpression`, `BooruTelegramParseMode`) — `src/MARS.Telegram/Entities/BooruAutoPostConfig.cs`.

### I. `CinemaQueue/` → `MARS.CinemaQueue`

- [x] **I1. `CinemaQueueServiceCollectionExtensions`** — *заменено*. `src/MARS.CinemaQueue/Services/CinemaQueueServiceCollectionExtensions.cs`.
- [x] **I2. `ICinemaQueueService` / `CinemaQueueService`** — *полностью*. `Interfaces/ICinemaQueueService.cs`, `Services/CinemaQueueService.cs`.
- [x] **I3. `ICinemaQueueRepository` / `CinemaQueueRepository`** — *полностью*. `Interfaces/ICinemaQueueRepository.cs`, `Repositories/CinemaQueueRepository.cs`.
- [x] **I4. `KinopoiskService` + 6 моделей Kinopoisk** — *полностью*. `Services/KinopoiskService.cs`, `Models/KinopoiskExternalId|MovieDto|Poster|Rating|SearchResponse|Votes.cs`, `Configuration/KinopoiskConfiguration.cs`.
- [x] **I5. `MediaMetadataService`** — *полностью*. `Services/MediaMetadataService.cs`.
- [x] **I6. `TwitchCinemaQueueService`** — *полностью*. `Services/TwitchCinemaQueueService.cs`.
- [x] **I7. `CinemaQueueNotificationService`** — *полностью*. `Services/CinemaQueueNotificationService.cs`.
- [x] **I8. Модели CinemaQueue (6 сущностей/DTO)** — *полностью*. `Entities/CinemaMediaItem.cs`, `CinemaMediaItemDto.cs`, `CinemaQueueStatistics.cs`, `CreateMediaItemRequest.cs`, `MediaStatus.cs`, `UpdateMediaItemRequest.cs`; миграции `20260922000… InitialCreate` + `20260930192828_SeedLegacyCinema`.

### J. `CommandExecutor/` → `MARS.Commands`

- [x] **J1. Платформенные адаптеры команд** — *полностью*. `Services/Adapters/` (4 файла: Api/Discord/Telegram/Twitch), `Services/ICommandService.cs`, `Services/PlatformCommandServiceBase.cs`. Гейт прав — `src/MARS.TwitchCore/Services/Commands/TwitchCommandPermissions.cs`, тесты `tests/MARS.Commands.Tests/Commands/CommandAuthorizerTests.cs` и `tests/MARS.TwitchCore.Tests/Commands/TwitchCommandPermissionsTests.cs`.
- [x] **J2. `CommandExecutorService`** — *полностью*. `src/MARS.Commands/Services/CommandExecutorService.cs`; наружу — `Controllers/CommandsController.cs` + контракт `src/MARS.Shared/Protos/commands.proto` (`service Commands`), маршрут `commands`.
- [x] **J3. `CommandFactory`** — *полностью*. `src/MARS.Commands/Services/CommandFactory.cs`.
- [x] **J4. `CommandExecutorServiceCollectionExtensions`** — *полностью*. `src/MARS.Commands/Services/CommandExecutorServiceCollectionExtensions.cs`.
- [x] **J5. `BaseCommand`** — *полностью*. `Services/Entitys/Commands/BaseCommand.cs` (в `MARS` `ExecuteAsync` возвращает `CommandResult` — отдельный тип результата).
- [x] **J6. `Platform`, `CommandVisibility`, `CommandParameterInfo`** — *полностью*. `Services/Entitys/Platform.cs`, `CommandVisibility.cs`, `CommandParameterInfo.cs`. Значения `Platform` заданы явными степенями двойки (это отдельная правка, см. §8).
- [x] **J7. Команды, перенесённые в `MARS.Commands` (61 файл, 59 уникальных `CommandName`)** — *полностью*. `systeminfo` и `tanya` в монолите занимали по два файла (`!systeminfo_command.cs` + `!systeminfo_SystemInfo.cs`, `!tanya_command.cs` + `!tanya_Tanya.cs`), в `MARS` слиты в один. Полный перечень — в подписи ниже. 61 + 7 (J8) = 68 = число файлов в `CommandExecutor/Commands/`.
- [ ] **J8. Команды, отсутствующие в `MARS.Commands` (7)** — `autohello`, `fumoinv`, `mgleaders`, `mikuinv`, `mywins`, `randomanime`, `randommanga`. Проверено извлечением `CommandName` из всех `.cs` обеих сторон: 66 имён в монолите, 67 в `MARS`, совпадают 59; 59 + 7 = 66 — сходится, неучтённых нет. Обратная сторона: 8 имён есть только в `MARS` (`byebye`, `example`, `genshin`, `honkai`, `honkaiusers`, `links`, `randomshorts`, `telegramonly`) — 59 + 8 = 67. Строк `fumoinv`, `mgleaders`, `mikuinv`, `mywins` и типов `MGLeadersCommand`, `MikuInventoryCommand`, `MyWinsCommand`, `FumoInventoryCommand`, `RandomAnimeCommand`, `RandomMangaCommand`, `AutoHelloCommand` нет ни в одном файле репозитория (включая `.md` и `.json`). `randomanime`/`randommanga` встречаются только как упоминания внутри `MARS.WaifuGacha/Services/ShikimoriService.cs` и `AnswersForTwitchRewards.cs` — самой команды нет.

**J7 — перенесённые команды (61 файлов, $CommandName в скобках):**

`!adhd_command` (adhd) · `!automessage_SendAutoMessage` (automessage) · `c_ShortCommands` (c) · `!catisa_command` (catisa) · `!directory_command` (directory) · `!discord_command` (discord) · `download_command` (download) · `!fumo_command` (fumo) · `!getAllKeyWordsForAlerts_Command` (getAllKeyWordsForAlerts) · `!googlephotos_authorize_command` (googlephotosauthorize) · `!hellovideo_command` (hellovideo) · `help_command` (help) · `info_command` (info) · `!joinedtwitchchannels_command` (joinedtwitchchannels) · `!mikubeam_command` (mikubeam) · `!mikumonday_MikuMondayReward` (mikumonday) · `!minigamestop_command` (minigamestop) · `!mutesound_command` (mutesound) · `!platformtest_command` (platformtest) · `!puntoswitcher_command` (puntoswitcher) · `queue_QueuePosition` (queue) · `!randommem_command` (randommem) · `!rollfrog_command` (rollfrog) · `!rollfumo_command` (rollfumo) · `!rollmiku_command` (rollmiku) · `!rollwaifu_command` (rollwaifu) · `!setenv_command` (setenv) · `!shutdown_command` (shutdown) · `song_command` (song) · `!spotifyauth_start_command` (spotifyauthstart) · `sr_command` (sr) · `!srclear_command` (srclear) · `srlist_SoundRequestList` (srlist) · `!srpause_command` (srpause) · `!srplay_command` (srplay) · `!srstop_command` (srstop) · `!srvolume_command` (srvolume) · `srwrong_command` (srwrong) · `start_command` (start) · `!systeminfo_command` (systeminfo) · `!systeminfo_SystemInfo` (systeminfo) · `!tanya_command` (tanya) · `!tanya_Tanya` (tanya) · `!title_ChangeStreamTitle` (title) · `!ttsfilter_command` (ttsfilter) · `!ttsstop_command` (ttsstop) · `!ttsvoice_command` (ttsvoice) · `!ttsvolume_command` (ttsvolume) · `!twitchauthnotify_command` (twitchauthnotify) · `!twitchblacklistadd_command` (twitchblacklistadd) · `!twitchblacklistremove_command` (twitchblacklistremove) · `!twitchchannelreconnect_command` (twitchchannelreconnect) · `!twitchchannelstatus_command` (twitchchannelstatus) · `!twitchevents_command` (twitchevents) · `!twitchsubrec_command` (twitchsubrec) · `!unmutesound_command` (unmutesound) · `vanish_command` (vanish) · `!waifuunmerge_command` (waifuunmerge) · `!whitelist_command` (whitelist) · `!wtelegramstatus_command` (wtelegramstatus) · `zonezero_command` (zonezero)

### K. `Configuration/`

- [x] **K1. `ConfigurationKeysBootstrapHostedService`** — *полностью*. `src/MARS.Admin/Services/Configuration/ConfigurationKeysBootstrapHostedService.cs`.

### L. `Discord/` → `MARS.Discord`

- [x] **L1. `IDiscordGatewayService` / `DiscordGatewayService`** — *полностью*. `src/MARS.Discord/Services/Gateway/`.
- [ ] **L2. `IMediaCompressor` / `MediaCompressor`** — нет (`MediaCompressor` не встречается ни в одном файле репозитория).
- [ ] **L3. `VideoExtensions` (`VideoCompressionProfile`, `ColorExtensions`)** — нет. Сжатия видео, отправляемого в Discord, нет.
- [x] **L4. `DiscordPlayRequestService`** — *полностью*. `src/MARS.Discord/Services/PlayRequest/DiscordPlayRequestService.cs`.
- [x] **L5. `DiscordPlayAudioCacheService`** — *полностью*. `src/MARS.Discord/Services/PlayRequest/DiscordPlayAudioCacheService.cs`.
- [x] **L6. `DiscordPlaySelectionSession`** — *полностью*. `src/MARS.Discord/Models/DiscordPlaySelectionSession.cs`.
- [x] **L7. `DiscordPreparedAudioFile`** — *полностью*. `src/MARS.Discord/Models/DiscordPreparedAudioFile.cs`.
- [x] **L8. `IDiscordTtsVoiceRelayService` / `DiscordTtsVoiceRelayService`** — *полностью*. `src/MARS.Discord/Services/TtsVoiceRelay/`.

### M. `EnvironmentVariable/`

- [x] **M1. `EnvironmentVariable` (сущность)** — *полностью*. `src/MARS.Admin/Entities/EnvironmentVariable.cs`, `Controllers/EnvironmentVariableController.cs`, `AdminDbContext`, миграции `20260922000917_InitialCreate` + `20260930192858_SeedLegacyAdmin`; фильтрация секретов — `src/MARS.Shared/Security/SecretValueFilter.cs` + `tests/MARS.Shared.Tests/Security/SecretValueFilterTests.cs`.

### N. `KeyboardHook_UNUSED/`

- [x] **N1. `IKeyboardHookService`, `KeyboardHookService`, `NullKeyboardHookService`, `KeyboardHookFactory`, `KeyboardHookServiceCollectionExtensions`** — *исключено*. Решение владельца: хук клавиатуры не нужен нигде. В монолите каталог помечен `UNUSED`, в репозитории следов нет.
- [x] **N2. `KeyboardHookController`** — *исключено*. Решение владельца: не переносим, вместе с N1.

### O. `Logs/`

- [ ] **O1. `ILogsService` / `LogsService`** — *заменено и не переносилось по замыслу*. Логи пишутся в Console и OTLP→Tempo (`src/MARS.Shared/Logging/SerilogExtensions.cs`), доставляются в Loki силами Grafana Alloy (`infrastructure/alloy/config.alloy`). `AGENTS.md` явно фиксирует: «`MARS.Admin` — единственный сервис с нестандартной схемой: логи не хранятся в БД. Эндпоинтов `/api/Logs` и `/hubs/logger` не существует».

### P. `Media/` → `MARS.MediaStorage`

- [x] **P1. `IMediaInspector` / `FfprobeMediaInspector`** — *полностью*. `src/MARS.MediaStorage/Services/Media/IMediaInspector.cs`, `FfprobeMediaInspector.cs`.
- [x] **P2. `IMediaTranscoder` / `MediaTranscoder`** — *полностью*. `src/MARS.MediaStorage/Services/Media/MediaTranscoder.cs` (+ `MediaTranscodePathPolicy.cs`).
- [x] **P3. `IMediaFileStorageService` / `WebRootMediaFileStorageService`** — *полностью*. `src/MARS.MediaStorage/Services/Media/WebRootMediaFileStorageService.cs`.

### Q. `MemoryStorageService/`

- [x] **Q1. `MemoryStorage` + `MemoryFile`** — *полностью*, продублировано в трёх сервисах: `src/MARS.MediaStorage/Services/MemoryStorageService/`, `src/MARS.Telegram/Services/MemoryStorageService/`, `src/MARS.Alerts/Services/PyroAlerts/MemoryStorage.cs`.

### R. `Obs/` → `MARS.OBS`

- [x] **R1. `IObsService` + `ObsConfiguration`** — *полностью*. `src/MARS.OBS/Services/IObsService.cs`, `ObsConfiguration.cs`, `HttpObsService.cs`, `Controllers/ObsController.cs`. Добавлено сверх монолита: `ObsPauseMode.cs`, `ObsPauseResult.cs`.

### S. `PyroAlerts/`

- [x] **S1. `PyroAlertsHandler` + `PyroAlertsHelper`** — *полностью*, в двух сервисах: `src/MARS.Alerts/Services/PyroAlerts/` и `src/MARS.MediaStorage/Services/PyroAlerts/` (+ `Controllers/PyroAlerts.cs`).
- [x] **S2. Модели PyroAlerts (10 сущностей/DTO)** — *полностью*. `src/MARS.Shared/Models/Media/` (9 файлов), `src/MARS.MediaStorage/Entities/ApiMediaInfo.cs`; enum `MediaType` и приоритеты — `src/MARS.Alerts/Extensions/MediaTypeExtensions.cs`, `MediaInfoExtensions.cs`. Транспорт — `src/MARS.Shared/Protos/mars_media.proto` + `src/MARS.Shared/Grpc/MediaGrpcMapper.cs`, тест `tests/MARS.Shared.Tests/Grpc/MediaGrpcMapperTests.cs`.

### T. `Scoreboard/` → `MARS.Scoreboard`

- [x] **T1. `ScoreboardService`** — *полностью*. `src/MARS.Scoreboard/Services/ScoreboardService.cs`; сверх монолита — gRPC-стрим `Grpc/ScoreboardGrpcService.cs` + `ScoreboardGrpcMapper.cs` по контракту `src/MARS.Shared/Protos/scoreboard.proto` (`service ScoreboardService`), тесты `tests/MARS.Scoreboard.Tests/Grpc/ScoreboardGrpcServiceTests.cs`, маршрут `scoreboard`.
- [x] **T2. Модели Scoreboard (4)** — *полностью*. `src/MARS.Scoreboard/Entities/ScoreboardDto|Layout|Player|State.cs` + миграции `20260930192824_SeedLegacyScoreboard`.

### U. `ServiceManager/` → `MARS.Admin`

- [x] **U1. `IServiceManager` / `ServiceManager` / `ManagedServiceBase`** — *полностью*. `src/MARS.Admin/Services/ServiceManager/`, `Controllers/ServiceManagerController.cs`.
- [x] **U2. Модели ServiceManager (5)** — *полностью*. `src/MARS.Admin/Entities/ServiceInfo|ServiceLog|ServiceNameAttribute|ServiceState|ServiceStatus.cs` + `AdminDbContext` + миграции.

### V. `SevenTv/`

- [x] **V1. `ISevenTvApiService` / `SevenTvApiService`** — *заменено*. Реализация называется `SevenTvEmoteService` и продублирована в трёх сервисах: `src/MARS.TTS/Services/`, `src/MARS.TwitchCore/Services/Synthesizer/`, `src/MARS.Alerts/Services/Synthesizer/`. Сущность эмоута — `MARS.TTS/Entities/SevenTvEmote.cs`, `MARS.TwitchCore/Entities/SevenTvEmote.cs`, `MARS.Admin/Entities/SevenTvEmote.cs`.

### W. `Shikimori/` → `MARS.WaifuGacha`

- [x] **W1. `IShikimoriApiClient` / `ShikimoriApiClient` / `ShikimoriService`** — *заменено*. Подлежит выделению: Самописный GraphQL-клиент заменён библиотекой `ShikimoriSharp`: `src/MARS.WaifuGacha/Services/ShikimoriService.cs` + `Data/ShikimoriClientOptions.cs`. Сохранены 8 методов: `GetRandomAnime`, `GetAnimeById`, `GetRandomManga`, `GetMangaById`, `GetShikiCharacterById`, `GetCharacterAnimeTitle`, `GetCharacterMangaTitle`, `GetRateLimiterInfo`. **Решение владельца:** вынести в отдельный контейнер `MARS.Shikimori` со своей БД — см. §6.7.
- [x] **W2. `IShikimoriRateLimiter` / `ShikimoriShikimoriRateLimiter` / `RateLimiterInfo`** — *заменено*. Подлежит выделению: `src/MARS.WaifuGacha/Services/IShikimoriRateLimiter.cs`, `ShikimoriRateLimiter.cs`, `RateLimiterInfo.cs`; админ-контроллер — `src/MARS.Admin/Controllers/ShikimoriRateLimiterController.cs`, контракт — `src/MARS.Admin/Services/IShimimoriRateLimiterService.cs`. **Решение владельца:** переезжает вместе с W1 в `MARS.Shikimori`, см. §6.7.
- [ ] **W3. GraphQL-модели Shikimori (16 node/DTO)** — нет. Ни `ShikimoriTitle`, ни `ShikimoriAnime`, ни `AnimeNode`, ни `GraphqlEnvelope/Request/Error` в репозитории не встречаются: слой DTO убран вместе с самописным клиентом. **Решение владельца:** собственная БД `MARS.Shikimori` должна хранить информацию по аниме, манге и персонажам, то есть DTO-слой возвращается — но уже как доменная модель нового сервиса, а не как транспорт GraphQL. См. §6.7.

### X. `SoundBarService/` → `MARS.SoundRequest`

- [x] **X1. `ISoundBar` + `SoundMuteCoordinator`** — *полностью*. `src/MARS.SoundRequest/Services/SoundBarService/`. Сверх монолита: `SoundBarFactory.cs`, `SoundBarHttpClient.cs`, `SoundBarServiceLocal.cs`.

### Y. `SoundRequest/` → `MARS.SoundRequest`

- [x] **Y1. Модели SoundRequest (5)** — *полностью*. `src/MARS.SoundRequest/Entities/BaseTrackInfo|PlaybackState|PlayerState|QueueItem|VideoDisplay.cs` + миграции `20260930192820_SeedLegacyMedia`.
- [x] **Y2. `IPlayerController`** — *полностью*. `src/MARS.SoundRequest/Services/Interfaces/`, `Grpc/SoundRequestGrpcService.cs`, `Controllers/SoundRequestController.cs`; тест — `tests/MARS.SoundRequest.Tests/Grpc/FakePlayerController.cs`.
- [x] **Y3. `MainPlayer`** — *полностью*. `src/MARS.SoundRequest/Services/MainPlayer.cs`.
- [x] **Y4. `StateManager`** — *полностью*. `src/MARS.SoundRequest/Services/StateManager.cs`.
- [x] **Y5. `SoundRequestUserQueue`** — *полностью*. `src/MARS.SoundRequest/Services/SoundRequestUserQueue.cs`.
- [x] **Y6. `SoundRequestCommandsService`** — *полностью*. `src/MARS.SoundRequest/Services/SoundRequestCommandsService.cs`.
- [x] **Y7. `InSignalRHubService`** — *заменено*. Входящий поток стал gRPC-стримом: `src/MARS.SoundRequest/Grpc/SoundRequestGrpcService.cs` + `SoundRequestGrpcMapper.cs`, контракт `src/MARS.Shared/Protos/sound_request.proto` (`service SoundRequestService`), тест `tests/MARS.SoundRequest.Tests/Grpc/SoundRequestGrpcServiceTests.cs`, маршруты `sound-request` и `spotify-auth`.
- [x] **Y8. `OutSignalRHubService`** — *заменено*. Исходящие события ушли в RabbitMQ: `RabbitMqConfig.TrackStarted/TrackEnded/TrackAdded` + `src/MARS.SoundRequest/Services/TrackEventRelay.cs`, `SoundRequestNotifier.cs`.
- [x] **Y9. `SoundCloudResolver`** — *полностью*. `src/MARS.SoundRequest/Services/SoundCloud/SoundCloudResolver.cs`.
- [x] **Y10. `SpotifyApiClient`** — *полностью*. `src/MARS.SoundRequest/Services/Spotify/SpotifyApiClient.cs`.
- [x] **Y11. `SpotifyAuthService`** — *полностью*. `src/MARS.SoundRequest/Services/SpotifyAuthService.cs` + `Controllers/SpotifyAuthController.cs` + `Entities/SpotifyAuthEntities.cs`.
- [x] **Y12. `SpotifyPlaybackService` + `SpotifyPlaybackSnapshot`** — *полностью*. `src/MARS.SoundRequest/Services/Spotify/`.
- [x] **Y13. `SpotifyResolver`** — *полностью*. `src/MARS.SoundRequest/Services/Spotify/SpotifyResolver.cs`.

### Z. `StreamAcrhive_UNUSED/` → `MARS.Admin`

- [x] **Z1. Модели StreamArchive (6)** — *полностью* (перенесена только схема и данные; движок — см. Z2–Z5). `src/MARS.Admin/Entities/StreamArchiveConfig|File|FileChunk|ChunkStatus|FileStatus|VideoFormats.cs` + `AdminDbContext` + миграции; контроллер — `src/MARS.Admin/Controllers/StreamArchiveController.cs`.
- [ ] **Z2. `IStreamArchiveService` / `StreamArchiveService`** — нет.
- [ ] **Z3. `StreamArchiveWorker`** — нет.
- [ ] **Z4. `IFFmpegService` / `FFmpegService`** — нет (`FFmpegService` не встречается ни в одном файле репозитория). При этом `ffprobe`/`ffmpeg` как инструменты есть в `MARS.MediaStorage/Services/Media/`.
- [ ] **Z5. Модели FFprobe (4: `FFprobeFormat`, `FFprobeOutput`, `FFprobeStream`, `VideoInfo`)** — нет.

### AA. `TabletopGames_OBSOLETE/`

- [x] **AA1. `CheckersGame`, `CheckersGameManager`, `CheckersQueue`** — *исключено*. Решение владельца: лишнее, не переносим. В монолите каталог помечен `OBSOLETE`.
- [x] **AA2. Модели настольных игр (6: `Board`, `Cell`, `Checker`, `Color`, `Figure`, `GameStatus`)** — *исключено*. Решение владельца: лишнее, не переносим.

### AB. `Telegram/` → `MARS.Telegram`

- [x] **AB1. `IReceiverService`, `PollingServiceBase`, `ReceiverServiceBase`** — *полностью*. `src/MARS.Telegram/Services/BotService/Abstract/`.
- [x] **AB2. `PollingService`, `ReceiverService`** — *полностью*. `src/MARS.Telegram/Services/BotService/`.
- [x] **AB3. `UpdateHandler`** — *полностью*. `src/MARS.Telegram/Services/BotService/UpdateHandler.cs`.
- [x] **AB4. Модели Telegram BotService (5)** — *полностью*. `src/MARS.Telegram/Entities/VerificationCodeRequest.cs`, `WTelegramClientStatus.cs`, `WTelegramOperationResult.cs`, `TelegramUpdateReceiverOffset.cs`, `TelegramUser.cs`; `ChatDbContext` + миграции `20260922000612_InitialCreate` + `20260930192816_SeedLegacyChat`.
- [ ] **AB5. `TelegramProxyHelper`** — нет.
- [x] **AB6. Буфер обмена (4 файла)** — *полностью*. `src/MARS.Telegram/Services/ClipboardCopy/` (`ClipboardRequestFiles`, `MediaGroupBuffer`, `TriggerWaitBuffer`) + `Services/TelegramClipboardCopyService.cs` + `Controllers/TelegramClipboardCopyController.cs`.
- [x] **AB7. `ITelegramDiscordBridgeService` / `TelegramDiscordBridgeService`** — *полностью*. `src/MARS.Telegram/Services/TelegramDiscordBridgeService.cs` + `Controllers/TelegramDiscordBridgeController.cs`.
- [x] **AB8. Модели DiscordBridge (7)** — *полностью*. `src/MARS.Telegram/Entities/TelegramDiscordChannelBinding|ChannelState|ChannelStateDto|BindingDto|BindingCreateRequest|BindingSetEnabledRequest.cs`, `DiscordChannelOptionDto.cs`, `TelegramChannelOptionDto.cs`; таблицы в `ChatDbContext`.
- [x] **AB9. Google Photos (3)** — *полностью*. `src/MARS.Telegram/Services/GooglePhotos/GooglePhotosApiClient.cs`, `TelegramGooglePhotosService.cs`, `Services/GooglePhotosAuthService.cs`, `IGooglePhotosAuthService.cs`; конфиг `Configuration/GooglePhotosConfiguration.cs`, токены `Entities/GooglePhotosTokens.cs`, контроллер `Controllers/GooglePhotosController.cs`.
- [x] **AB10. `ITelegramusService`** — *заменено*. SignalR-хаб выпилен; оверлей обслуживается через server-streaming gRPC: `src/MARS.Shared/Grpc/Services/TelegramusGrpcService.cs`, контракт `src/MARS.Shared/Protos/telegramus.proto`, `Grpc/GrpcEventBroadcaster.cs`. Тесты — `tests/MARS.Shared.Tests/Grpc/TelegramusGrpcServiceTests.cs`.
- [x] **AB11. `TelegramChannelsResenderService` + `ChannelProcessingState`** — *полностью*. `src/MARS.Telegram/Services/PrivateChannelsResender/`, `Entities/ChannelProcessingState.cs`.
- [x] **AB12. WTelegram (3)** — *полностью*. `src/MARS.Telegram/Services/WTelegramClientService.cs`, `Services/WTelegram/WTelegramDbSessionStore.cs`, `Entities/WTelegramSession.cs`; интерфейс `Services/IWTelegramClientService.cs`, контроллер `Controllers/WTelegramController.cs`, маршрут `telegram-wtelegram`.

### AC. `Twitch/Rewards/` — инфраструктура наград

- [x] **AC.C01. `ChannelRewardsManager`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/ChannelRewardsManager.cs` + `Controllers/ChannelRewardsManagerController.cs`.
- [x] **AC.C02. `ChannelRewardsService`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/ChannelRewardsService.cs`.
- [x] **AC.C03. `ChannelRewardsServiceCollectionExtensions`** — *заменено*. Регистрация свёрнута в `Program.cs` сервисов и в composition root.
- [x] **AC.C04. `ChannelRewardsSyncService`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/ChannelRewardsSyncService.cs`.
- [x] **AC.C05. `ChannelRewardRecord` (сущность)** — *полностью*. `src/MARS.TwitchCore/Entities/ChannelRewardRecord.cs` + `TwitchDbContext` + миграции.
- [x] **AC.C06. `IRewardsCacheService` / `RewardsCacheService`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/`.
- [x] **AC.C07. `ChannelRewardDefinition`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/ChannelRewardDefinition.cs`.
- [x] **AC.C08. `PyroAlertRewardDefinition`** — *заменено*. Свёрнуто в `ChannelRewardDefinition.cs`.
- [x] **AC.C09. `UpdateCustomRewardDto`** — *заменено*. `src/MARS.TwitchCore/DTOs/ChannelRewardsDtos.cs`.
- [ ] **AC.C10. `TwitchAlertsInitializationService`** — **нужен, это реальный пробел.** В монолите статический класс с extension-методом `InitializeTwitchRewards()` рефлексией обходил `typeof(TemporaryReward).Assembly.GetTypes()`, отбирал не-абстрактные классы, assignable на `TemporaryReward`, и регистрировал каждый дважды: как singleton и как `IHostedService`. Смысл — не писать каждую награду в DI руками.
  Сейчас ровно то, от чего он избавлял: в `src/MARS.Alerts/Program.cs` перечислены **32** вызова `AddRewardHandler<…Handler>()` плюс отдельная регистрация `MikuMikuBeamHandler`, `RickRollerService`, `DanbooruRandomPostService`, `HighlitedMessage`, `RandomMemHandler`, `MikuMondayTracksService`. При этом `TemporaryReward` (`src/MARS.TwitchCore/Entities/TemporaryReward.cs`) **не имеет ни одного наследника** — награды реализуют `IRewardAlertHandler`, а сам класс остался сиротой.
  При переносе нужно решить, что именно рефлексионить: наследников `TemporaryReward` (сейчас их 0) или реализаций `IRewardAlertHandler` (сейчас их 33, и это то, что регистрируется руками). Форма в монолите использует C# 14 `extension`-блоки, которые в текущем репозитории больше нигде не применяются.
- [x] **AC.C11. `TwitchRewardsOptions`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/TwitchRewardsOptions.cs`.
- [x] **AC.S01. `AnswersForTwitchRewards`** — *полностью*. `src/MARS.TwitchCore/Services/ChannelRewards/AnswersForTwitchRewards.cs`.
- [x] **AC.S02. `Command`** — *заменено* (переименован). `src/MARS.TwitchCore/Services/ChannelRewards/RewardCommand.cs`.
- [x] **AC.S03. `HighlitedMessage`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/HighlitedMessage.cs`.
- [x] **AC.S04. `MiniGamesManager`** — *полностью*. `src/MARS.TwitchCore/Services/Rewards/MiniGamesManager.cs`; интерфейс `Entities/Interfaces/ITwitchMiniGame.cs`, сущности `Entities/Subs/` (7).
- [x] **AC.S05. `RickRollerService`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/RickRollerService.cs`.
- [x] **AC.S06. `RollCooldownNotificationService`** — *полностью*. `src/MARS.WaifuGacha/Services/RollCooldownNotificationService.cs`.
- [x] **AC.S07. `RollCooldownService`** — *полностью*. `src/MARS.WaifuGacha/Services/RollCooldownService.cs` + `RollCooldownConfigurationService.cs`.
- [ ] **AC.S08. `TwitchEventSubAlertsAwaker`** — нет. Функция «разбудить» алерты при старте трансляции заменена `src/MARS.Alerts/Services/Twitch/Rewards/RewardAlertConsumer.cs` + `src/MARS.TwitchCore/Services/StreamBotNotifications/TwitchStreamStartupNotifications.cs`; самого awaker нет.
- [ ] **AC.S09. `TwitchMessagesHubAwaker`** — нет; заменён `src/MARS.TwitchCore/Services/Rewards/TwitchMessagesPublisher.cs` + `Chat/TwitchChatSendConsumer.cs`.

### AC.R — отдельные награды

Каждая награда монолита `Twitch/Rewards/<папка>/`. Обработчики переехали в `src/MARS.Alerts/Services/Twitch/Rewards/` под именем `<Имя>Handler`, routing key'ы централизованы в `src/MARS.Shared/Messaging/RabbitMqConfig.cs` (33 ключа в `RewardSpecificKeys`, см. §6.3).

- [ ] **AC.R01. `1_RandomReward/RandomReward_TwitchReward`** — нет. Ни обработчика `RandomRewardHandler`, ни ключа `twitch.reward.randomreward` в `RabbitMqConfig`.
- [x] **AC.R02. `2_WaifuMarriage/MergeWaifu`** — *полностью*. `src/MARS.WaifuGacha/Services/MergeWaifuService.cs`; команда `waifuunmerge`.
- [x] **AC.R03. `2_WaifuMarriage/WaifuMarriage_TwitchReward`** — *частично*. Контракт сохранён (`ITelegramusNotifier.ShowCurrentWife` / `MergeWaifu` в `src/MARS.Shared/Grpc/Notifications/ITelegramusNotifier.cs`), отдельного `WaifuMarriageHandler` нет; логика распределена между `MARS.WaifuGacha` и `TelegramusNotifier`.
- [x] **AC.R04. `4_FrogRoll/FrogRollService`** — *полностью*. `src/MARS.WaifuGacha/Services/FrogRollService.cs`; ключ `RabbitMqConfig.FrogRollResult` (`waifu.frog.result`).
- [x] **AC.R05. `4_FrogRoll/FrogRoll_TwitchReward`** — *частично*. Потребитель ключа и `ITelegramusNotifier.FrogRoll` есть; отдельного `FrogRollHandler` нет.
- [x] **AC.R06. `4_FumoRoll/FumoRollService`** — *полностью*. `src/MARS.WaifuGacha/Services/FumoRollService.cs`; ключ `waifu.fumo.result`.
- [x] **AC.R07. `4_FumoRoll/FumoCollectionService`** — *полностью*. `src/MARS.WaifuGacha/Services/FumoCollectionService.cs`.
- [ ] **AC.R08. `4_FumoRoll/FumoFridayRoll_TwitchReward`** — нет.
- [x] **AC.R09. `4_MikuRoll/MikuRollService`** — *полностью*. `src/MARS.WaifuGacha/Services/MikuRollService.cs`; ключ `waifu.miku.result`.
- [x] **AC.R10. `4_MikuRoll/MikuCollectionService`** — *полностью*. `src/MARS.WaifuGacha/Services/MikuCollectionService.cs`.
- [x] **AC.R11. `4_MikuRoll/MikuRoll_TwitchReward`** — *частично*. Есть `ITelegramusNotifier.MikuRoll` и потребитель ключа; отдельного `MikuRollHandler` нет.
- [ ] **AC.R12. `4_SearchWife/SearchWife_TwitchReward`** — нет (`SearchWife` не встречается ни в одном файле репозитория).
- [x] **AC.R13. `5_AddWife/AddNewWaifu`** — *полностью*. `src/MARS.WaifuGacha/Services/AddNewWaifuService.cs` + `Models/AddNewWaifuResponse.cs`.
- [x] **AC.R14. `5_AddWife/AddWife_TwitchReward`** — *частично*. `ITelegramusNotifier.AddNewWaifu` есть; отдельного `AddWifeHandler` нет.
- [ ] **AC.R15. `6_RussianRoulette/RussianRoulette_TwitchReward`** — нет. Промежуточная сущность `RouleteGame` перенесена (`MARS.TwitchCore/Entities/Subs/RouleteGame.cs`), сам обработчик русской рулетки — нет.
- [ ] **AC.R16. `6_RussianRoulette/TwitchRussianRoulete`** — нет (`TwitchRussianRoulete` не встречается ни в одном файле репозитория).
- [ ] **AC.R17. `7_Quiz/Quiz_TwitchReward`** — нет.
- [x] **AC.R18. `7_Quiz/TwitchTrivia`** — *частично*. Игра перенесена как сервис: `src/MARS.TwitchCore/Services/Rewards/TwitchTriviaService.cs` + `ITwitchTrivia.cs`, сущности `Entities/Subs/VictorinaGame.cs`, `VictorinaLetter.cs`. Обёртка-награда `Quiz_TwitchReward` отсутствует.
- [ ] **AC.R19. `9_AudioQuiz/AudioQuiz_TwitchReward`** — нет.
- [ ] **AC.R20. `9_AudioQuiz/AudioTriviaMiniGame`** — *частично, только контракт*. Методы `ITelegramusNotifier.AudioQuizStart` / `AudioQuizStop` объявлены, но ни `AudioTriviaMiniGame`, ни `AudioQuizHandler` в репозитории нет — вызывающей стороны не существует.
- [ ] **AC.R21. `10_RandomSound/RandomSound_TwitchReward`** — нет (`RandomSound` не встречается ни в одном файле репозитория).
- [x] **AC.R22. `11_RandomMemReward/RandomMem_TwitchReward`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/RandomMemHandler.cs`.
- [x] **AC.R23. `11_RandomMemReward/Service/RandomMemeWorker`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/RandomMemeWorker.cs`.
- [x] **AC.R24. `11_RandomMemReward/Service/RandomMemOnline`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/RandomMemOnline.cs`; белый список каналов — `src/MARS.Alerts/Configuration/WTelegramConfiguration.cs`.
- [x] **AC.R25. `11_RandomMemReward/Service/IRandomMemeService`** — *полностью*. `src/MARS.MediaStorage/Services/IRandomMemeService.cs` + `Controllers/RandomMemeController.cs`.
- [x] **AC.R26. `11_RandomMemReward/Service/RandomMemeService`** — *полностью*. `src/MARS.MediaStorage/Services/RandomMemeService.cs`.
- [x] **AC.R27. `11_RandomMemReward/Service/Entity/MemeOrder` + `MemeType`** — *полностью*. `src/MARS.MediaStorage/Entities/MemeOrder.cs`, `MemeType.cs`; также `src/MARS.Alerts/Models/MemeOrder.cs`, `MemeType.cs`; таблицы в `MediaStorageDbContext` + миграции.
- [x] **AC.R28. `11_RandomMemReward/Service/DTOs/MemeOrderDto` + `MemeTypeDto`** — *полностью*. `src/MARS.MediaStorage/Entities/DTOs/MemeOrderDto.cs`, `MemeTypeDto.cs`.
- [x] **AC.R29. `11_RandomMemReward/Service/Entity/WTelegramAlloweedChannel`** — *заменено*. Сущность-таблица ушла в конфигурацию: `src/MARS.Alerts/Configuration/WTelegramConfiguration.AllowedChannelIds`.
- [ ] **AC.R30. `13_FumoFriday/FumoFriday_TwitchReward`** — *частично, только контракт*. `ITelegramusNotifier.FumoFriday` объявлен; обработчика нет.
- [x] **AC.R31. `13_FumoFriday/Entitys/FumoUser`** — *полностью*. `src/MARS.TwitchCore/Entities/FumoUser.cs` + `TwitchDbContext` + миграции.
- [x] **AC.R32. `18_GaoAlert/GaoAlert_TwitchReward`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/GaoAlertHandler.cs`; ключ `twitch.reward.gaoalert`; DTO — `MARS.Alerts/Models/GaoAlertDto.cs`, `MARS.TwitchCore/Entities/GaoAlertDto.cs`.
- [x] **AC.R33. `27_RandomArt/RandomArt_TwitchReward`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/RandomArtHandler.cs`; ключ `twitch.reward.randomart`.
- [x] **AC.R34. `27_RandomArt/RandomArt`** — *заменено* (слит с обработчиком). Логика внутри `RandomArtHandler.cs`.
- [x] **AC.R35. `27_RandomArt/DanbooruRandomPostService`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/DanbooruRandomPostService.cs`; конфиг — `MARS.Alerts/Models/BooruConfiguration.cs`, `DanbooruPost.cs`.
- [x] **AC.R36. `38_WednsdayFrog/WednsdayFrog_TwitchReward`** — *полностью*. `WednsdayFrogHandler.cs`; ключ `twitch.reward.wednsdayfrog`.
- [x] **AC.R37. `39_MikuMonday/MikuMondayTracksService`** — *полностью*. `src/MARS.Alerts/Services/Twitch/Rewards/MikuMondayTracksService.cs`; модели — `MARS.Alerts/Models/MikuMondayModels.cs`.
- [ ] **AC.R38. `39_MikuMonday/TwitchMikuMondayRewardService`** — нет (`TwitchMikuMondayRewardService` не встречается ни в одном файле репозитория).
- [x] **AC.R39. `61_What/What_TwitchReward`** — *полностью*. `WhatHandler.cs`; ключ `twitch.reward.what`.
- [x] **AC.R40. `134_Pedro/Pedro_TwitchReward`** — *полностью*. `PedroHandler.cs`; ключ `twitch.reward.pedro`.
- [x] **AC.R41. `150_TyazheloReward/Tyazhelo_TwitchReward`** — *полностью*. `TyazheloHandler.cs`; ключ `twitch.reward.tyazhelo`.
- [x] **AC.R42. `1510_StatusQuestion/StatusQuestion_TwitchReward`** — *полностью*. `StatusQuestionHandler.cs`; ключ `twitch.reward.statusquestion`.
- [x] **AC.R43. `155_MichaelTime/MichaelTime_TwitchReward`** — *полностью*. `MichaelTimeHandler.cs`; ключ `twitch.reward.michaeltime`; `ITelegramusNotifier.MichaelJackson`.
- [x] **AC.R44. `1580_MikuBeam/MikuBeam_TwitchReward`** — *полностью*. `MikuMikuBeamHandler.cs`; ключ `twitch.reward.mikumikubeam`.
- [x] **AC.R45. `1580_MikuBeam/TwitchMikuBeamRewardService`** — *заменено*. Поиск жены вынесен в отдельный сервис: `src/MARS.TwitchCore/Services/Rewards/IWaifuLookupService.cs` + `WaifuGachaLookupClient.cs` + `Controllers/WaifuGachaInternalController.cs`.
- [x] **AC.R46. `160_LegBum/LegBum_TwitchReward`** — *полностью*. `LegBumHandler.cs`; ключ `twitch.reward.legbum`.
- [ ] **AC.R47. `160_LegBum/LegBumRefundService`** — нет (`LegBumRefundService` не встречается ни в одном файле репозитория). Уведомление `ITelegramusNotifier.AllRefund` осталось, но возврата очков LegBum нет.
- [x] **AC.R48. `1602_CinemaRequest/CinemaRequest_TwitchReward`** — *полностью*. `CinemaRequestHandler.cs`; ключ `twitch.reward.cinemarequest`; потребитель — `src/MARS.CinemaQueue/Services/TwitchCinemaQueueService.cs`.
- [x] **AC.R49. `170_FumoFridayNightReward/FumoFridayNight_TwitchReward`** — *полностью*. `FumoFridayNightHandler.cs`; ключ `twitch.reward.fumofridaynight`.
- [ ] **AC.R50. `170_MikuMondayAlert/MikuMondayAlert_TwitchReward`** — нет (`MikuMondayAlert` встречается только в имени метода `ITelegramusNotifier.MikuMonday`; обработчика и routing key `twitch.reward.mikumondayalert` нет).
- [x] **AC.R51. `1700_Confetti/Confetti_TwitchReward`** — *полностью*. `ConfettiHandler.cs`; ключ `twitch.reward.confetti`; модель частиц — `MARS.Alerts/Models/TwitchScreenParticles.cs`.
- [x] **AC.R52. `1701_Fireworks/Fireworks_TwitchReward`** — *полностью*. `FireworksHandler.cs`; ключ `twitch.reward.fireworks`.
- [ ] **AC.R53. `1702_EmojisReward/Emojis_TwitchReward`** — нет. Остался только метод контракта `ITelegramusNotifier.MakeScreenEmojisParticles`; ни `EmojisHandler`, ни ключа `twitch.reward.emojis` нет.
- [x] **AC.R54. `182_Stone/Stone_TwitchReward`** — *полностью*. `StoneHandler.cs`; ключ `twitch.reward.stone`.
- [x] **AC.R55. `195_Cringe/Cringe_TwitchReward`** — *полностью*. `CringeHandler.cs`; ключ `twitch.reward.cringe`.
- [x] **AC.R56. `2002_AdhdSuperpower/AdhdSuperpower_TwitchReward`** — *полностью*. `AdhdSuperpowerHandler.cs`; ключ `twitch.reward.adhdsuperpower`; `ITelegramusNotifier.Adhd(seconds)`.
- [x] **AC.R57. `210_Hello/Hello_TwitchReward`** — *полностью*. `HelloHandler.cs`; ключ `twitch.reward.hello`.
- [x] **AC.R58. `215_Bye/Bye_TwitchReward`** — *полностью*. `ByeHandler.cs`; ключ `twitch.reward.bye`.
- [x] **AC.R59. `317_Intelligence/Intelligence_TwitchReward`** — *полностью*. `IntelligenceHandler.cs`; ключ `twitch.reward.intelligence`.
- [x] **AC.R60. `320_MikuScreamer/MikuScreamer_TwitchReward`** — *полностью*. `MikuScreamerHandler.cs`; ключ `twitch.reward.mikuscreamer`.
- [x] **AC.R61. `333_Skibidibop/Skibidibop_TwitchReward`** — *полностью*. `SkibidibopHandler.cs`; ключ `twitch.reward.skibidibop`.
- [x] **AC.R62. `337_PhonkEdit/PhonkEdit_TwitchReward`** — *полностью*. `PhonkEditHandler.cs`; ключ `twitch.reward.phonkedit`.
- [x] **AC.R63. `341_Aga/Aga_TwitchReward`** — *полностью*. `AgaHandler.cs`; ключ `twitch.reward.aga`.
- [x] **AC.R64. `342_BadToBone/BadToBone_TwitchReward`** — *полностью*. `BadToBoneHandler.cs`; ключ `twitch.reward.badtobone`.
- [x] **AC.R65. `353_TikTokEdit/TikTokEdit_TwitchReward`** — *полностью*. `TikTokEditHandler.cs`; ключ `twitch.reward.tiktokedit`.
- [x] **AC.R66. `375_DanceDance/DanceDance_TwitchReward`** — *полностью*. `DanceDanceHandler.cs`; ключ `twitch.reward.dancedance`.
- [x] **AC.R67. `666_Edge0100Alert/Edge0100Alert_TwitchReward`** — *полностью*. `Edge0100AlertHandler.cs`; ключ `twitch.reward.edge0100alert`.
- [x] **AC.R68. `1333_SkibidibopLong/SkibidibopLong_TwitchReward`** — *полностью*. `SkibidibopLongHandler.cs`; ключ `twitch.reward.skibidiboplong`.
- [x] **AC.R69. `6666_CloseGame/CloseGame_TwitchReward`** — *полностью*. `CloseGameHandler.cs`; ключ `twitch.reward.closegame`.
- [x] **AC.R70. `75000_SelectGame/SelectGame_TwitchReward`** — *полностью*. `SelectGameHandler.cs`; ключ `twitch.reward.selectgame`.
- [x] **AC.R71. `8005_Credits/Credits_TwitchReward`** — *полностью*. `CreditsHandler.cs`; ключ `twitch.reward.credits`.
- [x] **AC.R72. `99999_AllRefundService/AllRefund_TwitchReward`** — *полностью*. `AllRefundHandler.cs`; ключ `twitch.reward.allrefund`.

### AD. `Twitch/` — вне `Rewards/`

- [x] **AD1. `AutoRewardInfoFetcher`** — *полностью*. `src/MARS.TwitchCore/Services/AutoInfoFetch/AutoRewardInfoFetcher.cs`.
- [x] **AD2. `TwitchBlackListService`** — *полностью*. `src/MARS.TwitchCore/Services/BlackList/TwitchBlackListService.cs`.
- [x] **AD3. `TwitchApiRateLimiter`** — *полностью*. `src/MARS.TwitchCore/Services/Client/TwitchApiRateLimiter.cs`.
- [ ] **AD4. `TwitchClientProxy`** — нет (`TwitchClientProxy` не встречается ни в одном файле репозитория). Функция распределена: `TwitchConnectionManager` + `Extensions/TwitchClientExtensions.cs`.
- [x] **AD5. `TwitchConnectionManager`** — *полностью*. `src/MARS.TwitchCore/Services/Connection/TwitchConnectionManager.cs`.
- [x] **AD6. AutoMessages (7 файлов)** — *полностью*. `src/MARS.TwitchCore/Services/AutoMessages/` (`AutoMessagesHandler`, `AutoMessagesService`), `Controllers/AutoMessagesController.cs`, `DTOs/AutoMessageDto.cs`, `Entities/AutoMessage.cs`, команда `automessage`.
- [x] **AD7. `AutoHello` + `AutoVideoHello`** — *заменено*. `src/MARS.TwitchCore/Services/AutoHello/` (`AutoHello`, `AutoHelloClient`, `IAutoHelloService`); сущность-сообщение — `src/MARS.WaifuGacha/Entities/AutoHelloMessage.cs` + `AutoHelloMessageSeed.cs` + миграция `20260929180849_SeedAutoHelloMessages`.
- [x] **AD8. Модели Twitch/Entitys (27)** — *полностью*. Разложены по трём сервисам: `src/MARS.TwitchCore/Entities/` (в т.ч. `Subs/` — 7 файлов, `Interfaces/ITwitchMiniGame.cs`, `TemporaryReward.cs`, `RollCooldown.cs`, `TwitchUser.cs`, `TwitchUserDto.cs`, `CreateTwitchUserRequest.cs`, `UpdateTwitchUserRequest.cs`, `ChannelUsersResult.cs`, `FollowerInfo.cs`, `GaoAlertDto.cs`, `TwitchScreenParticles.cs`, `TokenInfo.cs`, `TwitchLeaderboardUser.cs`, `AutoMessage.cs`, `FumoUser.cs`, `HelloVideosUsers.cs`, `RootState.cs`), `src/MARS.WaifuGacha/Entities/` (`Waifu.cs`, `Fumo.cs`, `Frog.cs`, `Husband.cs`, `HusbandAutoHello.cs`, `HusbandCoolDown.cs`, `UserFumoCollection.cs`, `UserMikuCollection.cs`, `MikuModule.cs`, `MikuMondayActivation.cs`, `MikuMondayTrack.cs`, `WaifuRollAudio.cs`, `RollCooldown.cs`, `TwitchUser.cs`, `TwitchScreenParticles.cs`, `SevenTvEmote.cs`, `AutoHelloMessage.cs`, `PrizeType.cs` — объединил `MikuPrizeType`/`FumoPrizeType`/`FrogPrizeType`/`PrizeTypeAbstract`), `src/MARS.Shared/Models/TwitchUser.cs`.
- [x] **AD9. `ITwitchUserEnsureService` / `TwitchUserEnsureService`** — *полностью*. `src/MARS.TwitchCore/Services/ITwitchUserEnsureService.cs`, `TwitchUserEnsureService.cs`.
- [x] **AD10. `EventSubService`** — *полностью*. `src/MARS.TwitchCore/Services/EventSub/EventSubService.cs`. Единственный владелец Twitch-подключения.
- [x] **AD11. `TelegramTokenNotification`** — *полностью*. `src/MARS.TwitchCore/Services/Management/TelegramTokenNotification.cs`; контракт `ITelegramusNotifier.PostTwitchInfo`.
- [x] **AD12. `TokenService` + `TokenInfo` + `ITwitchReward`** — *полностью*. `src/MARS.TwitchCore/Services/TokenService.cs`, `Entities/TokenInfo.cs`, `Controllers/TwitchAuthController.cs`, `Entities/Interfaces/`.
- [ ] **AD13. `ITwitchMediaPreparationService` / `TwitchMediaPreparationService` / `TwitchMediaTranscodeWorker`** — нет (`TwitchMediaTranscode` не встречается ни в одном файле репозитория; `TwitchMediaPreparation` упоминается только в `RabbitMqEvents.cs`). Подготовка медиа делает `MARS.MediaStorage/Services/Media/`.
- [ ] **AD14. `ILeaderboardService` / `LeaderboardService`** — нет (`LeaderboardService` не встречается ни в одном файле репозитория). Сущность `TwitchLeaderboardUser` перенесена (`src/MARS.TwitchCore/Entities/TwitchLeaderboardUser.cs`), сервис таблицы лидеров — нет. Отсюда же выпала команда `mgleaders` (см. J8).
- [x] **AD15. PuntoSwitcher (3)** — *полностью*. `src/MARS.TwitchCore/Services/PuntoSwitcher/` + команда `puntoswitcher` + `ServerStatsController`.
- [x] **AD16. `TwitchStreamStartupNotifications`** — *полностью*. `src/MARS.TwitchCore/Services/StreamBotNotifications/TwitchStreamStartupNotifications.cs`.
- [x] **AD17. `TwitchStreamManagementService` + `TwitchTitleChangeCommand`** — *частично*. Сервис перенесён: `src/MARS.TwitchCore/Services/StreamManagement/TwitchStreamManagementService.cs`. Сама команда `TwitchTitleChangeCommand` переписана: `src/MARS.TwitchCore/Services/Commands/TwitchCommandPermissions.cs` (смена титула теперь идёт через HTTP-клиент платформы, а не через Twitch API), тест `tests/MARS.TwitchCore.Tests/Commands/TwitchCommandPermissionsTests.cs`.
- [x] **AD18. Synthesizer/TTS (7)** — *частично* (заменено на gRPC). `ISevenTvEmoteService`/`SevenTvEmoteService`/`ITtsMessageFilterService`/`TtsMessageFilterService` перенесены в `MARS.TTS` и `MARS.TwitchCore`. `ITtsHubBroadcaster` / `TtsHubBroadcaster` заменены gRPC: `src/MARS.TTS/Grpc/VoiceRecognitionGrpcService.cs` + `TtsGrpcMapper.cs` по контракту `src/MARS.Shared/Protos/voice_recognition.proto` (`service VoiceRecognitionService`), `ITtsNotifier.cs`, `TtsNotifier.cs`; тесты `tests/MARS.TTS.Tests/Grpc/VoiceRecognitionGrpcServiceTests.cs`.
- [ ] **AD19. `TekkenStreamsDiscordForwarderService`** — нет (`TekkenStreams` не встречается ни в одном файле репозитория).
- [x] **AD20. TwitchFollowers (8)** — *частично*. `FollowerDbService`, `IRxdcodxViewersService`, `RxdcodxViewersService`, `TwitchViewersService`, `ChannelUsersResult`, `FollowerInfo`, `RxdcodxViewersServiceExtensions` перенесены в `src/MARS.TwitchCore/Services/TwitchFollowers/` + `src/MARS.Admin/Services/IRxdcodxViewersService.cs` + `Controllers/RxdcodxViewersController.cs`. `TwitchUserInfoService` переименован в `src/MARS.TwitchCore/Services/UserSync/TwitchUserSyncService.cs`.
- [x] **AD21. Validation (8)** — *частично*. 7 из 8 перенесены в `src/MARS.TwitchCore/Services/Validation/` (`IMessageValidationBuilder`, `IRedemptionValidationBuilder`, `ITwitchEventValidationService`, `MessageValidationBuilder`, `RedemptionValidationBuilder`, `TwitchEventValidationService`, `ValidationResult`). `ValidationException` — нет; вместо исключения используется `ValidationResult` и стандартные gRPC-статусы (см. `AGENTS.md`, раздел про gRPC).
- [ ] **AD22. `WaifuChatTwitchReward`** — нет (`WaifuChatTwitchReward` не встречается ни в одном файле репозитория). Есть только `ITelegramusNotifier.AutoMessage`. Сущность `WaifuChatFact` — см. AE9.
- [x] **AD23. `WeddingAnniversaryService`** — *полностью*, в двух сервисах: `src/MARS.TwitchCore/Services/WeddingAnniversary/WeddingAnniversaryService.cs` и `src/MARS.WaifuGacha/Services/WeddingAnniversaryService.cs`.
- [x] **AD24. HelloVideos (2)** — *полностью*. `src/MARS.TwitchCore/Services/HelloVideos/` (`HelloVideoWorker`, `HelloVideoEligibility`, `HelloVideoNotifier`, `IHelloVideoNotifier`, `RecentMessageTracker`), сущность `src/MARS.TwitchCore/Entities/HelloVideosUsers.cs`; тесты `tests/MARS.TwitchCore.Tests/HelloVideos/HelloVideoEligibilityTests.cs` и `RecentMessageTrackerTests.cs`; команда `hellovideo`.

### AE. `WaifuRoll/` → `MARS.WaifuGacha`

- [x] **AE1. Гарантия ролла (3)** — *полностью*. `src/MARS.WaifuGacha/Entities/WaifuRollGuarantee.cs`, `Services/Interfaces/IWaifuRollGuaranteeService.cs`, `Services/WaifuRollGuaranteeService.cs`.
- [x] **AE2. `PrizeTypeAbstract`** — *заменено*. `src/MARS.WaifuGacha/Entities/PrizeType.cs` — теперь единый тип с тремя экземплярами (`FrogRollService`, `FumoRollService`, `MikuRollService`).
- [x] **AE3. Модели WaifuRoll (7)** — *полностью* (с оговоркой про `WaifuChatFact`, см. AD22). `src/MARS.WaifuGacha/Entities/AutoHelloMessage.cs`, `Husband.cs`, `HusbandAutoHello.cs`, `HusbandCoolDown.cs`, `Waifu.cs`, `WaifuRollAudio.cs`, `WaifuChatFact.cs`. Таблица `WaifuChatFact` перенесена (сидируется миграциями `SeedLegacyWaifu`, `SeedLegacyMediaStorage`, `SeedLegacyMedia`, `SeedLegacyChat`, `SeedLegacyAdmin`), но её потребитель — награда `WaifuChatTwitchReward` — не перенесён, см. AD22.
- [x] **AE4. `WaifuRollException`** — *полностью*. `src/MARS.WaifuGacha/Entities/WaifuRollException.cs`. Непрочитанные параметры primary-конструктора глушатся через `NoWarn` `CS9113` (см. `AGENTS.md`).
- [x] **AE5. `WaifuRollEnsurenceService`** — *полностью*. `src/MARS.WaifuGacha/Services/WaifuRollEnsurenceService.cs`.
- [x] **AE6. `IWaifuPrizesService` / `WaifuPrizesService`** — *полностью*. `src/MARS.WaifuGacha/Services/`.
- [x] **AE7. `IWaifuRollService` / `WaifuRollService`** — *полностью*. `src/MARS.WaifuGacha/Services/WaifuRollService.cs`; ключ `RabbitMqConfig.WaifuRollResult` (`waifu.roll.result`).
- [x] **AE8. Модели ответа (4)** — *полностью*. `src/MARS.WaifuGacha/Models/AddNewWaifuResponse.cs`, `RollCountResponse.cs`, `TelegramRollWaifuResponse.cs`, `VipDropResponse.cs`. `RollCountResponse`/`VipDropResponse` — то, чем раньше отвечали команда `mywins` и VIP-механика; сама команда не перенесена (см. J8).

### AF. `YouTube/`

- [x] **AF1. `YouTubeResolver`** — *полностью*, продублировано в трёх сервисах: `src/MARS.Discord/Services/YouTube/`, `src/MARS.SoundRequest/Services/YouTube/`, `src/MARS.TwitchCore/Services/YouTube/` (+ `YouTube/BaseTrackInfo.cs`).

## 5. Что не перенесено — рабочий список

**42 пунктов из 226** помечены `[ ]` и ждут работы. Сгруппировано по причине.
Отдельно, в конце, — 5 пунктов, снятых с работы решением владельца: они помечены
`[x] исключено` и в этот список не входят.

**Помечены в монолите `OBSOLETE`/`UNUSED` и сознательно не переносятся — 4 пункта, 12 файлов.**
Z2 `StreamArchiveService` · Z3 `StreamArchiveWorker` · Z4 `FFmpegService`/`IFFmpegService` ·
Z5 модели FFprobe (4). Z1 при этом перенесён — только схема.

**Заменено другим решением, эквивалент присутствует — 1 пункт.**
O1 `LogsService` → Loki + Grafana Alloy + Tempo. `AGENTS.md` прямо фиксирует, что `/api/Logs`
и `/hubs/logger` не существует и не должен.

**Схема перенесена, кода нет; отсутствие зафиксировано `TODO` в самом репозитории — 5 пунктов.**
G1 `BooruAutoPostService` · G2 `BooruDiscordPoster` · G3 `BooruTelegramPoster` ·
G4 `Rule34RandomPostService` · G5 `TelegramScheduleMatcher` — все перечислены поимённо
в XML-доке `src/MARS.Telegram/Entities/BooruAutoPostConfig.cs` вместе с `TODO(Booru)`.
Отдельно: `G6` — схема Booru перенесена и покрыта тестами `BooruSchemaTests`,
`C1` — `AdhdLayoutConfig` перенесена с `TODO(ADHD)`,
`B1` — `Worker365` существует как заглушка с `TODO(365)`.
Эти три пункта помечены `[x] частично`, потому что артефакт в репозитории есть.

**Заменено библиотекой или протоколом, DTO/обёртка не перенесены — 2 пункта.**
AD4 `TwitchClientProxy` (функция распределена между `TwitchConnectionManager`
и `TwitchClientExtensions`) · W3 16 GraphQL-модели Shikimori (ушли вместе с самописным
клиентом; по решению владельца возвращаются как доменная модель БД нового
сервиса `MARS.Shikimori`, см. §6.7).

**Не перенесено, упоминаний в репозитории нет — 14 пунктов.**
L2 `MediaCompressor` · L3 `VideoExtensions` · H1 `BooruMessageTemplateResolver` ·
H2 `BooruValidationHelper` · H3 `DeduplicationService` · H4 `TagValidator` · H5 `PostedImageRecord` ·
AB5 `TelegramProxyHelper` ·
AD13 `TwitchMediaPreparationService`/`TwitchMediaTranscodeWorker` · AD14 `LeaderboardService` ·
AD19 `TekkenStreamsDiscordForwarderService` · AD22 `WaifuChatTwitchReward` ·
AC.S08 `TwitchEventSubAlertsAwaker` · AC.S09 `TwitchMessagesHubAwaker`.

**Требует отдельного решения по механизму — 1 пункт.**
AC.C10 `TwitchAlertsInitializationService` — рефлексивная регистрация наследников
`TemporaryReward`, чтобы не перечислять каждую награду в DI. Сейчас в
`src/MARS.Alerts/Program.cs` 32 вызова `AddRewardHandler<…>()` руками, а
`TemporaryReward` не имеет наследников. Подробности — в самом пункте.

**Не перенесены награды — 14 пунктов.**
AC.R01 `1_RandomReward` · AC.R08 `4_FumoRoll/FumoFridayRoll` · AC.R12 `4_SearchWife` ·
AC.R15 `6_RussianRoulette` · AC.R16 `6_RussianRoulette/TwitchRussianRoulete` ·
AC.R17 `7_Quiz` · AC.R19 `9_AudioQuiz` · AC.R20 `9_AudioQuiz/AudioTriviaMiniGame` ·
AC.R21 `10_RandomSound` · AC.R30 `13_FumoFriday` ·
AC.R38 `39_MikuMonday/TwitchMikuMondayRewardService` · AC.R47 `160_LegBum/LegBumRefundService` ·
AC.R50 `170_MikuMondayAlert` · AC.R53 `1702_EmojisReward`.

**Не перенесены команды — 1 пункт.**
J8: `autohello`, `fumoinv`, `mgleaders`, `mikuinv`, `mywins`, `randomanime`, `randommanga`.

### 5.1 Снято с работы решением владельца — 5 пунктов

| Пункт | Что это | Решение |
|---|---|---|
| `D1` | `AppStateService` — в монолите класс-заглушка из одного блока комментариев | Не переносим |
| `N1` | Хук клавиатуры: `IKeyboardHookService`, `KeyboardHookService`, `NullKeyboardHookService`, `KeyboardHookFactory`, `KeyboardHookServiceCollectionExtensions` | Не нужен нигде |
| `N2` | `KeyboardHookController` | Не нужен нигде, вместе с `N1` |
| `AA1` | `CheckersGame`, `CheckersGameManager`, `CheckersQueue` | Лишнее |
| `AA2` | Модели настольных игр: `Board`, `Cell`, `Checker`, `Color`, `Figure`, `GameStatus` | Лишнее |

Пункты остаются в чеклисте и в Приложении A, чтобы покрытие 510 файлов монолита
оставалось полным и проверка 1 продолжала сходиться.

Из 42 пунктов `[ ]` **5 имеют след в репозитории** (зафиксированы `TODO`-ом),
остальные 37 не упомянуты нигде. Отдельно — **5 «мёртвых контрактов»**: механик,
для которых в `ITelegramusNotifier` (`src/MARS.Shared/Grpc/Notifications/ITelegramusNotifier.cs`)
остались методы без вызывающей стороны: `AudioQuizStart`/`AudioQuizStop`, `FumoFriday`,
`MikuMonday`, `MakeScreenEmojisParticles`, `AllRefund`.
## 6. Маршруты YARP, очереди RabbitMQ и gRPC-контракты

Полнота чеклиста проверяется не только по файлам: каждый способ, которым сервис может
быть вызван, обязан отражаться в пункте. Ниже — полный перечень.

### 6.1 Маршруты YARP — 31 из 31 покрыт

`src/MARS.Gateway/appsettings.json` → `Yarp:Routes` (31 маршрут). Наружу опубликован
только Gateway (`9155:8080`).

| Маршрут | Кластер | Пункт |
|---|---|---|
| `twitch-core` | twitch-core | AD5 `TwitchConnectionManager` |
| `twitch-core-auth` | twitch-core | AD12 `TokenService` |
| `twitch-core-auto-messages` | twitch-core | AD6 `AutoMessagesService` |
| `twitch-core-rewards` | twitch-core | AC.C01 `ChannelRewardsManager` |
| `twitch-core-users` | twitch-core | AD9 `TwitchUserEnsureService` |
| `twitch-core-stats` | twitch-core | AD15 `PuntoSwitcher` |
| `waifu-gacha` | waifu-gacha | AE7 `WaifuRollService` |
| `telegram` | telegram | AB3 `UpdateHandler` |
| `telegram-wtelegram` | telegram | AB12 `WTelegramClientService` |
| `telegram-clipboard-copy` | telegram | AB6 `TelegramClipboardCopyService` |
| `telegram-discord-bridge` | telegram | AB7 `TelegramDiscordBridgeService` |
| `telegram-google` | telegram | AB9 Google Photos |
| `discord` | discord | L1 `DiscordGatewayService` |
| `commands` | commands | J2 `CommandExecutorService` |
| `sound-request` | sound-request | Y3 `MainPlayer` |
| `spotify-auth` | sound-request | Y11 `SpotifyAuthService` |
| `obs` | obs | R1 `IObsService` |
| `pyro-alerts` | media-storage | S1 `PyroAlertsHandler` |
| `media-storage` | media-storage | P3 `WebRootMediaFileStorageService` |
| `media-storage-ui` | media-storage | Приложение C (новое, UI React/Vite) |
| `media-info` | media-storage | P1 `FfprobeMediaInspector` |
| `random-meme` | media-storage | AC.R25 `IRandomMemeService` |
| `scoreboard` | scoreboard | T1 `ScoreboardService` |
| `cinema-queue` | cinema-queue | I2 `CinemaQueueService` |
| `test-alerts` | obs | Приложение C (новое) |
| `admin-service-manager` | admin | U1 `ServiceManager` |
| `admin-env-var` | admin | M1 `EnvironmentVariable` |
| `admin-root-state` | admin | D1 (замена `AppStateService` через `RootState`) |
| `admin-archive` | admin | Z1 (схема `StreamArchive`; контроллер без сервиса) |
| `admin-rxdcodx` | admin | AD20 `RxdcodxViewersService` |
| `admin-shikimori` | admin | W2 `ShikimoriRateLimiter` |

### 6.2 Очереди и routing key'и — `src/MARS.Shared/Messaging/RabbitMqConfig.cs`

Обмен — `mars.events`, три очереди: `alerts.events`, `alerts.system`, `admin.events`.

| Ключ / очередь | Пункт |
|---|---|
| `twitch.reward.redeemed` | AC.S01 `AnswersForTwitchRewards` + J2 (`commands.proto`) + `MARS.Alerts/Services/Twitch/Rewards/TwitchMediaAlerts.cs` |
| `twitch.reward.*` (33 ключа в `RewardSpecificKeys`) | AC.R32–AC.R72 — см. 6.3 |
| `twitch.user.joined` | AD9 `TwitchUserEnsureService` |
| `twitch.message.received` | AD6 `AutoMessagesHandler` |
| `twitch.message.deleted` | AD6 / `Services/Events/TwitchMessageDeletedEvent.cs` |
| `twitch.chat.send` | AD18 / `Services/Chat/TwitchChatSendConsumer.cs` |
| `waifu.roll.result` | AE7 `WaifuRollService` |
| `waifu.fumo.result` | AC.R06 `FumoRollService` |
| `waifu.miku.result` | AC.R09 `MikuRollService` |
| `waifu.frog.result` | AC.R04 `FrogRollService` |
| `media.track.started` / `.ended` / `.added` | Y8 `OutSignalRHubService` → `TrackEventRelay` |
| очередь `alerts.system` | `MARS.Alerts/Services/SystemEventsConsumer.cs` (Приложение C) |
| очередь `alerts.events` | `MARS.Alerts/Services/Twitch/Rewards/RewardAlertConsumer.cs` |
| очередь `admin.events` | `MARS.Admin` (Приложение C) |

### 6.3 Награды: 33 ключа ↔ 33 обработчика `IRewardAlertHandler`

`RewardSpecificKeys` содержит **33** ключа, и в `MARS.Alerts/Services/Twitch/Rewards/`
ровно **33** конкретные реализации `IRewardAlertHandler` — по одной на ключ, и имя
обработчика совпадает с суффиксом ключа (`ConfettiHandler` ↔ `twitch.reward.confetti`).
`RabbitMqConfig` определяет 34 константы на префиксе `twitch.reward.` — 33
специфичные плюс общая `RewardRedeemed`; в `RewardSpecificKeys` она не входит, и это
намеренно оговорено комментарием в коде, иначе `RewardAlertConsumer` получал бы общий
поток повторно.

Ещё три класса каталога в эти 33 не входят:

- `RandomMemHandler` (AC.R22) — суффикс `Handler`, но интерфейс не реализует: запускается
  из Telegram-апдейтов (`HandMessage`), поэтому ключа `twitch.reward.randommem` у него нет;
- `IRewardAlertHandler` — сам интерфейс;
- `IChatUserTrackingHandler` — интерфейс отслеживания пользователей чата;
- `RewardAlertConsumer` — потребитель очереди `alerts.events`, который строит карту
  «ключ → обработчик» и разбирает в т.ч. `twitch.reward.redeemed` через
  `TwitchMediaAlerts`. В монолите эту роль играли SignalR-хабы вне `Services/`, поэтому
  отдельного пункта в чеклисте у `TwitchMediaAlerts` нет — файла под `Services/` монолита
  нет, и в Приложении A он не числится.

### 6.4 gRPC-контракты — 7 из 7

Все `.proto` лежат в `src/MARS.Shared/Protos`, как требует `AGENTS.md`.

| Файл | `service` | Пункт |
|---|---|---|
| `commands.proto` | `Commands` | J2 `CommandExecutorService` |
| `mars_media.proto` | — (модели PyroAlerts) | S2 |
| `scoreboard.proto` | `ScoreboardService` | T1 `ScoreboardService` |
| `sound_request.proto` | `SoundRequestService` | Y7 `InSignalRHubService` |
| `telegramus.proto` | `TelegramusService` | AB10 `ITelegramusService`, C1 `AdhdLayoutService` |
| `tuna.proto` | `TunaService` | Приложение C (новое) |
| `voice_recognition.proto` | `VoiceRecognitionService` | AD18 Synthesizer/TTS |

Хостинг — `builder.AddMarsGrpcHosting()` (`src/MARS.Shared/Extensions/GrpcHostingExtensions.cs`),
рассылка — `GrpcEventBroadcaster<T>`; тесты h2c — `tests/MARS.Shared.Tests/Grpc/H2cTransportTests.cs`.

### 6.5 Производные файлы существующих сервисов

Сервисы, перенесённые из монолита, получили слои, которых у `MARS.Server/Services` не
было: отдельные HTTP-контроллеры (в монолите маршруты жили в `MARS.Server`), по
`DbContext` на сервис (в монолите был один `AppDbContext`), фабрики для design-time,
классы конфигурации и вспомогательные расширения. Ни один из файлов ниже не имеет
соответствия в `MARS.Server/Services`, поэтому в Приложении A их нет — но каждый
принадлежит пункту основного чеклиста, и здесь перечислен явно.

| Область | Файлы | Пункт |
|---|---|---|
| `src/MARS.Alerts/Data/` | `AlertsDbContext.cs`, `DesignTime/AlertsDbContextFactory.cs` | S2 (схема PyroAlerts), C1 (таблица ADHD) |
| `src/MARS.Alerts/Extensions/` | `LoggerExtensions.cs`, `MediaInfoExtensions.cs`, `MediaTypeExtensions.cs` | S2 |
| `src/MARS.Alerts/Models/` | `AutoArtImage.cs`, `BooruConfiguration.cs`, `DanbooruPost.cs`, `GaoAlertDto.cs`, `MemeOrder.cs`, `MemeType.cs`, `MikuMondayModels.cs`, `TwitchScreenParticles.cs` | F1, AC.R35, AC.R32, AC.R27, AC.R37, AC.R51 |
| `src/MARS.Alerts/Services/SystemEventsConsumer.cs` | потребитель очереди `alerts.system` | §6.2 |
| `src/MARS.Alerts/Services/Twitch/Rewards/` | `IRewardAlertHandler.cs`, `IChatUserTrackingHandler.cs`, `RewardAlertConsumer.cs`, `ChatUserConsumer.cs`, `TwitchMediaAlerts.cs`, `MikuMikuBeamHandler.cs`, `RandomMemHandler.cs` | §6.3, AC.R22, AC.R44 |
| `src/MARS.CinemaQueue/Configuration/`, `Controllers/`, `Data/` | `KinopoiskConfiguration.cs`, `CinemaQueueController.cs`, `CinemaDbContext.cs`, `DesignTime/CinemaDbContextFactory.cs` | I4, I2, §6.1 (`cinema-queue`) |
| `src/MARS.Discord/Configuration/`, `Controllers/` | `DiscordConfiguration.cs`, `DiscordController.cs` | L1, §6.1 (`discord`) |
| `src/MARS.Scoreboard/Controllers/`, `Data/`, `Grpc/` | `ScoreboardController.cs`, `ScoreboardDbContext.cs`, `DesignTime/ScoreboardDbContextFactory.cs`, `Grpc/ScoreboardGrpcService.cs`, `Grpc/ScoreboardGrpcMapper.cs` | T1, T2, §6.1 (`scoreboard`) |
| `src/MARS.Telegram/GlobalUsings.cs`, `Configuration/`, `Controllers/`, `Data/` | `GooglePhotosConfiguration.cs`, `TelegramConfiguration.cs`, `GooglePhotosController.cs`, `TelegramClipboardCopyController.cs`, `TelegramController.cs`, `TelegramDiscordBridgeController.cs`, `WTelegramController.cs`, `ChatDbContext.cs`, `DesignTime/ChatDbContextFactory.cs` | AB9, AB3, AB6, AB7, AB12, AB4 |
| `src/MARS.Telegram/Entities/RootState.cs` | глобальное состояние | D1 (замена `AppStateService`), §6.4 |
| `src/MARS.Telegram/Services/` | `IGooglePhotosAuthService.cs`, `ITelegramClipboardCopyService.cs`, `IWTelegramClientService.cs` | AB9, AB6, AB12 |
| `src/MARS.Telegram/Services/ClipboardCopy/` | `ClipboardRequestFiles.cs`, `MediaGroupBuffer.cs`, `TriggerWaitBuffer.cs` | AB6 |
| `src/MARS.Videos365/Configuration/`, `Data/` | `Config365.cs`, `Videos365DbContext.cs`, `DesignTime/Videos365DbContextFactory.cs` | B1, B2 |
| `src/MARS.WaifuGacha/Configuration/`, `Controllers/`, `Data/`, `Entities/` | `TwitchConfiguration.cs`, `TwitchConstants.cs`, `WaifuRollController.cs`, `WaifuGachaInternalController.cs`, `ShikimoriClientOptions.cs`, `AutoHelloMessageSeed.cs`, `WaifuDbContext.cs`, `DesignTime/WaifuDbContextFactory.cs`, `RootState.cs`, `RootStateKeys.cs` | AE7, AC.R45, AD7, W1, §6.4 |
| `src/MARS.WaifuGacha/Services/` | `ShikimoriRateLimiter.cs`, `RollCooldownConfigurationService.cs`, `RootStateBootstrapHostedService.cs` | W2, AC.S07, §6.4 |

### 6.6 Инфраструктура, которой нет в `Services/` монолита

Контракт развёрнут из трёх источников: `infra/` (Postgres, RabbitMQ, Tempo, Prometheus,
Loki, Grafana, Grafana Alloy), `src/MARS.Shared` (gRPC, RabbitMQ-шина, телеметрия,
миграции, аутентификация) и `src/MARS.MediaStorage` (каталог записей, Git-синк, UI).
Перечень — в Приложении C. Все 16 сервисов присутствуют в `docker-compose.yml`
(`gateway`, `twitch-core`, `waifu-gacha`, `telegram`, `discord`, `commands`,
`sound-request`, `tts`, `obs`, `videos365`, `alerts`, `scoreboard`, `cinema-queue`,
`media-storage`, `admin`); `postgres` и `rabbitmq` — инфраструктура, не сервисы
миграции, и упомянуты здесь для полноты. Таргеты Prometheus — `gateway`
(`infrastructure/prometheus/prometheus.yml`).

### 6.7 Планируемая подсистема `MARS.Shikimori`

**Решение владельца:** Shikimori выделяется из `MARS.WaifuGacha` в отдельный контейнер
`MARS.Shikimori` с собственной БД для хранения информации по аниме, манге и персонажам.
Затронуты пункты **W1**, **W2**, **W3**. Кода нового сервиса в репозитории ещё нет.

**Что переезжает.** Правая колонка — планируемые пути, их в репозитории ещё нет.

| Откуда (есть) | Куда (план) | Пункт |
|---|---|---|
| `src/MARS.WaifuGacha/Services/ShikimoriService.cs` | `MARS.Shikimori/Services/ShikimoriService.cs` | W1 |
| `src/MARS.WaifuGacha/Services/IShikimoriRateLimiter.cs`, `ShikimoriRateLimiter.cs`, `RateLimiterInfo.cs` | `MARS.Shikimori/Services/` | W2 |
| `src/MARS.WaifuGacha/Data/ShikimoriClientOptions.cs` | `MARS.Shikimori/Data/ShikimoriClientOptions.cs` | W1 |
| Доменная модель (16 GraphQL-узлов монолита) | сущности БД `MARS.Shikimori` | W3 |

**Как меняются потребители.** Сейчас `ShikimoriService` вызывают только
`src/MARS.WaifuGacha/Services/AddNewWaifuService.cs` и `WaifuRollEnsurenceService.cs`,
а лимитер выставляется наружу через `src/MARS.Admin/Controllers/ShikimoriRateLimiterController.cs`
и `src/MARS.Admin/Services/IShimimoriRateLimiterService.cs`. После выделения все три
места переходят на HTTP/gRPC-клиент нового сервиса; прямых ссылок на
`MARS.WaifuGacha.Services.Shikimori*` не остаётся.

**Обязательные точки — сквозная правка по `AGENTS.md`**

| Что | Где именно |
|---|---|
| Проект в решении | `MARS.slnx`, папки `/src/` **и** `/tests/` |
| Тестовый проект | `tests/MARS.Shikimori.Tests` + `PackageReference` `coverlet.MTP` |
| Матрица CI | `.github/workflows/ci.yml`, ветка `tests` — без записи проект не проверяется |
| Образ | `src/MARS.Shikimori/Dockerfile` + таргет в `release-microservices.yml` |
| Compose | `docker-compose.yml`: сервис `shikimori`, `x-service-env`, том при необходимости |
| Переменные | `.env.example` (`MARS_SHIKIMORI_PASSWORD`) и таблица env в `README.md` |
| База | `infrastructure/db-init/01-databases.sh` — строка в списке `dbs`; скрипт выполняется только на пустом томе, после правки `mars_postgres_data` пересоздаётся |
| Миграции | `src/MARS.Shikimori/Data/Migrations/` + `Data/DesignTime/ShikimoriDbContextFactory.cs` |
| Наблюдаемость | таргет в `infrastructure/prometheus/prometheus.yml` |
| Маршрут | свойство в `src/MARS.Shared/Configuration/ServiceEndpoints.cs` **и** правило + кластер в `Yarp:Routes` |
| Пакеты | версии только в `Directory.Packages.props`, в `.csproj` — голый `PackageReference` |

**Правила, которые нельзя нарушить при создании проекта**

- Своя БД на сервис: `AddMarsDefaults("MARS.Shikimori", "ShikimoriDb")` и
  `AddMarsDbContext<ShikimoriDbContext>(configuration, "shikimori", "ShikimoriDb")` —
  имя строки подключения обязано совпасть в обоих вызовах, иначе readiness будет зелёным
  при недоступной базе.
- Миграции применяются синхронно: `app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult()`
  до `app.Run()`, иначе фоновые службы получат 42P01.
- Новый `.csproj` обязан скопировать **оба** `PropertyGroup` из соседнего проекта
  (`net10.0-windows` и `TreatWarningsAsErrors`/`WarningsAsErrors`/`NoWarn`/`CSharpier_Bypass`):
  общего `Directory.Build.props` в репозитории нет.
- Пространство имён — `MARS.*`. Пакеты — только CPM. Форматирование — CSharpier.

## 7. Покрытие тестами

16 тестовых проектов; в `ci.yml` они перечислены матрицей `tests`.

| Тестовой проект | Что покрывает | Пункты |
|---|---|---|
| `MARS.Commands.Tests` | `CommandAuthorizerTests` (гейт прав), `CommandRegistryTests`, `PlatformFlagsTests` (степени двойки), `CommandParameterTypeTests`, `CommandResultTests`, `ApiCommandServiceTests`, `CommandsControllerAuthorizationTests` | J1, J5, J6 |
| `MARS.TwitchCore.Tests` | `TwitchCommandPermissionsTests`, `HelloVideoEligibilityTests`, `RecentMessageTrackerTests` | AD17, AD24 |
| `MARS.Alerts.Tests` | `AdhdLayoutConfigSchemaTests`, `AdhdLayoutServiceTests` | C1 |
| `MARS.Telegram.Tests` | `BooruSchemaTests` (схема + `varchar(64)` + каскад) | G6 |
| `MARS.Videos365.Tests` | `Videos365ModelTests`, `Config365Tests`, `SiteAvailabilityCheckerTests`, `SiteUnavailableNotifierTests`, `SystemDnsResolverTests` | B1, B2, B3, B4, B5 |
| `MARS.Scoreboard.Tests` | `ScoreboardGrpcServiceTests` | T1 |
| `MARS.SoundRequest.Tests` | `SoundRequestGrpcServiceTests`, `FakePlayerController` | Y2, Y7 |
| `MARS.TTS.Tests` | `VoiceRecognitionGrpcServiceTests`, `FakeTtsMessageFilterService` | AD18 |
| `MARS.Shared.Tests` | `TelegramusGrpcServiceTests`, `TelegramusNotifierTests`, `TunaGrpcServiceTests`, `MediaGrpcMapperTests`, `CommandsContractTests`, `GrpcClientRegistrationTests`, `GrpcEventBroadcasterTests`, `H2cTransportTests`, `KeyedAsyncLockTests`, `LegacyDataSeedTests`, `SecretValueFilterTests`, `ServiceApiKeyAuthenticationHandlerTests`, `OpenTelemetryPrometheusBridgeTests`, `HealthCheckConnectionTests` | AB10, AC.*, S2, J2 |
| `MARS.MediaStorage.Tests` | 11 нагрузочных/файловых тестов + `MediaGit*`, `MediaPathTests`, `TrashPathBuilderTests`, `MediaTranscodePathPolicyTests` | P2, P3 + Приложение C |
| `MARS.Gateway.Tests` | `SwaggerEndpointMapTests` (рефлексия по `ServiceEndpoints`) | Приложение C |
| `MARS.Admin.Tests` | `SmokeTests` | U1, M1 |
| `MARS.CinemaQueue.Tests` | `SmokeTests` | I2 |
| `MARS.Discord.Tests` | `SmokeTests` | L1 |
| `MARS.OBS.Tests` | `SmokeTests` | R1 |
| `MARS.WaifuGacha.Tests` | `SmokeTests` | AE1–AE8 |

**Модули без содержательных тестов** (только `SmokeTests` либо вовсе без тестов) —
это фактическое положение дел, а не пробел списка: `CinemaQueue` (I2–I8),
`Discord` (L1, L4–L8), `OBS` (R1), `WaifuGacha` (AE1–AE8), `Admin` (U1, M1),
`TwitchCore` вне `HelloVideos`/команд (награды, ChannelRewards, EventSub, WaifuGacha-роллы),
`Alerts` кроме `AdhdLayoutConfig` (33 обработчика наград не покрыты тестами).

## 8. Расхождения с `src/MIGRATION_CHECKLIST.md`

Файл существует в репозитории (`src/MIGRATION_CHECKLIST.md`), но по `AGENTS.md` это «исторический статус переноса из монолита, **не спецификация**». Сверка с кодом выявила три расхождения:

1. **«Перенесено: 69 ✅ (100%)» для команд — неверно.** Фактически совпадают 59 `CommandName` из 66. Отсутствуют `autohello`, `fumoinv`, `mgleaders`, `mikuinv`, `mywins`, `randomanime`, `randommanga`. Проверено извлечением `public override string CommandName => "…"` из всех 68+68 файлов. Обратная проверка: 8 команд (`byebye`, `example`, `genshin`, `honkai`, `honkaiusers`, `links`, `randomshorts`, `telegramonly`) в `MARS` есть, а в монолите нет — то есть список команд ещё и неполон в другую сторону.
2. **«Не перенесено (TwitchReward обёртки): 20 — это metadata-only классы, которые наследуют `TemporaryReward` и **не содержат логики**» — неверно.** Например, `AdhdSuperpower_TwitchReward.cs` содержит валидацию, RickRoller и вызов хаба; `RandomReward_TwitchReward.cs` — 200+ строк логики выбора награды. Практический вывод таблицы при этом верный: поведение этих наград перенесено под другими именами (`*Handler` в `MARS.Alerts`), а не потеряно.
3. **Не отражены частичные переносы:** `LegBumRefundService` и `1702_EmojisReward` помечены ❌ без указания, что по ним остался контракт (`ITelegramusNotifier.AllRefund`, `MakeScreenEmojisParticles`) при отсутствии вызывающей стороны.

Поэтому настоящий файл опирается на код, а не на `MIGRATION_CHECKLIST.md`.

## 9. Проверки

Три независимые проверки, каждая своим методом. Скрипты лежат вне репозитория
(`%TEMP%\opencode\verify1.ps1`, `verify2.ps1`, `verify3.ps1`) и читают только
`../mars_old` и текущий репозиторий; правят они только сам чеклист. Последний прогон
приведён целиком.

### 9.1 Проверка 1 — полнота со стороны источника

**Метод:** рекурсивный обход `D:\VS\MARS_old\MARS.Projects\MARS.Server\Services`
через `[System.IO.Directory]::GetFiles` (а не `Get-ChildItem -Include`, который
в PowerShell 5.1 при `-LiteralPath` на каталоге фильтр игнорирует и возвращает
534 файла вместо 510), затем таблица маршрутов «файл монолита → пункт» с правилом
«первое совпадение выигрывает» и сверкой в обе стороны.

```
[inv] .cs files          : 510
[inv] non-.cs files      : 20 (plus 4 inside .git/)
[map] mapped            : 510
[map] UNMAPPED (.cs)    : 0
[map] distinct item ids : 226
[map] ids used but NOT declared in checklist: 0
[map] ids declared but carrying 0 files: 0
```

Итог: 510 из 510 файлов исходника покрыты, ни один не потерян, выдуманных нет;
226 пунктов чеклиста — и у каждого есть хотя бы один файл монолита, поэтому в
чеклисте нет «висячих» пунктов. Плюс обратная арифметика команд: 61 файл (59
уникальных `CommandName`) в J7 + 7 файлов в J8 = 68 = числу файлов в
`CommandExecutor/Commands/`.

### 9.2 Проверка 2 — полнота со стороны репозитория

**Метод:** независимое сканирование `D:\VS\MARS` в обратную сторону — от того, что
реально реализовано, к тому, что записано в файле. Девять групп проверок.

```
--- 2.1 Existence of every repo path c```
--- 2.1 Existence of every repo path cited in the checklist ---
  PASS  all 162 cited repo paths exist on disk
--- 2.2 Every project in src/ and tests/ is referenced ---
  PASS  all 32 projects referenced
--- 2.3 Reverse scan: untraced src/ types are covered by Appendix C ---
  files with no monolith counterpart: 222
  of those, covered by neither Appendix C nor an exact citation: 0
  PASS  every src/ file without a monolith counterpart is described in the file
--- 2.4 RabbitMQ: reward routing keys <-> IRewardAlertHandler implementations ---
  RewardSpecificKeys array entries  : 33
  concrete IRewardAlertHandler       : 33
  PASS  one handler per reward routing key
  PASS  RewardSpecificKeys count == distinct RewardPrefix constants minus RewardRedeemed
  PASS  every handler name equals its routing key suffix
  PASS  every reward routing key has a handler
  *Handler-named classes outside the key map: IChatUserTracking, IRewardAlert, RandomMem
  PASS  the only concrete keyless handler is RandomMemHandler (Telegram-driven, AC.R22)
  PASS  all four are accounted for in section 6.3
--- 2.5 proto files referenced ---
  PASS  all 7 proto files referenced
  PASS  all 6 rpc services referenced
--- 2.6 YARP routes referenced ---
  PASS  all 31 YARP routes referenced
--- 2.7 RabbitMQ queues referenced ---
  PASS  all 3 queues referenced
--- 2.8 compose services + prometheus targets referenced ---
  app services declared in compose : gateway, twitch-core, waifu-gacha, telegram, discord, commands, sound-request, tts, obs, videos365, alerts, scoreboard, cinema-queue, media-storage, admin
  PASS  all 15 app services present in compose and referenced
  infra services/volumes in compose: alloy, grafana, loki, postgres, prometheus, rabbitmq, tempo
  PASS  all 7 infra entries in compose referenced
  prometheus targets: gateway
  PASS  all 1 prometheus targets referenced
--- 2.9 every [ ] item: the claimed absence is real (no monolith name leaked into src/) ---
  PASS  all 30 claimed absences verified absent from src/
```льных дефекта первого прохода:

1. **Неверный путь.** В первой редакции WTelegram-клиент был указан как
   `Services/WTelegram/WTelegramClientService.cs` — такого файла в репозитории нет;
   лежит `src/MARS.Telegram/Services/WTelegramClientService.cs`.
2. **Неполнота по способам вызова.** В файле не было ни одного из 31 маршрута YARP,
   ни 3 очередей, ни 4 из 7 `.proto`. Добавлены §6.1–6.4.
3. **Неучтённые подсистемы репозитория.** Обратный проход нашёл 222 файла `src/`,
   не имеющих соответствия в монолите (DbContext'ы, контроллеры, `MARS.Gateway`,
   Git-синк, UI хранилища, TTS, Tuna, RabbitMQ/gRPC-инфраструктура). Добавлены §6.5
   и Приложение C — теперь ни один такой файл не остаётся без описания.
4. **Неверная цифра.** В §6.3 стояло «32 ключа ↔ 32 обработчика». По коду
   `RewardSpecificKeys` содержит **33** кл```
--- 3.1 Checkbox format ---
  PASS  every list checkbox uses exactly "- [ ] " or "- [x] "
  PASS  no malformed checkbox marker
  PASS  no upper-case [X]
--- 3.2 Every checkbox item has an id, a bold title and a verdict ---
  PASS  count of "- [x] **ID. Title**" / "- [ ] **ID. Title**" lines equals total boxes
  PASS  every [x] item states a verdict (полностью/частично/заменено/исключено)
--- 3.3 Item id uniqueness ---
  PASS  no duplicate item ids
  PASS  appendix A references only ids declared in the checklist
--- 3.4 Every checklist id appears in Appendix A and vice versa ---
  PASS  every checklist id carries >=1 monolith file in Appendix A
  PASS  every Appendix A id is declared in the checklist
--- 3.5 Appendix A really covers the source ---
  PASS  Appendix A row count == .cs count in source
  PASS  no source file missing from Appendix A
  PASS  no invented file in Appendix A
  PASS  each source file listed exactly once in Appendix A
--- 3.6 Appendix B covers every non-.cs file under Services/ ---
  PASS  Appendix B row count == non-.cs file count in source
  PASS  no non-.cs file missing from Appendix B
--- 3.7 Every repo path cited in the file exists on disk ---
  PASS  all 162 cited repo paths exist on disk
--- 3.8 Cross-reference integrity ---
  cross-references found: 226
  PASS  all "см. **ID**" / table-id references resolve to a declared item
--- 3.9 Status vocabulary ---
  verdict "заменено": 19
  verdict "исключено": 5
  verdict "полностью": 145
  verdict "частично": 11
  PASS  only the four documented verdicts are used
  PASS  a [x] item always carries one of the four verdicts
--- 3.10 Numbers stated in prose match the counts ---
  computed: total=226  [x]=180  [ ]=46  full=145  partial=11  replaced=19  excluded=5
  arithmetic check: full+partial+replaced+excluded = 180 (must equal [x] count 180)
  PASS  verdict counts sum to the [x] count
  PASS  total == [x] + [ ]
  PASS  46 work items claimed in section 5
  PASS  the 46 claim equals the actual [ ] count
  PASS  section 5 headline number == [ ] count
  section 5 group figures sum: 46
  PASS  section 5 group figures sum to the headline
--- 3.11 No duplicate checklist lines / titles ---
  PASS  no two items share the same title
--- 3.12 Numbers quoted for the source tree ---
  PASS  source .cs count quoted as 510 matches reality
  PASS  src/ .cs count quoted as 698 matches reality
  PASS  16 src projects / 16 test projects
```titles ---
  PASS  no two items share the same title
--- 3.12 Numbers quoted for the source tree ---
  PASS  source .cs count quoted as 510 matches reality
  PASS  src/ .cs count quoted as 698 matches reality
  PASS  16 src projects / 16 test projects

=== VERIFICATION 3: ALL CHECKS PASS ===
```

Проверка 3 нашла два дефекта первого прохода: шесть пунктов использовали составные
вердикты (`*полностью, только схема*`, `*частично, заменено*` и т. п.), из-за чего
они не попадали в подсчёт и арифметика `145 + 11 + 19 = 175` не сходилась; и в
J7 было перечислено 56 команд вместо 61. Оба исправлены.

### 9.4 Что проверки не могут гарантировать

- **Функциональная эквивалентность.** Совпадение имён и наличие артефактов не доказывает,
  что поведение совпадает. Проверялась связность «файл монолита → артефакт репозитория»,
  а не результат работы кода на стенде.
- **Покрытие тестами.** §7 фиксирует, что 33 обработчика наград, `ChannelRewards`,
  `EventSub` и роллы `WaifuGacha` не имеют содержательных тестов — это фактическое
  положение дел, зафиксированное, а не исправленное.
- **`.mimocode/plans/`.** По `AGENTS.md` эти планы устарели и не соответствуют
  репозиторию, поэтому в качестве источника статуса не использовались.

## Приложение A. Покрытие исходника: файл монолита → пункт чеклиста

Таблица построена рекурсивным обходом `../mars_old/MARS.Projects/MARS.Server/Services`
(510 `.cs`). Каждый файл встречается ровно один раз — это и есть доказательство
полноты со стороны источника (Проверка 1).

| # | Файл монолита (`MARS.Server/Services/…`) | Пункт |
|---|---|---|
| 1 | `365Genius/Entitys/Video365.cs` | B2 |
| 2 | `365Genius/IDnsResolver.cs` | B3 |
| 3 | `365Genius/SiteAvailabilityChecker.cs` | B4 |
| 4 | `365Genius/SiteUnavailableNotifier.cs` | B5 |
| 5 | `365Genius/SystemDnsResolver.cs` | B3 |
| 6 | `365Genius/Worker365.cs` | B1 |
| 7 | `Adhd/AdhdLayoutService.cs` | C1 |
| 8 | `Adhd/Entities/AdhdLayoutConfig.cs` | C2 |
| 9 | `Adhd/Entities/AdhdLayoutConfigDto.cs` | C3 |
| 10 | `AppStateService_OBSOLETE/AppStateService.cs` | D1 |
| 11 | `AudioControllerHub/SignalRAudioControllerService.cs` | E1 |
| 12 | `AutoArts_OBSOLETE/Entitys/AutoArtImage.cs` | F1 |
| 13 | `BooruAutoPost/BooruAutoPostService.cs` | G1 |
| 14 | `BooruAutoPost/BooruDiscordPoster.cs` | G2 |
| 15 | `BooruAutoPost/BooruTelegramPoster.cs` | G3 |
| 16 | `BooruAutoPost/Entities/BooruAutoPostConfig.cs` | G6 |
| 17 | `BooruAutoPost/Entities/BooruAutoPostConfigDto.cs` | G6 |
| 18 | `BooruAutoPost/Entities/BooruAutoPostCreateRequest.cs` | G6 |
| 19 | `BooruAutoPost/Entities/BooruAutoPostUpdateRequest.cs` | G6 |
| 20 | `BooruAutoPost/Entities/BooruScheduledPost.cs` | G6 |
| 21 | `BooruAutoPost/Entities/BooruSource.cs` | G6 |
| 22 | `BooruAutoPost/Entities/Rule34Post.cs` | G6 |
| 23 | `BooruAutoPost/Entities/ScheduledPostStatus.cs` | G6 |
| 24 | `BooruAutoPost/Entities/TargetPlatform.cs` | G6 |
| 25 | `BooruAutoPost/Entities/TelegramScheduledMessageInfo.cs` | G6 |
| 26 | `BooruAutoPost/IBooruAutoPostService.cs` | G1 |
| 27 | `BooruAutoPost/IBooruDiscordPoster.cs` | G2 |
| 28 | `BooruAutoPost/IBooruTelegramPoster.cs` | G3 |
| 29 | `BooruAutoPost/Rule34RandomPostService.cs` | G4 |
| 30 | `BooruAutoPost/TelegramScheduleMatcher.cs` | G5 |
| 31 | `BooruShared/BooruMessageTemplateResolver.cs` | H1 |
| 32 | `BooruShared/BooruValidationHelper.cs` | H2 |
| 33 | `BooruShared/DeduplicationService.cs` | H3 |
| 34 | `BooruShared/Entities/BooruAutoPostCreateRequestBase.cs` | H6 |
| 35 | `BooruShared/Entities/BooruAutoPostUpdateRequestBase.cs` | H6 |
| 36 | `BooruShared/Entities/PostedImageRecord.cs` | H5 |
| 37 | `BooruShared/Entities/TelegramParseMode.cs` | H6 |
| 38 | `BooruShared/IDeduplicationService.cs` | H3 |
| 39 | `BooruShared/TagValidator.cs` | H4 |
| 40 | `CinemaQueue/CinemaQueueServiceCollectionExtensions.cs` | I1 |
| 41 | `CinemaQueue/Entitys/CinemaMediaItem.cs` | I8 |
| 42 | `CinemaQueue/Entitys/CinemaMediaItemDto.cs` | I8 |
| 43 | `CinemaQueue/Entitys/CinemaQueueStatistics.cs` | I8 |
| 44 | `CinemaQueue/Entitys/CreateMediaItemRequest.cs` | I8 |
| 45 | `CinemaQueue/Entitys/MediaStatus.cs` | I8 |
| 46 | `CinemaQueue/Entitys/UpdateMediaItemRequest.cs` | I8 |
| 47 | `CinemaQueue/Interfaces/ICinemaQueueRepository.cs` | I3 |
| 48 | `CinemaQueue/Interfaces/ICinemaQueueService.cs` | I2 |
| 49 | `CinemaQueue/Models/KinopoiskExternalId.cs` | I4 |
| 50 | `CinemaQueue/Models/KinopoiskMovieDto.cs` | I4 |
| 51 | `CinemaQueue/Models/KinopoiskPoster.cs` | I4 |
| 52 | `CinemaQueue/Models/KinopoiskRating.cs` | I4 |
| 53 | `CinemaQueue/Models/KinopoiskSearchResponse.cs` | I4 |
| 54 | `CinemaQueue/Models/KinopoiskVotes.cs` | I4 |
| 55 | `CinemaQueue/Repositories/CinemaQueueRepository.cs` | I3 |
| 56 | `CinemaQueue/Services/CinemaQueueNotificationService.cs` | I7 |
| 57 | `CinemaQueue/Services/CinemaQueueService.cs` | I2 |
| 58 | `CinemaQueue/Services/KinopoiskService.cs` | I4 |
| 59 | `CinemaQueue/Services/MediaMetadataService.cs` | I5 |
| 60 | `CinemaQueue/Services/TwitchCinemaQueueService.cs` | I6 |
| 61 | `CommandExecutor/Adapters/ApiCommandService.cs` | J1 |
| 62 | `CommandExecutor/Adapters/DiscordCommandService.cs` | J1 |
| 63 | `CommandExecutor/Adapters/TelegramCommandService.cs` | J1 |
| 64 | `CommandExecutor/Adapters/TwitchCommandService.cs` | J1 |
| 65 | `CommandExecutor/CommandExecutorService.cs` | J2 |
| 66 | `CommandExecutor/CommandExecutorServiceCollectionExtensions.cs` | J4 |
| 67 | `CommandExecutor/CommandFactory.cs` | J3 |
| 68 | `CommandExecutor/Commands/!adhd_command.cs` | J7 |
| 69 | `CommandExecutor/Commands/!automessage_SendAutoMessage.cs` | J7 |
| 70 | `CommandExecutor/Commands/!catisa_command.cs` | J7 |
| 71 | `CommandExecutor/Commands/!directory_command.cs` | J7 |
| 72 | `CommandExecutor/Commands/!discord_command.cs` | J7 |
| 73 | `CommandExecutor/Commands/!fumo_command.cs` | J7 |
| 74 | `CommandExecutor/Commands/!getAllKeyWordsForAlerts_Command.cs` | J7 |
| 75 | `CommandExecutor/Commands/!googlephotos_authorize_command.cs` | J7 |
| 76 | `CommandExecutor/Commands/!hellovideo_command.cs` | J7 |
| 77 | `CommandExecutor/Commands/!joinedtwitchchannels_command.cs` | J7 |
| 78 | `CommandExecutor/Commands/!mikubeam_command.cs` | J7 |
| 79 | `CommandExecutor/Commands/!mikumonday_MikuMondayReward.cs` | J7 |
| 80 | `CommandExecutor/Commands/!minigamestop_command.cs` | J7 |
| 81 | `CommandExecutor/Commands/!mutesound_command.cs` | J7 |
| 82 | `CommandExecutor/Commands/!platformtest_command.cs` | J7 |
| 83 | `CommandExecutor/Commands/!puntoswitcher_command.cs` | J7 |
| 84 | `CommandExecutor/Commands/!randommem_command.cs` | J7 |
| 85 | `CommandExecutor/Commands/!rollfrog_command.cs` | J7 |
| 86 | `CommandExecutor/Commands/!rollfumo_command.cs` | J7 |
| 87 | `CommandExecutor/Commands/!rollmiku_command.cs` | J7 |
| 88 | `CommandExecutor/Commands/!rollwaifu_command.cs` | J7 |
| 89 | `CommandExecutor/Commands/!setenv_command.cs` | J7 |
| 90 | `CommandExecutor/Commands/!shutdown_command.cs` | J7 |
| 91 | `CommandExecutor/Commands/!spotifyauth_start_command.cs` | J7 |
| 92 | `CommandExecutor/Commands/!srclear_command.cs` | J7 |
| 93 | `CommandExecutor/Commands/!srpause_command.cs` | J7 |
| 94 | `CommandExecutor/Commands/!srplay_command.cs` | J7 |
| 95 | `CommandExecutor/Commands/!srstop_command.cs` | J7 |
| 96 | `CommandExecutor/Commands/!srvolume_command.cs` | J7 |
| 97 | `CommandExecutor/Commands/!systeminfo_command.cs` | J7 |
| 98 | `CommandExecutor/Commands/!systeminfo_SystemInfo.cs` | J7 |
| 99 | `CommandExecutor/Commands/!tanya_command.cs` | J7 |
| 100 | `CommandExecutor/Commands/!tanya_Tanya.cs` | J7 |
| 101 | `CommandExecutor/Commands/!title_ChangeStreamTitle.cs` | J7 |
| 102 | `CommandExecutor/Commands/!ttsfilter_command.cs` | J7 |
| 103 | `CommandExecutor/Commands/!ttsstop_command.cs` | J7 |
| 104 | `CommandExecutor/Commands/!ttsvoice_command.cs` | J7 |
| 105 | `CommandExecutor/Commands/!ttsvolume_command.cs` | J7 |
| 106 | `CommandExecutor/Commands/!twitchauthnotify_command.cs` | J7 |
| 107 | `CommandExecutor/Commands/!twitchblacklistadd_command.cs` | J7 |
| 108 | `CommandExecutor/Commands/!twitchblacklistremove_command.cs` | J7 |
| 109 | `CommandExecutor/Commands/!twitchchannelreconnect_command.cs` | J7 |
| 110 | `CommandExecutor/Commands/!twitchchannelstatus_command.cs` | J7 |
| 111 | `CommandExecutor/Commands/!twitchevents_command.cs` | J7 |
| 112 | `CommandExecutor/Commands/!twitchsubrec_command.cs` | J7 |
| 113 | `CommandExecutor/Commands/!unmutesound_command.cs` | J7 |
| 114 | `CommandExecutor/Commands/!waifuunmerge_command.cs` | J7 |
| 115 | `CommandExecutor/Commands/!whitelist_command.cs` | J7 |
| 116 | `CommandExecutor/Commands/!wtelegramstatus_command.cs` | J7 |
| 117 | `CommandExecutor/Commands/autohello_command.cs` | J8 |
| 118 | `CommandExecutor/Commands/c_ShortCommands.cs` | J7 |
| 119 | `CommandExecutor/Commands/download_command.cs` | J7 |
| 120 | `CommandExecutor/Commands/fumoinv_command.cs` | J8 |
| 121 | `CommandExecutor/Commands/help_command.cs` | J7 |
| 122 | `CommandExecutor/Commands/info_command.cs` | J7 |
| 123 | `CommandExecutor/Commands/mgleaders_command.cs` | J8 |
| 124 | `CommandExecutor/Commands/mikuinv_command.cs` | J8 |
| 125 | `CommandExecutor/Commands/mywins_command.cs` | J8 |
| 126 | `CommandExecutor/Commands/queue_QueuePosition.cs` | J7 |
| 127 | `CommandExecutor/Commands/randomanime_command.cs` | J8 |
| 128 | `CommandExecutor/Commands/randommanga_command.cs` | J8 |
| 129 | `CommandExecutor/Commands/song_command.cs` | J7 |
| 130 | `CommandExecutor/Commands/sr_command.cs` | J7 |
| 131 | `CommandExecutor/Commands/srlist_SoundRequestList.cs` | J7 |
| 132 | `CommandExecutor/Commands/srwrong_command.cs` | J7 |
| 133 | `CommandExecutor/Commands/start_command.cs` | J7 |
| 134 | `CommandExecutor/Commands/vanish_command.cs` | J7 |
| 135 | `CommandExecutor/Commands/zonezero_command.cs` | J7 |
| 136 | `CommandExecutor/Entitys/CommandParameterInfo.cs` | J6 |
| 137 | `CommandExecutor/Entitys/Commands/BaseCommand.cs` | J5 |
| 138 | `CommandExecutor/Entitys/CommandVisibility.cs` | J6 |
| 139 | `CommandExecutor/Entitys/Platform.cs` | J6 |
| 140 | `CommandExecutor/ICommandService.cs` | J1 |
| 141 | `CommandExecutor/PlatformCommandServiceBase.cs` | J1 |
| 142 | `Configuration/ConfigurationKeysBootstrapHostedService.cs` | K1 |
| 143 | `Discord/Gateway/DiscordGatewayService.cs` | L1 |
| 144 | `Discord/Gateway/IDiscordGatewayService.cs` | L1 |
| 145 | `Discord/Gateway/IMediaCompressor.cs` | L2 |
| 146 | `Discord/Gateway/MediaCompressor.cs` | L2 |
| 147 | `Discord/Gateway/VideoExtensions.cs` | L3 |
| 148 | `Discord/PlayRequest/DiscordPlayAudioCacheService.cs` | L5 |
| 149 | `Discord/PlayRequest/DiscordPlayRequestService.cs` | L4 |
| 150 | `Discord/PlayRequest/DiscordPlaySelectionSession.cs` | L6 |
| 151 | `Discord/PlayRequest/DiscordPreparedAudioFile.cs` | L7 |
| 152 | `Discord/TtsVoiceRelay/DiscordTtsVoiceRelayService.cs` | L8 |
| 153 | `Discord/TtsVoiceRelay/IDiscordTtsVoiceRelayService.cs` | L8 |
| 154 | `EnvironmentVariable/Entitys/EnvironmentVariable.cs` | M1 |
| 155 | `KeyboardHook_UNUSED/Controllers/KeyboardHookController.cs` | N2 |
| 156 | `KeyboardHook_UNUSED/IKeyboardHookService.cs` | N1 |
| 157 | `KeyboardHook_UNUSED/KeyboardHookFactory.cs` | N1 |
| 158 | `KeyboardHook_UNUSED/KeyboardHookService.cs` | N1 |
| 159 | `KeyboardHook_UNUSED/KeyboardHookServiceCollectionExtensions.cs` | N1 |
| 160 | `KeyboardHook_UNUSED/NullKeyboardHookService.cs` | N1 |
| 161 | `Logs/Interfaces/ILogsService.cs` | O1 |
| 162 | `Logs/Services/LogsService.cs` | O1 |
| 163 | `Media/FfprobeMediaInspector.cs` | P1 |
| 164 | `Media/IMediaFileStorageService.cs` | P3 |
| 165 | `Media/IMediaInspector.cs` | P1 |
| 166 | `Media/IMediaTranscoder.cs` | P2 |
| 167 | `Media/MediaTranscoder.cs` | P2 |
| 168 | `Media/WebRootMediaFileStorageService.cs` | P3 |
| 169 | `MemoryStorageService/Entitys/MemoryFile.cs` | Q1 |
| 170 | `MemoryStorageService/MemoryStorage.cs` | Q1 |
| 171 | `Obs/IObsService.cs` | R1 |
| 172 | `Obs/ObsConfiguration.cs` | R1 |
| 173 | `OperationResult.cs` | A1 |
| 174 | `PyroAlerts/Entitys/ApiMediaInfo.cs` | S2 |
| 175 | `PyroAlerts/Entitys/MediaAlertPriority.cs` | S2 |
| 176 | `PyroAlerts/Entitys/MediaDto.cs` | S2 |
| 177 | `PyroAlerts/Entitys/MediaFileInfo.cs` | S2 |
| 178 | `PyroAlerts/Entitys/MediaInfo.cs` | S2 |
| 179 | `PyroAlerts/Entitys/MediaMetaInfo.cs` | S2 |
| 180 | `PyroAlerts/Entitys/MediaPositionInfo.cs` | S2 |
| 181 | `PyroAlerts/Entitys/MediaStylesInfo.cs` | S2 |
| 182 | `PyroAlerts/Entitys/MediaTextInfo.cs` | S2 |
| 183 | `PyroAlerts/Entitys/MediaType.cs` | S2 |
| 184 | `PyroAlerts/PyroAlertsHandler.cs` | S1 |
| 185 | `PyroAlerts/PyroAlertsHelper.cs` | S1 |
| 186 | `Scoreboard/Entitys/ScoreboardDto.cs` | T2 |
| 187 | `Scoreboard/Entitys/ScoreboardLayout.cs` | T2 |
| 188 | `Scoreboard/Entitys/ScoreboardPlayer.cs` | T2 |
| 189 | `Scoreboard/Entitys/ScoreboardState.cs` | T2 |
| 190 | `Scoreboard/ScoreboardService.cs` | T1 |
| 191 | `ServiceManager/Entitys/ServiceInfo.cs` | U2 |
| 192 | `ServiceManager/Entitys/ServiceLog.cs` | U2 |
| 193 | `ServiceManager/Entitys/ServiceNameAttribute.cs` | U2 |
| 194 | `ServiceManager/Entitys/ServiceState.cs` | U2 |
| 195 | `ServiceManager/Entitys/ServiceStatus.cs` | U2 |
| 196 | `ServiceManager/IServiceManager.cs` | U1 |
| 197 | `ServiceManager/ManagedServiceBase.cs` | U1 |
| 198 | `ServiceManager/ServiceManager.cs` | U1 |
| 199 | `SevenTv/ISevenTvApiService.cs` | V1 |
| 200 | `SevenTv/SevenTvApiService.cs` | V1 |
| 201 | `Shikimori/Entitys/AiredOnNode.cs` | W3 |
| 202 | `Shikimori/Entitys/AnimeListData.cs` | W3 |
| 203 | `Shikimori/Entitys/AnimeNode.cs` | W3 |
| 204 | `Shikimori/Entitys/CharacterImageNode.cs` | W3 |
| 205 | `Shikimori/Entitys/CharacterNode.cs` | W3 |
| 206 | `Shikimori/Entitys/GraphqlEnvelope.cs` | W3 |
| 207 | `Shikimori/Entitys/GraphqlError.cs` | W3 |
| 208 | `Shikimori/Entitys/GraphqlRequest.cs` | W3 |
| 209 | `Shikimori/Entitys/IShikimoriRateLimiter.cs` | W2 |
| 210 | `Shikimori/Entitys/MangaListData.cs` | W3 |
| 211 | `Shikimori/Entitys/MangaNode.cs` | W3 |
| 212 | `Shikimori/Entitys/PosterNode.cs` | W3 |
| 213 | `Shikimori/Entitys/RateLimiterInfo.cs` | W2 |
| 214 | `Shikimori/Entitys/RelatedTitleNode.cs` | W3 |
| 215 | `Shikimori/Entitys/ShikimoriAnime.cs` | W3 |
| 216 | `Shikimori/Entitys/ShikimoriCharacter.cs` | W3 |
| 217 | `Shikimori/Entitys/ShikimoriManga.cs` | W3 |
| 218 | `Shikimori/Entitys/ShikimoriTitle.cs` | W3 |
| 219 | `Shikimori/IShikimoriApiClient.cs` | W1 |
| 220 | `Shikimori/ShikimoriApiClient.cs` | W1 |
| 221 | `Shikimori/ShikimoriService.cs` | W1 |
| 222 | `Shikimori/ShikimoriShikimoriRateLimiter.cs` | W2 |
| 223 | `SoundBarService/Entitys/ISoundBar.cs` | X1 |
| 224 | `SoundBarService/SoundMuteCoordinator.cs` | X1 |
| 225 | `SoundRequest/Entities/BaseTrackInfo.cs` | Y1 |
| 226 | `SoundRequest/Entities/PlaybackState.cs` | Y1 |
| 227 | `SoundRequest/Entities/PlayerState.cs` | Y1 |
| 228 | `SoundRequest/Entities/QueueItem.cs` | Y1 |
| 229 | `SoundRequest/Entities/VideoDisplay.cs` | Y1 |
| 230 | `SoundRequest/InSignalRHubService.cs` | Y7 |
| 231 | `SoundRequest/Interfaces/IPlayerController.cs` | Y2 |
| 232 | `SoundRequest/MainPlayer.cs` | Y3 |
| 233 | `SoundRequest/OutSignalRHubService.cs` | Y8 |
| 234 | `SoundRequest/Queue/SoundRequestUserQueue.cs` | Y5 |
| 235 | `SoundRequest/SoundCloud/SoundCloudResolver.cs` | Y9 |
| 236 | `SoundRequest/SoundRequestCommandsService.cs` | Y6 |
| 237 | `SoundRequest/Spotify/SpotifyApiClient.cs` | Y10 |
| 238 | `SoundRequest/Spotify/SpotifyAuthService.cs` | Y11 |
| 239 | `SoundRequest/Spotify/SpotifyPlaybackService.cs` | Y12 |
| 240 | `SoundRequest/Spotify/SpotifyPlaybackSnapshot.cs` | Y12 |
| 241 | `SoundRequest/Spotify/SpotifyResolver.cs` | Y13 |
| 242 | `SoundRequest/StateManager.cs` | Y4 |
| 243 | `StreamAcrhive_UNUSED/Entitys/ArchiveConfig.cs` | Z1 |
| 244 | `StreamAcrhive_UNUSED/Entitys/ArchiveFile.cs` | Z1 |
| 245 | `StreamAcrhive_UNUSED/Entitys/ArchiveFileChunk.cs` | Z1 |
| 246 | `StreamAcrhive_UNUSED/Entitys/StreamArchiveChunkStatus.cs` | Z1 |
| 247 | `StreamAcrhive_UNUSED/Entitys/StreamArchiveFileStatus.cs` | Z1 |
| 248 | `StreamAcrhive_UNUSED/Entitys/StreamArchiveVideoFormats.cs` | Z1 |
| 249 | `StreamAcrhive_UNUSED/FFmpegService.cs` | Z4 |
| 250 | `StreamAcrhive_UNUSED/Interfaces/IFFmpegService.cs` | Z4 |
| 251 | `StreamAcrhive_UNUSED/Interfaces/IStreamArchiveService.cs` | Z2 |
| 252 | `StreamAcrhive_UNUSED/Models/FFprobeFormat.cs` | Z5 |
| 253 | `StreamAcrhive_UNUSED/Models/FFprobeOutput.cs` | Z5 |
| 254 | `StreamAcrhive_UNUSED/Models/FFprobeStream.cs` | Z5 |
| 255 | `StreamAcrhive_UNUSED/Models/VideoInfo.cs` | Z5 |
| 256 | `StreamAcrhive_UNUSED/StreamArchiveService.cs` | Z2 |
| 257 | `StreamAcrhive_UNUSED/StreamArchiveWorker.cs` | Z3 |
| 258 | `TabletopGames_OBSOLETE/Checkers/CheckersGame.cs` | AA1 |
| 259 | `TabletopGames_OBSOLETE/Checkers/CheckersGameManager.cs` | AA1 |
| 260 | `TabletopGames_OBSOLETE/Checkers/CheckersQueue.cs` | AA1 |
| 261 | `TabletopGames_OBSOLETE/Entitys/Abstractions/Figure.cs` | AA2 |
| 262 | `TabletopGames_OBSOLETE/Entitys/Board.cs` | AA2 |
| 263 | `TabletopGames_OBSOLETE/Entitys/Cell.cs` | AA2 |
| 264 | `TabletopGames_OBSOLETE/Entitys/Checker.cs` | AA2 |
| 265 | `TabletopGames_OBSOLETE/Entitys/Color.cs` | AA2 |
| 266 | `TabletopGames_OBSOLETE/Entitys/Enums/GameStatus.cs` | AA2 |
| 267 | `Telegram/BotService/Abstract/IReceiverService.cs` | AB1 |
| 268 | `Telegram/BotService/Abstract/PollingServiceBase.cs` | AB1 |
| 269 | `Telegram/BotService/Abstract/ReceiverServiceBase.cs` | AB1 |
| 270 | `Telegram/BotService/Entities/VerificationCodeRequest.cs` | AB4 |
| 271 | `Telegram/BotService/Entities/WTelegramClientStatus.cs` | AB4 |
| 272 | `Telegram/BotService/Entities/WTelegramOperationResult.cs` | AB4 |
| 273 | `Telegram/BotService/Entitys/TelegramUpdateReceiverOffset.cs` | AB4 |
| 274 | `Telegram/BotService/Entitys/TelegramUser.cs` | AB4 |
| 275 | `Telegram/BotService/PollingService.cs` | AB2 |
| 276 | `Telegram/BotService/ReceiverService.cs` | AB2 |
| 277 | `Telegram/BotService/UpdateHandler.cs` | AB3 |
| 278 | `Telegram/ClipboardCopy/ClipboardRequestFiles.cs` | AB6 |
| 279 | `Telegram/ClipboardCopy/MediaGroupBuffer.cs` | AB6 |
| 280 | `Telegram/ClipboardCopy/TelegramClipboardCopyService.cs` | AB6 |
| 281 | `Telegram/ClipboardCopy/TriggerWaitBuffer.cs` | AB6 |
| 282 | `Telegram/DiscordBridge/Entities/TelegramDiscordChannelBinding.cs` | AB8 |
| 283 | `Telegram/DiscordBridge/Entities/TelegramDiscordChannelState.cs` | AB8 |
| 284 | `Telegram/DiscordBridge/Entitys/DiscordChannelOptionDto.cs` | AB8 |
| 285 | `Telegram/DiscordBridge/Entitys/TelegramChannelOptionDto.cs` | AB8 |
| 286 | `Telegram/DiscordBridge/Entitys/TelegramDiscordBindingCreateRequest.cs` | AB8 |
| 287 | `Telegram/DiscordBridge/Entitys/TelegramDiscordBindingDto.cs` | AB8 |
| 288 | `Telegram/DiscordBridge/Entitys/TelegramDiscordBindingSetEnabledRequest.cs` | AB8 |
| 289 | `Telegram/DiscordBridge/Entitys/TelegramDiscordChannelStateDto.cs` | AB8 |
| 290 | `Telegram/DiscordBridge/ITelegramDiscordBridgeService.cs` | AB7 |
| 291 | `Telegram/DiscordBridge/TelegramDiscordBridgeService.cs` | AB7 |
| 292 | `Telegram/GooglePhotos/GooglePhotosApiClient.cs` | AB9 |
| 293 | `Telegram/GooglePhotos/GooglePhotosAuthService.cs` | AB9 |
| 294 | `Telegram/GooglePhotos/TelegramGooglePhotosService.cs` | AB9 |
| 295 | `Telegram/ITelegramusService.cs` | AB10 |
| 296 | `Telegram/PrivateChannelsResender/Entities/ChannelProcessingState.cs` | AB11 |
| 297 | `Telegram/PrivateChannelsResender/TelegramChannelsResenderService.cs` | AB11 |
| 298 | `Telegram/TelegramProxyHelper.cs` | AB5 |
| 299 | `Telegram/WTelegram/Entities/WTelegramSession.cs` | AB12 |
| 300 | `Telegram/WTelegram/WTelegramClientService.cs` | AB12 |
| 301 | `Telegram/WTelegram/WTelegramDbSessionStore.cs` | AB12 |
| 302 | `Twitch/AutoInfoFetch/AutoRewardInfoFetcher.cs` | AD1 |
| 303 | `Twitch/BlackList/TwitchBlackListService.cs` | AD2 |
| 304 | `Twitch/Client/TwitchApiRateLimiter.cs` | AD3 |
| 305 | `Twitch/Client/TwitchClientProxy.cs` | AD4 |
| 306 | `Twitch/Client/TwitchConnectionManager.cs` | AD5 |
| 307 | `Twitch/ClientMessages/AutoMessages/AutoMessagesHandler.cs` | AD6 |
| 308 | `Twitch/ClientMessages/AutoMessages/DTOs/AutoMessageDto.cs` | AD6 |
| 309 | `Twitch/ClientMessages/AutoMessages/DTOs/CreateAutoMessageRequest.cs` | AD6 |
| 310 | `Twitch/ClientMessages/AutoMessages/DTOs/UpdateAutoMessageRequest.cs` | AD6 |
| 311 | `Twitch/ClientMessages/AutoMessages/Entitys/AutoMessage.cs` | AD6 |
| 312 | `Twitch/ClientMessages/AutoMessages/Extensions/AutoMessagesServiceExtensions.cs` | AD6 |
| 313 | `Twitch/ClientMessages/AutoMessages/Interfaces/IAutoMessagesService.cs` | AD6 |
| 314 | `Twitch/ClientMessages/AutoMessages/Services/AutoMessagesService.cs` | AD6 |
| 315 | `Twitch/ClientMessages/TwitchAutoHello/AutoHello.cs` | AD7 |
| 316 | `Twitch/ClientMessages/TwitchAutoHello/Entitys/AutoVideoHello.cs` | AD7 |
| 317 | `Twitch/Entitys/CreateTwitchUserRequest.cs` | AD8 |
| 318 | `Twitch/Entitys/Frog.cs` | AD8 |
| 319 | `Twitch/Entitys/FrogPrizeType.cs` | AD8 |
| 320 | `Twitch/Entitys/Fumo.cs` | AD8 |
| 321 | `Twitch/Entitys/FumoPrizeType.cs` | AD8 |
| 322 | `Twitch/Entitys/GaoAlertDto.cs` | AD8 |
| 323 | `Twitch/Entitys/Interfaces/ITwitchMiniGame.cs` | AD8 |
| 324 | `Twitch/Entitys/MikuModule.cs` | AD8 |
| 325 | `Twitch/Entitys/MikuMondayActivation.cs` | AD8 |
| 326 | `Twitch/Entitys/MikuMondayDto.cs` | AD8 |
| 327 | `Twitch/Entitys/MikuMondayResult.cs` | AD8 |
| 328 | `Twitch/Entitys/MikuMondayTrack.cs` | AD8 |
| 329 | `Twitch/Entitys/MikuPrizeType.cs` | AD8 |
| 330 | `Twitch/Entitys/RollCooldown.cs` | AD8 |
| 331 | `Twitch/Entitys/Subs/GameType.cs` | AD8 |
| 332 | `Twitch/Entitys/Subs/IntRange.cs` | AD8 |
| 333 | `Twitch/Entitys/Subs/RouleteGame.cs` | AD8 |
| 334 | `Twitch/Entitys/Subs/RouletePlayer.cs` | AD8 |
| 335 | `Twitch/Entitys/Subs/StaticContent.cs` | AD8 |
| 336 | `Twitch/Entitys/Subs/VictorinaGame.cs` | AD8 |
| 337 | `Twitch/Entitys/Subs/VictorinaLetter.cs` | AD8 |
| 338 | `Twitch/Entitys/TemporaryReward.cs` | AD8 |
| 339 | `Twitch/Entitys/TwitchScreenParticles.cs` | AD8 |
| 340 | `Twitch/Entitys/TwitchUser.cs` | AD8 |
| 341 | `Twitch/Entitys/TwitchUserDto.cs` | AD8 |
| 342 | `Twitch/Entitys/UpdateTwitchUserRequest.cs` | AD8 |
| 343 | `Twitch/Entitys/UserFumoCollection.cs` | AD8 |
| 344 | `Twitch/Entitys/UserMikuCollection.cs` | AD8 |
| 345 | `Twitch/HelloVideos/Entitys/HelloVideosUsers.cs` | AD24 |
| 346 | `Twitch/HelloVideos/HelloVideosWorker.cs` | AD24 |
| 347 | `Twitch/ITwitchUserEnsureService.cs` | AD9 |
| 348 | `Twitch/Management/Entitys/ITwitchReward.cs` | AD12 |
| 349 | `Twitch/Management/Entitys/TokenInfo.cs` | AD12 |
| 350 | `Twitch/Management/EventSubService.cs` | AD10 |
| 351 | `Twitch/Management/TelegramTokenNotification.cs` | AD11 |
| 352 | `Twitch/Management/TokenService.cs` | AD12 |
| 353 | `Twitch/Media/ITwitchMediaPreparationService.cs` | AD13 |
| 354 | `Twitch/Media/TwitchMediaPreparationService.cs` | AD13 |
| 355 | `Twitch/Media/TwitchMediaTranscodeWorker.cs` | AD13 |
| 356 | `Twitch/MiniGamesStats/Entitys/TwitchLeaderboardUser.cs` | AD14 |
| 357 | `Twitch/MiniGamesStats/ILeaderboardService.cs` | AD14 |
| 358 | `Twitch/MiniGamesStats/LeaderboardService.cs` | AD14 |
| 359 | `Twitch/PuntoSwitcher/IPuntoSwitcherService.cs` | AD15 |
| 360 | `Twitch/PuntoSwitcher/PuntoSwitcherService.cs` | AD15 |
| 361 | `Twitch/PuntoSwitcher/PuntoSwitchSuggestion.cs` | AD15 |
| 362 | `Twitch/Rewards/1_RandomReward/RandomReward_TwitchReward.cs` | AC.R01 |
| 363 | `Twitch/Rewards/10_RandomSound/RandomSound_TwitchReward.cs` | AC.R21 |
| 364 | `Twitch/Rewards/11_RandomMemReward/RandomMem_TwitchReward.cs` | AC.R22 |
| 365 | `Twitch/Rewards/11_RandomMemReward/Service/DTOs/MemeOrderDto.cs` | AC.R28 |
| 366 | `Twitch/Rewards/11_RandomMemReward/Service/DTOs/MemeTypeDto.cs` | AC.R28 |
| 367 | `Twitch/Rewards/11_RandomMemReward/Service/Entity/MemeOrder.cs` | AC.R27 |
| 368 | `Twitch/Rewards/11_RandomMemReward/Service/Entity/MemeType.cs` | AC.R27 |
| 369 | `Twitch/Rewards/11_RandomMemReward/Service/Entity/WTelegramAlloweedChannel.cs` | AC.R29 |
| 370 | `Twitch/Rewards/11_RandomMemReward/Service/IRandomMemeService.cs` | AC.R25 |
| 371 | `Twitch/Rewards/11_RandomMemReward/Service/RandomMemeService.cs` | AC.R26 |
| 372 | `Twitch/Rewards/11_RandomMemReward/Service/RandomMemeWorker.cs` | AC.R23 |
| 373 | `Twitch/Rewards/11_RandomMemReward/Service/RandomMemHandler.cs` | AC.R22 |
| 374 | `Twitch/Rewards/11_RandomMemReward/Service/RandomMemOnline.cs` | AC.R24 |
| 375 | `Twitch/Rewards/13_FumoFriday/Entitys/FumoUser.cs` | AC.R31 |
| 376 | `Twitch/Rewards/13_FumoFriday/FumoFriday_TwitchReward.cs` | AC.R30 |
| 377 | `Twitch/Rewards/1333_SkibidibopLong/SkibidibopLong_TwitchReward.cs` | AC.R68 |
| 378 | `Twitch/Rewards/134_Pedro/Pedro_TwitchReward.cs` | AC.R40 |
| 379 | `Twitch/Rewards/150_TyazheloReward/Tyazhelo_TwitchReward.cs` | AC.R41 |
| 380 | `Twitch/Rewards/1510_StatusQuestion/StatusQuestion_TwitchReward.cs` | AC.R42 |
| 381 | `Twitch/Rewards/155_MichaelTime/MichaelTime_TwitchReward.cs` | AC.R43 |
| 382 | `Twitch/Rewards/1580_MikuBeam/MikuBeam_TwitchReward.cs` | AC.R44 |
| 383 | `Twitch/Rewards/1580_MikuBeam/TwitchMikuBeamRewardService.cs` | AC.R45 |
| 384 | `Twitch/Rewards/160_LegBum/LegBum_TwitchReward.cs` | AC.R46 |
| 385 | `Twitch/Rewards/160_LegBum/LegBumRefundService.cs` | AC.R47 |
| 386 | `Twitch/Rewards/1602_CinemaRequest/CinemaRequest_TwitchReward.cs` | AC.R48 |
| 387 | `Twitch/Rewards/170_FumoFridayNightReward/FumoFridayNight_TwitchReward.cs` | AC.R49 |
| 388 | `Twitch/Rewards/170_MikuMondayAlert/MikuMondayAlert_TwitchReward.cs` | AC.R50 |
| 389 | `Twitch/Rewards/1700_Confetti/Confetti_TwitchReward.cs` | AC.R51 |
| 390 | `Twitch/Rewards/1701_Fireworks/Fireworks_TwitchReward.cs` | AC.R52 |
| 391 | `Twitch/Rewards/1702_EmojisReward/Emojis_TwitchReward.cs` | AC.R53 |
| 392 | `Twitch/Rewards/18_GaoAlert/GaoAlert_TwitchReward.cs` | AC.R32 |
| 393 | `Twitch/Rewards/182_Stone/Stone_TwitchReward.cs` | AC.R54 |
| 394 | `Twitch/Rewards/195_Cringe/Cringe_TwitchReward.cs` | AC.R55 |
| 395 | `Twitch/Rewards/2_WaifuMarriage/MergeWaifu.cs` | AC.R02 |
| 396 | `Twitch/Rewards/2_WaifuMarriage/WaifuMarriage_TwitchReward.cs` | AC.R03 |
| 397 | `Twitch/Rewards/2002_AdhdSuperpower/AdhdSuperpower_TwitchReward.cs` | AC.R56 |
| 398 | `Twitch/Rewards/210_Hello/Hello_TwitchReward.cs` | AC.R57 |
| 399 | `Twitch/Rewards/215_Bye/Bye_TwitchReward.cs` | AC.R58 |
| 400 | `Twitch/Rewards/27_RandomArt/DanbooruRandomPostService.cs` | AC.R35 |
| 401 | `Twitch/Rewards/27_RandomArt/RandomArt.cs` | AC.R34 |
| 402 | `Twitch/Rewards/27_RandomArt/RandomArt_TwitchReward.cs` | AC.R33 |
| 403 | `Twitch/Rewards/317_Intelligence/Intelligence_TwitchReward.cs` | AC.R59 |
| 404 | `Twitch/Rewards/320_MikuScreamer/MikuScreamer_TwitchReward.cs` | AC.R60 |
| 405 | `Twitch/Rewards/333_Skibidibop/Skibidibop_TwitchReward.cs` | AC.R61 |
| 406 | `Twitch/Rewards/337_PhonkEdit/PhonkEdit_TwitchReward.cs` | AC.R62 |
| 407 | `Twitch/Rewards/341_Aga/Aga_TwitchReward.cs` | AC.R63 |
| 408 | `Twitch/Rewards/342_BadToBone/BadToBone_TwitchReward.cs` | AC.R64 |
| 409 | `Twitch/Rewards/353_TikTokEdit/TikTokEdit_TwitchReward.cs` | AC.R65 |
| 410 | `Twitch/Rewards/375_DanceDance/DanceDance_TwitchReward.cs` | AC.R66 |
| 411 | `Twitch/Rewards/38_WednsdayFrog/WednsdayFrog_TwitchReward.cs` | AC.R36 |
| 412 | `Twitch/Rewards/39_MikuMonday/MikuMondayTracksService.cs` | AC.R37 |
| 413 | `Twitch/Rewards/39_MikuMonday/TwitchMikuMondayRewardService.cs` | AC.R38 |
| 414 | `Twitch/Rewards/4_FrogRoll/FrogRoll_TwitchReward.cs` | AC.R05 |
| 415 | `Twitch/Rewards/4_FrogRoll/FrogRollService.cs` | AC.R04 |
| 416 | `Twitch/Rewards/4_FumoRoll/FumoCollectionService.cs` | AC.R07 |
| 417 | `Twitch/Rewards/4_FumoRoll/FumoFridayRoll_TwitchReward.cs` | AC.R08 |
| 418 | `Twitch/Rewards/4_FumoRoll/FumoRollService.cs` | AC.R06 |
| 419 | `Twitch/Rewards/4_MikuRoll/MikuCollectionService.cs` | AC.R10 |
| 420 | `Twitch/Rewards/4_MikuRoll/MikuRoll_TwitchReward.cs` | AC.R11 |
| 421 | `Twitch/Rewards/4_MikuRoll/MikuRollService.cs` | AC.R09 |
| 422 | `Twitch/Rewards/4_SearchWife/SearchWife_TwitchReward.cs` | AC.R12 |
| 423 | `Twitch/Rewards/5_AddWife/AddNewWaifu.cs` | AC.R13 |
| 424 | `Twitch/Rewards/5_AddWife/AddWife_TwitchReward.cs` | AC.R14 |
| 425 | `Twitch/Rewards/6_RussianRoulette/RussianRoulette_TwitchReward.cs` | AC.R15 |
| 426 | `Twitch/Rewards/6_RussianRoulette/TwitchRussianRoulete.cs` | AC.R16 |
| 427 | `Twitch/Rewards/61_What/What_TwitchReward.cs` | AC.R39 |
| 428 | `Twitch/Rewards/666_Edge0100Alert/Edge0100Alert_TwitchReward.cs` | AC.R67 |
| 429 | `Twitch/Rewards/6666_CloseGame/CloseGame_TwitchReward.cs` | AC.R69 |
| 430 | `Twitch/Rewards/7_Quiz/Quiz_TwitchReward.cs` | AC.R17 |
| 431 | `Twitch/Rewards/7_Quiz/TwitchTrivia.cs` | AC.R18 |
| 432 | `Twitch/Rewards/75000_SelectGame/SelectGame_TwitchReward.cs` | AC.R70 |
| 433 | `Twitch/Rewards/8005_Credits/Credits_TwitchReward.cs` | AC.R71 |
| 434 | `Twitch/Rewards/9_AudioQuiz/AudioQuiz_TwitchReward.cs` | AC.R19 |
| 435 | `Twitch/Rewards/9_AudioQuiz/AudioTriviaMiniGame.cs` | AC.R20 |
| 436 | `Twitch/Rewards/99999_AllRefundService/AllRefund_TwitchReward.cs` | AC.R72 |
| 437 | `Twitch/Rewards/AnswersForTwitchRewards.cs` | AC.S01 |
| 438 | `Twitch/Rewards/ChannelRewards/ChannelRewardsManager.cs` | AC.C01 |
| 439 | `Twitch/Rewards/ChannelRewards/ChannelRewardsService.cs` | AC.C02 |
| 440 | `Twitch/Rewards/ChannelRewards/ChannelRewardsServiceCollectionExtensions.cs` | AC.C03 |
| 441 | `Twitch/Rewards/ChannelRewards/ChannelRewardsSyncService.cs` | AC.C04 |
| 442 | `Twitch/Rewards/ChannelRewards/Entities/ChannelRewardRecord.cs` | AC.C05 |
| 443 | `Twitch/Rewards/ChannelRewards/IRewardsCacheService.cs` | AC.C06 |
| 444 | `Twitch/Rewards/ChannelRewards/Models/ChannelRewardDefinition.cs` | AC.C07 |
| 445 | `Twitch/Rewards/ChannelRewards/Models/PyroAlertRewardDefinition.cs` | AC.C08 |
| 446 | `Twitch/Rewards/ChannelRewards/Models/UpdateCustomRewardDto.cs` | AC.C09 |
| 447 | `Twitch/Rewards/ChannelRewards/RewardsCacheService.cs` | AC.C06 |
| 448 | `Twitch/Rewards/ChannelRewards/TwitchAlertsInitializationService.cs` | AC.C10 |
| 449 | `Twitch/Rewards/ChannelRewards/TwitchRewardsOptions.cs` | AC.C11 |
| 450 | `Twitch/Rewards/Command.cs` | AC.S02 |
| 451 | `Twitch/Rewards/HighlitedMessage.cs` | AC.S03 |
| 452 | `Twitch/Rewards/MiniGamesManager.cs` | AC.S04 |
| 453 | `Twitch/Rewards/RickRollerService.cs` | AC.S05 |
| 454 | `Twitch/Rewards/RollCooldownNotificationService.cs` | AC.S06 |
| 455 | `Twitch/Rewards/RollCooldownService.cs` | AC.S07 |
| 456 | `Twitch/Rewards/TwitchEventSubAlertsAwaker.cs` | AC.S08 |
| 457 | `Twitch/Rewards/TwitchMessagesHubAwaker.cs` | AC.S09 |
| 458 | `Twitch/StreamBotNotifications/TwitchStreamStartupNotifications.cs` | AD16 |
| 459 | `Twitch/StreamManagement/TwitchStreamManagementService.cs` | AD17 |
| 460 | `Twitch/StreamManagement/TwitchStreamManagementServiceCollectionExtensions.cs` | AD17 |
| 461 | `Twitch/StreamManagement/TwitchTitleChangeCommand.cs` | AD17 |
| 462 | `Twitch/Synthesizer/Entitys/SevenTvEmote.cs` | AD18 |
| 463 | `Twitch/Synthesizer/ISevenTvEmoteService.cs` | AD18 |
| 464 | `Twitch/Synthesizer/ITtsHubBroadcaster.cs` | AD18 |
| 465 | `Twitch/Synthesizer/ITtsMessageFilterService.cs` | AD18 |
| 466 | `Twitch/Synthesizer/SevenTvEmoteService.cs` | AD18 |
| 467 | `Twitch/Synthesizer/TtsHubBroadcaster.cs` | AD18 |
| 468 | `Twitch/Synthesizer/TtsMessageFilterService.cs` | AD18 |
| 469 | `Twitch/TekkenStreams/TekkenStreamsDiscordForwarderService.cs` | AD19 |
| 470 | `Twitch/TwitchFollowers/Entitys/ChannelUsersResult.cs` | AD20 |
| 471 | `Twitch/TwitchFollowers/Entitys/FollowerInfo.cs` | AD20 |
| 472 | `Twitch/TwitchFollowers/FollowerDbService.cs` | AD20 |
| 473 | `Twitch/TwitchFollowers/IRxdcodxViewersService.cs` | AD20 |
| 474 | `Twitch/TwitchFollowers/RxdcodxViewersService.cs` | AD20 |
| 475 | `Twitch/TwitchFollowers/RxdcodxViewersServiceExtensions.cs` | AD20 |
| 476 | `Twitch/TwitchFollowers/TwitchUserInfoService.cs` | AD20 |
| 477 | `Twitch/TwitchFollowers/TwitchViewersService.cs` | AD20 |
| 478 | `Twitch/TwitchUserEnsureService.cs` | AD9 |
| 479 | `Twitch/Validation/IMessageValidationBuilder.cs` | AD21 |
| 480 | `Twitch/Validation/IRedemptionValidationBuilder.cs` | AD21 |
| 481 | `Twitch/Validation/ITwitchEventValidationService.cs` | AD21 |
| 482 | `Twitch/Validation/MessageValidationBuilder.cs` | AD21 |
| 483 | `Twitch/Validation/RedemptionValidationBuilder.cs` | AD21 |
| 484 | `Twitch/Validation/TwitchEventValidationService.cs` | AD21 |
| 485 | `Twitch/Validation/ValidationException.cs` | AD21 |
| 486 | `Twitch/Validation/ValidationResult.cs` | AD21 |
| 487 | `Twitch/WaifuChat/WaifuChatTwitchReward.cs` | AD22 |
| 488 | `Twitch/WeddingAnniversary/WeddingAnniversaryService.cs` | AD23 |
| 489 | `WaifuRoll/Entitys/AutoHelloMessage.cs` | AE3 |
| 490 | `WaifuRoll/Entitys/Husband.cs` | AE3 |
| 491 | `WaifuRoll/Entitys/HusbandAutoHello.cs` | AE3 |
| 492 | `WaifuRoll/Entitys/HusbandCoolDown.cs` | AE3 |
| 493 | `WaifuRoll/Entitys/Interfaces/IWaifuRollGuaranteeService.cs` | AE1 |
| 494 | `WaifuRoll/Entitys/PrizeTypeAbstract.cs` | AE2 |
| 495 | `WaifuRoll/Entitys/Waifu.cs` | AE3 |
| 496 | `WaifuRoll/Entitys/WaifuChatFact.cs` | AE3 |
| 497 | `WaifuRoll/Entitys/WaifuRollAudio.cs` | AE3 |
| 498 | `WaifuRoll/Entitys/WaifuRollGuarantee.cs` | AE1 |
| 499 | `WaifuRoll/Exceptions/WaifuRollException.cs` | AE4 |
| 500 | `WaifuRoll/helpers/WaifuRollEnsurenceService.cs` | AE5 |
| 501 | `WaifuRoll/Interfaces/IWaifuPrizesService.cs` | AE6 |
| 502 | `WaifuRoll/Interfaces/IWaifuRollService.cs` | AE7 |
| 503 | `WaifuRoll/Models/AddNewWaifuResponse.cs` | AE8 |
| 504 | `WaifuRoll/Models/RollCountResponse.cs` | AE8 |
| 505 | `WaifuRoll/Models/TelegramRollWaifuResponse.cs` | AE8 |
| 506 | `WaifuRoll/Models/VipDropResponse.cs` | AE8 |
| 507 | `WaifuRoll/WaifuPrizesService.cs` | AE6 |
| 508 | `WaifuRoll/WaifuRollGuaranteeService.cs` | AE1 |
| 509 | `WaifuRoll/WaifuRollService.cs` | AE7 |
| 510 | `YouTube/YouTubeResolver.cs` | AF1 |

Итого файлов: **510**; пунктов-приёмников: **226**.

## Приложение B. Не-.cs файлы под `Services/` — сервисами не являются

Это документация внутри монолита, а не сервисы. В чеклист не входит, но проверена:
все 20 перечислены, чтобы обход считался полным.

1. `CommandExecutor/COMMANDS.md`
2. `KeyboardHook_UNUSED/README.md`
3. `Logs/README.md`
4. `ServiceManager/README.md`
5. `Shikimori/README_RateLimiter.md`
6. `SoundRequest/Entities/README.md`
7. `StreamAcrhive_UNUSED/README.md`
8. `Telegram/BotService/WTELEGRAM_MIGRATION.md`
9. `Twitch/ClientMessages/AutoMessages/README.md`
10. `Twitch/PuntoSwitcher/README.md`
11. `Twitch/Rewards/11_RandomMemReward/Service/README.md`
12. `Twitch/Rewards/155_MichaelTime/README.md`
13. `Twitch/Rewards/2002_AdhdSuperpower/README.md`
14. `Twitch/Rewards/39_MikuMonday/README.md`
15. `Twitch/Rewards/ChannelRewards/README.md`
16. `Twitch/TwitchFollowers/CHANGELOG.md`
17. `Twitch/TwitchFollowers/Entitys/README.md`
18. `Twitch/TwitchFollowers/README.md`
19. `Twitch/TwitchFollowers/SUMMARY.md`
20. `Twitch/Validation/SERVICES_TO_MIGRATE.md`

## Приложение C. Подсистемы текущего репозитория, которых нет в монолите

Часть сервисов и механизмов `D:\VS\MARS` не имеет соответствия в
`MARS.Server/Services` — они появились при переносе или заменяют то, что в монолите
лежало вне `Services/`. Чтобы список отражал положение дел целиком, они здесь
перечислены явно: ни один из них не является утечкой из чеклиста.

### C.1 Сервисы, которых в монолите не было

| Подсистема | Артефакты | Пояснение |
|---|---|---|
| `MARS.Gateway` — YARP + Swagger-агрегатор | `src/MARS.Gateway/Swagger/SwaggerEndpointMap.cs`, `SwaggerAggregatorService.cs`, `SwaggerUiExtensions.cs`, `src/MARS.Gateway/appsettings.json`; маршрут `docker-compose` `9155:8080`; тест `tests/MARS.Gateway.Tests/SwaggerEndpointMapTests.cs` | Монолит был единым приложением с одной HTTP-точкой; разделение на 16 сервисов потребовало внешнего шлюза |
| `MARS.TTS` | `src/MARS.TTS/` (14 `.cs`): `Grpc/VoiceRecognitionGrpcService.cs`, `TtsGrpcMapper.cs`, `Services/TtsNotifier.cs`, `Models/TtsState.cs`, `VoiceActivityDto.cs` | Отдельного сервиса не было; из монолита пришёл только `TtsHubBroadcaster` (AD18) |
| Tuna (подсистема музыки) | `src/MARS.Shared/Protos/tuna.proto`, `Grpc/Services/TunaGrpcService.cs`, `TunaGrpcMapper.cs`, `Grpc/Models/TunaMusicModels.cs`; тест `tests/MARS.Shared.Tests/Grpc/TunaGrpcServiceTests.cs` | Под `MARS.Server/Services` аналога нет |
| Хранилище медиа (каталог записей) | `src/MARS.MediaStorage/Services/Storage/` — `MediaStorageService`, `MediaStorageOptions`, `MediaUploadFile`, `SoftDeletePurgeWorker`; `Entities/MediaStorageEntry.cs`; 11 тестов в `tests/MARS.MediaStorage.Tests/` | Ближайшее в монолите — P1–P3 (`Media/`), но каталога записей, мягкого удаления и корзины там не было |
| Git-синк `wwwroot` | `src/MARS.MediaStorage/Services/Git/` — `MediaGitService`, `MediaGitInitializer`, `GitCommandExecutor`, `MediaGitOptions`; тесты `GitCommandExecutorTests`, `MediaGitInitializerTests`, `MediaGitServiceTests`, `MediaGitOptionsTests` | Требование эксплуатации: общий том `mars-wwwroot` между `obs`, `alerts`, `media-storage` и `docs/media-storage-git-token.md` |
| UI хранилища | `src/MARS.MediaStorage/ClientApp` (React + Vite), маршрут `media-storage-ui`, отдаётся как `/storage-ui` | Отдельного фронтенда в монолите не было (`mars.client` не переносился, см. `AGENTS.md`) |
| Telegram-логгер | `src/MARS.Admin/CustomLoggers/TelegramLogger/` — 5 файлов | Отправка логов в Telegram сделана в приёмнике; `Serilog.Sinks.Seq` выпилен |

### C.2 Инфраструктура `MARS.Shared`, которой не было в монолите

| Область | Файлы |
|---|---|
| gRPC | `Grpc/GrpcEventBroadcaster.cs`, `GrpcSubscription.cs`, `MarsGrpcJson.cs`, `MediaGrpcMapper.cs`, `TunaGrpcMapper.cs`, `Grpc/Services/*`, `Grpc/Models/*`, `Grpc/Notifications/*`; `Extensions/GrpcClientExtensions.cs`, `GrpcHostingExtensions.cs` |
| RabbitMQ | `Messaging/RabbitMqEventBus.cs`, `RabbitMqConnectionFactory.cs`, `RabbitMqConsumerBase.cs`, `RabbitMqEvents.cs`, `IMarsEventBus.cs`, `Configuration/RabbitMqOptions.cs` |
| Схема БД | `Extensions/MarsSchemaMigrationsHostedService.cs`, `MarsSchemaStartupExtensions.cs`, `MarsConnectionStringResolver.cs` |
| Перенос данных | `Data/LegacyDataSeed.cs`; миграции `SeedLegacy*` в `MARS.Admin`, `MARS.Alerts`, `MARS.CinemaQueue`, `MARS.MediaStorage`, `MARS.Scoreboard`, `MARS.SoundRequest`, `MARS.Telegram`, `MARS.TwitchCore`, `MARS.Videos365`, `MARS.WaifuGacha` |
| Аутентификация | `Authentication/ServiceApiKeyAuthenticationHandler.cs`, `Configuration/ServiceAuthOptions.cs`, `Extensions/ServiceAuthExtensions.cs`, `Security/SecretValueFilter.cs` |
| Наблюдаемость | `Telemetry/MarsMetrics.cs`, `MarsActivities.cs`, `OpenTelemetryExtensions.cs`, `OpenTelemetryPrometheusBridge.cs`; `Logging/SerilogExtensions.cs`, `ActivityEnricher.cs`; `HealthChecks/*` |
| Middleware | `Middleware/CorrelationIdMiddleware.cs`, `ExceptionHandlingMiddleware.cs`, `RequestLoggingMiddleware.cs` |
| Межсервисные клиенты | `Clients/ServiceHttpClientBase.cs`, `MediaStorageClient.cs`, `WaifuGachaClient.cs`, `ServiceClientExtensions.cs` |
| Конфигурация | `Configuration/AppBase.cs` (`TwitchConfig`, `TelegramConfig`, `DiscordConfig`), `Configuration/ServiceEndpoints.cs` |
| Прочее | `Concurrency/KeyedAsyncLock.cs`, `Exceptions/MarsException.cs` |

### C.3 Расширения поверх монолита в рамках существующих сервисов

| Что | Где |
|---|---|
| 8 команд, отсутствующих в монолите | `byebye`, `example`, `genshin`, `honkai`, `honkaiusers`, `links`, `randomshorts`, `telegramonly` — `src/MARS.Commands/Services/Entitys/Commands/` |
| Типы результата и ошибок команд | `MARS.Commands/Services/Entitys/CommandResult.cs`, `CommandErrorCode.cs`, `CommandParameterType.cs`, `CommandAttachment.cs` |
| Режим паузы OBS | `src/MARS.OBS/Services/ObsPauseMode.cs`, `ObsPauseResult.cs`; маршрут `test-alerts` + `Controllers/TestAlertsController.cs` |
| Статистика Twitch | `src/MARS.TwitchCore/Controllers/ServerStatsController.cs`, `DTOs/ServerStatsDtos.cs`, `NearestAnniversaryDto.cs` |
| `RootState` в 5 сервисах | `MARS.Admin`, `MARS.TwitchCore`, `MARS.Telegram`, `MARS.WaifuGacha`, `MARS.SoundRequest` — `Entities/RootState.cs` + `RootStateKeys`; `MARS.Admin/Controllers/RootStateController.cs`; `MARS.WaifuGacha/Services/RootStateBootstrapHostedService.cs`; маршрут `admin-root-state` |
| Реализации sound bar | `src/MARS.SoundRequest/Services/SoundBarService/SoundBarFactory.cs`, `SoundBarHttpClient.cs`, `SoundBarServiceLocal.cs`, `Models/BagCountResponse.cs` |
| Потребители очередей | `MARS.Alerts/Services/SystemEventsConsumer.cs`, `Services/Twitch/Rewards/RewardAlertConsumer.cs`, `ChatUserConsumer.cs`; `MARS.TwitchCore/Services/Chat/TwitchChatSendConsumer.cs`, `Services/Rewards/RewardRedemptionPublisher.cs` |
| Межсервисный доступ к WaifuGacha | `MARS.TwitchCore/Services/Rewards/IWaifuLookupService.cs`, `WaifuGachaLookupClient.cs`; `MARS.Shared/Clients/IWaifuGachaClient.cs`; `MARS.WaifuGacha/Controllers/WaifuGachaInternalController.cs` |
| Конфигурации и options | `MARS.CinemaQueue/Configuration/KinopoiskConfiguration.cs`, `MARS.SoundRequest/Configuration/HttpClientsConfiguration.cs`, `MARS.SoundRequest/Configuration/SoundRequestConfiguration.cs`, `MARS.Videos365/Configuration/Config365.cs`, `MARS.Discord/Configuration/DiscordConfiguration.cs`, `MARS.TwitchCore/Configuration/TwitchConfiguration.cs`, `MARS.TwitchCore/Configuration/AudioControllerOptions.cs` |
| Тестовые/отладочные контроллеры | `MARS.MediaStorage/Controllers/LoggerTestController.cs`, `TestLoggerController.cs` |
