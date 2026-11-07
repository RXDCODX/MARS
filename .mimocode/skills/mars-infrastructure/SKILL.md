---
name: mars-infrastructure
description: Work safely with MARS service infrastructure: PostgreSQL migrations, RabbitMQ, gRPC, YARP, Docker Compose, ports, environment files, and local host debugging.
---

# MARS infrastructure

Treat live configuration and code as authoritative. This guide records invariants and failure modes; verify current service names, port assignments, and endpoints in compose/config before editing.

## PostgreSQL and migrations

- Each service owns its database, role and connection-string key. Database creation in `infrastructure/db-init/01-databases.sh` runs only when the PostgreSQL data volume is initialized for the first time.
- EF migrations are applied synchronously during service startup, before `app.Run()`. Keep design-time factories under the relevant `Data/DesignTime` directory.
- Test database access uses PostgreSQL Testcontainers and real migrations; see `backend-test-helpers`.
- UTC conversion is centralized in `MarsUtcDates` and verified by `UtcDatesConventionVerifier`. PostgreSQL reads `timestamptz` as UTC; presentation code converts to local time explicitly.

## RabbitMQ

- Typed pub/sub goes through Wolverine. Use skill `rabbitmq-pubsub-events` for contracts, publishing, listeners, exchange/queue naming and DLQ behavior.
- Register the bus with `builder.AddMarsWolverine("MARS.<Service>", typeof(Program).Assembly)`; the service name defines queue prefixes and the service DLQ.
- Do not add events to a central catalog and do not publish by routing key. The exchange is derived from the contract type (`mars.{kebab-name}`); each `MarsEventListener<T>` gets its own queue and the service has `mars.{service}.dlq`.
- Only `MARS.TwitchCore` owns the Twitch IRC/EventSub connection. Other services communicate through RabbitMQ events.
- Poison messages must have bounded retries and go to the DLQ; do not replace this with unbounded requeue.
- Check existing listeners before adding a subscription: each listener adds a queue, and removing a listener leaves its old queue and bindings behind.
- The legacy `RabbitMqConfig`/`IMarsEventBus`/`RabbitMqConsumerBase` path was removed during the migration; do not reintroduce it for new events.

## gRPC and contracts

- Protobuf files live in `src/MARS.Shared.Contracts/Protos`, each with its own `csharp_namespace`. Generated contracts stay in that shared contracts assembly.
- Streaming subscriptions use server-streaming `Subscribe`; ordinary client-to-server calls are unary unless the contract explicitly requires otherwise.
- Use standard gRPC status codes via `RpcException(Status)`; do not invent a parallel error field in each proto.
- Host setup uses `builder.AddMarsGrpcHosting()`. Register service implementations through the existing shared hosting abstractions.
- gRPC listens on the service's internal endpoint (currently `:8081`); verify `Grpc:Endpoints` and compose before assuming a host port is REST port + 1.
- The Swagger aggregator reflects over string properties of `ServiceEndpoints`; new public HTTP endpoints need the matching endpoint property as well as YARP configuration.

## YARP Gateway

- Routes and clusters are configured in `src/MARS.Gateway/appsettings.json`. Cluster addresses use Docker service DNS names and the internal service port.
- A new externally reachable endpoint needs a YARP route, cluster and `ServiceEndpoints` entry. Use skill `gateway-route-add` for the exact procedure.
- Keep the SPA catch-all route last; otherwise it can shadow API, hub, storage and media routes. Preserve existing route ordering and tests.
- The client UI is served by the dedicated `client-ui` container. It is not a service with a separately published public port.

## Docker Compose and readiness

- `docker-compose.yml` is the production stack. Development overrides are explicit: `docker compose -f docker-compose.yml -f docker-compose.dev.yml ...`. Do not rely on implicit override discovery.
- Compose commands must pass the selected `--env-file`; there is no committed default `.env` file. Use the repository stack scripts where possible.
- Never print, commit, or copy real environment files. Templates (`.env*.example`) may contain placeholders only.
- Health checks must verify actual service readiness. PostgreSQL health checks must probe TCP, not only its temporary initialization socket. RabbitMQ checks must verify the listener, not only node process liveness.
- Keep shared `mars-wwwroot` storage mounted for the services that exchange media/alert files. Preserve the separate Git metadata volume for MediaStorage.
- Dockerfiles for layered services must copy every referenced project file before `dotnet restore`; keep compose Dockerfile paths, watch targets and solution references aligned.
- Linux CI runs tests too. Avoid Windows-only path assumptions; build test paths with `Path.Combine` and test cross-platform behavior.

## Host ports and local debugging

- Host ports are allocated by `scripts/windows/ports.ps1` and `scripts/unix/ports.sh` from `MARS_PORT_BASE` plus the ordered keys in the environment template. Do not manually renumber a port without updating templates, compose, the allocation test and local launch settings.
- Production exposes only the intended public endpoints; dev compose publishes additional service/infrastructure and gRPC ports. Verify the actual files rather than relying on remembered port numbers.
- `scripts/local-launch.conf` is the source for generated `MARS.Local` launch profiles. Edit the data file, regenerate/check with the local script, and do not hand-edit generated `launchSettings.json`.
- Values in local launch config may resolve only from committed example env files. Secrets belong in the ignored environment file, never in the launch profile.
- In Development, `MarsEnvironmentFile` loads missing configuration values for host-run services. Environment variable names are mapped to configuration keys using compose's declared mapping; preserve that single source of truth.
- For services serving static files locally, preserve the configured `ASPNETCORE_WEBROOT` root and the matching ignored local directory.

## Telegram integration notes

- The owner bot in `MARS.Telegram` uses MTProto through the local `tg-ws-proxy`; do not assume it uses Telegram Bot API. Other services may still use Telegram.Bot for their own limited messaging.
- WTelegram configuration key names and API credentials must match the library's expected names exactly. Keep the client behind the existing `I*Client` abstraction: its concrete methods are not a safe unit-test seam.
- The log sink must not send logs back to its owning Telegram service. Rate limiting belongs on the receiving side, and a permanent `FAILED_PRECONDITION` should disable that sink rather than retry forever.
- Do not duplicate Telegram transport, authentication, or logging implementation notes in general agent instructions; inspect the owning service and tests for current behavior.
