# fix-loki-log-delivery - Work Plan

## TL;DR (For humans)

**Who this is for and what changes for them:** The stand operator (and any agent debugging it) currently opens Grafana → Loki and gets nothing, because Alloy's config is corrupt and the OTLP log path points at Tempo, which cannot store logs. After this work, every service's log lines appear in Grafana under a predictable service-name label, whether the service runs in a container or on the host.

**What you'll get:** A working log pipeline: Alloy reads container stdout and also accepts OTLP log records from Serilog, both written to the same Loki `loki.write`; the corrupted Alloy config is rebuilt; the half-finished file-log experiment is removed so nothing is duplicated; and the repo builds green again (merge conflicts and stale test assumptions fixed).

**Why this approach:** Two independent bugs kept Loki empty — a structurally broken Alloy config and a log exporter aimed at a trace-only store. Both are fixed at the source, with one delivery path per log line and one shared label contract so queries never fork.

**What it will NOT do:** Will not add file-based log copies, will not touch Tempo traces, Prometheus metrics, Grafana dashboards, or enable trace→logs correlation, and will not scrape non-MARS containers.

**Effort:** Medium
**Risk:** Medium - Alloy OTLP receiver + a new config key change runtime wiring; a mislabelled source would duplicate or drop lines.
**Decisions to sanity-check:** OTLP logs terminate in Alloy (not Tempo); file logs removed entirely; a separate `Serilog:OtlpEndpoint` so traces stay on Tempo.

Your next move: run `$ulw-execute fix-loki-log-delivery` in a worker session. Full execution detail follows below.

---

> TL;DR (machine): Medium/Medium - rebuild `infrastructure/alloy/config.alloy` (docker + OTLP sources), add Alloy OTLP receiver + `MARS_PORT_ALLOY_OTLP` (index 33), split Serilog log endpoint from trace endpoint, add `Serilog__OtlpEndpoint` to `x-service-env`, drop MARS app containers from the docker source via a `mars.logs=otlp` label, remove file-log path and its tests, resolve merge conflicts, update docs/env/ports, verify live Loki labels.

## Scope
### Affected user and ideal state

The affected user is the repository owner/operator debugging the MARS stand, and any future agent asked "why are there no logs?". Today Grafana → Explore → Loki is empty and telemetry/OTLP logs never appear; after this work each service's logs are queryable by service name with no extra setup, and the build/test baseline is trustworthy again.

| Row | Statement | Reason |
| --- | --- | --- |
| IS-1 | Grafana → Explore → Loki, query `{container="twitch-core"}` returns recent log lines with no extra setup. | This is the documented entry point for logs; empty results make the whole stack look broken. |
| IS-2 | The `container` label equals the compose service name (`twitch-core`, `gateway`, ...), for both docker-scraped and OTLP logs. | Predictable queries; README and existing tests define this contract. |
| IS-3 | OTLP log records from Serilog reach Loki via Alloy, not Tempo. | Tempo stores traces only; today those records are lost. |
| IS-4 | Exactly one delivery path per log line - no duplication between stdout scraping and OTLP. | User decision: avoid duplication; file logs removed. |
| IS-5 | `docker compose up` yields a clean Alloy: no config parse errors, all components running, `/loki/api/v1/labels` returns `container` and `project`. | Broken Alloy is silent in Grafana (just empty); it must be checkable from CLI. |
| IS-6 | `dotnet build MARS.slnx -c Release` and affected test projects are green - no conflict markers, no duplicate package versions. | A log fix on a red baseline cannot be verified. |
| GAP-1 | `infrastructure/alloy/config.alloy` is corrupt: 31 duplicate comment lines, `loki.source.docker` without `host`/`targets`/`labels`, `loki.write` with self-referencing `forward_to`, floating `url`, stray `}`. | Alloy cannot load it, so nothing reaches Loki. |
| GAP-2 | No OTLP log receiver anywhere; Serilog OTLP endpoint is `http://tempo:4317` (`docker-compose.yml:37`, `SerilogExtensions.cs:32-43`), and Tempo stores no logs. | OTLP log records are lost by construction. |
| GAP-3 | `SerilogExtensions` has no file sink, yet `SerilogFileSinkTests` expects `local-logs/*.log`; `ObservabilityStackTests` expects `local.file_match`/`loki.source.file`/`__path__`/`\.log` and a `data/local-logs:/var/log/mars` mount. | Half-implemented second path; tests encode an intention the user removed. |
| GAP-4 | `ObservabilityStackTests.cs:94-100` and `:205-209` contain literal conflict markers. | Test project cannot compile; matrix entry is red. |
| GAP-5 | A grep view showed duplicate `PackageVersion` entries, but the clean read (`Directory.Packages.props:65-86`) has single entries. | Must be confirmed; a wrong "fix" would churn the file. |
| GAP-6 | README/AGENTS.md say "no Loki sink in code, Alloy reads stdout", which becomes stale once OTLP logs terminate in Alloy. | Stale docs send the next debugging session down the same dead end. |
| GAP-7 | Duplication risk: if an app service logs to stdout and Alloy scrapes it, while the same service also sends OTLP, every line is duplicated in Loki. | User explicitly required no duplication. Mitigation: docker label `mars.logs=otlp` on app services + Alloy `discovery.relabel` drop rule; infra containers keep being scraped. |
| GAP-8 | `Serilog__OtlpEndpoint` is not defined in `docker-compose.yml` `x-service-env`, so containers would fall back to the Tempo endpoint. | Without the env key the code split alone does not change runtime behavior in containers. |

### Must have
- Rebuilt `infrastructure/alloy/config.alloy` matching the README topology, with `discovery.docker`, `discovery.relabel` (project filter `mars`, `container` from `/mars-(.*)-\d+$`, plus a drop rule for `mars.logs=otlp`), a correct `loki.source.docker`, and a single `loki.write` with a real `endpoint { url = "http://loki:3100/loki/api/v1/push" }`.
- Alloy OTLP log receiver built from real Alloy 1.20.1 components: `otelcol.receiver.otlp` (gRPC `0.0.0.0:4317`) → `otelcol.processor.attributes` (sets `loki.resource.labels`) → `otelcol.exporter.loki` → the same `loki.write`. The label contract is `service.name` mapped to `container`.
- A separate `Serilog:OtlpEndpoint` (default `http://alloy:4317`) read by `AddMarsLogging`; `Otlp:Endpoint` stays `http://tempo:4317` for traces. `Serilog__OtlpEndpoint: http://alloy:4317` added to `x-service-env`.
- Docker label `mars.logs=otlp` on every MARS application service (via `&service-defaults`), and an Alloy drop rule so those containers are not also stdout-scraped. Infra containers (loki, alloy, grafana, prometheus, tempo, postgres, rabbitmq) are still scraped.
- Alloy OTLP port published in `docker-compose.dev.yml` under a new `MARS_PORT_ALLOY_OTLP` at index 33 (dev `6034`, prod `7034`); present in all three `.env.*.example` files and `PortAllocationTests`.
- File-log path removed: no `WriteTo.File`, no `Dev:LogDirectory`, no `data/local-logs` mount, no `local.file_match`/`loki.source.file`; `SerilogFileSinkTests` deleted and file-related cases in `ObservabilityStackTests` rewritten.
- Merge conflicts resolved in `ObservabilityStackTests.cs`; `Directory.Packages.props` confirmed free of duplicates.
- README "Наблюдаемость" and AGENTS.md Loki/Alloy paragraphs updated to the two-source Alloy design.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- No file sink, no `data/local-logs` directory, no `loki.source.file`/`local.file_match`.
- No changes to Tempo config or the trace export path (`Otlp:Endpoint` stays Tempo).
- No changes to Prometheus/metrics (`OpenTelemetryPrometheusBridge`, `MarsMetrics`).
- No new microservice, no new NuGet package beyond what is already referenced.
- No Grafana dashboard changes; no trace→logs correlation enablement.
- No `loki.source.docker` for non-MARS containers (project filter stays `mars`).
- No change to console logging (`docker logs` must keep working).
- No rewrite of `docker-compose.yml` beyond the listed keys/labels.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: TDD for the .NET-side test changes; tests-after is acceptable for config/compose/docs but every changed assertion must be run.
- Commands: `dotnet build MARS.slnx -c Release`; `dotnet test tests/MARS.Gateway.Tests/MARS.Gateway.Tests.csproj -c Release`; `dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release`; `docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml config`; `docker run --rm grafana/alloy:v1.20.1 components`; live stand checks (`docker compose logs alloy`, `curl -s localhost:6025/loki/api/v1/labels`, Grafana query).
- Evidence: `.omo/evidence/task-<N>-fix-loki-log-delivery.<ext>` (text output of the command, or a saved JSON/log excerpt for live checks).

## Execution strategy
### Parallel execution waves
- Wave 1 (baseline + config): todos 1-6 - confirm package file, resolve conflicts/delete file-sink tests, rebuild Alloy config, add OTLP receiver, split Serilog endpoint + env key, add port key.
- Wave 2 (tests + docs): todos 7-8 - rewrite `ObservabilityStackTests`, update README/AGENTS.md.
- Wave 3 (host + live): todos 9-10 - local-launch config, live stand verification.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | - | 2 | - |
| 2 | 1 | 3,7 | 5 |
| 3 | 1 | 4,10 | 5,6 |
| 4 | 3 | 7,9 | 5,6 |
| 5 | 1 | 7,9 | 2,3,4,6 |
| 6 | 1 | 7 | 2,3,4,5 |
| 7 | 2,3,4,5,6 | 8,9 | - |
| 8 | 4,7 | 10 | 9 |
| 9 | 4,7 | 10 | 8 |
| 10 | 3,7,8,9 | - | - |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. Confirm `Directory.Packages.props` has no duplicate `PackageVersion` entries; record the clean state.
  Recommended task executor category: quick
  What to do / Must NOT do: read the whole file; assert exactly one `PackageVersion` for each package name. Do NOT edit versions unless a real duplicate exists; if one exists, keep the highest compatible version and remove the other.
  Closes: GAP-5
  Parallelization: Wave 1 | Blocked by: none | Blocks: 2
  References: `Directory.Packages.props:65-86` (clean read shows `Serilog.Sinks.OpenTelemetry` 4.0.0, `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.17.0, single entries).
  Acceptance criteria: a command that extracts all `PackageVersion Include="..."` names returns no duplicate; `dotnet restore MARS.slnx` succeeds.
  QA scenarios: happy - `dotnet restore MARS.slnx` exit 0; failure - if duplicates existed and were removed, restore stays 0 and no NU1504/NU1008 appears. Evidence `.omo/evidence/task-1-fix-loki-log-delivery.txt`.
  Commit: N | chore(packages): confirm no duplicate PackageVersion entries

- [x] 2. Resolve merge-conflict markers and delete the obsolete file-sink test.
  Recommended task executor category: quick
  What to do / Must NOT do: in `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs` keep the single-line `File.ReadAllLines(ClientUiImageWorkflowTests.FindRepositoryFile(".gitignore"))` form and remove `<<<<<<<`/`=======`/`>>>>>>>`; delete `tests/MARS.Shared.Tests/Logging/SerilogFileSinkTests.cs` entirely (file logs removed by decision). Do NOT touch `Job(...)` logic or other tests.
  Closes: GAP-3, GAP-4
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 3,7
  References: `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:94-100,205-209`; `tests/MARS.Shared.Tests/Logging/SerilogFileSinkTests.cs:1-99`.
  Acceptance criteria: `grep -n "<<<<<<<\|=======\|>>>>>>>" tests/MARS.Gateway.Tests/ObservabilityStackTests.cs` returns no matches; `SerilogFileSinkTests.cs` does not exist; `dotnet build tests/MARS.Gateway.Tests/MARS.Gateway.Tests.csproj -c Release` succeeds.
  QA scenarios: happy - build succeeds and grep is empty; failure - a stray marker makes `dotnet build` fail, proving the check. Evidence `.omo/evidence/task-2-fix-loki-log-delivery.txt`.
  Commit: Y | fix(tests): resolve merge conflict and drop file-log sink test

- [x] 3. Rebuild `infrastructure/alloy/config.alloy` with a valid docker source, the OTLP drop rule, and a single `loki.write` endpoint.
  Recommended task executor category: deep-low
  What to do / Must NOT do: keep `discovery.docker "containers"` and `discovery.relabel "mars"` (`config.alloy:11-36`) as they are: filter on `__meta_docker_container_label_com_docker_compose_project` = `mars`, and `container` from `__meta_docker_container_name` regex `/mars-(.*)-\d+$`. Delete the 31 duplicated comment lines; keep only the file header (`1-9`) and the relabel comments. **Add a second relabel rule** that drops containers carrying label `mars.logs=otlp` (they deliver via OTLP only): source label `__meta_docker_container_label_mars_logs`, regex `otlp`, action `drop`. Rebuild `loki.source.docker "mars"` with `host = "unix:///var/run/docker.sock"`, `targets = discovery.relabel.mars.output`, `labels = { project = "mars" }`, `forward_to = [loki.write.mars.receiver]`. Rebuild `loki.write "mars"` with exactly one `endpoint { url = "http://loki:3100/loki/api/v1/push" }`. Remove the stray `url` line and the extra closing brace. Do NOT add file sources, do NOT self-reference `loki.write`.
  Closes: GAP-1, GAP-7
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 4,10
  References: `infrastructure/alloy/config.alloy:1-49`; `README.md` "Наблюдаемость"; `infrastructure/grafana/datasources/loki.yml:8-14`.
  Acceptance criteria: `docker run --rm -v "$PWD/infrastructure/alloy/config.alloy:/etc/alloy/config.alloy:ro" grafana/alloy:v1.20.1 fmt /etc/alloy/config.alloy` exits 0; exactly one `loki.write` block and one `endpoint {`; zero `forward_to` inside `loki.write`; `grep -c "forward_to = \[loki.write.mars.receiver\]"` equals 2 after todo 4 (1 before); the `drop` rule is present.
  QA scenarios: happy - `fmt` exits 0 and grep counts match; failure - a deliberately broken brace makes `fmt` exit non-zero. Evidence `.omo/evidence/task-3-fix-loki-log-delivery.txt`.
  Commit: Y | fix(alloy): rebuild config with docker source and valid loki.write endpoint

- [x] 4. Add the Alloy OTLP log receiver using real Alloy 1.20.1 components, and publish/expose its gRPC port.
  Recommended task executor category: deep-low
  What to do / Must NOT do: first prove `loki.source.otlp` is ABSENT: `docker run --rm grafana/alloy:v1.20.1 components | grep -c 'loki.source.otlp'` must print `0`. Append to `config.alloy` exactly:
  ```alloy
  otelcol.receiver.otlp "logs" {
    grpc {}
    output {
      logs = [otelcol.processor.attributes.otlp_labels.input]
    }
  }

  otelcol.processor.attributes "otlp_labels" {
    action {
      key    = "loki.resource.labels"
      action = "insert"
      value  = "service.name, container, project"
    }
    output {
      logs = [otelcol.exporter.loki.otlp.input]
    }
  }

  otelcol.exporter.loki "otlp" {
    forward_to = [loki.write.mars.receiver]
  }
  ```
  `grpc {}` defaults to `0.0.0.0:4317`. `otelcol.exporter.loki` does NOT convert attributes to labels without the `loki.resource.labels` hint, hence the processor. Do NOT invent `loki.source.otlp`; do NOT add an `http {}` block. In `docker-compose.yml` alloy service add `expose: ["4317"]`; in `docker-compose.dev.yml` alloy ports add `- "${MARS_PORT_ALLOY_OTLP}:4317"`.
  Closes: GAP-2
  Parallelization: Wave 1 | Blocked by: 3 | Blocks: 7,9
  References: `https://grafana.com/docs/alloy/latest/reference/components/otelcol/otelcol.receiver.otlp/`; `https://grafana.com/docs/alloy/latest/reference/components/otelcol/otelcol.exporter.loki/`; `https://grafana.com/docs/alloy/latest/reference/components/loki/` (no `loki.source.otlp`); `docker-compose.yml:590-606`; `docker-compose.dev.yml:369-371`.
  Acceptance criteria: `components` grep prints `0`; `components` lists all three otelcol components; `fmt` exits 0; `docker compose ... config` shows `4317` exposed for alloy.
  QA scenarios: happy - `fmt` exits 0 and the four components are wired end to end; failure - removing the `loki.resource.labels` action makes Alloy startup fail. Evidence `.omo/evidence/task-4-fix-loki-log-delivery.txt`.
  Commit: Y | feat(alloy): receive OTLP logs into the same Loki writer

- [x] 5. Split the Serilog OTLP log endpoint from the trace endpoint and add it to the shared compose env.
  Recommended task executor category: deep-low
  What to do / Must NOT do: in `SerilogExtensions.AddMarsLogging`, read `configuration["Serilog:OtlpEndpoint"]` (fallback `configuration["Otlp:Endpoint"]` only if unset) and pass it to `WriteTo.OpenTelemetry`; keep the Console sink unchanged; add resource attributes `service.name` and `container` derived from the existing `serviceName` argument (helper method + tests); do NOT change `OpenTelemetryExtensions` (`Otlp:Endpoint` stays Tempo). Add `Serilog__OtlpEndpoint: http://alloy:4317` to `x-service-env` in `docker-compose.yml` and update the stale comment there.
  Closes: GAP-2, GAP-8
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 7,9
  References: `src/MARS.Shared/Logging/SerilogExtensions.cs:28-43`; `src/MARS.Shared/Telemetry/OpenTelemetryExtensions.cs:31-51`; `docker-compose.yml:29-41` (`&service-env` anchor).
  Acceptance criteria: `SerilogExtensions.cs` references `Serilog:OtlpEndpoint`; `docker-compose.yml` has `Serilog__OtlpEndpoint: http://alloy:4317`; `dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-namespace "MARS.Shared.Tests.Logging"` passes.
  QA scenarios: happy - unit test constructs `AddMarsLogging` with `Serilog:OtlpEndpoint` and asserts the sink receives it; failure - omitting the key falls back and is asserted. Evidence `.omo/evidence/task-5-fix-loki-log-delivery.txt`.
  Commit: Y | feat(logging): send Serilog OTLP logs to Alloy, keep traces on Tempo

- [x] 6. Add `MARS_PORT_ALLOY_OTLP` at index 33 to all env templates and `PortAllocationTests`.
  Recommended task executor category: quick
  What to do / Must NOT do: append `MARS_PORT_ALLOY_OTLP` as the **last** line of the port block (index 33; dev `6034`, prod `7034`) in `.env.example`, `.env.development.example`, `.env.production.example`. Add the tuple to the port table in `tests/MARS.Gateway.Tests/PortAllocationTests.cs`; run `scripts/windows/ports.ps1` (or `scripts/unix/ports.sh`) so the values are written. Do NOT renumber existing ports.
  Closes: GAP-2
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 7
  References: `.env.example`, `.env.development.example`, `.env.production.example` (port blocks); `tests/MARS.Gateway.Tests/PortAllocationTests.cs`; `scripts/windows/ports.ps1` / `scripts/unix/ports.sh`.
  Acceptance criteria: `grep MARS_PORT_ALLOY_OTLP` finds all three templates; `PortAllocationTests` contains the new tuple; `scripts/windows/ports.ps1 -Check` passes.
  QA scenarios: happy - check passes; failure - a missing template key makes the check fail. Evidence `.omo/evidence/task-6-fix-loki-log-delivery.txt`.
  Commit: Y | chore(ports): add Alloy OTLP host port

- [x] 7. Rewrite `ObservabilityStackTests` for the no-file-log, two-source Alloy design.
  Recommended task executor category: deep-low
  What to do / Must NOT do: delete `LogsMount`, the file assertions in `Alloy_Читает_Логи_Процессов_На_Хосте`, and the file-mount assertion in `Dev_Оверрайд_Отдаёт_Все_Каталоги`; add assertions that `config.alloy` contains `loki.source.docker`, `otelcol.receiver.otlp`, `otelcol.exporter.loki`, `discovery.relabel`, the `mars.logs=otlp` drop rule, and exactly one `endpoint {` in `loki.write`; assert `docker-compose.yml` contains `Serilog__OtlpEndpoint` and the `mars.logs=otlp` label on `&service-defaults`; keep Prometheus job tests unchanged. Do NOT assert any `local.file_match`/`loki.source.file`.
  Closes: GAP-3, GAP-7, GAP-8
  Parallelization: Wave 2 | Blocked by: 2,3,4,5,6 | Blocks: 8,9
  References: `tests/MARS.Gateway.Tests/ObservabilityStackTests.cs:26-29,114-154`; new `infrastructure/alloy/config.alloy`; `docker-compose.yml:16-41`; `docker-compose.dev.yml` alloy block.
  Acceptance criteria: `dotnet test tests/MARS.Gateway.Tests/MARS.Gateway.Tests.csproj -c Release -- --filter-class "*ObservabilityStackTests*"` passes; no test mentions `local-logs` or `loki.source.file`.
  QA scenarios: happy - filter-class run is green; failure - temporarily removing `otelcol.receiver.otlp` from config makes the test red. Evidence `.omo/evidence/task-7-fix-loki-log-delivery.txt`.
  Commit: Y | test(observability): assert docker+OTLP Alloy sources, drop file-log assertions

- [x] 8. Update README and AGENTS.md to describe the delivered pipeline.
  Recommended task executor category: writing
  What to do / Must NOT do: in README "Наблюдаемость" table and paragraph, state that Alloy reads container stdout **and** accepts OTLP logs from Serilog on `alloy:4317`, both into one `loki.write`; state that application services are labelled `mars.logs=otlp` so their stdout is not scraped twice; state file logs are not used. Update AGENTS.md Loki/Alloy section likewise. Do NOT claim trace→logs correlation works.
  Closes: GAP-6
  Parallelization: Wave 2 | Blocked by: 4,7 | Blocks: 10
  References: `README.md` "Наблюдаемость"; `AGENTS.md` Loki/Alloy paragraphs; `infrastructure/grafana/datasources/tempo.yml:8-18`.
  Acceptance criteria: `grep -n "otelcol.receiver.otlp" README.md AGENTS.md` finds both; no doc says logs are delivered only by stdout scraping without mentioning OTLP.
  QA scenarios: happy - grep finds both; failure - stale sentence "сервисы никуда логи не шлют" remains and is caught by grep. Evidence `.omo/evidence/task-8-fix-loki-log-delivery.txt`.
  Commit: Y | docs(observability): document Alloy docker+OTLP log sources

- [x] 9. Update `scripts/local-launch.conf` for host-run OTLP logs and regenerate profiles.
  Recommended task executor category: quick
  What to do / Must NOT do: add `Serilog__OtlpEndpoint=http://localhost:${MARS_PORT_ALLOY_OTLP}` next to the existing `Otlp__Endpoint` in `scripts/local-launch.conf:41`; run `local -Check` and regenerate `launchSettings.json`. Do NOT put secrets there.
  Closes: GAP-2, GAP-6
  Parallelization: Wave 3 | Blocked by: 4,7 | Blocks: 10
  References: `scripts/local-launch.conf:41`; `src/*/Properties/launchSettings.json` (generated); `tests/MARS.Gateway.Tests/LocalLaunchSettingsTests.cs`.
  Acceptance criteria: `scripts/windows/local.ps1 -Check` passes; `launchSettings.json` contains `Serilog__OtlpEndpoint` for services that call `AddMarsDefaults`.
  QA scenarios: happy - check passes; failure - a wrong key makes `LocalLaunchSettingsTests` red. Evidence `.omo/evidence/task-9-fix-loki-log-delivery.txt`.
  Commit: Y | chore(local): point host-run Serilog OTLP at Alloy

- [ ] 10. Live verification on the dev stand.
  Recommended task executor category: deep-low
  What to do / Must NOT do: `docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up -d --build`; wait healthy; `docker compose logs alloy` clean; `curl -s localhost:6025/loki/api/v1/labels` contains `container`,`project`; Grafana query `{container="twitch-core"}` returns lines; generate a log line from a service and confirm it appears exactly once (no duplicate). Do NOT leave the stand in a broken state.
  Closes: GAP-1, GAP-2, GAP-3, GAP-7
  Parallelization: Wave 3 | Blocked by: 3,7,8,9 | Blocks: none
  References: `README.md` verification commands; `docker-compose.dev.yml`; `MARS_PORT_LOKI=6025`; `MARS_PORT_ALLOY=6026`; `MARS_PORT_ALLOY_OTLP=6034`.
  Acceptance criteria: labels endpoint returns both labels; a known log line appears exactly once; `docker compose ps` all healthy.
  QA scenarios: happy - the above commands produce cited evidence; failure - stop `alloy` and confirm Loki stops receiving new lines, then restart. Evidence `.omo/evidence/task-10-fix-loki-log-delivery.txt`.
  Commit: N | live verification only

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit - every todo row executed or explicitly justified; GAP rows closed.
  Recommended task executor category: unspecified-high
- [ ] F2. Code quality review - config syntax valid, no leftover conflict markers, no dead file-sink code, no duplicate package versions.
  Recommended task executor category: unspecified-high
- [ ] F3. Real manual QA - live stand produces Loki labels and a non-duplicated log line in Grafana.
  Recommended task executor category: deep-low
- [ ] F4. Ideal-state fidelity - IS-1..IS-6 each proven by its QA evidence path; shortfall becomes a new todo.
  Recommended task executor category: unspecified-high

## Commit strategy
- Commit 2,3,4,5,6,7,8,9 each as one logical commit (`fix`/`feat`/`test`/`docs`/`chore`), linear, no squash of Red/Green pairs.
- Todo 1 and 10 are verification-only (`Commit: N`).
- Branch: `fix/loki-log-delivery`; PR body must list cross-cutting edits (both env templates + `.env.example`, compose, `PortAllocationTests`, README, AGENTS.md, local-launch.conf).

## Success criteria
> One row per IS row. The plan is complete only when every IS row has a delivering todo and a proving QA scenario; F4 checks the delivered behavior against these rows 1:1, and a shortfall becomes new `- [ ] N.` rows, never a note.
| IS | Delivering todo(s) | Proving QA scenario | Evidence |
| --- | --- | --- | --- |
| IS-1 | 3,4,10 | Grafana/Loki query `{container="twitch-core"}` returns lines | `.omo/evidence/task-10-fix-loki-log-delivery.txt` |
| IS-2 | 3,5 | `container` label equals compose service name for both sources | `.omo/evidence/task-3-fix-loki-log-delivery.txt` |
| IS-3 | 4,5,10 | OTLP log line reaches Loki via Alloy | `.omo/evidence/task-10-fix-loki-log-delivery.txt` |
| IS-4 | 3,4,5,7 | a line appears exactly once | `.omo/evidence/task-10-fix-loki-log-delivery.txt` |
| IS-5 | 3,4,10 | `docker compose logs alloy` clean + labels endpoint | `.omo/evidence/task-10-fix-loki-log-delivery.txt` |
| IS-6 | 1,2,7 | `dotnet build`/affected test projects green | `.omo/evidence/task-7-fix-loki-log-delivery.txt` |
