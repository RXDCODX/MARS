# Полная миграция MARS на типизированные pub/sub события Wolverine + RabbitMQ

## Цель

1. Создать новый skill по новой инфраструктуре pub/sub событий RabbitMQ на основе
   `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/WolverineMessagingExtensions.cs`
   и закрепить его в `AGENTS.md`.
2. Полностью убрать legacy-каталог событий из
   `src/MARS.Shared.Contracts/Messaging/RabbitMqConfig.cs`, переведя все публикаторы и
   потребители на типизированные контракты Wolverine.

Режим: полная миграция в одном branch/PR, последовательными коммитами.

## Ветка

`feat/rabbitmq-pubsub-events` от `main`.

---

## Фаза 0. Починить маршрутизацию Wolverine

Файл: `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/WolverineMessagingExtensions.cs`

**Дефект.** `UseConventionalRouting` по умолчанию берёт имя exchange из Wolverine
(`ToMessageTypeName()` → полное имя CLR-типа), а `ConfigurePublishing` публикует в
`mars.kebab-case`, рассчитанное `MarsMessageNaming.ExchangeNameFor`. Листенер
привязывается к одному exchange, публикация уходит в другой.

**Правки**

1. В конвенцию добавить привязку exchange к тому же имени, куда публикует
   `ConfigurePublishing`:

   ```csharp
   convention.ExchangeNameForSending(MarsMessageNaming.ExchangeNameFor);
   ```

2. Добавить регрессионный тест в
   `tests/MARS.Shared.Tests/Messaging/MarsMessagingInfrastructureTests.cs`:
   листенер должен быть привязан к `mars.{message}`, а не к полному имени CLR-типа.
3. Проверить, что публикация и подписка используют один exchange.

Без этого шага остальная миграция бессмысленна: листенеры не получат сообщения.

---

## Фаза 1. Скилл и документация

1. Создать `.opencode/skills/rabbitmq-pubsub-events/SKILL.md`.
2. Создать зеркало `.mimocode/skills/rabbitmq-pubsub-events/SKILL.md`.
3. В `AGENTS.md` добавить строку в таблицу Skills.
4. Обновить RabbitMQ-раздел `mars-infrastructure` в обеих копиях:
   - убрать устаревший путь `src/MARS.Shared/Messaging/RabbitMqConfig.cs`;
   - разделить legacy-путь и новый путь Wolverine;
   - сослаться на новый skill.

**Содержание skill**

1. Контракт: класс в `src/MARS.Shared.Contracts/Messaging`, реализует
   `IMarsEventMessage`; имя `XxxEvent` → exchange `mars.xxx`; только конкретные,
   закрытые классы; контракты обязаны лежать в `MARS.Shared.Contracts`, иначе
   публикация не найдёт маршрут.
2. Публикация через Wolverine `IMessageBus.PublishAsync(message)`; не добавлять
   событие в `RabbitMqConfig`; не использовать legacy `IMarsEventBus` для новых
   событий.
3. Подписка: класс-наследник `MarsEventListener<TEvent>` с `Handle`; ровно один тип
   сообщения на листенер; регистрация не через `AddHostedService`, а через discovery
   Wolverine.
4. `Program.cs`: `builder.AddMarsWolverine("MARS.Service", typeof(Program).Assembly)`;
   имя сервиса определяет очередь и DLQ.
5. Конфигурация и поведение: `RabbitMq:Host`, AutoProvision, publisher confirms, DLQ,
   отсутствие брокера не валит сервис.
6. Семантика: exchange на тип события, у каждого листенера своя очередь; добавление
   листенера создаёт новую очередь, удаление листенера очередь автоматически не
   удаляет.
7. Тесты: naming, discovery, queue naming; ссылки на
   `MarsMessagingInfrastructureTests`.
8. Миграция с legacy: замена `RabbitMqConsumerBase`/`IMarsEventBus`, удаление ключей
   из `RabbitMqConfig`, необходимость dual-publish при rolling deploy.
9. Типовые ошибки: событие не в `Shared.Contracts`, два `IMarsMessageListener<T>` у
   листенера, неверное имя сервиса, ручная регистрация consumer'а, публикация сырого
   JSON.
10. Проверка: `dotnet build`, целевые `MARS.Shared.Tests`, `dotnet csharpier format .`.

---

## Фаза 2. Типизированные контракты

Все контракты — в `src/MARS.Shared.Contracts/Messaging`, реализуют
`IMarsEventMessage`.

| Legacy | Новый контракт |
|---|---|
| `twitch.reward.redeemed` + `twitch.reward.*` (все 33) | `RewardRedeemedEvent`, фильтрация по `RewardTitle` |
| `twitch.message.received` | `ChatEventMessageEvent` (существующий) |
| `twitch.message.rewardinput` | `RewardInputChatMessageEvent` |
| `twitch.message.deleted` | `ChatMessageDeletedEvent` (перенести из `TwitchCore`) |
| `twitch.chat.send` | `ChatSendEvent` (существующий) |
| `twitch.eventsub.raw` | `EventSubRawEvent` (существующий) |
| `waifu.*.result` | `WaifuRollResultEvent` / `CollectionRollResultEvent` |
| `media.track.*` | `MediaTrackStartedEvent`, `MediaTrackEndedEvent`, `MediaTrackAddedEvent` |
| `twitch.user.joined`, `admin.events` | удалить как мёртвый код после проверки отсутствия публикаторов |

Дополнительно:

- `RewardRedeemedEvent` остаётся единственным reward-событием.
- Нормализацию названия награды вынести в небольшой helper
  (`RewardRouting.KeyFor(title)`), не в `RabbitMqConfig`.
- `IRewardAlertHandler.RoutingKey` заменить на локальный `RewardKey` без префикса
  `twitch.reward.`; централизованный список `RewardSpecificKeys` удаляется.
- Тесты: уникальность exchange-имён, контракты только concrete/closed, отсутствие
  дублей, покрытие новых типов.

---

## Фаза 3. Публикаторы

Перевести на Wolverine `IMessageBus.PublishAsync`:

- `MARS.TwitchCore`: `RewardRedemptionPublisher`, `HelloVideoNotifier`,
  `TwitchMessagesPublisher`, `EventSubRawForwarder`.
- `MARS.WaifuGacha`: `RollCooldownNotificationService`.

Удалить:

- вычисление `RabbitMqConfig.RewardKey` и публикацию по routing key;
- `RabbitMqConfig.ChatSend`, `MessageReceived`, `RewardInputMessage`,
  `MessageDeleted`.

В `Program.cs` затронутых сервисов добавить:

```csharp
builder.AddMarsWolverine("MARS.TwitchCore", typeof(Program).Assembly);
builder.AddMarsWolverine("MARS.Alerts", typeof(Program).Assembly);
builder.AddMarsWolverine("MARS.WaifuGacha", typeof(Program).Assembly);
```

Имена — строго как в `AddMarsDefaults`, иначе разъедутся очереди и DLQ.

---

## Фаза 4. Потребители

Перевести на `MarsEventListener<T>`:

- `MARS.Alerts`: `RewardAlertConsumer`, `TwitchMediaAlerts`, `ChatUserConsumer`,
  `TriggerWordAlertConsumer`, `RewardInputMessageConsumer`,
  `CostMatchedRewardConsumer`, `SystemEventsConsumer`.
- `MARS.TwitchCore`: `TwitchChatSendConsumer`.
- `MARS.WaifuGacha`: `RollCooldownNotificationService` → отдельные листенеры на
  `RewardRedeemedEvent` и `ChatEventMessageEvent` (правило «ровно один тип сообщения
  на листенер»).

Особенности:

- `RewardAlertConsumer` теперь получает все `RewardRedeemedEvent`; обработчик
  выбирается по `RewardRouting.KeyFor(RewardTitle)`.
- `TwitchMediaAlerts` фильтрует `Media != null`, как и сейчас.
- `TriggerWordAlertConsumer` получает и обычный чат, и reward-input через два тонких
  листенера, чтобы не нарушать контракт `MarsQueues`.
- `SystemEventsConsumer` удаляется, если поиск по репозиторию подтвердит отсутствие
  публикаторов; иначе для него создаются типизированные контракты.
- Ручные `AddHostedService` для этих потребителей удаляются — discovery делает
  Wolverine.

---

## Фаза 5. Удаление legacy

Удалить:

- `src/MARS.Shared.Contracts/Messaging/RabbitMqConfig.cs`;
- `RabbitMqEventBus.cs`, `RabbitMqConsumerBase.cs`, `IMarsEventBus.cs` — если не
  останется пользователей;
- регистрацию `IMarsEventBus` из
  `src/MARS.Platform.Hosting.Infrastructure/Extensions/WebApplicationBuilderExtensions.cs`;
- неиспользуемые legacy-контракты (`TrackEvent`, `UserJoinedEvent` и т.п.);
- тесты legacy-шины (`RabbitMqEventBusTests`, `RabbitMqConsumerBaseTests`) или
  переписать их под новый путь.

Обновить все тесты, которые ссылаются на `RabbitMqConfig`: `MARS.Alerts.Tests`,
`MARS.TwitchCore.Tests`, `MARS.WaifuGacha.Tests`, `MARS.Shared.Tests`.

Исторические документы (`docs/SERVICE_MIGRATION_CHECKLIST.md`,
`docs/MIGRATION_QUESTIONS.md`, `src/MIGRATION_CHECKLIST.md`) не переписываем — по
`AGENTS.md` это исторический статус, не контракт.

---

## Проверка

```powershell
dotnet build MARS.slnx -c Release
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release
dotnet test tests/MARS.Alerts.Tests/MARS.Alerts.Tests.csproj -c Release
dotnet test tests/MARS.TwitchCore.Tests/MARS.TwitchCore.Tests.csproj -c Release
dotnet test tests/MARS.WaifuGacha.Tests/MARS.WaifuGacha.Tests.csproj -c Release
dotnet csharpier format . && dotnet csharpier check .
```

Плюс `.\scripts\windows\verify.ps1` перед PR.

---

## Миграция и риск

- Старые exchange `mars.events` и очереди `alerts.*`, `waifu.rolls` и т.д. после
  переключения публикаторов перестанут получать новые сообщения. Новые очереди
  создаст Wolverine AutoProvision.
- Для rolling deploy нужен либо dual-publish на один релиз, либо согласованный деплой
  публикаторов и потребителей. В PR это раздел «Миграция и откат».
- Откат — revert PR: legacy-код остаётся в истории, старые очереди не удаляются
  автоматически.
- Отдельно проверить, что удаление `waifu.*`/`media.track.*`/`UserJoined` не ломает
  внешнего публикатора; если он есть, эти события получат типизированные контракты
  вместо удаления.

---

## Ключевые файлы

- `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/WolverineMessagingExtensions.cs`
- `src/MARS.Shared.Contracts/Messaging/RabbitMqConfig.cs`
- `src/MARS.Shared.Contracts/Messaging/MarsMessageNaming.cs`
- `src/MARS.Shared.Contracts/Messaging/MarsQueues.cs`
- `src/MARS.Shared.Contracts/Messaging/IMarsEventMessage.cs`
- `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/IMarsEventListener.cs`
- `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/RabbitMqConsumerBase.cs`
- `src/MARS.Platform.Messaging.RabbitMq.Infrastructure/RabbitMqEventBus.cs`
- `src/MARS.Platform.Hosting.Infrastructure/Extensions/WebApplicationBuilderExtensions.cs`
- `tests/MARS.Shared.Tests/Messaging/MarsMessagingInfrastructureTests.cs`
- `src/MARS.Alerts/Services/Twitch/Rewards/RewardAlertConsumer.cs`
- `src/MARS.Alerts/Services/SystemEventsConsumer.cs`
- `src/MARS.TwitchCore/Services/Rewards/RewardRedemptionPublisher.cs`
- `src/MARS.WaifuGacha/Services/RollCooldownNotificationService.cs`
- `AGENTS.md`
- `.opencode/skills/mars-infrastructure/SKILL.md`
- `.mimocode/skills/mars-infrastructure/SKILL.md`
