# План: восстановление пути активации оверлеев из MARS.Twitch

> Аудит 2026-11-23: путь `src/MARS.Twitch` → RabbitMQ (Wolverine) →
> MARS.Alerts / MARS.Scoreboard / MARS.SoundRequest → SignalR-хабы
> (`/hubs/overlay`, `/hubs/scoreboard`, `/hubs/soundrequest`) →
> `src/MARS.ClientUi/src/overlays/`.
>
> Все находки проверены чтением файлов; критические — дважды (сабагент + основная сессия).
> Каркас пути корректен (маршруты YARP на все три хаба, манифест 36 событий =
> `IAlertsHub` = `OVERLAY_EVENT_NAMES` с живой сверкой в vitest). Разрывы — ниже.

## Вердикт аудита

Бизнес-логика **не** приведена в порядок; контракт имён актуален, контракт поведения разорван:

- в production из MARS.Twitch публикуется только синтетический hello-video
  (`HelloVideoNotifier` через зарегистрированный `HelloVideoWorker`);
- 4 реальных продюсера событий не зарегистрированы в DI → голодают ~15 листенеров
  MARS.Alerts и 27 из 36 событий оверлея отвалены от источника;
- `ScoreboardEvent` никем не публикуется → `/hubs/scoreboard` никогда не рассылает состояние;
- 8 событий `IAlertsHub` не имеют продюсера вообще, `Highlite` — мёртвый продюсер;
- клиент шлёт в хаб вызовы, которых нет (`TwitchMsg`, `ObsFreeze`…), а живой `MuteAll`
  несовместим по аргументам.

## Как выполнять

- Каждый пункт — по возможности TDD: Red → Green → Refactor.
- Фаза = своя ветка `fix/*` (или `feat/*`) + отдельный PR в `main`; не смешивать фазы.
- Перед PR: `scripts/windows/verify.ps1` (или unix-эквивалент); затронутый фронтенд:
  `cd src/MARS.ClientUi && npx tsc -b --noEmit && yarn test && yarn build`.
- После правок кода: `graphify update .`.
- Приоритеты: **P0** — путь мёртв, чинить первыми; **P1** — сломанные фичи/контракты;
  **P2** — гигиена, можно отдельным «chore» PR.
- Пристрелка каждого P0-пункта — на локальном compose-стенде реальным событием.

---

## Фаза 1 — Источник: публикация событий из MARS.Twitch (P0)

Причина: `src/MARS.Twitch/MARS.Twitch.Host/Program.cs` — 15 `AddHostedService`, ни один
не продюсер; Docker entrypoint — `MARS.Twitch.Host.dll` (Dockerfile:78). Регистрации
удалены коммитами `1e359ce3` («process reward redemptions in raw EventSub pipeline») и
`52765ec5` («Wolverine pub/sub migration»), замещающего механизма не появилось.

- [ ] **1.1** P0 Зарегистрировать в `Program.cs` все четыре продюсера:
      `RewardRedemptionPublisher`, `TwitchMessagesPublisher`, `EventSubRawForwarder`,
      `TwitchCommandForwarderService`. Учесть скоуп `IMessageBus` (паттерн
      `HelloVideoWorker`: scoped-нотификатор + `IServiceScopeFactory`).
- [ ] **1.2** P0 Удалить/переписать ложный комментарий `Program.cs:239-240`
      («Издатель reward-событий работает через Wolverine-лисенер, регистрируемый
      автоматически при добавлении шины») — он маскирует разрыв.
- [ ] **1.3** P0 Защитный тест: все классы MARS.Twitch, публикующие шинные события,
      зарегистрированы в DI (например, скан `PublishAsync`-классов по сборке +
      проверка `IHostedService`-регистраций; стиль `PublicContractVerifier`).
- [ ] **1.4** P0 Тесты на `TwitchCommandForwarderService` и `EventSubRawForwarder`
      (сейчас покрытие нулевое; у `TwitchMessagesPublisher` — задвоенные тест-классы,
      слить в один).
- [ ] **1.5** P0 Проверка на стенде: реальный redemption / сообщение чата / `!`-команда →
      сообщения в exchanges `mars.reward-redeemed`, `mars.chat-event-message`,
      `mars.event-sub-raw`, `mars.command-execute-requested` (RabbitMQ UI).

## Фаза 2 — Scoreboard: рассылка состояния (P0)

Причина: repo-wide нет ни одного `new ScoreboardEvent` / `BroadcastAsync` —
`EventBroadcaster<ScoreboardEvent>` (`MARS.Scoreboard.Host/Program.cs:32`) не питается;
`ScoreboardService` мутирует молча; `/scoreboard-admin` вообще не открывает хаб
(`useScoreboardHub` зовёт только `Scoreboard.tsx:15`), правки копятся в
`pendingServerCommands` (`scoreboardStore.ts:422-448`, кап 40).

- [ ] **2.1** P0 Запитать бродкастер: `ScoreboardService.UpdateStateAsync /
      SetVisibilityAsync / UpdatePlayerScoreAsync / SetPlayerFinalAsync` →
      `EventBroadcaster<ScoreboardEvent>` (`StateUpdated`) после каждой мутации. Тест.
- [ ] **2.2** P0 `AdminPanel.tsx` (`/scoreboard-admin`) открывает scoreboard-хаб
      (переиспользовать `useScoreboardHub`) + flush `pendingServerCommands` при
      коннекте. Тест (`useScoreboardHub.test.ts`).
- [ ] **2.3** P1 Relay-устойчивость: `ScoreboardHubRelay` и `SoundRequestHubRelay`
      ловят только `OperationCanceledException` — исключение в send останавливает луп
      навсегда. Сделать log-and-continue. Тест с бросающим клиентом.
- [ ] **2.4** P1 Судьба `ScoreboardHub.UpdatePlayerScore/SetPlayerFinal` (вызывающих
      нет): подключить в панели или удалить из хаба.
- [ ] **2.5** P2 Неиспользуемый REST-клиент `shared/api/http-clients/Scoreboard.ts`
      (экспорт `shared/api/index.ts:31`, потребителей нет): использовать или удалить.

## Фаза 3 — События хаба без продюсера (P1)

Матрица 36 методов `IAlertsHub`: 27 с живым продюсером, 8 без продюсера
(`UpdateWaifuPrizes`, `UpdateFumoPrizes`, `UpdateFrogPrizes`, `UpdateMikuPrizes`,
`PostTwitchInfo`, `AutoMessage`, `Explosion`, `LeroyAlert`), 1 с мёртвым (`Highlite`).

- [ ] **3.1** P1 `Update*Prizes` (×4): продюсер в MARS.Alerts — там, где призы
      меняются (хендлеры роллов/REST WaifuGacha), либо листенер событий WaifuGacha.
      Клиент уже подписан (`telegramusHubStore.ts:392-418`).
- [ ] **3.2** P1 `PostTwitchInfo`: продюсер при подключении (`AlertsHub.OnConnectedAsync`,
      паттерн `AdhdConfig`) или из Twitch через новый контракт. ⚠️ Без `clientId`/`secret`
      — только публичные поля (сейчас `twitchStore.ts:45` ждёт секреты в эфире).
- [ ] **3.3** P1 `AutoMessage`: продюсер из `AutoMessagesHandler` (MARS.Twitch) →
      контракт → листенер MARS.Alerts → хаб. Клиент подписан
      (`AutoMessageBillboard.tsx:141`). Альтернатива — удалить событие+подписку.
- [ ] **3.4** P1 `Explosion`: продюсер (награда или команда через
      `InternalCommandsController`, как `Adhd/Mikubeam`) или удалить событие
      + маршрут `/explosion` + `ExplosionVideo.tsx`.
- [ ] **3.5** P1 `LeroyAlert`: нет ни продюсера, ни консюмера — удалить из
      манифеста, `IAlertsHub`, `overlayEvents.ts`, `telegramusHubStore`-стаба.
- [ ] **3.6** P1 `Highlite`: единственный продюсер
      `Services/Chat/HighlitedMessage.cs:38` не имеет вызовов (только DI и тесты).
      Подключить к чат-пути (триггер-слова/мод-команда) или удалить.
- [ ] **3.7** P1 `AudioQuizStart/Stop`: продюсер есть (`AudioQuizHandler:117/139`),
      консюмера нет (только стаб стора). Добавить UI-обработку или убрать события.
- [ ] **3.8** P1 После удаления событий: обновить `overlay-hub.manifest.json`,
      `overlayEvents.manifest.test.ts`, `FakeHubAdapter.test.ts` (счётчик 36),
      клиентские типы-карты.
- [ ] **3.9** P1 Решить доступность `POST api/internal/commands/invoke`
      (единственный вход для Adhd/Mikubeam-команд): сейчас не маршрутизирован в
      Gateway — либо добавить маршрут (skill `gateway-route-add`), либо задокументировать
      как service-to-service.

## Фаза 4 — Клиентские вызовы «в пустоту» на /hubs/overlay (P1)

Живой `MuteAll` несовместим по аргументам; остальные методов не существует.
`useHubInvoke.ts` глотает отказы одним warn — разрывы невидимы на стенде.

- [ ] **4.1** P1 `MuteAll` (8 вызовов в 6 оверлеях: MikuMikuBeam:65, Credits:474,
      AllRefund:148, PyroAlerts Audio:81/Voice:56/Video:167, mikuMondayAudio:28):
      серверу дать default `string[] excludedProcessNames = []` (AlertsHub.cs:54)
      **или** клиенту шлёт `[]` + `HubInvocationMap.MuteAll: [excluded: string[]]`.
      Тест протокольного уровня (не только прямой вызов, как OverlayHubTests).
- [ ] **4.2** P1 `TwitchMsg` (анонсы роллов из Waifu/Fumo/Frog/MikuAlerts и MikuMonday
      ResultStage → `twitchStore.ts:116`): hub-метод в `AlertsHub`, публикующий
      `ChatSendEvent` (сквозной путь к `TwitchChatSendListener` уже существует),
      либо перенести анонсы в WaifuGacha (паттерн `RollCooldownChatListener:53`).
      Выбрать вариант, реализовать, тест с обеих сторон.
- [ ] **4.3** P1 `ObsFreeze/Unfreeze` (PyroAlerts primitives ×5): перевести на REST
      `Obs.ts` → `POST /api/Obs/freeze|unfreeze` (сервис MARS.OBS существует).
      Проверить YARP-маршрут до `obs`; если нет — добавить (`gateway-route-add`).
- [ ] **4.4** P1 `MikuMondayTracks`/`DecrementAvailableMikuTrack`
      (`mikuMondayStore.ts:180/209`): перевести на REST `IWaifuGachaClient`
      (`GetMikuMondayTracksAsync` есть; decrement-эндпоинт добавить) или добавить
      hub-методы в `AlertsHub`.
- [ ] **4.5** P2 `ExplosionGo` (ADHDController:145), `MikuMikuDeleteTwitchMessages`
      (MikuMikuBeam:89), `LogError` (RandomMem:151/164): удалить вызовы, либо
      реализовать (если фичи нужны) — решить по каждому.
- [ ] **4.6** P1 Актуализировать клиентские доки: `hubAdapter.ts` («ни один метод
      карты не обслуживается хабом», «адаптеров четыре, включая тунца» — их три),
      `useHubInvoke.ts` («MuteAll не реализован»), `overlayEvents.ts:4-11`
      (`telegramus.proto`/`ITelegramusHub`), `adhdLayoutStore.ts:87` («реле перекладывает»).
- [ ] **4.7** P1 `PostTwitchInfo`-секреты: клиент `twitchStore.ts:45-51` не должен
      требовать `clientId`/`secret` из хаба (см. 3.2); публичные поля only.

## Фаза 5 — Контракты и идентичность (P1)

- [ ] **5.1** P1 XML-доки направления «ИЗ MARS.Twitch => В …» для 6 контрактов
      `MARS.Shared.Contracts\Messaging\` (`RewardRedeemedEvent`,
      `EventSubRawEvent`, `ChatEventMessageEvent`, `ChatMessageDeletedEvent`,
      `RewardInputChatMessageEvent`, `CommandExecuteRequestedEvent`) — сейчас
      направления нет ни у одного (конвенция AGENTS.md нарушена). Перенос в
      контракты-получатели — согласовать со skill `mars-project-structure`.
- [ ] **5.2** P1 Убрать «MARS.TwitchCore» из `Program.cs:52-53`, default
      `EventSubRawEvent.SourceService` (`MarsMessageContracts.cs:20`), доков
      (`RabbitMqEvents.cs:29`, `TwitchMessagesPublisher.cs:14`, усечённый док
      дописать) и названия тест-проекта `MARS.TwitchCore.Tests`
      (переименование — по `mars-project-structure`).
- [ ] **5.3** P1 `RewardRedeemedEvent`-док: Media заполняется
      `IMediaStorageClient.GetMediaInfoAsync` (`RewardRedemptionPublisher:149-152`),
      а не «TwitchCore через ITwitchMediaPreparationService».
- [ ] **5.4** P1 Stale-комментарии MARS.Alerts: `telegramus.proto` (proto-файлов нет),
      `<see cref="HubEventRelay"/>`/EventBroadcaster (`AlertsHub.cs:24-29`,
      `OverlayChatMessageListener.cs:23`, `AdhdConfigPayload.cs:21`,
      `MARS.Alerts.Api/Program.cs:60-62`), «RewardAlertConsumer» в ~20 хендлерах
      (реально — `RewardAlertListener`), несуществующий track-hub
      (`Program.cs:146-147`), gRPC-8081 (`Program.cs:143`), gRPC-фрейминг
      `TwitchMediaAlertsListener.cs:20`.
- [ ] **5.5** P2 Легаси-имя стора `telegramusHubStore.ts` — переименовать в
      overlayHubStore (большой механический рефактор, отдельный chore-PR).

## Фаза 6 — Надёжность бизнес-логики и гигиена (P1/P2)

- [ ] **6.1** P1 `RewardEventManager` (`Routing/RewardEventManager.cs:13-43`):
      изоляция хендлеров — try/catch per subscriber + лог; сейчас один бросивший
      `onRedeem` прерывает остальных на этой награде. Тест «плохой хендлер не мешает
      хорошему».
- [ ] **6.2** P1 Fire-and-forget без защиты: `AudioQuizHandler:135-143` (старт-стоп
      аудиоквиза: сбой → вечно muted), `AllRefundHandler:81-91` (окно рефанда),
      `EmojisHandler:71` (discarded task). Обернуть try/catch + `CancellationToken`.
- [ ] **6.3** P1 `HighlitedMessage:30` — `Task.Factory.StartNew(async …)`-ловушка;
      исправить или удалить (зависит от 3.6).
- [ ] **6.4** P1 Payload-валидация клиента (wire = `unknown`): `mikuMondayStore:101`
      (worst: битый JSON убивает экран), gacha-roll ветки `telegramusHubStore:363-388`
      (early return при неудачном decode), `PyroAlerts.tsx:42` /
      `RandomMem.tsx:118` (гварды `mediaInfo`), `Manager.tsx:33-49` (enum-membership,
      ChatMessage shape), `AllRefundManager:79`, `GaoAlertController:27`,
      `adhdLayoutStore:70`, prizes-касты `telegramusHubStore:396-417`. Общие
      shape-гварды — в `shared/api/realtime/`.
- [ ] **6.5** P2 Мёртвый код: `Checkers.tsx` (пустой), `overlays/index.ts`
      (мертвый barrel), `signalr-clients/*/SignalRHubWrapper.tsx` ×3 (+ экспорты
      `shared/api/index.ts:53-61`), `HelloVideoWorker.TestVideo` (без вызовов),
      `RewardBindingResolver` (готовый движок, 0 вызовов — реализовать по плану
      `twitch-rewards-identity-and-binding.md` или удалить).
- [ ] **6.6** P1 `SyncRewardServicesToLocalAsync` синхронизирует 0 строк (нет ни одной
      регистрации `ChannelRewardDefinition`; `PyroAlertRewardDefinition` не
      зарегистрирован) — зарегистрировать определения или убрать эндпоинт
      (`ChannelRewardsManagerController.cs:244`).
- [ ] **6.7** P1 `AutoRewardInfoFetcher` (`AutoInfoFetch/AutoRewardInfoFetcher.cs:60-63`)
      каждые 10 минут собирает все награды и выбрасывает (только лог количества) —
      подсоединить к `ChannelRewards`-синхронизации или упростить до осмысленного.
- [ ] **6.8** P2 Тест-скаффолдинг из прода: маршрут `/automessage-test`
      (`obsComponentRoutes.tsx:309-320`), глобальный хук `testFumoFriday`
      (`FumoFridayController.tsx:83-100`) — вынести в dev-only/удалить.
- [ ] **6.9** P2 Copy-paste: default-экспорт `RandomMem.tsx:13` называется `PyroAlerts`;
      перепутанные заголовки Announce (`TikTokLayoutManager.tsx:125` «Phonk Layout» /
      `PhonkLayoutManager.tsx:80` «TikTok Edit»); дубль playlist-ID
      `AFKScreen.tsx:8-12`.
- [ ] **6.10** P2 Лишние overlay-hub-коннекты: `/scoreboard`, `/afkscreen`, `/matrix*`,
      `/avatarka*` открывают `hubs/overlay` без единой подписки — тонкая обёртка
      без автозапуска хаба (или осознанно оставить, зафиксировать в доке).
- [ ] **6.11** P1 SoundRequest-заглушки: `useSoundRequestPlayer.ts:486-497`
      (handleRemoveFromQueue/handlePlayTrackFromQueue — console.warn-стабы,
      «функция удалена из API») + `QueueList.tsx:43` «TODO: Реализовать
      проигрывание трека из очереди» — реализовать или убрать кнопки из UI.
- [ ] **6.12** P2 `TriggerWordAlertDispatcher.cs:40` TODO «переделать на хранимую
      процедуру» (полный список алертов на каждое сообщение чата) — оценить и
      либо сделать, либо снять TODO с обоснованием.
- [ ] **6.13** P2 Рефлексия в `EventSubRawForwarder:101-155` по приватным полям
      TwitchLib (`_websocketClient`, `OnDataReceived`) — задокументировать хрупкость
      или перевести на публичный API при обновлении TwitchLib.

## Definition of Done (на каждый PR и на план целиком)

- [ ] `scripts/windows/verify.ps1` (или `scripts/unix/verify.sh`) — зелёный.
- [ ] Затронутый фронтенд: `npx tsc -b --noEmit && yarn test && yarn build` в
      `src/MARS.ClientUi`.
- [ ] `graphify update .` после правок кода.
- [ ] PR в `main` с разделами: Что / Зачем / Как / Проверка / Миграция и откат /
      Сквозные правки (конвенция репозитория).
- [ ] Живой прогон на compose-стенде: реальное событие Twitch видно в RabbitMQ →
      в листенере MARS.Alerts → в хабе → в оверлее (скрин/лог в PR).
- [ ] После фаз 1-3: повторный аудит-сверка матрицы 36 событий
      (продюсер ↔ консюмер ↔ подписчик клиента) — расхождений нет.

---

## Приложение: сводка разрывов (по файлам)

| # | Разрыв | Где |
|---|--------|-----|
| 1 | 4 продюсера не в DI | `MARS.Twitch.Host/Program.cs` (Docker: Host.dll) |
| 2 | Scoreboard-рассылка не питается | `ScoreboardService` ↔ `EventBroadcaster<ScoreboardEvent>` |
| 3 | 8 событий без продюсера | `IAlertsHub.cs:53,68,80,86,89,125,131,137` |
| 4 | `Highlite` — мёртвый продюсер | `Services/Chat/HighlitedMessage.cs:38` |
| 5 | `MuteAll` сигнатурный разрыв | `AlertsHub.cs:54` ↔ `hubAdapter.ts:65` |
| 6 | `TwitchMsg` не существует | `twitchStore.ts:116` ← 5 оверлеев |
| 7 | `ObsFreeze/Unfreeze` не существуют (REST есть) | PyroAlerts primitives ↔ `Obs.ts` |
| 8 | `MikuMondayTracks/Decrement…` не существуют | `mikuMondayStore.ts:180/209` |
| 9 | `ExplosionGo/MikuMikuDelete/LogError` не существуют | ADHDController/MikuMikuBeam/RandomMem |
| 10 | Секреты в `PostTwitchInfo` | `twitchStore.ts:45-51` |
| 11 | 6 контрактов без направления «ИЗ => В» | `MARS.Shared.Contracts\Messaging\` |
| 12 | «TwitchCore» в идентичности | `Program.cs:52-53`, `MarsMessageContracts.cs:20` |
| 13 | Stale-доки (proto, HubEventRelay, RewardAlertConsumer…) | MARS.Alerts ~9 мест, клиент ~4 |
| 14 | RewardEventManager без изоляции хендлеров | `Routing/RewardEventManager.cs:13-43` |
| 15 | Fire-and-forget без try/catch | `AudioQuizHandler:135`, `AllRefundHandler:81`, `EmojisHandler:71` |
| 16 | Payload-касты без проверки | `mikuMondayStore:101` и др. |
| 17 | Мёртвый код/заглушки | Checkers, index.ts, react-signalr, RewardBindingResolver, SoundRequest-стабы |
