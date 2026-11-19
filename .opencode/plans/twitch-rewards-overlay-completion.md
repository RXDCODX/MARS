# Перенос бизнес-логики Twitch-наград в MARS.Alerts (доведение вызовов IAlertsHub)

Статус: план согласован (Plan mode), ожидает переключения в Build.
Дата: 2026-11-19.
Источник: миграция `D:\VS\MARS_old\MARS.Projects\MARS.Server\Services\Twitch\Rewards` → `D:\VS\MARS\src\MARS.Alerts\MARS.Alerts.Application.Twitch.Rewards`.
Смежный план: `twitch-rewards-identity-and-binding.md` (идентичность/привязка RewardCode — уже реализована).

---

## 1. Контекст и текущее состояние

Из 48 папок наград в `MARS.Alerts.Application.Twitch.Rewards`:

| Состояние | Кол-во | Комментарий |
|---|---|---|
| Готово (публикует событие) | 2 | `170_MikuMondayAlert` (эталон), `6666_CloseGame` (в монолите хаб не звал — нечего публиковать) |
| Частично (логика есть, отправки нет) | ~15 | `RandomSound`, `RandomArt`, `MikuBeam`, `Confetti`, `Fireworks`, `PhonkEdit`, `TikTokEdit`, `Credits`, `Adhd`, `GaoAlert`, `FumoFridayNight`, `AllRefund`, `WednsdayFrog`, `MichaelTime` |
| Заглушки `PendingRewardAlertHandler` (12) | 12 | `RandomReward`, `WaifuMarriage`, `FrogRoll`, `FumoRoll`, `MikuRoll`, `SearchWife`, `AddWife`, `RussianRoulette`, `Quiz`, `AudioQuiz`, `FumoFriday`, `Emojis` |
| Log-only без логики | ~9 | definition-only и input-награды |
| Хендлер `random_mem` отсутствует | 1 | редемпшены считаются `rabbitmq_unhandled_messages` |

Ключевые факты (проверено агентами):

- Все вызовы старого `ITelegramusHub` имеют методы 1:1 в `IAlertsHub`, а `HubEventRelay` уже разводит все 36 веток `oneof TelegramusEvent` — недостающих методов хаба нет, проблема только в publish-вызовах.
- Целевой паттерн задан `MikuMondayAlertHandler`: инъекция `EventBroadcaster<TelegramusEvent>` → `await broadcaster.BroadcastAsync(new TelegramusEvent { <ветка> = new ...Event {...} })`.
- csproj наград уже ссылается на `MARS.Platform.Infrastructure.Events` и proto-типы (`MARS.Shared.Contracts.Telegramus` генерируется внутри `MARS.Shared.Contracts`) — **новые проекты не нужны**.
- `MediaGrpcMapper` (`MediaDto/MediaInfo → MediaPayload/MediaInfoPayload`) удалён вместе с gRPC-инфраструктурой (коммит `a84821fc`); сигнатура восстановима из `git show a84821fc^`. `MediaDto` жив (`MARS.Shared.Contracts\Models\Media\MediaDto.cs`).
- Чат-отправка уже есть: контракт `ChatSendEvent` → Wolverine (`mars.chat-send`) → `TwitchChatSendListener` в MARS.Twitch.Api. Хендлеры его просто не используют.
- ~18 definition-only наград обслуживаются тремя общими диспетчерами, у которых закомментирована только финальная отправка.
- Издатель `RewardRedemptionPublisher` (MARS.Twitch.Api) крепит `Media` из ChannelRewards-привязки и `User` через `ITwitchUserEnsureService`; валидация там только broadcaster-identity (и спец-кейс `random_sound`), старый `RequireFollower` более нигде не проверяется — осознанное расхождение, фиксируем как принятое.
- `RewardEventManager` маршрутизирует по `RewardCode` (каталог `RewardCatalog`), группы `roll4`/`day170` разрешают конфликты дней; неизвестные коды → `rabbitmq_unhandled_messages`.
- Тест-инфраструктура: реальные in-memory `EventBroadcaster<TelegramusEvent>` + Moq клиенты; `RewardHandlerRegistrationTests` содержит захардкоженный список «ровно 12 pending»; `HubEventRelayDispatchTests` покрывает все ветки; Postgres-фикстуры наградам не нужны.

## 2. Зафиксированные решения (ответы пользователя)

1. **Scope**: все 9 файлов с закомментированными вызовами, включая смежные проекты — три диспетчера, `RandomMemOnline`, проводка `HighlitedMessage`, `PyroAlertsHandler`.
2. **Refund**: новый Wolverine-контракт `RewardRefundEvent` → MARS.Twitch.Api (по образцу `ChatSendEvent`), не HTTP-клиент.
3. **Мини-игры** (`RussianRoulette`, `Quiz`, `AudioQuiz`): бизнес-логика реализуется **в MARS.Alerts** (не в Twitch.Api).
4. **WaifuGacha**: расширить API `MARS.WaifuGacha` и `IWaifuGachaClient` под полные DTO (сущность + цвет + счётчики коллекций) — без этого роллы/вайфу не доделать.
5. **FumoFriday** (13): хранение подписок **in-memory** в MARS.Alerts (таблица `FumoUsers` монолита не мигрируется).
6. **Объём первого захода в Build**: фазы 0–4 одним заходом; мини-игры (фаза 5) — сразу после.

## 3. Целевой паттерн вызова IAlertsHub

```
хендлер/диспетчер → EventBroadcaster<TelegramusEvent>.BroadcastAsync(
    new TelegramusEvent { <oneof-ветка> = new <Proto>Event { ... } })
    → HubEventRelay.DispatchAsync (switch по EventCase)
    → IHubContext<AlertsHub>.Clients.All.SendCoreAsync(nameof(IAlertsHub.<Метод>), [payload])
    → IAlertsHub.<Метод> (имена совпадают со старым ITelegramusHub 1:1)
```

Протокол payload'ов (по заголовку `telegramus.proto`): «всё, что было object/списком объектов, едет JSON-байтами в bytes с теми же именами свойств, что отдавал SignalR».

**Правило анти-дубля**: показ медиа у наград, где `RewardRedeemedEvent.Media` уже заполнено издателем, выполняет только `TwitchMediaAlertsListener`. Такие хендлеры (`170_FumoFridayNight` и definition-only) сами `Alert` не публикуют — они ведут только лимиты/чат/refund.

## 4. Полная карта «награда → событие»

Обозначения фаз: 0 — общие механизмы; 1 — самодостаточные; 2 — чат/трекер/диспетч случайной награды; 3 — WaifuGacha; 4 — Twitch-контракты; 5 — мини-игры.

| Папка | RewardCode | Старый вызов хаба | Новая ветка TelegramusEvent | Состояние | Фаза |
|---|---|---|---|---|---|
| 1_RandomReward | `random_reward` | — (re-dispatch по cost) | — (re-dispatch через `RewardEventManager`) | Заглушка | 2 |
| 2_WaifuMarriage | `waifu_marriage` | `MergeWaifu(waifu, host)`, `ShowCurrentWife(waifu, host)` | `merge_waifu`, `show_current_wife` | Заглушка; включить неиспользуемый `NotDaysPolicy(Fri/Wed/Mon)` | 3 |
| 4_SearchWife | `search_wife` | `WaifuRoll(waifu, husband)` | `waifu_roll` | Заглушка | 3 |
| 4_FrogRoll | `frog_roulette` | `FrogRoll(frog, user)` | `frog_roll` | Заглушка | 3 |
| 4_FumoRoll | `fumo_roulette` | `FumoRoll(fumo, user, collected, total)` | `fumo_roll` | Заглушка | 3 |
| 4_MikuRoll | `miku_roulette` | `MikuRoll(module, user, collected, total)` | `miku_roll` | Заглушка | 3 |
| 5_AddWife | `add_wife` | `AddNewWaifu(waifu, user)` | `add_new_waifu` | Заглушка | 3 (+VIP в 4) |
| 6_RussianRoulette | `russian_roulette` | — (чат-игра без хаба) | — | Заглушка | 5 |
| 7_Quiz | `quiz` | — (чат-игра без хаба) | — | Заглушка | 5 |
| 9_AudioQuiz | `audio_quiz` | `AudioQuizStart(round)`, `AudioQuizStop()` | `audio_quiz_start`, `audio_quiz_stop` | Заглушка | 5 |
| 10_RandomSound | `random_sound` | `RandomMem(MediaDto)` | `random_mem` (AlertEvent) | Частично, закомм. блок L66–73 (без открывающей строки) | 1 |
| 11_RandomMemReward | `random_mem` | `RandomMem(MediaDto)` | `random_mem` | **Хендлер отсутствует**; ingest частично | 1 |
| 13_FumoFriday | `fumo_friday` | `FumoFriday(twitchUser)` | `fumo_friday` (display_name, color) | Заглушка | 3 (in-memory) |
| 18_GaoAlert | `gao_alert` | `GaoAlert(GaoAlertDto)` | `gao_alert` (JSON DTO) | Частично, нет отправки | 1 |
| 27_RandomArt | `random_art` | `Alerts(MediaDto[])` | `alerts` (repeated MediaPayload) | Частично, список строится и не отправляется | 1 |
| 61_What | `what` | `Alert` (generic) | `alert` | Log-only | 0 (через диспетчер) |
| 134_Pedro | `pedro` | `Alert` (generic) | `alert` | Log-only | 0 |
| 1333_SkibidibopLong | `skibidibop_long` | `Alert` (generic) | `alert` | Log-only | 0 |
| 150_TyazheloReward | `tyazhelo` | `Alert` (generic) | `alert` | Log-only | 0 |
| 1510_StatusQuestion | `status_question` | `Alert` (generic) | `alert` | Log-only | 0 |
| 155_MichaelTime | `michael_time` | `MichaelJackson()` | `michael_jackson` (EmptyEvent) | Частично | 1 |
| 1580_MikuBeam | `miku_miku_beam` | `MikuMikuBeam(List<TwitchUser>)` | `miku_miku_beam` (repeated users_json) | Частично, users строятся и не отправляются | 2 |
| 160_LegBum | `leg_bum` | — (refund «asp/асп») | — (+`RewardRefundEvent`) | Log-only | 4 |
| 1602_CinemaRequest | `cinema_request` | — (CinemaQueue, вне папки) | — | Log-only, `DisabledPolicy` | **Вне scope** → MARS.CinemaQueue |
| 170_FumoFridayNightReward | `fumo_friday_night` | `Alert(MediaDto)` (seeded Guid) | `alert` (generic-путь; закомм. строка `notifier.FumoFriday` — **ошибочна**) | Частично | 1 (медиа) / 2 (чат) / 4 (refund) |
| 170_MikuMondayAlert | `miku_monday_alert` | `MikuMonday(MikuMondayDto)` (семантика старой 39) | `miku_monday` | **Готово, эталон** | — |
| 1700_Confetti | `confetti` | `MakeScreenParticles(Confetty)` | `make_screen_particles` (particles_json) | Частично | 1 |
| 1701_Fireworks | `fireworks` | `MakeScreenParticles(Fireworks)` | `make_screen_particles` | Частично | 1 |
| 1702_EmojisReward | `emojis` | `MakeScreenEmojisParticles(ChatMessage)` | `make_screen_emojis_particles` (message_json) | Заглушка (reward-input не подписан) | 1 |
| 182_Stone | `stone` | `Alert` (generic) | `alert` | Log-only | 0 |
| 195_Cringe | `cringe` | `Alert` (generic) | `alert` | Log-only | 0 |
| 210_Hello | `hello` | `Alert` (generic) | `alert` | Log-only | 0 |
| 215_Bye | `bye` | `Alert` (generic) | `alert` | Log-only | 0 |
| 317_Intelligence | `intelligence` | `Alert` (generic) | `alert` | Log-only | 0 |
| 320_MikuScreamer | `miku_screamer` | `Alert` (generic) | `alert` | Log-only | 0 |
| 333_Skibidibop | `skibidibop` | `Alert` (generic) | `alert` | Log-only | 0 |
| 337_PhonkEdit | `phonk_edit` | `PhonkEdit()` | `phonk_edit` (EmptyEvent) | Частично | 1 |
| 341_Aga | `aga` | `Alert` (generic input-путь по `TwitchGuid`) | `alert` (через `RewardBoundAlertDispatcher.DispatchRewardInputAsync`) | Log-only | 0 |
| 342_BadToBone | `bad_to_bone` | `Alert` (generic input-путь) | `alert` | Log-only | 0 |
| 353_TikTokEdit | `tik_tok_edit` | `TikTokEdit(Guid, text)` | `tik_tok_edit` (guid, text) | Частично, text не используется | 1 |
| 375_DanceDance | `dance_dance` | `Alert` (generic) | `alert` | Log-only | 0 |
| 38_WednsdayFrog | `wednesday_frog` | `Alert` (generic, среда) | `alert` | Частично (перенесена только `DayOfWeekPolicy(Wednesday)`) | 0 |
| 666_Edge0100Alert | `edge0100_alert` | `Alert` (generic) | `alert` | Log-only | 0 |
| 6666_CloseGame | `close_game` | — (kill процессов) | — | **Готово** | — |
| 75000_SelectGame | `select_game` | `Alert` (generic input-путь) | `alert` | Log-only | 0 |
| 8005_Credits | `credits` | `Credits()` | `credits` (EmptyEvent) | Частично | 1 |
| 99999_AllRefundService | `all_refund` | `AllRefund(TwitchUser)` + refund-цикл 1 мин + анонсы | `all_refund` (user_json) + `RewardRefundEvent` | Пустой `if (user is not null) { }` | 2 (анонсы) / 4 (refund) |

Сервисные файлы с закомментированными вызовами:

| Файл | Строка | Целевая ветка | Фаза |
|---|---|---|---|
| `Services\RickRollerService.cs` | L92 `//await notifier.Alert(...)` | `alert` — **римролл сейчас ничего не показывает, блокирует все rickroll-гейты** | 0 |
| `Listeners\TwitchMediaAlertsListener.cs` | L56 | `alert` | 0 |
| `MARS.Alerts.Application\RewardInput\RewardBoundAlertDispatcher.cs` | L124 | `alert` | 0 |
| `MARS.Alerts.Application\TriggerWords\TriggerWordAlertDispatcher.cs` | L66 | `alert` | 0 |
| `MARS.Alerts.Infrastructure.Integrations\Telegram\RandomMemOnline.cs` | L190, L226, L262 | `random_mem` | 1 |
| `Services\Chat\HighlitedMessage.cs` | L44 `//await notifier.Highlite(...)` | `highlite` — класс зарегистрирован, но **ни один листенер его не вызывает**; нужна проводка | 1 |
| `Services\PyroAlerts\PyroAlertsHandler.cs` | L22, L64 | `alert` | 1 |

Примечание: события `update_waifu_prizes / update_fumo_prizes / update_frog_prizes / update_miku_prizes` (PrizesEvent) в старом звались вне папок Rewards (обновление списков призов) — в аудит папок не попали; реле их поддерживает, добавить отдельной задачей при появлении источника.

## 5. Фазы

### Фаза 0 — общие механизмы (оживляет ~18 наград)

1. **Восстановить маппер** `MediaInfo/MediaDto → MediaPayload/MediaInfoPayload` как статический класс (сигнатуры из `git show a84821fc^`: `ToProto(MediaDto)`, `ToProto(MediaInfo)`, под-объекты text/file/position/meta/styles, enum'ы). Место по умолчанию — `MARS.Platform.Infrastructure.Clients.Http` (доступен и Alerts, и Twitch.Api, рядом с `ITelegramusEventSink`); финально сверить с skill `mars-project-structure`.
2. `RickRollerService`: инъекция `EventBroadcaster<TelegramusEvent>` → publish `Alert`.
3. `TwitchMediaAlertsListener` L56 → publish `Alert`.
4. `RewardBoundAlertDispatcher` L124 → publish `Alert`.
5. `TriggerWordAlertDispatcher` L66 → publish `Alert`.

Definition-only/input-награды, которые оживают через эти диспетчеры и уже проставленные политики: `1333, 134, 150, 1510, 182, 195, 210, 215, 317, 320, 333, 375, 61, 666, 38, 341, 342, 75000`.

### Фаза 1 — самодостаточные награды (только broadcaster)

- `155` → `MichaelJackson = new EmptyEvent()`.
- `337` → `PhonkEdit = new EmptyEvent()`.
- `8005` → `Credits = new EmptyEvent()`.
- `1700/1701` → `MakeScreenParticles` (JSON старого enum `Confetty`/`Fireworks`).
- `1702` → подписка на `OnTwitchRewardInput` → `MakeScreenEmojisParticles` (message_json).
- `2002` → `Adhd { Seconds = 60 }`.
- `353` → `TikTokEdit { Guid = Guid.NewGuid().ToString(), Text = UserInput }`.
- `18` → `GaoAlert` (JSON DTO; Helix-обогащение старого кода недоступно — базовый путь `IsJustText`; опционально `ITwitchUserClient.ResolveIdByLoginAsync`).
- `10_RandomSound` → раскомментировать блок, заменить `MediaGrpcMapper` на новый маппер → `RandomMem(AlertEvent)`.
- `11_RandomMemReward` → **создать хендлер** `RandomMemHandler : RewardHandlerBase`, `RewardCode = "random_mem"` (зеркало RandomSound, папка `Alerts/random_meme`, rickroll-гейт); обновить `RewardHandlerRegistrationTests` (12→11 pending + ожидание нового кода).
- `27_RandomArt` → publish `Alerts` (repeated MediaPayload).
- `170_FumoFridayNight` → медиа показывается generic-путём (см. правило анти-дубля); в хендлере остаётся per-user/day set + чат (фаза 2) + refund (фаза 4).
- `HighlitedMessage` → проводка вызова из чат-листенера (проверить наличие `IsHighlighted` в `ChatEventMessageEvent`; иначе условие mod/vip/broadcaster, как уже сведено в классе) + publish `Highlite { message_json, color, face_url_json }`.
- `PyroAlertsHandler` L22/L64 → publish `Alert`.
- `RandomMemOnline` L190/L226/L262 → publish `RandomMem(AlertEvent)`.
- `RandomMemeWorker` привести в соответствие: ротация уже в MediaStorage (`GetNextRandomMemeAsync`) — из воркера оставить только чистку/контроль.

### Фаза 2 — чат через `ChatSendEvent` + трекеры

- Инъекция `IMessageBus` в хендлеры, публикующие `ChatSendEvent` (кулдауны, ошибки, активации).
- `1580_MikuBeam`: трекер хранит полные данные пользователя из `ChatEventMessageEvent` (сейчас голые ID + анонимные объекты) → publish `MikuMikuBeam` (repeated users_json) + чат кулдауна (60 с).
- `1_RandomReward`: пул по `RewardCatalog` (`ExpectedCost > 0`, исключая `ExcludedFromRandomPool` и себя), `UserCooldown = 60 c` через `CooldownPolicy`, чат-сообщения, re-dispatch клоном события с выбранным `RewardCode` через `RewardEventManager` (исключать pending-хендлеры и конфликтные группы `roll4`/`day170`; рефлекшн-механизм монолита не переносим).
- Анонсы `99999_AllRefund` до контракта фазы 4 — через `ChatSendEvent` (полноценно в фазе 4).

### Фаза 3 — WaifuGacha (расширение API + клиента)

- Новые DTO/эндпоинты в `MARS.WaifuGacha`: полные результаты — `WaifuRollResult` (id/name/image_url/source + display_name/color + статус кулдауна для null-вайфу), `FumoRollResult`/`MikuRollResult` (сущность + collected_count/total_count + color), `FrogRollResult`, `MergeWaifuResult` (MergeWaifu/ShowCurrentWife данные), `AddNewWaifuResult`. Shikimori-обогащение остаётся внутри WaifuGacha.
- Расширить `IWaifuGachaClient` под эти DTO.
- Реализовать хендлеры: `4_SearchWife → waifu_roll`, `2_WaifuMarriage → merge_waifu / show_current_wife` (+ `NotDaysPolicy(Fri/Wed/Mon)`), `5_AddWife → add_new_waifu`, `4_FumoRoll → fumo_roll`, `4_MikuRoll → miku_roll`, `4_FrogRoll → frog_roll`. Кулдауны роллов уже обслуживает WaifuGacha (`RollCooldownRewardListener`) — хендлеры транслируют его ответ в чат/событие.
- `13_FumoFriday`: in-memory set с 24ч-дедупом; redemption-путь + чат-путь через `OnTwitchMessage`; `FumoFriday { display_name, color }` (цвет из события; Helix `GetUserChatColorAsync` недоступен — не переносим).
- Ошибки/ответы `AnswersForTwitchRewards`-шаблонов: аналог уже живёт в MARS.Twitch.Api — для чат-сообщений Alerts достаточно упрощённых формулировок, детальную шаблонизацию согласовать при реализации.

### Фаза 4 — новые Wolverine-контракты → MARS.Twitch.Api

Контракты в `MARS.Shared.Contracts\Messaging` (обмен `mars.<kebab-case>`):

1. **`RewardRefundEvent`** (`RewardId`, список redemption id) → новый листенер в Twitch.Api поверх существующего Helix-кода `UpdateRedemptionStatusAsync(CANCELED)` (логика `TwitchRewardsController`).
2. **`ChatAnnouncementEvent`** (`Message`, `Color`) → `SendChatAnnouncementAsync` (для `AllRefund`, `AddWife`, рулетки).
3. **`TwitchVipGrantEvent`** (`UserId`) → `AddChannelVIPAsync` (для `AddWife`).
4. Потребители:
   - `160_LegBum`: `UserInput` содержит «asp/асп» → refund + чат «баллы возвращены».
   - `170_FumoFridayNight`: per-user/day refund при повторе (проверить поле redemption-id в `RewardRedeemedEvent`, кандидат — `MessageId`).
   - `99999_AllRefund`: полный поток — publish `AllRefund` (user_json) → пауза 10 с → активное окно 1 мин (`AllRefundState`) → отдельный листенер `RewardRedeemedEvent`, публикующий `RewardRefundEvent` для чужих редемпшенов в окне → анонсы начала/конца через `ChatAnnouncementEvent`.
5. `1602_CinemaRequest` — остаётся `DisabledPolicy` + лог: домен у `MARS.CinemaQueue` (в монолите обработчик тоже жил в CinemaQueue-сервисе); отдельная задача вне этого плана.

### Фаза 5 — мини-игры в MARS.Alerts (по решению пользователя)

- **`6_RussianRoulette`**: конечный автомат в Alerts — окно вступления 60 с (вступление повторным редемпшеном cost-6), наррация/`/timeout` через `ChatSendEvent`, спасение вайфу через `IWaifuGachaClient.GetWaifuNameForUserAsync`, победа — через новый контракт **`MiniGameResultEvent`** → Twitch.Api `LeaderboardService` (у `ILeaderboardClient` сейчас только чтение).
- **`7_Quiz`**: файл вопросов `Trivia/bot_trivia_questions.txt` — копия ресурсом в MARS.Alerts; подсказки/ответы через `ChatSendEvent`; вайфу-шёпот через `GetWaifuNameForUserAsync`; результат — `MiniGameResultEvent`.
- **`9_AudioQuiz`**: `AudioQuizStart` (round_json: TrackUrl, ArtworkUrl, RoundSeconds = 30) и `AudioQuizStop` через broadcaster; пауза плеера — `ISoundRequestControlClient.MuteAllAsync/UnmuteAllAsync` (ближайший доступный контроль; старый `IPlayerController` не переносился); источник трека — небольшое расширение SoundRequest API + клиента (метод «случайный трек» из `SoundRequestBaseTrackInfos`); перенос логики сверки ответов (нормализация, транслит, смена раскладки, Левенштейн ≥ 0.8).
- `MiniGamesManager` в Twitch.Api остаётся невостребованным — не трогаем.

## 6. Тестирование

- TDD на каждую точку; соглашения: реальный in-memory `new EventBroadcaster<TelegramusEvent>()` в хендлер/реле + Moq для клиентов, `TestContext.Current.CancellationToken`, DB не нужен (Postgres-фикстуры наградам не требуются).
- Обновить: `RewardHandlerRegistrationTests` (карта «ровно 12 pending» тает по мере реализации; `random_mem` — новый зарегистрированный код), `RewardHandlerSuiteTests` (глубина `Stub.Resolve` из-за новых зависимостей broadcaster/`IMessageBus`), существующие `RandomSoundHandlerTests`, `RandomArtHandlerTests`, `MikuMikuBeamHandlerTests`, `HighlitedMessageTests`, `PyroAlertsHandlerTests`, `RewardBoundAlertDispatcherTests`, `TriggerWordAlertDispatcherTests`, `TwitchMediaAlertsListenerTests`.
- `HubEventRelayDispatchTests` / `OverlayHubContractTests` / `overlay-hub.manifest.json` — страховочная сеть имён методов; не менять (методы не добавляются).
- Для новых контрактов (`RewardRefundEvent` и др.) — тесты листенеров по образцу `TwitchMessagingListenersTests` / `RewardAlertConsumerTests` («брокер не поднимаем, вызываем напрямую»).

## 7. Процесс репозитория

- Ветка `feat/*` (например, `feat/twitch-rewards-overlay-completion`); коммиты малыми шагами по фазам/наградам.
- Каждый шаг: `dotnet csharpier format .` → сборка → точечные тесты (`dotnet-test-run`) → перед сдачей `.\scripts\windows\verify.ps1`.
- После правок кода — `graphify update .`.
- Новых csproj нет → матрица CI не меняется; изменения задевают существующие проекты: `MARS.Alerts.*`, `MARS.Platform.Infrastructure.Clients.Http`, `MARS.Shared.Contracts`, `MARS.Twitch.Api`, `MARS.WaifuGacha`, `MARS.SoundRequest` (по фазам 3–5).
- Предлагаемая разбивка PR: 1) фазы 0–1 (общие механизмы + самодостаточные); 2) фазы 2–3 (чат + WaifuGacha); 3) фаза 4 + 5 (контракты Twitch + мини-игры). Допустимо и одним PR с фазовыми коммитами — по вкусу.

## 8. Точки верификации при реализации

1. Поле redemption-id в `RewardRedeemedEvent` для refund (кандидат `MessageId`) — сверить с издателем `RewardRedemptionPublisher`.
2. Наличие `IsHighlighted` в `ChatEventMessageEvent` для проводки `HighlitedMessage`.
3. Финальное место маппера — по skill `mars-project-structure` (по умолчанию `MARS.Platform.Infrastructure.Clients.Http`).
4. Формы ответов расширенного WaifuGacha API (DTO уже перечислены в фазе 3).
5. Расположение файла вопросов викторины в новом репо (кандидат — активы Twitch.Api) и формат.
6. `RandomSoundHandlerTests` содержит пометку «MediaGrpcMapper удалён... проверять нечего» — тест восстановить вместе с маппером.

## 9. Вне scope

- `1602_CinemaRequest` → домен MARS.CinemaQueue (отдельная задача).
- Прямое восстановление старой 170-й семантики `MikuMondayAlert` (медиа-алерт по seeded Guid) — оставляем новую семантику WaifuGacha-треков; при необходимости per-user/refund-парити — фаза 4.
- `NewMessage`/`DeleteMessage` чат-мирроринг в оверлей (`TwitchMessagesHubAwaker`) — не награда; реле и методы хаба готовы, источник — отдельная задача.
- `update_*_prizes` (PrizesEvent) — источник вне папок наград, отдельная задача.
- `RequireFollower` для остальных наград — осознанно не переносится (валидация остаётся в издателе; спец-кейс `random_sound` уже там).
