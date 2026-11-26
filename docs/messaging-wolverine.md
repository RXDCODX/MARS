# Межсервисные события: Wolverine + RabbitMQ

Это руководство описывает действующую реализацию MARS. Источник истины для поведения — код `MARS.Platform.Infrastructure.RabbitMq` и тесты `MARS.Shared.Tests`; здесь зафиксированы правила, которые должны соблюдаться при добавлении новых сообщений и обработчиков.

## 0. Граница с бизнес-логами

Бизнес-активности не публикуются как универсальный RabbitMQ-event с полем `kind`.
Они записываются через `IBusinessActivityLogger` в общий структурированный журнал и
читаются MARS Dashboard из Loki. RabbitMQ остаётся механизмом типизированных
событий по смыслу, а не каналом для всех логов.

## 1. Где находится реализация

| Зона ответственности | Проект и файлы |
|---|---|
| Подключение Wolverine и discovery обработчиков | `src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/WolverineMessagingExtensions.cs` |
| Базовый тип обработчика и marker-интерфейс | `src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/IMarsEventListener.cs` |
| Маркер сообщения | `src/MARS.Shared/MARS.Shared.Contracts/Messaging/IMarsEventMessage.cs` |
| Сопоставление имени сообщения и exchange | `src/MARS.Shared/MARS.Shared.Contracts/Messaging/MarsMessageNaming.cs` |
| Правила именования очередей и DLQ | `src/MARS.Shared/MARS.Shared.Contracts/Messaging/MarsQueues.cs` |
| RabbitMQ connection factory | `src/MARS.Platform/MARS.Platform.Infrastructure.RabbitMq/RabbitMqConnectionFactory.cs` |
| Конфигурация брокера | `src/MARS.Platform/MARS.Platform.Infrastructure.Web.AspNetCore/Configuration/RabbitMqOptions.cs` |

Пакеты подключены в `MARS.Platform.Infrastructure.RabbitMq.csproj` и централизованно версионируются в `Directory.Packages.props`: `WolverineFx.RabbitMQ` и `WolverineFx.RuntimeCompilation` — `6.61.0`, `RabbitMQ.Client` — `7.2.2`.

## 2. Подключение шины в сервисе

В `Program.cs` подключите namespace и вызовите `AddMarsWolverine` рядом с общими настройками сервиса:

```csharp
using MARS.Platform.Infrastructure.RabbitMq;

builder.AddMarsDefaults("MARS.Alerts", "AlertsDb");
builder.AddMarsWolverine(
    "MARS.Alerts",
    typeof(Program).Assembly,
    typeof(TwitchEventSubListener).Assembly,
    typeof(RewardAlertListener).Assembly,
    typeof(RewardInputMessageListener).Assembly
);
```

Это соответствует текущей регистрации `MARS.Alerts`. Для сервисов, у которых все обработчики находятся в сборке host-проекта, достаточно:

```csharp
builder.AddMarsWolverine("MARS.TwitchCore", typeof(Program).Assembly);
```

Правила регистрации:

- `serviceName` должен совпадать с именем, переданным в `AddMarsDefaults`. Он участвует в именах очередей и DLQ.
- Первая сборка — основная сборка приложения. Дополнительные сборки передаются параметрами `params Assembly[]`.
- Wolverine отключает conventional discovery и ищет только типы, реализующие `IMarsEventListener<T>`, в явно переданных сборках.
- Если listener находится в отдельном Application/Infrastructure-проекте, обязательно передайте его сборку. Отсутствующая сборка означает, что обработчик не будет обнаружен.
- Listener не нужно регистрировать через `AddHostedService`: его обнаруживает Wolverine.

`AddMarsWolverine` проверяет `RabbitMq:Host`. При пустом или отсутствующем значении регистрация Wolverine пропускается. Это означает, что сервисы и компоненты, которым требуется `IMessageBus`, не должны запускаться в режиме без брокера без отдельного fallback.

## 3. Контракты сообщений

Контракт — конкретный класс в сборке `MARS.Shared.Contracts`, реализующий `IMarsEventMessage`:

```csharp
namespace MARS.Shared.Contracts.Messaging;

public sealed class OrderPlacedEvent : IMarsEventMessage
{
    public Guid OrderId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
```

Инфраструктура сканирует именно сборку `typeof(IMarsEventMessage).Assembly`, а не все сборки приложения. Поэтому недостаточно поместить класс в namespace `MARS.Shared.Contracts.Messaging`: он должен физически находиться в проекте `MARS.Shared.Contracts`. Иначе для него не будет автоматически зарегистрирован исходящий маршрут.

К контрактам применяются следующие ограничения:

- Это должен быть класс, не abstract и не open generic.
- Для каждого контракта вычисляется exchange по CLR-имени типа. Финальный суффикс `Event` удаляется, оставшаяся часть преобразуется в kebab-case.
- Имена exchange должны быть уникальны во всей сборке контрактов. При коллизии discovery выбрасывает `InvalidOperationException`.
- Имя CLR-типа — часть интеграционного контракта: переименование класса меняет exchange. Переименование требует согласованного обновления издателей и подписчиков.

Примеры из тестов:

| CLR-тип | Exchange |
|---|---|
| `ChatEventMessageEvent` | `mars.chat-event-message` |
| `ChatMessageDeletedEvent` | `mars.chat-message-deleted` |
| `ChatSendEvent` | `mars.chat-send` |
| `EventSubRawEvent` | `mars.event-sub-raw` |
| `RewardInputChatMessageEvent` | `mars.reward-input-chat-message` |
| `RewardRedeemedEvent` | `mars.reward-redeemed` |

Список контрактов проверяется в `MarsMessagingInfrastructureTests.SharedAssemblyExposesUniqueMarkedMessageContracts`.

## 4. Публикация событий

Публикация выполняется через Wolverine `IMessageBus`. Класс издателя не является специальным типом и не участвует в discovery: это обычный сервис приложения, который получает `IMessageBus` через DI.

```csharp
using Wolverine;

public sealed class OrderPlacedPublisher(IMessageBus bus)
{
    public async Task PublishAsync(OrderPlacedEvent message)
    {
        await bus.PublishAsync(message);
    }
}
```

При старте `WolverineMessagingExtensions` рефлексивно находит все конкретные классы `IMarsEventMessage` в сборке контрактов и для каждого вызывает `PublishMessage(type).ToRabbitExchange(exchangeName)`. Поэтому добавление нового контракта не требует ручного добавления маршрута в `Program.cs` или центральный каталог событий.

Важные ограничения:

- Публикуйте типизированный объект контракта, а не заранее сериализованную строку или произвольный JSON.
- Не задавайте routing key вручную и не возвращайте старую схему строковых routing keys.
- Сохраняйте совместимость payload между версиями сервисов, которые могут быть развёрнуты не одновременно.
- `IMessageBus` в текущей конфигурации Wolverine зарегистрирован как scoped. Не удерживайте его в singleton/background service. Для фоновой работы создавайте scope через `IServiceScopeFactory` и разрешайте издатель из этого scope.

## 5. Listener: автоматическое обнаружение обработчиков

Создайте отдельный класс на каждый тип сообщения и наследуйте его от `MarsEventListener<TMessage>`:

```csharp
using MARS.Platform.Infrastructure.RabbitMq;
using Microsoft.Extensions.Logging;

public sealed class OrderPlacedListener(
    ILogger<OrderPlacedListener> logger)
    : MarsEventListener<OrderPlacedEvent>
{
    public override Task Handle(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was placed", message.OrderId);
        return Task.CompletedTask;
    }
}
```

`MarsEventListener<TMessage>` уже реализует оба интерфейса:

- `IMarsEventListener<TMessage>` — marker для discovery Wolverine и контракт метода `Handle`.
- `IMarsMessageListener<TMessage>` — marker в `MARS.Shared.Contracts`, по которому инфраструктура определяет тип сообщения при построении имени очереди.

У одного класса listener должен быть ровно один `IMarsMessageListener<TMessage>`. Это проверяется при вычислении имени очереди. Если нужно реагировать на несколько типов событий, создайте несколько тонких listener-классов и вынесите общую бизнес-логику в обычный DI-сервис. Наследование от `MarsEventListener<TMessage>` предпочтительнее ручной реализации `IMarsEventListener<TMessage>`: одна только реализация `IMarsEventListener<T>` не создаёт маркер `IMarsMessageListener<T>`, необходимый `MarsQueues`.

Зависимости listener передаются через конструктор и разрешаются DI. Метод `Handle` должен принимать `CancellationToken` и возвращать `Task`. Не проглатывайте исключения без явной бизнес-причины: они нужны транспортному конвейеру для обработки неуспешной доставки.

## 6. Exchange, queue и DLQ

### Exchange

Exchange формируется методом `MarsMessageNaming.ExchangeNameFor(Type)`: `mars.{kebab-case имени контракта без финального суффикса Event}`.

### Queue listener

Для каждого listener создаётся своя очередь:

```text
mars.{service-kebab}.{message-name}.{listener-kebab}
```

При расчёте имени:

- из имени сервиса удаляется префикс `MARS.`, затем имя приводится к kebab-case;
- из имени сообщения берётся часть после `mars.` у exchange;
- финальный суффикс `Listener` удаляется из имени класса listener, затем имя приводится к kebab-case.

Проверенные примеры:

| Сервис | Контракт | Listener | Queue |
|---|---|---|---|
| `MARS.Alerts` | `RewardRedeemedEvent` | `RewardAlertListener` | `mars.alerts.reward-redeemed.reward-alert` |
| `MARS.Alerts` | `ChatEventMessageEvent` | `TwitchMessagesListener` | `mars.alerts.chat-event-message.twitch-messages` |
| `MARS.TwitchCore` | `ChatEventMessageEvent` | `TwitchMediaListener` | `mars.twitch-core.chat-event-message.twitch-media` |

Таким образом, два listener одного контракта получают независимые очереди и оба могут обрабатывать опубликованное событие. Подписка определяется очередью и binding, а не собственным routing key обработчика.

### Dead-letter queue

Для сервиса задаётся одна DLQ через `MarsQueues.DeadLetterQueueForService(serviceName)`. В отличие от очередей listener, имя DLQ не переводит имя сервиса в kebab-case: удаляется `MARS.`, а остаток переводится в нижний регистр.

| Имя сервиса | DLQ |
|---|---|
| `MARS.Alerts` | `mars.alerts.dlq` |
| `MARS.TwitchCore` | `mars.twitchcore.dlq` |

Wolverine подключён с `EnableEnhancedDeadLettering()` и этой DLQ сервиса. `AutoProvision()` автоматически объявляет требуемые exchanges, queues и bindings. Добавление listener создаёт новую очередь; удаление listener не удаляет старую очередь и её binding автоматически — при очистке topology удаляйте их отдельно.

## 7. Подключение RabbitMQ и поведение соединения

Секция конфигурации называется `RabbitMq`:

| Ключ | Назначение |
|---|---|
| `RabbitMq:Host` | Адрес брокера; пустое значение отключает регистрацию Wolverine |
| `RabbitMq:Port` | AMQP-порт, по умолчанию `5672` |
| `RabbitMq:UserName` / `RabbitMq:Password` | Учётные данные |
| `RabbitMq:Password_FILE` / `RabbitMq:PasswordFile` | Путь к файлу секрета вместо пароля в конфигурации |
| `RabbitMq:VirtualHost` | Virtual host, по умолчанию `/` |
| `RabbitMq:ReconnectDelayMilliseconds` | Интервал восстановления соединения, по умолчанию `5000` мс |

`RabbitMqConnectionFactory` единообразно задаёт имя клиента равным имени сервиса, включает automatic recovery и topology recovery, heartbeat `30` секунд и `ConsumerDispatchConcurrency = 1`. Network recovery interval равен `Math.Max(500, ReconnectDelayMilliseconds)`. При использовании `Password_FILE` содержимое файла читается при создании настроек подключения.

Для канала Wolverine включены publisher confirmations и tracking. Также включены `AutoProvision` и enhanced dead-lettering.

### Важное замечание о retry

`RabbitMqOptions.MaxDeliveryAttempts` пока остаётся в общей модели конфигурации и копируется в `IOptions`, но текущий `AddMarsWolverine` не использует это свойство для настройки retry policy Wolverine. В `WolverineMessagingExtensions` также нет явной настройки количества попыток или расписания повторов. Поэтому не считайте `MaxDeliveryAttempts` действующим лимитом доставок Wolverine и не указывайте конкретное число попыток в документации или эксплуатационных инструкциях без отдельной проверки. Если лимит понадобится, его следует явно настроить в `WolverineMessagingExtensions` и покрыть тестом.

## 8. Добавление нового события: чеклист

1. Создайте конкретный класс `XxxEvent : IMarsEventMessage` в `MARS.Shared.Contracts/Messaging`.
2. Проверьте, что имя CLR-типа даёт уникальный exchange. Добавьте контракт в тесты discovery, если нужно закрепить новое правило.
3. В сервисе-источнике внедрите `IMessageBus` в обычный scoped-сервис и вызывайте `PublishAsync(message)`.
4. В сервисе-получателе создайте `MarsEventListener<XxxEvent>` с одним типом сообщения.
5. Убедитесь, что сборка listener передана в `AddMarsWolverine` (основная или дополнительная).
6. Проверьте ожидаемые exchange и queue names, а также DI-зависимости listener.
7. Добавьте unit-тест бизнес-логики и инфраструктурный тест discovery/naming при необходимости.
8. При включённом `RabbitMq:Host` проверьте автосоздание topology в RabbitMQ.

## 9. Тесты

Основные тесты инфраструктуры находятся в `tests/MARS.Shared.Tests/Messaging/MarsMessagingInfrastructureTests.cs`. Они проверяют пропуск регистрации без `RabbitMq:Host`, marker-based discovery, соответствие имени exchange при публикации и подписке, правила queue/DLQ naming, список контрактов сборки и обнаружение коллизий exchange.

Проверка тестов:

```powershell
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-class "*MarsMessagingInfrastructureTests*"
dotnet test tests/MARS.Alerts.Tests/MARS.Alerts.Tests.csproj -c Release -- --filter-class "*RewardHandlerRegistrationTests*"
```

Дополнительный тест обнаружения обработчиков в дополнительных сборках находится в `tests/MARS.Alerts.Tests/RewardHandlerRegistrationTests.cs`.

## 10. Что не использовать для новых событий

Старый runtime-путь `RabbitMqConfig` / `IMarsEventBus` / `RabbitMqConsumerBase` с общим exchange `mars.events` и строковыми routing keys не является текущей реализацией. Не добавляйте туда новые контракты и не возвращайте ручные consumer registrations. Для новых интеграционных событий используйте только типизированные контракты `IMarsEventMessage`, публикацию Wolverine и `MarsEventListener<TMessage>`.