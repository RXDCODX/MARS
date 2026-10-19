---
slug: fix-loki-log-delivery
status: approved
intent: clear
review_required: false
pending-action: none
approach: Rewrite infrastructure/alloy/config.alloy to the documented README topology (discovery.docker -> discovery.relabel with drop rule -> loki.source.docker -> loki.write with a real endpoint block), add real Alloy 1.20.1 OTLP components (otelcol.receiver.otlp -> otelcol.processor.attributes with loki.resource.labels hint -> otelcol.exporter.loki) so Serilog OTLP logs land in Loki instead of Tempo, split Serilog:OtlpEndpoint from Otlp:Endpoint, add Serilog__OtlpEndpoint to x-service-env, add mars.logs=otlp label to app services + Alloy relabel drop rule to prevent duplication, add MARS_PORT_ALLOY_OTLP at index 33, remove the file-log path entirely (no file sink, no local-logs mounts, no file assertions), and resolve the merge conflict + verify no duplicate package versions.
---

# Draft: fix-loki-log-delivery

## Affected user and ideal state

The affected user is the **repository owner / operator debugging the MARS stand** (and any future agent asked "why are there no logs?"). Today they open Grafana -> Explore -> Loki and get an empty result, or they get logs only for containers that happen to be scraped, and "telemetry" (OTLP) logs never appear at all.

| Row | Statement | Reason |
| --- | --- | --- |
| IS-1 | Opening Grafana -> Explore -> Loki and querying `{container="twitch-core"}` returns the service's recent log lines, with no extra setup. | This is the single documented entry point for logs (README "Наблюдаемость", `infrastructure/grafana/datasources/loki.yml`). If it is empty, the whole observability stack looks broken and operators fall back to `docker logs`. |
| IS-2 | The `container` label equals the compose service name (`twitch-core`, `gateway`, `telegram`, ...), so a query by service is predictable. | README and `ObservabilityStackTests` define this contract; without it every query needs the `mars-<name>-1` container name. |
| IS-3 | OTLP log records emitted by Serilog reach Loki (via Alloy), not Tempo. | Tempo stores traces only; today `SerilogExtensions` points `Serilog.Sinks.OpenTelemetry` at `tempo:4317`, so those log records are dropped/error out. User decision: keep the OTLP path and terminate it in Alloy. |
| IS-4 | Exactly one delivery path exists per log line - no duplication between stdout scraping and OTLP. | User decision: avoid duplication; file-based logs are removed entirely. |
| IS-5 | `docker compose up` produces a clean Alloy: no config parse errors, all components running, `/loki/api/v1/labels` returns `container` and `project`. | A broken `config.alloy` is silent in Grafana (just empty); it must be checkable from the command line. |
| IS-6 | `dotnet build MARS.slnx -c Release` and the affected test projects are green - no merge-conflict markers, no duplicate `PackageVersion` entries. | The repo currently cannot be trusted as a baseline; a log fix on top of a red build cannot be verified. |
| GAP-1 | `infrastructure/alloy/config.alloy` is corrupted: 31 duplicated `// Открыть 30 доступных контейнеров` comment lines (10-40), `loki.source.docker` without `host`/`targets`/`labels` (41-43), `loki.write` containing 8 copies of `forward_to = [loki.write.mars.receiver]` (44-52) instead of an `endpoint {}` block, URL floating loose (57), stray `}` (59). | Alloy cannot load this config, so nothing reaches Loki at all - neither container logs nor OTLP. |
| GAP-2 | No `loki.source.otlp` receiver anywhere in `config.alloy`; Serilog OTLP endpoint is `http://tempo:4317` (`docker-compose.yml:37`, `SerilogExtensions.cs:32-43`), and Tempo does not store logs (`infrastructure/tempo/tempo.yaml`). | OTLP log records are lost by construction, regardless of container scraping. |
| GAP-3 | `SerilogExtensions.AddMarsLogging` has no file sink, yet `tests/MARS.Shared.Tests/Logging/SerilogFileSinkTests.cs` expects `local-logs/*.log`; `ObservabilityStackTests` expects `local.file_match`/`loki.source.file`/`__path__`/`\.log` in `config.alloy` and a `data/local-logs:/var/log/mars` mount in the dev override. | Half-implemented second path: tests encode an intention that no longer should exist (user removed file logs), and they are red. |
| GAP-4 | `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:94-100` and `:205-209` contain literal `<<<<<<< HEAD` / `=======` / `>>>>>>> 6604b013...` markers. | The test project cannot compile; the whole `MARS.Gateway.Tests` matrix entry is red. |
| GAP-5 | `Directory.Packages.props` has duplicate `PackageVersion` entries in one grep view (`Serilog.Sinks.OpenTelemetry` 4.1.0 and 10.0.0; `OpenTelemetry.Exporter.OpenTelemetryProtocol` twice at 10.0.0), while a clean read shows single 4.0.0/1.17.0 entries. | Either a real duplicate (NU1504-ish / restore ambiguity) or a stale editor buffer; must be confirmed and, if real, deduplicated before anything else is trusted. |
| GAP-6 | README (`Наблюдаемость` section) and `AGENTS.md` describe "Alloy reads stdout, no Loki sink in code, Seq removed" - both become partially stale once OTLP logs terminate in Alloy. | Documentation is the operator's only map; a stale map sends the next debugging session down the same dead end. |

## Components (topology ledger)

| id | outcome (one line) | status | evidence path |
| --- | --- | --- | --- |
| C1 | Alloy config parses, scrapes compose-project containers, labels `container`, writes to Loki | active | `infrastructure/alloy/config.alloy` |
| C2 | OTLP log receiver in Alloy terminates Serilog OTLP logs into the same `loki.write` | active | `infrastructure/alloy/config.alloy`, `src/MARS.Shared/Logging/SerilogExtensions.cs` |
| C3 | File-based log path removed (no sink, no mounts, no tests asserting it) | active | `src/MARS.Shared/Logging/SerilogExtensions.cs`, `docker-compose.dev.yml`, `tests/MARS.Shared.Tests/Logging/SerilogFileSinkTests.cs`, `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs` |
| C4 | Merge conflicts resolved and build/test baseline green | active | `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs`, `Directory.Packages.props` |
| C5 | Docs (README + AGENTS.md) match the delivered pipeline | active | `README.md`, `AGENTS.md` |
| C6 | Live verification on the dev stand (Loki labels + Grafana query) | active | `docker-compose.dev.yml`, `MARS_PORT_LOKI=6025` |

## Open assumptions (announced defaults)

| assumption | adopted default | rationale | reversible? |
| --- | --- | --- | --- |
| OTLP receiver port inside Alloy | `0.0.0.0:4317` gRPC, published only in the dev override as `MARS_PORT_ALLOY_OTLP` | Serilog's OTLP sink speaks gRPC by default; 4317 is the OTLP convention and matches what Tempo already exposes so no new mental model. | Yes - a port/env line |
| Where `Otlp:Endpoint` should point for logs | a **separate** `Serilog:OtlpEndpoint` (or `Otlp:LogsEndpoint`) pointing at `http://alloy:4317`; traces keep `Otlp:Endpoint` -> `tempo:4317` | One key for two signals with two different consumers was exactly the bug. Splitting keeps traces in Tempo and logs in Loki without overloading one value. | Yes - config key |
| Duplication prevention | when Serilog OTLP logging is enabled, stdout Console sink stays (it is what `docker logs` needs) but Alloy must **not** also scrape the same records via `loki.source.otlp` *and* docker for the same service... **no** - instead: OTLP logs are the single Loki path for services, docker scraping remains for anything not using Serilog OTLP (infra containers, Alloy itself). Labels must be identical (`container`, `project`) so queries do not fork. | User explicitly asked to avoid duplication; keeping both paths with identical labels would duplicate every application line. | Yes, but needs a decision if changed |
| File sink removal scope | delete `SerilogFileSinkTests` and the `local.file_match`/`loki.source.file` assertions + `LogsMount` from `ObservabilityStackTests`; do **not** add a file sink | User decision: all logs live in the infrastructure DB (Loki), no file copies. | Yes - revert commit |
| Merge-conflict resolution direction | take the `HEAD` side for formatting in `ObservabilityStackTests.cs` (single-line `File.ReadAllLines(...)` vs wrapped) and then rewrite the test bodies for the new no-file-sink reality | The conflict is formatting-only; the semantic change (file logs removed) is decided separately. | Yes |
| Package duplicates | treat the double-grep as suspect; first action is a clean read of `Directory.Packages.props`; only dedupe what is actually duplicated | A wrong "fix" to a non-existent duplicate would churn the file. | Yes |

## Findings (cited - path:lines)

- `infrastructure/alloy/config.alloy:10-40` - 31 identical `// Открыть 30 доступных контейнеров` lines (one typo `контейнов` at 18).
- `infrastructure/alloy/config.alloy:41-43` - `loki.source.docker "mars"` with only `forward_to`; missing `host`, `targets`, `labels`.
- `infrastructure/alloy/config.alloy:44-52` - `loki.write "mars"` opens, then eight `forward_to = [loki.write.mars.receiver]` lines (self-reference) instead of `endpoint { url = ... }`.
- `infrastructure/alloy/config.alloy:57` - bare `url = "http://loki:3100/loki/api/v1/push"` outside any `endpoint` block.
- `infrastructure/alloy/config.alloy:58-59` - two closing braces, one too many.
- `infrastructure/alloy/config.alloy:11-14,19-36` - correct `discovery.docker` + `discovery.relabel` blocks exist (filter `com_docker_compose_project=mars`, `container` label from `/mars-(.*)-\d+$`). These are salvageable and match the README contract.
- `src/MARS.Shared/Logging/SerilogExtensions.cs:28-30` - Console sink with template `[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}`.
- `src/MARS.Shared/Logging/SerilogExtensions.cs:32-43` - `WriteTo.OpenTelemetry` with `options.Endpoint = configuration["Otlp:Endpoint"]` (i.e. Tempo).
- `src/MARS.Shared/Telemetry/OpenTelemetryExtensions.cs:31-51` - `WithTracing` + `AddOtlpExporter` from `Otlp:Endpoint`; comment at 52-65 states metrics deliberately go through prometheus-net and OTel handles traces only.
- `docker-compose.yml:37` - `Otlp__Endpoint: http://tempo:4317` in `x-service-env` (applies to every service).
- `docker-compose.yml:38-41` - comment asserting services send no logs anywhere and `Loki__Url`/`Seq__Url` no longer exist.
- `docker-compose.yml:573-581` - `loki` service: image `grafana/loki:2.9.0`, config mounted to `/etc/loki/local-config.yaml`, volume `loki_data:/loki`, no `command` (relies on image default).
- `docker-compose.yml:590-606` - `alloy` service: image `grafana/alloy:v1.20.1`, `run --server.http.listen-addr=0.0.0.0:12345 --storage.path=/var/lib/alloy/data /etc/alloy/config.alloy`, volumes: config `:ro`, `/var/run/docker.sock:/var/run/docker.sock:ro`, `alloy_data:/var/lib/alloy/data`. **No OTLP port exposed.**
- `docker-compose.dev.yml:365-371` - dev override publishes `loki` on `${MARS_PORT_LOKI}:3100` and `alloy` on `${MARS_PORT_ALLOY}:12345` only; **no OTLP port, no volumes for alloy**.
- `infrastructure/grafana/datasources/loki.yml:8-14` - datasource `Loki`, uid `loki`, url `http://loki:3100`, `maxLines: 1000`.
- `infrastructure/grafana/datasources/tempo.yml:8-18` - comment documenting that trace->logs correlation is deliberately off because `service.name` (`MARS.TwitchCore`) != Loki `container` (`twitch-core`) and `{Properties}` is not in the console template.
- `infrastructure/loki/loki-config.yml:1-29` - `auth_enabled: false`, http port 3100, filesystem storage under `/loki`, schema v11 `boltdb-shipper`, `reject_old_samples_max_age: 168h`.
- `infrastructure/tempo/tempo.yaml:18-29` - OTLP gRPC `0.0.0.0:4317` and HTTP `0.0.0.0:4318`; `:31-40` trace-only local storage.
- `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:28-29` - `LogsMount = "data/local-logs:/var/log/mars"`.
- `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:94-100` and `:205-209` - unresolved conflict markers.
- `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:114-134` - `Alloy_Читает_Логи_Процессов_На_Хосте` asserts `local.file_match`, `loki.source.file`, `loki.write.mars.receiver`, `__path__`, `\.log` - all absent from the config.
- `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:140-154` - `Dev_Оверрайд_Отдаёт_Все_Каталоги` asserts `LogsMount` in the alloy block of `docker-compose.dev.yml` - absent.
- `tests/MARS.Shared.Tests/Logging/SerilogFileSinkTests.cs:41,60` - builds `local-logs` paths and asserts a log line is written to a file - no such sink exists in `SerilogExtensions`.
- `Directory.Packages.props:66-84` - clean read shows single entries: `Serilog` 4.4.0, `Serilog.AspNetCore` 10.0.0, `Serilog.Sinks.OpenTelemetry` 4.0.0, `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.17.0. The duplicated lines seen in one grep were a stale/incorrect view - **confirm in todo 1** before touching.
- `src/MARS.Shared/Extensions/WebApplicationBuilderExtensions.cs:48` - `builder.Host.UseSerilog((ctx, lc) => lc.AddMarsLogging(ctx.Configuration, serviceName));`
- `src/MARS.Shared/Extensions/WebApplicationBuilderExtensions.cs:51` - `builder.Services.AddMarsTelemetry(builder.Configuration, serviceName);`

## Decisions (with rationale)

1. **`config.alloy` is rewritten, not patched.** The corruption is structural (self-referencing `loki.write`, misplaced `url`, unbalanced braces); a minimal edit would leave the 31 comment lines and the wrong shape. Only `discovery.docker` and `discovery.relabel` are kept verbatim.
2. **OTLP logs terminate in Alloy (`loki.source.otlp`), not Tempo.** Tempo is trace-only by config (`tempo.yaml:31-40`); user chose to keep the OTLP path rather than delete the sink.
3. **Two distinct config keys for two signals.** `Otlp:Endpoint` stays `http://tempo:4317` for traces; a new `Serilog:OtlpEndpoint` (default `http://alloy:4317`) is what `AddMarsLogging` reads. Without the split, one of the two signals must lose its correct destination.
4. **File-based logs are removed entirely.** No `WriteTo.File`, no `data/local-logs` mount, no `local.file_match`/`loki.source.file`, and the two tests that assert them are deleted/rewritten. This matches the user's "all logs in the infrastructure DB" decision and removes the half-built second path.
5. **Duplication is prevented by path ownership, not by dedup filters.** Application services send logs via OTLP only; Alloy's docker source remains for containers that do not speak OTLP (infra, Alloy itself, third-party images). Both paths write identical `container`/`project` labels so a single query still works.
   - *Risk noted:* if a MARS service logs via OTLP **and** Alloy scrapes its stdout, the line appears twice. Mitigation in todo 5: Alloy's docker relabel drops MARS application containers when OTLP is enabled, keeping only infra + alloy; this is an explicit, testable relabel rule.
   - *Alternative if the user prefers stdout-only:* delete the `WriteTo.OpenTelemetry` sink instead - but the user explicitly asked to keep `loki.source.otlp`, so this is recorded as the rejected option.
6. **Merge conflicts resolved before any Alloy work.** A red test project makes every later "green" claim meaningless.
7. **Docs updated in the same change.** README "Наблюдаемость" table and AGENTS.md Loki/Alloy paragraphs must describe the new two-source Alloy (docker + OTLP) and the removed file path.

## Scope IN

- Rewrite `infrastructure/alloy/config.alloy` (docker source + OTLP source + single `loki.write` with real `endpoint`).
- Add OTLP gRPC receiver to Alloy in `docker-compose.yml` (and publish in `docker-compose.dev.yml` under a new `MARS_PORT_ALLOY_OTLP`).
- Split the Serilog OTLP endpoint from the trace OTLP endpoint; wire `http://alloy:4317` for logs.
- Remove file-log path: no `WriteTo.File`, delete `SerilogFileSinkTests`, rewrite `ObservabilityStackTests` file-related cases, remove `LogsMount` and any `local-logs` references, keep/repair `/data/local-logs/` gitignore entry only if still referenced (else drop).
- Resolve merge conflicts in `ObservabilityStackTests.cs`.
- Verify `Directory.Packages.props` has no duplicate `PackageVersion` entries; dedupe if real.
- Update `README.md` observability section and `AGENTS.md` Loki/Alloy text.
- Update `.env.example`, `.env.development.example`, `.env.production.example` with `MARS_PORT_ALLOY_OTLP` (per the cross-cutting edit checklist in AGENTS.md).
- Update `scripts/local-launch.conf` if the Alloy OTLP endpoint is needed for host runs (record only if actually needed; otherwise scope out explicitly).
- Update `PortAllocationTests` with the new port entry.

## Scope OUT (Must NOT have)

- No file sink, no `data/local-logs` directory, no `loki.source.file` / `local.file_match`.
- No changes to Tempo config or trace export path (`Otlp:Endpoint` stays Tempo).
- No changes to Prometheus / metrics pipeline (`OpenTelemetryPrometheusBridge`, `MarsMetrics`).
- No new microservice, no new NuGet package beyond what is already referenced (`Serilog.Sinks.OpenTelemetry` is already there).
- No Grafana dashboard changes (dashboards are metrics-only).
- No trace->logs correlation enablement (documented as deliberately off; separate decision).
- No `loki.source.docker` for non-MARS containers (project filter stays `mars`).
- No changes to how services log to console (`docker logs` must keep working).

## Open questions

None - the three owner decisions were answered: OTLP -> Alloy, no file logs, README schema.

## Approval gate

status: approved
User replied "окей" plus answers to all three forks on 2026-10-18. Approval authorizes writing this plan only; execution starts in a separate worker session (`$ulw-execute`).
