# План: TwitchRewards — прямой хаб, плоские контракты 1:1 с монолитом, REST-клиент у владельца

Обсуждён и принят в Plan mode 2026-11-22. Целевое место в репозитории после перехода в Build:
`.opencode/plans/alerts-rewards-direct-hub-and-contracts.md`.

## Контекст

Источники исследования:
- Текущая линия: `feat/alerts-rewards-refactor-duplicate` (HEAD `6f88c013`), поверх — наработка
  `f7165c34` (рефлексивный dispatch + генерация манифеста, ветка `feat/alerts-overlay-event-automation`).
- Монолит-эталон: `MARS_old/MARS.Projects/MARS.Server` — `Hubs/TelegramusHub.cs`,
  `Hubs/Interfaces/ITelegramusHub.cs`, `Services/Twitch/Rewards/**`, `Services/Twitch/Entitys/GaoAlertDto.cs`.
- Проблема: 21 хендлер TwitchRewards шлют события через конверт `TelegramusEvent` +
  `IOverlayEventSink` → `OverlayHubEventSink` → `OverlayEventDispatcher` (словарь), а 2 хендлера
  (`RandomSoundHandler`, `SearchWifeHandler`) уже ходят в `IHubContext<AlertsHub, IAlertsHub>` напрямую.
  Двойная модель в одном проекте. 14 `*Event`-DTO несут 18 полей `*Json` (JSON-в-JSON, наследие proto `bytes`),
  из-за чего на фронте существует декодер-слой `overlayPayload.ts` (13 файлов-потребителей).

Расклад 48 наследников `RewardHandlerBase`:
- 21 — через `IOverlayEventSink` (фаза A переводит их);
- 2 — уже напрямую `IHubContext`;
- 25 — не публикуют события оверлея вообще (чат/мини-игры/REST) — не трогаем.

Судьба тройки из `MARS.Alerts.Application` (корень), которая держит порт `IOverlayEventSink`:
- `RickRollerService` (потребители: 24 хендлера TwitchRewards + RewardBound + HighlitedMessage) и
  `RewardBoundAlertDispatcher` (перенос монолитного `TwitchEventSubAlertsAwaker`: редемпшен без
  собственного хендлера → алерт по `TwitchGuid`/цене из MediaStorage; вызывается
  `RewardInputMessageListener`, `CostMatchedRewardListener`) → переезжают в
  `MARS.Alerts.Application.TwitchRewards`;
- `TriggerWordAlertDispatcher` (перенос `TwitchMessagesHubAwaker.ClientKeyTriggerAlert`: триггерное
  слово в чате → случайный привязанный алерт; вызывается `TriggerWordChatListener`,
  `TriggerWordRewardInputListener`) → переезжает в `MARS.Alerts.Application.TwitchMessages`
  (его csproj уже ссылается на Presentation.Overlay).

После переносов корневой `MARS.Alerts.Application` перестаёт знать об оверлее — порт умирает сам,
менять правило `mars-project-structure` не пришлось.

Решения по открытым вопросам:
1. Промежуточный подпроект — только `MARS.Alerts.Models.Overlay` в фазе B; для фазы A не нужен.
2. Порядок: сначала бекенд (фазы A–C), фронт (ClientUi/OBS-компоненты, 13 файлов декодеров) —
   отдельной задачей после; между фазой B и подгонкой фронта оверлей на старом проводе не работает —
   осознанно.
3. Helix-зависимости: REST, реализация клиента у `src/MARS.Twitch` (образец — `WaifuGachaClient`);
   Alerts добавляет ссылку и регистрацию. Закрепить правилом-скиллом.

## Фаза A — доставка без адаптеров (wire не меняется)

Ветка: `feat/alerts-overlay-direct-hubcontext` (от текущей линии, включить наработку `f7165c34`).

1. Переносы:
   - `src/MARS.Alerts/MARS.Alerts.Application/Services/RickRollerService.cs` →
     `MARS.Alerts.Application.TwitchRewards/Services/RickRollerService.cs`;
     `IOverlayEventSink` → `IHubContext<AlertsHub, IAlertsHub>`, вызов
     `overlaySink.BroadcastAsync(new TelegramusEvent { Alert = ... })` →
     `hubContext.Clients.All.Alert(new AlertEvent { ... })`.
   - `Application/RewardInput/RewardBoundAlertDispatcher.cs` + `RewardInputMessageListener.cs` +
     `CostMatchedRewardListener.cs` → `Application.TwitchRewards/RewardInput/`.
   - `Application/TriggerWords/TriggerWordAlertDispatcher.cs` + `TriggerWordChatListener.cs` +
     `TriggerWordRewardInputListener.cs` → `Application.TwitchMessages/TriggerWords/`.
2. 21 хендлер → `IHubContext<AlertsHub, IAlertsHub>`; обёртки `TelegramusEvent` убираются;
   прямые вызовы как в монолите (`hubContext.Clients.All.GaoAlert(new GaoAlertEvent { ... })`).
3. `OverlayInternalController`: приватный метод
   `typeof(TelegramusEvent)` → активная ветка → `typeof(IAlertsHub).GetMethod(name)` →
   `Invoke(hubContext.Clients.All, payload|нет аргументов)` — логика `f7165c34` внутрь контроллера,
   без отдельного класса и без словарей.
4. Удалить: `Application/Overlay/IOverlayEventSink.cs`, `Presentation.Overlay/Hubs/OverlayHubEventSink.cs`,
   `Presentation.Overlay/Hubs/OverlayEventDispatcher.cs`, `Program.cs:62` регистрацию
   `AddSingleton<IOverlayEventSink, OverlayHubEventSink>`, `tests/MARS.Alerts.Tests/TestOverlayHub.cs`.
5. `Program.cs`: обновить Wolverine-сборки (`RewardInputMessageListener` теперь в TwitchRewards-сборке;
   TriggerWord-листенеры — в TwitchMessages-сборке; проверить, что все листенеры реально обнаруживаются).
6. Тесты: `RewardBoundAlertDispatcherTests` (перенос), `OverlayEventDispatcherTests` →
   покрытие контроллера, обновить затронутые `Hubs/*` тесты.
7. Проверка: `dotnet build` + `dotnet test tests/MARS.Alerts.Tests`, точечно по `dotnet-test-run`
   (`--filter-class` после `--`), `scripts/windows/verify.ps1`, `graphify update .`.

## Фаза B — контракты 1:1 с монолитом

Ветка: `feat/alerts-overlay-contracts`.

1. Новый проект `MARS.Alerts.Models.Overlay` (Models-деление; по `mars-project-structure`):
   ссылки на `MARS.Shared.Contracts` (TwitchUser, MediaDto), `MARS.WaifuGacha.Models.Rest`
   (Waifu, Husband, Fumo, Frog, MikuModule; Models→Models — законно), PackageReference `SignalRSwaggerGen`
   (атрибуты на IAlertsHub). Не забыть `MARS.slnx` + solution folders; Docker build contexts проверить
   в «Сквозных правках».
2. Перенести из `MARS.Shared.Contracts`: 33 `*Event`, `TelegramusEvent` (остаётся конвертом только для
   межсервисного HTTP-входа), `IAlertsHub`.
3. Плоские объекты вместо `*Json` (18 полей в 14 классах). Сигнатуры — один `*Event`-объект на метод.
   Целевые формы по `ITelegramusHub` монолита:
   - `GaoAlertEvent { Id, TwitchUser, IsJustText, JustText }`;
   - `FumoRollEvent { Fumo, TwitchUser, CollectedCount, TotalCount }`;
   - `MikuMikuBeamEvent { Users: List<TwitchUser> }`;
   - `TikTokEditEvent { Guid, Text }`;
   - `HighliteEvent { Message, Color, FaceUrl }`;
   - `NewMessageEvent { Id, Message }`; `DeleteMessageEvent { Id }`;
   - `PrizesEvent { Prizes }`; `ScreenParticlesEvent { Particles }`;
   - `AudioQuizStartEvent { Round }`; `MikuMondayEvent { MikuMonday }`; `AdhdConfigEvent { Config }`;
   - `AllRefundEvent { User }`; `FumoFridayEvent { User }`; и остальные по сверке с монолитом.
   - `EmptyEvent`-ветки остаются без payload.
4. B2 отдельными коммитами: медиа-ветки `Alert/Alerts/RandomMem` (и `Highlite`/`ScreenEmojisParticles`)
   на `MediaDto`-форму 1:1 (в монолите `Alert(MediaDto info)`, `RandomMem(MediaDto mediaInfo)`);
   соприкасается с планом `refactor/media-payload-mapper` (удаление `MediaPayloadMapper`/двойников
   MarsMedia) — согласовать порядок, не смешивать в один коммит.
5. Обновить бекенд-publisher'ов в той же фазе: `MARS.Telegram` `OverlayTelegramusEventPublisher`,
   `MARS.Twitch` `HttpTelegramusEventSink`, `MARS.OBS` `Program.cs`;
   `OverlayInternalController` — `UnmappedMemberHandling.Disallow` остаётся, форма конверта обновлена.
6. Обновить `OverlayPayloadWireFormatTests` под новую camelCase-форму; `overlay-hub.manifest.json`
   регенерировать (скрипты уже есть из `f7165c34`).
7. ClientUi/OBS-фронт НЕ трогаем — отдельная задача после (решение пользователя).
8. Проверка: полный `verify.ps1` + сборки Telegram/Twitch/OBS.

## Фаза C — REST-клиент у MARS.Twitch + правило-скилл

Ветка: `feat/twitch-rest-users-client`.

1. Новый проект `MARS.Twitch.Models.Rest` — DTO межсервисного REST (канонический `TwitchUserDto`
   и остальные контракты).
2. Новый проект `MARS.Twitch.Infrastructure.Http` — `ITwitchUsersClient` +
   `TwitchUsersClient : ServiceHttpClientBase` (образец — `MARS.WaifuGacha.Infrastructure.Http/WaifuGachaClient`;
   ServiceApiKey подставляет базовый класс).
3. Endpoint в `MARS.Twitch.Api/Controllers/TwitchUsersController.cs`: полный пользователь по логину
   через Helix (`ITwitchAPI` уже в `MARS.Twitch.Infrastructure`) — закрывает дыру 1:1
   (сейчас `GaoAlertHandler` шлёт анонимный `{ DisplayName }` без аватара).
4. Alerts: две `ProjectReference` (`MARS.Twitch.Models.Rest`, `MARS.Twitch.Infrastructure.Http`) +
   `AddMarsServiceClient<ITwitchUsersClient, TwitchUsersClient>(...)` в `Program.cs`.
5. `GaoAlertHandler` — монолитная логика 1:1: пробел → `IsJustText`; `^[a-zA-Z0-9_]+$` и длина ≤ 25;
   REST-lookup полного TwitchUser; фолбэк `JustText` при неудаче.
6. Новый скилл `mars-rest-service-clients` — зеркально `.opencode/skills/` и `.mimocode/skills/`
   (правило репозитория: зеркальные копии), строка в таблицу Skills в `AGENTS.md`.
   Содержание правила: «Межсервисный REST-контракт и клиент живут у сервиса-владельца
   (`MARS.<Service>.Models.Rest` + `MARS.<Service>.Infrastructure.Http`); потребитель добавляет
   ProjectReference и `AddMarsServiceClient`, отдельные проекты под одно изменение не плодятся».
7. C2 (следующая итерация): пофайловый аудит остальных 47 хендлеров против
   `MARS_old/MARS.Server/Services/Twitch/Rewards/**`; каждый новый API-запрос — через REST-клиент
   `MARS.Twitch` по правилу из п. 6.

## Порядок работы и проверки

1. Каждая фаза — своя ветка от текущей линии (`feat/alerts-rewards-refactor-duplicate` с `f7165c34`),
   коммиты маленькими шагами, TDD (Red → Green → Refactor).
2. Перед пушем: `scripts/windows/verify.ps1`; затронутые тесты по `dotnet-test-run` (фильтры MTP
   после `--`). Frontend в фазах A–C не трогаем.
3. После изменений кода: `graphify update .`.
4. Пуш только своей ветки, PR в `main` с разделами Что/Зачем/Как/Проверка/Миграция и откат/Сквозные правки.
5. Перед завершением: полный diff, остаточные ссылки на удалённые символы
   (`IOverlayEventSink`, `OverlayHubEventSink`, `OverlayEventDispatcher`, `BroadcastAsync` у конверта,
   `*Json`-поля), `MARS.slnx`, CI-матрица `tests` при новых тест-проектах.
6. Задача считается завершённой только после пуша ветки и открытого PR в `main`.
