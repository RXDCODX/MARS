---
name: rabbitmq-pubsub-events
description: Add, publish or consume typed pub/sub events over RabbitMQ with Wolverine in MARS: message contracts in MARS.Shared.Contracts, IMessageBus.PublishAsync, MarsEventListener<T>, exchange/queue naming, DLQ and migration from the legacy RabbitMqConfig bus.
---

# RabbitMQ pub/sub events (Wolverine)

The typed event bus is built in `src/MARS.Platform.Messaging.RabbitMq.Infrastructure`
(`WolverineMessagingExtensions.AddMarsWolverine`). Every event is a concrete class that
implements `IMarsEventMessage`; Wolverine derives the exchange from the type name and
creates one queue per listener. Do not add strings to a central event catalog and do not
publish raw JSON.

## 1. Register the bus in `Program.cs`

```csharp
builder.AddMarsWolverine("MARS.Alerts", typeof(Program).Assembly);
```

- The service name must match the `AddMarsDefaults(...)` name. It is part of every queue
  and of the service DLQ.
- The second argument is the assembly scanned for `MarsEventListener<T>` handlers.
- If `RabbitMq:Host` is empty, Wolverine is not registered at all. The service still
  starts; publishing and consuming are simply unavailable.
- Never register listeners or consumers with `AddHostedService`; handler discovery does it.

## 2. Define a contract

Put the class in `src/MARS.Shared.Contracts/Messaging` and implement
`IMarsEventMessage`.

```csharp
public sealed class RewardRedeemedEvent : IMarsEventMessage
{
    public string RewardTitle { get; set; } = string.Empty;
    // ...
}
```

Rules:

- Only concrete, closed classes. Abstract types and open generics are rejected.
- The contract must live in the `MARS.Shared.Contracts` assembly. Publishing discovery
  scans `typeof(IMarsEventMessage).Assembly`, so a contract in a service assembly would
  have no outgoing route.
- Name it `XxxEvent`. `MarsMessageNaming.ExchangeNameFor` strips the `Event` suffix and
  converts PascalCase to kebab-case: `RewardRedeemedEvent` → exchange
  `mars.reward-redeemed`.
- Exchange names must be unique across the assembly. `DiscoverMessageContracts` throws on
  duplicates at startup.
- The CLR type name is the integration contract. Renaming the class renames the exchange
  and breaks compatibility with already deployed publishers/consumers.
- Use one event type per meaning. Do not multiplex unrelated payloads with a "kind" field
  or one event per reward title.

## 3. Publish

Inject `IMessageBus` and publish the typed message:

```csharp
await bus.PublishAsync(new ChatSendEvent { /* ... */ });
```

- `PublishAsync<T>(T message, DeliveryOptions? options = null)` is the only supported path.
- Do not compute routing keys and do not call the legacy `IMarsEventBus`.
- Serialization is handled by Wolverine; pass a typed object, not a `JsonDocument`,
  `JsonElement` or pre-serialized string.
- Outgoing routes are registered for every discovered contract, so a new message type
  needs no `Program.cs` change.

## 4. Subscribe

Create a class that derives from `MarsEventListener<TMessage>`:

```csharp
public sealed class RewardAlertListener(IEnumerable<IRewardAlertHandler> handlers)
    : MarsEventListener<RewardRedeemedEvent>
{
    public override Task Handle(RewardRedeemedEvent message, CancellationToken ct)
        => /* ... */;
}
```

Rules:

- Exactly one `IMarsMessageListener<TMessage>` per listener class.
  `MarsQueues.QueueNameForListener` rejects 0 or 2+ implementations. To react to two
  message types, create two thin listener classes over a shared service.
- Register dependencies in DI as usual; the listener itself is discovered by Wolverine.
- Exceptions propagate to Wolverine and drive retry/DLQ. Do not swallow them.
- Only `MARS.TwitchCore` owns the Twitch connection. Other services react to events
  instead of opening their own IRC/EventSub connection.

## 5. Topology and naming

- Exchange per message type: `mars.{kebab-message-name}`, e.g. `mars.reward-redeemed`.
- Queue per listener:
  `mars.{service}.{message}.{listener}` without the `Listener` suffix, e.g.
  `RewardAlertListener` for `RewardRedeemedEvent` in `MARS.Alerts` →
  `mars.alerts.reward-redeemed.reward-alert`.
- Service DLQ: `mars.{service}.dlq` (name lowercased, prefix `MARS.` stripped).
- `UseConventionalRouting` is configured with `ExchangeNameForSending` so listener bindings
  use the same `mars.*` exchange as publishers. Do not remove that call, or listeners stop
  receiving messages.
- Adding a listener creates its queue on startup via AutoProvision. Removing a listener
  does not delete the old queue or its bindings; clean those up manually if needed.
- RabbitMQ exchanges are fanout by default, so the routing key is not used for dispatch.
  Each listener's own queue is what isolates consumers.

## 6. Configuration and runtime behavior

- `RabbitMq:Host`, `Port`, `UserName`, `Password`/`PasswordFile`, `VirtualHost` come from
  `RabbitMqOptions`; connection creation is centralized in `RabbitMqConnectionFactory`.
- AutoProvision declares exchanges, queues and bindings on startup.
- Publisher confirmations are enabled; publish failures surface instead of being lost.
- Enhanced dead-lettering sends exhausted messages to the service DLQ.
- A missing broker prevents bus startup but does not require code changes to keep the rest
  of the service working.

## 7. Tests

- Naming, discovery, exchange/queue rules and the publisher/listener exchange match live in
  `tests/MARS.Shared.Tests/Messaging/MarsMessagingInfrastructureTests.cs`.
- Add contract/discovery assertions when introducing a new event or listener.
- Test listener logic by constructing it directly with stubbed dependencies; do not start
  RabbitMQ in unit tests.
- Run:

```powershell
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-class "*MarsMessagingInfrastructureTests*"
dotnet csharpier format . && dotnet csharpier check .
```

## 8. Migrating from the legacy bus

The legacy path (`RabbitMqConfig`, `IMarsEventBus`, `RabbitMqConsumerBase`, the
`mars.events` exchange with routing keys such as `twitch.reward.*`) is removed. When
converting code:

1. Replace each routing key with one typed contract. Reward-specific keys collapse into
   `RewardRedeemedEvent`; handlers filter on the payload (`RewardTitle`).
2. Replace `IMarsEventBus.PublishAsync(routingKey, payload)` with
   `IMessageBus.PublishAsync(payload)`.
3. Replace `RabbitMqConsumerBase` subclasses with `MarsEventListener<T>` and delete the
   manual `AddHostedService` registration.
4. Delete the old key from any config/constants helper instead of keeping both paths.
5. A rolling deploy needs dual-publish for one release or a coordinated deploy of
   publishers and consumers: old queues stop receiving traffic once publishers move.
6. Rollback is a revert; exchanges and queues are not deleted automatically.

## 9. Common mistakes

- Contract in a service assembly → no outgoing route; publish silently has no destination.
- Two message types in one listener → `MarsQueues` throws at startup.
- Wrong service name in `AddMarsWolverine` → queues and DLQ under the wrong prefix.
- Manual `AddHostedService` for a consumer → double consumption or duplicate connections.
- Publishing raw JSON/`JsonElement` → no route and no compile-time contract.
- Keeping reward routing keys as strings → reintroduces the catalog the migration removed.
- Assuming the routing key routes the message → binding is by exchange name; keep the
  `ExchangeNameForSending` convention.
