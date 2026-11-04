# План: форвард сырого Twitch EventSub в RabbitMQ для внешнего балансировщика

> **Статус (2026-11-04): реализовано.** Форвардер — `EventSubRawForwarder`,
> ключ `eventsub.received`, очередь `mars.alerts.eventsub` читает
> `MARS.Alerts/TwitchEventSubConsumer` (оверлеи и обработчики наград). Внешнего
> балансировщика, ради которого писался план, нет: очередь создаёт сам
> потребитель, а не внешний клиент. Прежние имена `twitch.eventsub.raw` и
> `alerts.twitch.eventsub` удалены без алиасов — внешнего потребителя не
> существовало. Остальной текст ниже — исходный план, а не текущее описание.

## Цель

Добавить в `MARS.TwitchCore` перехват оригинальных сырых JSON-фреймов Twitch EventSub и публиковать их в RabbitMQ обмен `mars.events` с роутинг-ключом `twitch.eventsub.raw` для потребления внешним балансировщиком. Текущие in-process обработчики EventSub оставить без изменений.

## Контекст

### Текущая архитектура
- `MARS.TwitchCore/Services/EventSub/EventSubService.cs` — управляет подключением к Twitch EventSub WebSocket, создаёт подписки: `channel.raid`, `stream.online`, `stream.offline`, `channel.channel_points_custom_reward_redemption.add`, `channel.moderator.add`, `channel.vip.add`, `channel.follow` v2.
- `MARS.TwitchCore/Services/EventSub/EventSubWebsocketClientCustom.cs` — пустой untracked класс, потенциальное место под логику форварда.
- `MARS.TwitchCore/Program.cs` — регистрация `EventSubWebsocketClient` как singleton.
- `MARS.TwitchCore` использует TwitchLib.EventSub.Websockets 0.8.0.

### Текущие обработчики EventSub (оставить без изменений)
| Сервис | События | Назначение |
|---|---|---|
| `RewardRedemptionPublisher` | `channel.channel_points_custom_reward_redemption.add` | Публикует `twitch.reward.redeemed` и `twitch.reward.<title>` в `mars.events`. Потребляется `MARS.Alerts/RewardAlertConsumer`. **Критично не трогать.** |
| `TwitchStreamStartupNotifications` | `stream.online/offline` | Уведомления в чате, взаимодействие с аудио-контроллером. |
| `RxdcodxViewersService` | `channel.moderator.add`, `channel.vip.add`, `channel.follow` v2 | Кеширование модеров/випов/фолловеров в БД, синхронизация с API. |
| `MiniGamesManager` | `channel.channel_points_custom_reward_redemption.add` | Мёртвый код, не зарегистрирован в DI. Игнорировать. |

### RabbitMQ
- Обмен: `mars.events` (topic, уже существует)
- Роутинг-ключ: `twitch.eventsub.raw`
- Dev-брокер: `localhost:6018` (AMQP), креды из `.env.development.example` (`mars/mars`), vhost `/`
- Очередь и биндинги создаст внешний балансировщик
- `IMarsEventBus` / `RabbitMqEventBus` уже используется

### TwitchLib особенности
- `EventSubWebsocketClient.OnDataReceived` приватный — парсит фрейм, диспатчит в типизированные события
- `WebsocketClient.OnDataReceived` (internal) получает `DataReceivedArgs` с полным массивом байт оригинального JSON-фрейма
- `DataReceivedArgs` внутренний класс
- `EventSubWebsocketClient` создаёт `WebsocketClient` самостоятельно в конструкторе

## Требования

- **Только форвард**. Ничего не изменять в существующих хендлерах, их подписках, DI-регистрациях, логике создания EventSub-подписок.
- **Сырые данные**. Публиковать **оригинальный полный JSON-фрейм** Twitch EventSub (`{"metadata":{...},"payload":{...}}`) без потери полей.
- **Все события**. Форвардить все типы сообщений: `notification`, `session_welcome`, `session_keepalive`, `session_reconnect`, `session_disconnect`, `revocation` и прочие.
- **Best-effort**. Без персистентного outbox, без гарантированной доставки. При перегрузке дропать с метрикой.
- **Быстрая работа**. Тап не должен блокировать обработку TwitchLib. Fire-and-forget + bounded-буфер.
- **Минимальные изменения**. Не добавлять очередь в `RabbitMqConfig`, не менять текущую бизнес-логику.
- **Следовать правилам репозитория**: TDD где применимо, CSharpier, `TreatWarningsAsErrors`, не трогать unrelated файлы.

## Решение

### Подход к перехвату сырого фрейма
Использовать **reflection-тап** на внутренний `WebsocketClient`:

1. Получить экземпляр `EventSubWebsocketClient` из DI.
2. Через рефлексию достать приватное поле `_websocketClient` типа `WebsocketClient`.
3. Подписаться на его `internal event OnDataReceived` через `GetEvent` + `AddEventHandler`.
4. В обработчике получить `DataReceivedArgs.Bytes` — полный оригинальный JSON-фрейм.
5. Собрать конверт с метаданными и опубликовать в RabbitMQ асинхронно в фоне.

Альтернативу с наследованием использовать нельзя (метод приватный). Замена `WebsocketClient` через DI сложна из-за создания внутри конструктора.

### Конверт сообщения
```json
{
  "schemaVersion": 1,
  "messageId": "<metadata.message_id>",
  "messageType": "<session_welcome|session_keepalive|session_reconnect|session_disconnect|notification|revocation>",
  "subscriptionType": "<channel.*|stream.*|... или null для session_* >",
  "subscriptionVersion": "<версия или null>",
  "subscriptionId": "<payload.subscription.id или null>",
  "sessionId": "<текущий SessionId wsClient.SessionId>",
  "eventName": "<subscriptionType если есть, иначе messageType>",
  "observedAtUtc": "2026-10-24T...",
  "sourceService": "MARS.TwitchCore",
  "traceParent": "<из текущего Activity>",
  "raw": { /* полный оригинальный фрейм {"metadata": {...}, "payload": {...}} */ }
}
```

### Публикация
- Использовать `IMarsEventBus.PublishAsync` (существующий механизм)
- Роутинг-ключ: `twitch.eventsub.raw`
- Сериализация конверта в JSON
- Best-effort: при ошибки логировать, не ретраить
- Для снижения нагрузки в хуке: десериализовать минимально только для извлечения метаданных (`messageId`, `messageType`, `subscriptionType`, `subscriptionVersion`) из оригинального JSON для конверта, при этом **сам `raw` сохранять как исходный объект/JSON**

### Метрики
Добавить в `MARS.Shared/Telemetry/MarsMetrics` или использовать существующие счётчики:
- `eventsub.raw.forwarded` — получен фрейм с WebSocket
- `eventsub.raw.published` — успешно опубликован в RabbitMQ
- `eventsub.raw.dropped` — дропнут из-за переполнения буфера
- `eventsub.raw.errors` — ошибка при публикации/обработке

## Задачи по этапам

### Этап 1. Подготовка инфраструктуры
- [ ] **Изучить** текущую реализацию `RabbitMqEventBus` и `IMarsEventBus` для понимания API публикации
- [ ] **Проверить** наличие метрик в `MarsMetrics`, при необходимости добавить новые счётчики
- [ ] **Определить** место размещения логики форварда (использовать `EventSubWebsocketClientCustom.cs` или новый класс `EventSubRawForwarder.cs` в `Services/EventSub/`)

### Этап 2. Реализация reflection-тапа
- [ ] **Создать** `EventSubRawForwarder` (IHostedService) который:
  - Получает `EventSubWebsocketClient` из DI
  - Через рефлексию достаёт `_websocketClient` (`WebsocketClient`)
  - Подписывается на internal `OnDataReceived` через reflection
  - Сохраняет ссылку на `SessionId` из `EventSubWebsocketClient`
- [ ] **Реализовать** bounded-очередь для буферизации фреймов (например `Channel<byte[]>` с ограниченной ёмкостью)
- [ ] **Реализовать** фоновый воркер для публикации из очереди через `IMarsEventBus`

### Этап 3. Формирование конверта и публикация
- [ ] **Реализовать** парсинг оригинального JSON для извлечения метаданных в конверт (минимальный, только нужные поля из `metadata` и `payload.subscription`)
- [ ] **Сохранить** полный оригинальный фрейм в поле `raw`
- [ ] **Добавить** `observedAtUtc = DateTimeOffset.UtcNow`, `sourceService = "MARS.TwitchCore"`, `schemaVersion = 1`
- [ ] **Прокинуть** `traceParent` из текущего `Activity.Current?.Id`
- [ ] **Опубликовать** через `IMarsEventBus` с ключом `twitch.eventsub.raw` в обмен `mars.events`
- [ ] **Реализовать** обработку переполнения очереди — дроп с инкрементом метрики

### Этап 4. Интеграция
- [ ] **Зарегистрировать** `EventSubRawForwarder` как hosted-сервис в `MARS.TwitchCore/Program.cs`
- [ ] **Добавить** конфигурацию (опционально): включение/выключение форварда, размер буфера (с разумными дефолтами)
- [ ] **Убедиться** что текущие хендлеры не затронуты
- [ ] **Проверить** что `EventSubWebsocketClientCustom` можно использовать или удалить (если не нужен)

### Этап 5. Тестирование и валидация
- [ ] **Локально проверить** подключение к dev-RabbitMQ, публикацию сообщений
- [ ] **Проверить** что все типы событий доходят (notification, session_*, revocation)
- [ ] **Убедиться** что текущие обработчики продолжают работать (`RewardRedemptionPublisher`, `TwitchStreamStartupNotifications`, `RxdcodxViewersService`)
- [ ] **Проверить** метрики при нормальной работе и при переполнении буфера

## Риски и митигации

| Риск | Вероятность | Влияние | Митигация |
|---|---|---|---|
| Reflection ломается при обновлении TwitchLib | Средняя | Среднее | Минимум рефлексии — только одно поле `_websocketClient` и подписка на событие. Покрыть комментарием с версией пакета. |
| Блокировка `OnDataReceived` в TwitchLib | Низкая | Высокое | Fire-and-forget, копировать байты, класть в bounded-канал, всю тяжёлую работу (десериализация конверта, публикация) выносить в фоновый воркер. |
| Переполнение очереди под нагрузкой | Средняя | Низкое | Bounded-канал с `DropOldest` или явный дроп + метрика `eventsub.raw.dropped`. Best-effort по требованию. |
| Неполный конверт из-за ошибок парсинга | Низкая | Низкое | Парсить метаданные в try/catch, при ошибке заполнять доступные поля, `raw` всегда сохранять целиком. |
| Конфликт с логированием TwitchLib | Низкая | Низкое | Не трогать логику парсинга TwitchLib, только подписаться параллельно на событие. |

## Критерии приёмки

- [ ] События Twitch EventSub публикуются в `mars.events` с routing key `twitch.eventsub.raw`
- [ ] Каждое сообщение содержит полный оригинальный JSON-фрейм в поле `raw`
- [ ] Конверт содержит минимум: `schemaVersion`, `messageId`, `messageType`, `subscriptionType`, `sessionId`, `eventName`, `observedAtUtc`, `sourceService`, `traceParent`, `raw`
- [ ] Все типы сообщений форвардятся: `notification`, `session_welcome`, `keepalive`, `reconnect`, `disconnect`, `revocation`
- [ ] Текущие обработчики продолжают работать без изменений (алерты по наградам не ломаются)
- [ ] Реализованы метрики `eventsub.raw.*`
- [ ] Best-effort семантика: нет блокировок, при переполнении дроп с метрикой
- [ ] Код соответствует стилю репозитория (CSharpier, `TreatWarningsAsErrors`)

## Файлы для изменения

| Файл | Изменение |
|---|---|
| `src/MARS.TwitchCore/Services/EventSub/EventSubRawForwarder.cs` (новый) | Основная логика reflection-тапа, bounded-очередь, воркер публикации |
| `src/MARS.TwitchCore/Services/EventSub/EventSubWebsocketClientCustom.cs` (новый/модификация) | При необходимости вынести туда логику тапа (опционально) |
| `src/MARS.TwitchCore/Program.cs` | Регистрация `EventSubRawForwarder` как hosted-сервиса |
| `src/MARS.Shared/Telemetry/MarsMetrics.cs` (при необходимости) | Добавить метрики для форварда |
| `appsettings.json`/конфиг (опционально) | Настройки размера буфера/включения форварда |

## Вопросы для уточнения

Нет блокирующих вопросов. Архитектура согласована:
1. Балансировщик может сам подключаться к Twitch EventSub
2. Балансировщик будет публиковать бизнес-события обратно при необходимости (но на текущем этапе не требуется)
3. Нужно шарить все события
4. Только форвард + оставить хендлеры