# План: паритет reward-алертов MARS.Alerts с legacy и переход на `RewardCode`/`RewardId`

## Статус

Это полный план, записанный по итогам read-only анализа. Код, git-ветка и рабочее дерево в ходе анализа не менялись. Сейчас зафиксирован только этот `.md`.

Источники:

- текущие награды: `D:\VS\MARS\src\MARS.Alerts\MARS.Alerts.Application.Twitch.Rewards`;
- legacy-награды: `D:\VS\MARS_old\MARS.Projects\MARS.Server\Services\Twitch\Rewards`;
- PR #64: https://github.com/RXDCODX/MARS/pull/64;
- publisher: `D:\VS\MARS\src\MARS.Twitch.Api\Services\Rewards\RewardRedemptionPublisher.cs`;
- контракт события: `D:\VS\MARS\src\MARS.Shared\MARS.Shared.Contracts\Messaging\RabbitMqEvents.cs`;
- хранилище наград Twitch: `MARS.Twitch.Api` / `ChannelRewardRecord`.

---

## 1. Цель

Достичь паритета со старым проектом и устранить главный дефект текущей маршрутизации: runtime-идентичностью награды сейчас является нормализованный `RewardTitle`, что ломается при изменении названия, при одинаковой цене и при нестабильной нормализации emoji.

Новая модель:

```text
RewardCatalog.Code      → стабильная идентичность бизнес-логики
RewardId                → внешняя привязка к Twitch
(Cost, Emoji)           → bootstrap/rebind fingerprint
Group                   → координация наград с одинаковой ценой
ActivationPolicy        → когда награда активна
RewardTitle             → только логи, диагностика и миграция
```

Принципиальное решение пользователя:

1. Не использовать `RewardTitle` какruntime-ключ.
2. Ориентироваться на стоимость награды, но добавлять фильтр активности при пересечениях цен — как в старом проекте.
3. Добавить `RewardId` и внутренний Stability key, чтобы бизнес-логика не теряла привязку после перезапуска и изменения title в Twitch.
4. Для перепривязки использовать не отдельно emoji, а fingerprint `(Cost, NormalizedLeadingEmoji)`.
5. Живую привязку по `RewardId` никогда не переопределять по title/emoji; перепривязка допускается только для orphanedbinding.
6. При неоднозначной перепривязке не угадывать, а переводить запись в `NeedsManualBinding`/`Conflicting`.

---

## 2. Что уже выяснено

### 2.1. Текущий проект

В `MARS.Alerts.Application.Twitch.Rewards` найдено **47 конкретных reward-хендлеров**:

- **34** — реальные наследники `RewardHandlerBase`;
- **13** — `PendingRewardAlertHandler`, то есть по факту stub;
- **2** legacy-награды не имеют reward-хендлера вообще: `random_mem`, `miku_monday`.

Текущий контракт:

```csharp
public interface IRewardAlertHandler
{
    string RewardKey { get; }
    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
```

`RewardKey` — нормализованное название, например `phonkedit`. Это и есть проблемный runtime-ключ.

`RewardEventManager` сейчас:

- редемпшн маршрутизирует через `RewardRouting.KeyFor(RewardTitle)`;
- chat/reward-input/EventSub рассылает **всем** зарегистрированным `RewardHandlerBase`;
- фильтрация событий обязана делаться внутри каждого подписчика;
- `GetInvocationList()` выполняет подписчиков последовательно;
- `try/catch` нет: ошибка одного подписчика прерывает всю рассылку и уходит в Wolverine.

### 2.2. PR #64

PR #64 добавил в `RewardHandlerBase` события:

- `OnTwitchMessage`;
- `OnTwitchRewardInput`;
- `OnTwitchEvent`.

Это оставляем как основу. Но важно:

- production-подписчиков на `OnTwitchRewardInput` почти нет;
- production-подписчиков на `OnTwitchEvent` нет;
- из сделанных подписок пока только `MikuMikuBeamHandler` подписан на `OnTwitchMessage`;
- события пока работают вхолостую и требуют отдельной фазы паритета/фильтрации.

PR #64 сам по себе не решает проблему identity и не убирает title-based routing.

### 2.3. Legacy-проект

В старом проекте найдено **49 наград**:

- 48 файлов `*_TwitchReward.cs`;
- 1 отдельный `39_MikuMonday/TwitchMikuMondayRewardService.cs`.

Активность награды в legacy определялась не только `Cost`, а комбинацией:

- `IsRewardEnabled()`;
- `EnabledByCost`;
- `ExcludeFromRandomPool`;
- день недели;
- cooldown;
- `IsServiceActive`;
- `MiniGamesManager` для cost `9`, `7`, `6`;
- пользовательский ввод и refund-логика.

Поэтому простая замена `RewardTitle → Cost` недостаточна. Нужны `RewardGroup` и `IRewardActivationPolicy`.

### 2.4. Закомментированные overlay-вызовы

Следующие хендлеры/сервисы имеют закомментированные или неактивные overlay-вызовы:

- `RandomSoundHandler`;
- `RandomArtHandler`;
- `MikuMikuBeamHandler`;
- `RickRollerService`;
- `TwitchMediaAlertsListener`;
- `PyroAlertsHandler`;
- `ConfettiHandler`;
- `FireworksHandler`;
- `AdhdSuperpowerHandler`.

Это означает, что “наличие хендлера” ещё не равно паритету. В отдельной фазе нужно либо восстановить вызовы через внедряемые интерфейсы, либо явно пометить награду как stub и не считать её реально перенесённой.

---

## 3. Полная инвентаризация всех 49 наград

Статусы:

- `real` — есть конкретный reward-хендлер;
- `stub` — есть `PendingRewardAlertHandler`, бизнес-логика не перенесена;
- `missing` — reward-хендлера нет.

| # | CodeKey | Cost | Emoji | Group | LegacyTitle | Old | New handler | Статус | Активность |
|---:|---|---:|---|---|---|---|---|---|---|
| 1 | `random_reward` | 1 | 🎲 | — | `🎲 Случайная награда!` | `1_RandomReward` | `RandomRewardHandler` | stub | cooldown 60с + pool exclusions |
| 2 | `waifu_marriage` | 2 | 💒 | — | `💒 Свадьба` | `2_WaifuMarriage` | `WaifuMarriageHandler` | stub | не Fri/Wed/Mon |
| 3 | `search_wife` | 4 | 🔍 | `roll4` | `🔍 Поиск супруга` | `4_SearchWife` | `SearchWifeHandler` | stub | не Fri/Wed/Mon |
| 4 | `miku_roulette` | 4 | 🎀 | `roll4` | `🎀 Мику Рулетка` | `4_MikuRoll` | `MikuRollHandler` | stub | Monday |
| 5 | `frog_roulette` | 4 | 🐸 | `roll4` | `🐸 Frog Roulette` | `4_FrogRoll` | `FrogRollHandler` | stub | Wednesday |
| 6 | `fumo_roulette` | 4 | 🧸 | `roll4` | `🧸 Fumo Roulette` | `4_FumoRoll` | `FumoFridayRollHandler` | stub | Friday |
| 7 | `add_wife` | 5 | 👰 | — | `👰 Добавить супруга` | `5_AddWife` | `AddWifeHandler` | stub | не Fri/Wed/Mon, user input |
| 8 | `russian_roulette` | 6 | 🔫 | — | `🔫 Русская рулетка` | `6_RussianRoulette` | `RussianRouletteHandler` | stub | мини-игра |
| 9 | `quiz` | 7 | 🧠 | — | `🧠 Викторина` | `7_Quiz` | `QuizHandler` | stub | мини-игра |
| 10 | `audio_quiz` | 9 | 🎵 | — | `🎵 Аудио викторина` | `9_AudioQuiz` | `AudioQuizHandler` | stub | `false` |
| 11 | `random_sound` | 10 | 🔊 | — | `🔊 Random sound` | `10_RandomSound` | `RandomSoundHandler` | real | follower + cost 10 |
| 12 | `random_mem` | 11 | 😂 | — | `😂 Random Mem` | `11_RandomMemReward` | **нет reward-хендлера** | missing | есть только Telegram-ingest/worker |
| 13 | `fumo_friday` | 13 | 🌙 | — | `🌙 Fumo Friday` | `13_FumoFriday` | `FumoFridayHandler` | stub | Friday |
| 14 | `gao_alert` | 18 | 🦁 | — | `🦁 GAO Alert` | `18_GaoAlert` | `GaoAlertHandler` | real | true |
| 15 | `random_art` | 27 | 🎨 | — | `🎨 Random Art` | `27_RandomArt` | `RandomArtHandler` | real | true + service active |
| 16 | `wednesday_frog` | 38 | 🐸 | — | `🐸 Жабья среда` | `38_WednsdayFrog` | `WednsdayFrogHandler` | real | Wednesday |
| 17 | `miku_monday` | 39 | 🎤 | — | `🎤 Miku Monday [BETA TEST]` | `39_MikuMonday` | **нет reward-хендлера** | missing | есть только `MikuMondayTracksService` |
| 18 | `what` | 61 | ❓ | — | `❓ WHAT` | `61_What` | `WhatHandler` | real | true |
| 19 | `pedro` | 134 | 👻 | — | `👻 PEDRO PEDRO PEDRO` | `134_Pedro` | `PedroHandler` | real | true |
| 20 | `tyazhelo` | 150 | 😩 | — | `😩 Тяжело...` | `150_TyazheloReward` | `TyazheloHandler` | real | true |
| 21 | `michael_time` | 155 | ⏰ | — | `⏰ Michael Time!` | `155_MichaelTime` | `MichaelTimeHandler` | real | true + `IsServiceActive` |
| 22 | `leg_bum` | 160 | 🦵? | — | `"\uFFFD발 НОГОЙ БОМЖА"` | `160_LegBum` | `LegBumHandler` | real | user input + refund |
| 23 | `fumo_friday_night` | 170 | 🧸 | `day170` | `🧸 Fumo Friday Night` | `170_FumoFridayNightReward` | `FumoFridayNightHandler` | real | Friday |
| 24 | `miku_monday_alert` | 170 | 🎤 | `day170` | `🎤 Miku Monday Alert` | `170_MikuMondayAlert` | `MikuMondayAlertHandler` | stub | Monday |
| 25 | `stone` | 182 | 🪨 | — | `🪨 Камень` | `182_Stone` | `StoneHandler` | real | true |
| 26 | `cringe` | 195 | 😬 | — | `😬 CRINGE` | `195_Cringe` | `CringeHandler` | real | true |
| 27 | `hello` | 210 | 👋 | — | `👋 Привет` | `210_Hello` | `HelloHandler` | real | true |
| 28 | `bye` | 215 | 👋 | — | `👋 Пока` | `215_Bye` | `ByeHandler` | real | true |
| 29 | `intelligence` | 317 | 🧠 | — | `🧠 Интеллект` | `317_Intelligence` | `IntelligenceHandler` | real | true |
| 30 | `miku_screamer` | 320 | 😱 | — | `😱 MIKU SCREAMER` | `320_MikuScreamer` | `MikuScreamerHandler` | real | true |
| 31 | `skibidibop` | 333 | 🎺 | — | `🎺 ШКИБИДИБОП` | `333_Skibidibop` | `SkibidibopHandler` | real | true |
| 32 | `phonk_edit` | 337 | 🎵 | — | `🎵 Phonk Edit` | `337_PhonkEdit` | `PhonkEditHandler` | real | true |
| 33 | `aga` | 341 | 📽️ | — | `📽️ AGA` | `341_Aga` | `AgaHandler` | real | true |
| 34 | `bad_to_bone` | 342 | 🎸 | — | `🎸 BAD TO THE BONE` | `342_BadToBone` | `BadToBoneHandler` | real | true |
| 35 | `tiktok_edit` | 353 | 🎬 | — | `🎬 Make a TikTok Edit` | `353_TikTokEdit` | `TikTokEditHandler` | real | true |
| 36 | `dance_dance` | 375 | 💃 | — | `💃 DANCE DANCE DANCE` | `375_DanceDance` | `DanceDanceHandler` | real | true |
| 37 | `edge0100_alert` | 666 | 👑 | — | `👑 Специальный алерт для Edge0100` | `666_Edge0100Alert` | `Edge0100AlertHandler` | real | true |
| 38 | `skibidibop_long` | 1333 | 🎺 | — | `🎺 SKIBIDIBOP LONG` | `1333_SkibidibopLong` | `SkibidibopLongHandler` | real | true |
| 39 | `status_question` | 1510 | ❓ | — | `❓ STATUS?` | `1510_StatusQuestion` | `StatusQuestionHandler` | real | true |
| 40 | `miku_miku_beam` | 1580 | ✨ | — | `✨ MIKU MIKU BEAM` | `1580_MikuBeam` | `MikuMikuBeamHandler` | real | chat-tracking + cooldown 60с |
| 41 | `cinema_request` | 1602 | 🎬 | — | `🎬 CINEMA/ANIME REQUEST` | `1602_CinemaRequest` | `CinemaRequestHandler` | real | `EnabledByCost=false` |
| 42 | `confetti` | 1700 | 🎉 | — | `🎉 Конфетти!` | `1700_Confetti` | `ConfettiHandler` | real | true |
| 43 | `fireworks` | 1701 | 🎆 | — | `🎆 Феерверк!` | `1701_Fireworks` | `FireworksHandler` | real | true |
| 44 | `emojis` | 1702 | 😀 | — | `😀 Эмодзи!` | `1702_EmojisReward` | `EmojisHandler` | stub | true |
| 45 | `adhd_superpower` | 2002 | ⚡ | — | `⚡ ADHD Superpower` | `2002_AdhdSuperpower` | `AdhdSuperpowerHandler` | real | true |
| 46 | `close_game` | 6666 | 💻 | — | `💻 Выключить игру` | `6666_CloseGame` | `CloseGameHandler` | real | true |
| 47 | `credits` | 8005 | 🎬 | — | `🎬 Credits` | `8005_Credits` | `CreditsHandler` | real | true |
| 48 | `select_game` | 75000 | 🎮 | — | `🎮 Выберите мне игру` | `75000_SelectGame` | `SelectGameHandler` | real | true |
| 49 | `all_refund` | 99999 | 💣 | — | `💣 Алармагеддон 💣` | `99999_AllRefundService` | `AllRefundHandler` | real | user input + refund |

Итог покрытия:

```text
legacy наград:              49
новых конкретных хендлеров: 47
  real:                    34
  stub:                    13
  missing:                 2   (random_mem, miku_monday)
```

---

## 4. Проверка fingerprint

### 4.1. Уникальность `(Cost, Emoji)`

Проверено по исходникам:

```text
Total=49 Unique=49
```

Все **49** пар `(Cost, NormalizedEmoji)` уникальны. Дублей нет.

### 4.2. Почему нельзя использовать только emoji

Duplicates по emoji между разными cost:

- `🎵` — 9 и 337;
- `🧸` — 4 и 170;
- `🎤` — 39 и 170;
- `🐸` — 4 и 38;
- `🎬` — 353, 1602, 8005;
- `👋` — 210 и 215;
- `🎺` — 333 и 1333;
- `🧠` — 7 и 317;
- `❓` — 61 и 1510.

Поэтому fingerprint обязан быть составным: `(Cost, NormalizedLeadingEmoji)`.

### 4.3. Группы пересечений по цене

- `roll4`:
  - `search_wife`;
  - `miku_roulette`;
  - `frog_roulette`;
  - `fumo_roulette`.
- `day170`:
  - `fumo_friday_night`;
  - `miku_monday_alert`.

Внутри каждой группы emoji разные, но цена одинаковая. Без `RewardGroup` и activation policy старый `"первый по цене"` матчинг может выбрать не ту награду.

### 4.4. Проблемные fingerprint

1. `160_LegBum`
   - title в исходнике битый: `"\uFFFD발 НОГОЙ БОМЖА"`;
   - `🦵` взят из `AlertDescription`;
   - перед реальным backfill/сравнением нужно сверить живое Twitch-название;
   - в тесте извлечения emoji `leg_bum` исключаем или помечаем `EmojiSource.Description`.

2. `341_Aga`
   - title `📽️ AGA` содержит variation selector `FE0F`;
   - при нормализации `📽️` → `📽`;
   - сравнение делает `RewardEmojiExtractor.Normalize`, а не сырой `string`.

---

## 5. Контракт `RewardDefinition`

### 5.1. Где живут типы

| Тип | Проект | Назначение |
|---|---|---|
| `RewardDefinition` | `MARS.Shared.Contracts` | общий контракт награды для Alerts и TwitchCore |
| `RewardFingerprint` | `MARS.Shared.Contracts` | пара `(Cost, NormalizedEmoji)` |
| `RewardCatalog` | `MARS.Shared.Contracts` | все 49 наград, включая две без хендлера |
| `RewardBinding` / расширение `ChannelRewardRecord` | БД `MARS.Twitch.Api` | привязка `CodeKey → TwitchRewardId` |
| `IRewardAlertHandler` | `MARS.Alerts.Application.Twitch.Rewards` | обработчик со ссылкой на каталог |
| `IRewardActivationPolicy` | `MARS.Alerts.Application.Twitch.Rewards` | активна ли награда сейчас |

`RewardRouting.cs` в рантайме больше не нужен: title перестаёт быть ключом.

### 5.2. `RewardDefinition`

```csharp
public sealed record RewardDefinition
{
    public required string Code { get; init; }              // random_sound
    public required int ExpectedCost { get; init; }         // 10
    public required string NormalizedEmoji { get; init; }   // 🔊
    public required string LegacyTitle { get; init; }       // bootstrap only

    public string? Group { get; init; }                     // roll4 / day170
    public bool RequiresUserInput { get; init; }
    public bool RequiresFollower { get; init; }
    public bool ExcludedFromRandomPool { get; init; }
    public TimeSpan? UserCooldown { get; init; }

    public RewardFingerprint Fingerprint =>
        new(ExpectedCost, NormalizedEmoji);
}

public readonly record struct RewardFingerprint(
    int Cost,
    string NormalizedEmoji
)
{
    public override string ToString() => $"{Cost}:{NormalizedEmoji}";
}
```

`LegacyTitle` остаётся **только** для первичной привязки/диагностики. В маршрутизации он не участвует.

### 5.3. `RewardCatalog`

Один источник истины на 49 записей:

```csharp
public static class RewardCatalog
{
    public static IReadOnlyList<RewardDefinition> All { get; }
    public static IReadOnlyDictionary<string, RewardDefinition> ByCode { get; }
    public static IReadOnlyDictionary<RewardFingerprint, string> ByFingerprint { get; }

    public static RewardDefinition Get(string code) => ByCode[code];
}
```

В каталог обязательно входят:

- 47 наград с хендлерами;
- `random_mem`;
- `miku_monday` — пока без reward-хендлера.

Так тест сможет проверить **все 49**, а не только зарегистрированные DI-типы.

---

## 6. Изменение контракта хендлера

```csharp
public interface IRewardAlertHandler
{
    string RewardCode { get; }
    RewardDefinition Definition { get; }
    IRewardActivationPolicy Activation { get; }

    Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct);
}
```

`RewardHandlerBase` реализует общую часть:

```csharp
public abstract string RewardCode { get; }

public RewardDefinition Definition => RewardCatalog.Get(RewardCode);

public virtual IRewardActivationPolicy Activation =>
    AlwaysActiveRewardPolicy.Instance;
```

Конкретный хендлер объявляет только код:

```csharp
public sealed class RandomSoundHandler : RewardHandlerBase
{
    public override string RewardCode => "random_sound";

    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct)
    {
        // ...
    }
}
```

`Cost=10`, `Emoji=🔊`, `LegacyTitle="🔊 Random sound"` берутся из каталога, а не дублируются в хендлере.

---

## 7. Политика активности

```csharp
public interface IRewardActivationPolicy
{
    ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken ct
    );
}
```

Базовые реализации:

| Политика | Где нужна |
|---|---|
| `AlwaysActiveRewardPolicy` | большинство наград |
| `DayOfWeekPolicy(Friday)` | `fumo_friday`, `fumo_friday_night`, `fumo_roulette` |
| `DayOfWeekPolicy(Monday)` | `miku_monday`, `miku_monday_alert`, `miku_roulette` |
| `DayOfWeekPolicy(Wednesday)` | `wednesday_frog`, `frog_roulette` |
| `NotDaysPolicy(Fri, Wed, Mon)` | `waifu_marriage`, `add_wife`, `search_wife` |
| `DisabledPolicy` | `audio_quiz`, `cinema_request` через `EnabledByCost` |
| `ServiceActivePolicy` | `michael_time`, `random_art` |
| `CooldownPolicy` | `random_reward`, roll-награды |

`TimeProvider` внедряется в политики, чтобы тесты дней/кулдаунов не зависели от реального времени.

---

## 8. Изменение `RewardRedeemedEvent`

Добавляем одно поле:

```csharp
public string RewardCode { get; set; } = string.Empty;
```

Итоговая публикация из TwitchCore:

```text
RewardId    = Twitch Reward.Id        // уже есть
RewardCode  = binding.CodeKey         // новое
Cost        = Twitch Reward.Cost      // уже есть
RewardTitle = только для логов        // уже есть
```

Если код пуст, событие всё равно публикуется, но инкрементится метрика `unbound_reward`. Молча дропать нельзя.

---

## 9. Новый `RewardEventManager`

### 9.1. Конструктор

Сейчас:

```text
RewardRouting.KeyFor(RewardTitle) → handler
```

Стало:

```text
RewardCode → handler
```

### 9.2. Маршрутизация редемпшена

```text
1. RewardCode непуст        → ByCode → handler
2. RewardCode пуст,
   RewardId непуст          → BindingCache: RewardId → RewardCode → handler
3. fallback на время миграции
   Cost + Group             → ровно один активный handler
4. иначе                    → метрика unbound_reward, no-op
```

`RewardTitle` не используется ни в одном шаге.

Для шага 2 источник `BindingCache` — привязки, которые TwitchCore публикует/обновляет вместе с `ChannelRewardRecord`. Основной путь — publisher всегда кладёт `RewardCode`; fallback существует только для старых сообщений при rolling deploy.

### 9.3. Группы

Для `roll4` и `day170` менеджер не берёт «первого»:

```text
Group "roll4":
  frog_roulette   → IsActiveAsync
  fumo_roulette   → IsActiveAsync
  miku_roulette   → IsActiveAsync
  search_wife     → IsActiveAsync

Ровно один active → HandleAsync
Ноль active       → no-op
Два active        → conflict metric, не вызываем наугад
```

Fan-out событий `OnTwitchMessage`, `OnTwitchRewardInput`, `OnTwitchEvent` из PR #64 остаётся. Требуется добавить:

- фильтрацию по `RewardCode` внутри подписчиков;
- изоляцию ошибок подписчика, чтобы один handler не прерывал всю рассылку;
- метрику/лог на конфликтующих подписках.

---

## 10. `RewardBindingSyncService`

### 10.1. Важная находка в текущем коде

В `ChannelRewardsSyncService.cs:49-54` матчинг идёт так:

```csharp
remote.FirstOrDefault(r =>
    r.Cost == record.Cost
    || r.Title.Equals(record.Title, StringComparison.OrdinalIgnoreCase))
```

Это ровно та проблема, которую мы исправляем:

- для cost `4` четыре награды;
- для cost `170` две награды;
- первое совпадение по цене может привязать не ту награду.

Новый sync должен **заменить матчинг**, а не просто дополнить этот код.

### 10.2. Не отдельная таблица, а расширение `ChannelRewards`

В `MARS.Twitch.Api` уже есть `ChannelRewardRecord` с полями:

- `Title`;
- `Cost`;
- `TwitchRewardId`;
- `IsDeleted`;
- `MediaInfoId`;
- и т.д.

Добавляем колонки:

```text
RewardCode        string?   // random_sound
NormalizedEmoji   string?   // 🔊
BindingStatus     string    // Bound/Orphaned/NeedsManualBinding/Conflicting
```

Ограничения:

- `UNIQUE (RewardCode)`, filtered where not null;
- `UNIQUE (TwitchRewardId)`, filtered where not null;
- `UNIQUE (Cost, NormalizedEmoji)`, filtered where not null.

Отдельный `RewardBinding` не нужен: это было бы дублирование владельца.

### 10.3. Размещение сервиса

```csharp
public interface IRewardBindingSyncService
{
    Task<RewardBindingReport> SyncAsync(CancellationToken ct);
}
```

`ChannelRewardsSyncService.SyncOnce` в начале вызывает:

```csharp
await bindingSync.SyncAsync(cancellationToken);
```

После этого текущий create/update/delete работает с уже правильными `TwitchRewardId`.

Сетевые вызовы идут только через уже существующий `IChannelRewardsService`. В конструкторе никаких скрытых сетевых/файловых побочных эффектов.

### 10.4. DTO для тестов без Helix

`IChannelRewardsService.GetRewardsAsync()` возвращает `CustomReward` из TwitchLib — в тестах неудобно. Вводим snapshot:

```csharp
public sealed record RemoteReward(
    string Id,
    string Title,
    int Cost
);
```

Адаптер: `CustomReward → RemoteReward`.

Resolver работает только с snapshot, поэтому тесты не требуют сети.

### 10.5. Результат сопоставления

```csharp
public enum RewardBindingStatus
{
    Bound,
    AlreadyBound,
    Rebound,
    CostDrift,
    Orphaned,
    NeedsManualBinding,
    Conflicting
}

public sealed record RewardBindingResult(
    string Code,
    RewardBindingStatus Status,
    string? TwitchRewardId,
    string? Reason
);

public sealed record RewardBindingReport(
    IReadOnlyList<RewardBindingResult> Results
);
```

### 10.6. Алгоритм

Для каждого `RewardDefinition` из каталога:

#### Случай A — живая привязка

```text
local.RewardCode == definition.Code
local.TwitchRewardId непуст
remote содержит этот Id
```

Действуем:

- оставляем `TwitchRewardId` как есть;
- если `remote.Cost != definition.ExpectedCost` → `CostDrift`, но **не перепривязываем**;
- если title изменился → обновляем `Title` в БД как диагностику;
- `NormalizedEmoji` держим из каталога, а не из Twitch;
- статус `AlreadyBound` / `CostDrift`.

#### Случай B — orphaned binding

```text
local.RewardCode == definition.Code
local.TwitchRewardId непуст
remote НЕ содержит этот Id
```

Кандидаты ищем строго по fingerprint:

```text
remote.Cost == definition.ExpectedCost
&& ExtractEmoji(remote.Title) == definition.NormalizedEmoji
```

- ровно 1 кандидат и он не занят другим `RewardCode` → `Rebound`, сохраняем новый `TwitchRewardId`;
- 2+ кандидата → `NeedsManualBinding`, ничего не меняем;
- кандидат уже привязан к другому коду → `Conflicting`;
- 0 кандидатов → fallback по точному `LegacyTitle`, если он уникален;
- иначе → `Orphaned`, старый `TwitchRewardId` **оставляем** для диагностики.

#### Случай C — локальная строка без кода

Ищем legacy-строку по `Title == definition.LegacyTitle` (`OrdinalIgnoreCase`):

- нашли уникальную → проставляем `RewardCode`, `NormalizedEmoji`, статус `Bound`;
- нашли несколько → `Conflicting`;
- не нашли → создаём новую `ChannelRewardRecord` с кодом/ценой/emoji; Helix-создание остаётся текущему `ChannelRewardsSyncService`.

#### Глобальные проверки

- два `ChannelRewardRecord` с одним `RewardCode` → `Conflicting`;
- два с одним `TwitchRewardId` → `Conflicting`;
- два definition с одним fingerprint → должно ловиться тестом каталога, в проде = fatal log.

---

## 11. Изменение publisher

### 11.1. Валидация

Сейчас:

```csharp
if (IsRandomSoundReward(twitchEvent.Reward.Title))
    redemptionValidator.RequireCost(10).RequireFollower();
```

Стало:

```csharp
if (binding?.RewardCode == "random_sound")
    redemptionValidator.RequireCost(10).RequireFollower();
```

follower/cost-политика перестаёт зависеть от title.

### 11.2. `BuildRewardEventAsync`

Сейчас ищет по `TwitchRewardId`, потом по title.

Стало:

```text
1. по TwitchRewardId
2. по RewardCode из каталога/записи
3. title-fallback только до завершения миграции, с warning
```

В событие:

```csharp
RewardCode = record?.RewardCode ?? string.Empty
```

---

## 12. Тесты

### 12.1. Тест полноты и уникальности каталога

Это key-тест, который заменяет часть старых тестов на `RewardKey`:

```csharp
[Fact]
public void RewardCatalog_IsCompleteAndFingerprintsAreUnique()
{
    var all = RewardCatalog.All;

    Assert.Equal(49, all.Count);
    Assert.Equal(all.Count, all.Select(d => d.Code).Distinct().Count());
    Assert.Equal(all.Count, all.Select(d => d.Fingerprint).Distinct().Count());

    var groupViolations = all
        .Where(d => d.Group is not null)
        .GroupBy(d => d.Group)
        .Where(g =>
            g.Select(d => d.ExpectedCost).Distinct().Count() != 1
            || g.Count() < 2
        );

    Assert.Empty(groupViolations);

    var handlerCodes = RewardHandlerRegistration
        .DiscoverHandlerTypes()
        .Select(GetRewardCodeFromType)
        .ToArray();

    var missing = all
        .Select(d => d.Code)
        .Except(handlerCodes)
        .ToArray();

    Assert.Equal(
        new[] { "random_mem", "miku_monday" },
        missing.OrderBy(c => c)
    );
}
```

Инварианты:

- 49 наград, ни одной не потеряли;
- `Code` уникален;
- `(Cost, Emoji)` уникален;
- группа существует только если в ней ≥2 награды;
- все участники группы имеют одинаковый `ExpectedCost`;
- единственные награды без хендлера — ожидаемые `random_mem` и `miku_monday`.

### 12.2. Тест извлечения emoji

```csharp
[Fact]
public void ExtractedEmoji_MatchesCatalogDefinition()
{
    foreach (var definition in RewardCatalog.All)
    {
        if (definition.Code == "leg_bum")
            continue; // title в исходнике битый, emoji взят из AlertDescription

        var emoji = RewardEmojiExtractor.FirstGrapheme(definition.LegacyTitle);
        var normalized = RewardEmojiExtractor.Normalize(emoji);

        Assert.Equal(definition.NormalizedEmoji, normalized);
    }
}
```

Отдельные case-тесты:

- `📽️ AGA` → `📽` после снятия `FE0F`;
- `🎲 Случайная награда!` → `🎲`;
- leading whitespace → emoji после `Trim()`;
- ZWJ/флаги не разбиваются;
- `leg_bum` помечен как `EmojiSource.Description`, пока title не сверен с Twitch.

### 12.3. `RewardBindingResolverTests`

| Тест | Ожидание |
|---|---|
| `LiveBinding_SurvivesTitleChange` | привязка не меняется, `AlreadyBound` |
| `LiveBinding_ReportsCostDrift` | `CostDrift`, `TwitchRewardId` прежний |
| `Orphaned_RebindsByCostAndEmoji` | новый Id, `Rebound` |
| `EmojiAlone_IsNotEnough` | `(9, 🎵)` не подходит для `(10, 🔊)` → `Orphaned` |
| `CostAlone_IsNotEnough` | `(10, 🎲)` не подходит для `(10, 🔊)` → `Orphaned` |
| `AmbiguousFingerprint_NeedsManualBinding` | два `(4, 🐸)` → `NeedsManualBinding` |
| `CandidateAlreadyBound_IsConflict` | `(4, 🐸)` уже у `wednesday_frog` → `Conflicting` |
| `LegacyTitleFallback_WorksWhenEmojiChanged` | уникальный точный title → `Rebound` |
| `NoCandidate_KeepsOldId` | `Orphaned`, Id не очищается |
| `DuplicateCodeRows_AreConflict` | два `RewardCode=random_sound` → `Conflicting` |
| `DuplicateTwitchIds_AreConflict` | два одинаковых `TwitchRewardId` → `Conflicting` |
| `CatalogOnlyRewards_AreResolvedToo` | `random_mem`, `miku_monday` получают binding |

### 12.4. Интеграционные тесты с PostgreSQL

По требованиям репозитория — только Testcontainers и реальные миграции. Не использовать EF InMemory или SQLite. `CancellationToken` брать из `TestContext.Current.CancellationToken`.

`RewardBindingSyncServiceTests`:

1. `BackfillsRewardCodesFromLegacyTitles`
   - сидим 49 legacy-строк без `RewardCode`;
   - запуск sync;
   - assertions: 47 кодов проставлены, два catalog-only остаются без хендлера, но код есть.

2. `ReboundBindingIsPersisted`
   - `TwitchRewardId` удалён из remote;
   - один кандидат по fingerprint;
   - в БД сохранился новый Id.

3. `AmbiguousBindingDoesNotMutateDatabase`
   - два кандидата;
   - БД осталась прежней.

4. `SyncIsIdempotent`
   - два запуска;
   - `CreateRewardAsync` не вызывается второй раз;
   - у всех строк стабильные `RewardCode`.

5. `CancellationTokenIsPropagated`
   - отменённый token → выход до Helix-запроса.

### 12.5. Тесты publisher в `MARS.TwitchCore.Tests`

`RewardRedemptionPublisherTests`:

- `PublishesRewardCodeFromBinding`;
- `RandomSoundValidationUsesCodeNotTitle`;
- `TitleFallbackStillWorksDuringMigration` + warning;
- `UnboundRewardIsPublishedWithEmptyCodeAndMetric`;
- `RewardIdIsAlwaysPresent`.

### 12.6. Тесты в Alerts

`RewardEventManagerTests` дополняем:

- `RoutesByRewardCodeIgnoringTitle`;
- `FallsBackToRewardIdWhenCodeIsEmpty`;
- `UnknownCodeIncrementsUnboundMetric`;
- `Roll4GroupHasExactlyOneActivePolicy`;
- `TwoActivePoliciesInGroupProduceConflictMetric`.

### 12.7. Что ломается в текущих тестах

Нужно будет переписать, но в ходе анализа не трогать:

- `RewardHandlerRegistrationTests` — ожидает `RewardKey == KeyFor(title)` и `14` pending-хендлеров;
- `RewardAlertConsumerTests.RewardKeyIsNormalizedTitle`;
- `RewardHandlerSuiteTests` — проверяет уникальность `RewardKey`, теперь проверяем `RewardDefinition`;
- `RewardEventManagerTests` — добавить случаи `RewardCode`, `RewardId`-fallback и group-conflict;
- publisher-тесты `MARS.TwitchCore.Tests` — проверить, что публикуется `RewardCode`.

### 12.8. Конфликт в `RandomSoundHandlerTests`

Сейчас тесты противоречат коду:

- ожидают broadcast в `EventBroadcaster<TelegramusEvent>`;
- но вызов в `RandomSoundHandler.cs:66-74` закомментирован;
- broadcaster в хендлер не внедряется.

Нужно либо восстановить реальный вызов через интерфейс, либо явно изменить тест и статус награды. Нельзя оставлять «формально зелёный» тест для несуществующего поведения.

---

## 13. Фазы реализации

### Фаза 0. Подготовка

1. Создать отдельную ветку `feat/twitch-reward-identity` от `main`.
2. Не.merge в `main`, не пушить напрямую в `main`.
3. Зафиксировать текущий PR #64 как base.
4. Не трогать legacy-репозиторий, кроме сверки фактического title `160_LegBum`.

### Фаза 1. Каталог и контракты

1. Добавить `RewardDefinition`, `RewardFingerprint`, `RewardCatalog` в `MARS.Shared.Contracts`.
2. Заполнить 49 записей строго по таблице выше.
3. Добавить `RewardEmojiExtractor` с нормализацией grapheme/FE0F.
4. Добавить `RewardCode` в `RewardRedeemedEvent`.
5. Добавить тесты каталога и emoji.
6. Запустить targeted tests.

### Фаза 2. Хранилище и resolver в TwitchCore

1. Расширить `ChannelRewardRecord` колонками `RewardCode`, `NormalizedEmoji`, `BindingStatus`.
2. Добавить EF migration с filtered unique indexes.
3. Добавить `RemoteReward`, `IRewardBindingSyncService`, `RewardBindingResolver`.
4. Заменить матчинг по `Cost || Title` в `ChannelRewardsSyncService`.
5. Добавить feature flag `Rewards:UseCodeBinding`.
6. Добавить resolver/integration tests на PostgreSQL Testcontainers.

### Фаза 3. Publisher

1. В `RewardRedemptionPublisher` Resolve по `RewardId`/binding.
2. Публиковать `RewardCode`.
3. Заменить валидацию `random_sound` по title на валидацию по коду.
4. Добавить метрику `unbound_reward`.
5. Обновить тесты publisher.

### Фаза 4. Alerts routing

1. Заменить `RewardKey` на `RewardCode` в `IRewardAlertHandler`.
2. `RewardHandlerBase` получает `Definition` и `Activation` из каталога.
3. `RewardEventManager` маршрутизирует по `RewardCode`, затем `RewardId`, затем group fallback.
4. Добавить `IRewardActivationPolicy` и конкретные политики.
5. Добавить изоляцию ошибок подписчиков PR #64.
6. Обновить DI-discovery и тесты регистрации.

### Фаза 5. Паритет бизнес-логики и overlays

1. Разобрать 13 stub-хендлеров по приоритету.
2. Перенести недостающие правила из legacy:
   - `IsRewardEnabled`;
   - day-gating;
   - cooldown;
   - random pool exclusions;
   - service active;
   - user input/refund;
   - мини-игры cost `9`, `7`, `6`.
3. Для `random_mem` и `miku_monday` либо добавить реальные хендлеры, либо явно оставить catalog-only с причиной.
4. Восстановить или явно закрыть закомментированные overlay-вызовы.
5. Обновить тесты, которые сейчас проверяют несуществующее поведение.

### Фаза 6. Верификация

1. `dotnet csharpier format .`
2. `dotnet csharpier check .`
3. `scripts/windows/verify.ps1` или `scripts/unix/verify.sh`.
4. Targeted tests для:
   - `MARS.Shared.Tests`;
   - `MARS.TwitchCore.Tests`;
   - `MARS.Alerts.Tests`.
5. `graphify update .` после изменения кода.
6. Проверить residual references:
   - `RewardKey`;
   - `RewardRouting.KeyFor`;
   - `IsRandomSoundReward`;
   - title-based validation;
   - старые pending-хендлеры;
   - закомментированные overlay-вызовы.

---

## 14. Миграция БД и откат

### Миграция

1. Добавить колонки и unique-indexes в `ChannelRewards`.
2. Backfill `RewardCode` по `LegacyTitle` из каталога.
3. Для `leg_bum` backfill делать не по битому title из исходника, а по уточнённому названию из Twitch.
4. Включить `RewardBindingSyncService` под feature flag `Rewards:UseCodeBinding=true`.
5. Publisher начинает слать `RewardCode`.
6. Alerts переключается на `RewardCode`.
7. После стабильного периода убрать title-fallback.

### Откат

1. Выключить feature flag `Rewards:UseCodeBinding`.
2. Оставить старый sync по `Cost || Title`.
3. Publisher снова получает пустой `RewardCode` и работает по старому title-логу.
4. При необходимости убрать колонки.
5. `RewardRedeemedEvent.RewardCode` — новое поле с default `""`, поэтому откат безопасен для старых consumer/producer.

---

## 15. Риски и открытые вопросы

1. **`160_LegBum`**
   - title в исходнике битый;
   - `
🦵` взят из description;
   - нужно подтвердить живое Twitch-название до backfill.

2. **`341_Aga`**
   - `📽️` содержит variation selector;
   - normalization обязательна.

3. **Три награды с одинаковым emoji `🎬`**
   - `tiktok_edit` cost 353;
   - `cinema_request` cost 1602;
   - `credits` cost 8005;
   - cost спасает fingerprint, но title-fallback должен быть точным.

4. **`roll4`**
   - четыре награды cost 4;
   - activation policy должна давать ровно одного active;
   - при двух active — conflict metric, не угадывать.

5. **`day170`**
   - две награды cost 170;
   - Friday и Monday должны чётко расходиться по policies.

6. **Старые сообщения при rolling deploy**
   - часть сообщений придёт без `RewardCode`;
   - поэтому fallback по `RewardId`/fingerprint нужен, но не должен стать постоянным.

7. **Wolverine fan-out**
   - ошибка одного подписчика сейчас прерывает всю рассылку;
   - нужно добавить изоляцию, иначе один сломанный handler может заблокировать остальные награды.

8. **Stub-хендлеры**
   - 13 из 47 хендлеров не содержат реальной бизнес-логики;
   - простая замена ключа не даст паритета со старым проектом.

---

## 16. Критерии готовности

План считается реализованным, когда:

1. `RewardCatalog` содержит ровно 49 наград.
2. Все `Code` уникальны.
3. Все `(Cost, NormalizedEmoji)` уникальны.
4. `random_mem` и `miku_monday` есть в каталоге, даже если без хендлеров.
5. `RewardTitle` не используется в runtime-маршрутизации, валидации и identity.
6. `RewardRedeemedEvent` содержит `RewardCode`.
7. Publisher кладёт `RewardCode` из binding.
8. TwitchCore умеет перепривязывать только orphaned binding по `(Cost, Emoji)`.
9. Живая привязка не переопределяется по title/emoji.
10. Неоднозначные кандидаты переводятся в `NeedsManualBinding`/`Conflicting`.
11. `RewardEventManager` маршрутизирует по `RewardCode`.
12. Для `roll4` и `day170` гарантируется ровно одна активная политика.
13. При двух active policy конфликт фиксируется метрикой, а не случайным выбором.
14. Все текущие тесты, ожидавшие `RewardKey`/title, обновлены.
15. Stub-хендлеры и закомментированные overlay-вызовы явно классифицированы.
16. `verify.ps1`/`verify.sh`, CSharpier и targeted tests проходят.
17. Полный diff проверен на residual references старых ключей, имён, портов и файлов.

---

## 17. Итоговая формула

```text
RewardCatalog.Code      → стабильная идентичность бизнес-логики
RewardId                → внешняя привязка к Twitch
(Cost, Emoji)           → bootstrap/rebind fingerprint
Group                   → координация наград с одинаковой ценой
ActivationPolicy        → когда награда активна
RewardTitle             → только логи и миграция
```

Этот план готов к реализации после переключения в Build-режим и создания отдельной ветки.
