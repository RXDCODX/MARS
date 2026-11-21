# План: автоматизация контрактов оверлея и ручных реестров

## Контекст и цель

Пользователь не хочется вручную править 6–7 файлов при добавлении новой награды/события оверлея.
Выбран **вариант A: рефлексия без изменения wire-формата**. Выполнять в отдельном git worktree,
последовательно, одной веткой/PR на фазу. Объём: фазы 1–3 полного аудита.

## Источник истины сегодня (до правок)

Одно новое событие оверлея = правки в:
1. `src/MARS.Shared/MARS.Shared.Contracts/TelegramusEvent.cs` (свойство + `EventOneofCase` + цепочка `EventCase`);
2. `MARS.Alerts.Presentation.Overlay/Hubs/Interfaces/IAlertsHub.cs` (метод);
3. `Hubs/OverlayEventDispatcher.cs` (ветка switch);
4. `Hubs/overlay-hub.manifest.json` (имя);
5. `tests/MARS.Alerts.Tests/Hubs/OverlayHubManifestTests.cs` (хардкод `ExpectedEventNames`);
6. `MARS.ClientUi/src/shared/api/realtime/overlayEvents.ts` (`OVERLAY_EVENT_NAMES` + `OverlayEventArgs`);
7. `overlayEvents.manifest.test.ts` (хардкод количества 36).

Крест-сервисные края: `MARS.Twitch.HttpTelegramusEventSink`, `MARS.Telegram.OverlayTelegramusEventPublisher`
сериализуют `TelegramusEvent` в JSON → `MARS.Alerts.Api/Controllers/OverlayInternalController.cs`
(`UnmappedMemberHandling.Disallow`). Wire (имена методов SignalR, формы payload) потребляют
OBS-браузерные источники и ~20 компонентов ClientUi — **не меняем**.

Уже автоматизировано хорошо и служит эталоном: `RewardHandlerRegistration.DiscoverHandlerTypes()`,
`CommandFactory`, `WolverineMessagingExtensions` (`Implements<>`), `SwaggerEndpointMap`, SignalRSwaggerGen.

## Фаза 1 — ветка `feat/alerts-overlay-event-automation`

Сервер:
- `TelegramusEvent.cs`: удалить `EventOneofCase` + ручную цепочку; `EventCase` → вычисляемое строковое
  свойство через рефлексию (имя сохраняется ⇒ логи Twitch/Telegram компилируются без правок);
- `OverlayEventDispatcher.cs`: кеш `PropertyInfo → (methodName, hasPayload)`, `hasPayload = тип != EmptyEvent`,
  dispatch через нетипизированный `IHubContext<AlertsHub>.Clients.All.SendAsync(...)`;
- `OverlayHubEventSink.cs`, `OverlayInternalController.cs`: перейти на новый dispatch;
- `IAlertsHub.cs` оставить как контракт SignalRSwaggerGen; синхронность с юнионом проверяется рефлексией.

Генерация контракта:
- `overlay-hub.manifest.json` → `[{name, hasPayload}]`, генерируется из рефлексии юниона;
- env-gated тест-генератор в `tests/MARS.Alerts.Tests/Hubs/` (`MARS_REGEN=1`);
- `scripts/windows/regenerate-overlay-contract.ps1` + unix-двойник (паритет обязателен);
- `MARS.ClientUi/scripts/generate-overlay-events.mjs` + npm script → генерирует `overlayEvents.ts`;
- `MARS.Alerts.Presentation.Overlay.csproj`: EmbeddedResource остаётся.

Тесты (убрать хардкод): `OverlayHubManifestTests.cs`, `OverlayHubContractTests.cs`,
`OverlayEventDispatcherTests.cs` (generic-цикл по свойствам юниона), `overlayEvents.manifest.test.ts`.

Итог: новое событие = 2 правки на сервере (юнион + интерфейс) + 1 фронт-компонент.

## Фаза 2 — ветка `feat/union-relay-automation`

- `ScoreboardEvent.cs`, `SoundRequestEvent.cs`: тот же рецепт;
- `ScoreboardHubRelay.cs`, `SoundRequestHubRelay.cs`: рефлексивный dispatch; явно решить судьбу
  молча отбрасываемых веток Scoreboard;
- `VoiceEvent.cs`: удалить мёртвый юнион (TTS публикует без подписчика) отдельным коммитом;
- переписать `ScoreboardHubRelayTests.cs`, `SoundRequestHubRelayTests.cs`.

## Фаза 3a — ветка `fix/admin-service-registry`

- удалить дубль `MARS.Admin.Application/Configuration/ServiceEndpoints.cs`;
- `ServiceManager.BuildServiceRegistry()` — рефлексия по `ServiceEndpoints` + `ServiceNameAttribute`
  (фикс: в реестре 12 из 14 сервисов); дописать `ServiceManagerTests.cs`, `ServiceNameAttributeTests.cs`.

## Фаза 3b — ветка `refactor/media-payload-mapper`

- `MarsMedia.cs`: удалить двойники `MediaTypeKind`/`MediaAlertPriorityKind`/`*Payload`;
- `MediaPayloadMapper.cs`: ~300 строк ручного маппинга + 4 enum-свича → отражение/конвенция;
- 8 call-сайтов (включая `MARS.Telegram.TelegramRandomMemService.cs:273`), wire хранит
  `OverlayPayloadWireFormatTests.cs`.

## Фаза 3c — ветка `refactor/mime-table`

- создать единую таблицу extension → MediaType/MIME/категория в существующем проекте Shared/Platform
  (выбор проекта по `mars-project-structure`);
- заменить 11 ручных копий: TwitchRewards `MediaTypeExtensions`, MediaStorage `StringExtension`,
  `MediaStorageService.ResolveMediaType`, `MediaPath.ContentTypes`, `MediaStorageController`,
  `Index.cshtml.cs`, `IndexModel.Navigation.cs`, Discord `VideoExtensions`, WaifuGacha `WaifuRollController`,
  Admin `StreamArchiveController`, Telegram `MemoryStorage`;
- `ClientUi/src/shared/MIME_types.ts` — генерировать из таблицы.

## Фаза 3d — ветка `feat/obs-component-routes-registry`

- `obsComponentRoutes.tsx`: само-декларация экранов + `import.meta.glob`, один источник вместо четырёх;
  фикс разошедшегося prefetch; упростить `OBS_Components/index.ts`, точечно `allRoutes.ts`.

## Отдельные быстрые ветки

- `fix/twitch-minigames-di`: зарегистрировать `MiniGamesManager` и `ITwitchMiniGame` в DI (сейчас пусто).

## Не включаем осознанно

YARP-инвентарь в `Gateway/appsettings.json` (скилл `gateway-route-add`), EventSub-подписки
(уникальный внешний конфиг), поштучные регистрации в Twitch `Program.cs`, Wolverine-список сборок
(семантика discovery), `hubAdapter.ts` (замороженное легаси), `manual/index.ts` (отдельная большая фаза).

## Порядок работы и проверки

1. Все работы — в отдельном worktree; каждая фаза — своя ветка от `main`, коммиты маленькими шагами (TDD).
2. Перед пушем: `scripts/windows/verify.ps1`; для затронутого ClientUi —
   `cd src/MARS.ClientUi && npx tsc -b --noEmit && yarn test && yarn build`.
3. Тесты MTP: фильтры после `--` (`--filter-class`, `--filter-method`).
4. После изменений кода: `graphify update .`.
5. Пуш только своей ветки, PR в `main` с разделами Что/Зачем/Как/Проверка/Миграция и откат/Сквозные правки.
6. В конце проверить полный diff и остаточные ссылки на удалённые символы.
7. Задача считается завершённой только после пуша ветки и открытого PR в `main`.

## Прогресс

- **Фаза 1** — выполнено, ветка `feat/alerts-overlay-event-automation`, PR [#116](https://github.com/RXDCODX/MARS/pull/116) (база — родительская ветка с открытым PR #115; база `main` включила бы 198 посторонних файлов). Диспетчер, генерация манифеста и `overlayEvents.ts`, рефлексивный `EventCase`, тесты без хардкод-списков.
- **Фаза 2** — выполнено, ветка `feat/union-relay-automation`. Общий `OneofEventContract` + `OneofHubRelay`; Scoreboard/SoundRequest-реле и их тесты; `VoiceEvent` обездублирован (удаление публикаций TTS оставлено отдельным поведение-изменением).
- **Фаза 3a** — выполнено, ветка `fix/admin-service-registry`. `ServiceEndpoints` перенесён в `MARS.Shared.Contracts` (Platform-версия — тонкий наследник для совместимости); реестр админки строится из `[ManagedService]` на свойствах, Shikimori и Admin теперь видны (14 вместо 12); дубль Admin.ServiceEndpoints и неиспользуемый ServiceNameAttribute удалены.
- Дальше: фазы 3b–3d (MediaPayloadMapper, MIME-таблица, OBS-роуты).

### Известные красные шаги полного гейта (не связаны с изменениями)

`MARS.Admin.Tests` — «Foreground threads were left running» при 100/100 зелёных; `MARS.Gateway.Tests` — 6 устаревших тестов про auto-format (workflow уже открывает PR, а не amend); `MARS.WaifuGacha.Tests` — 2 теста падают по Docker socket; `routesManifest.test.ts` — CRLF/LF в `routes.generated.json`. Сборка Release, CSharpier и правило одного класса зелёные.
