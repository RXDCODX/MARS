---
name: mars-coverage
description: Measure and improve MARS .NET method coverage; use the canonical local coverage pipeline, inspect gaps, and diagnose MTP/Coverlet issues.
---

# MARS coverage

## Canonical pipeline

Use `.github/scripts/coverage-local.ps1` as the source of truth. `scripts/windows/coverage.ps1` delegates to it; `scripts/unix/coverage.sh` mirrors its steps. Restore local tools from `.config/dotnet-tools.json` before running ReportGenerator.

```powershell
# Full CI-equivalent measurement and gate
.\.github\scripts\coverage-local.ps1

# One project, diagnostic only; not comparable to the merged CI result
.\.github\scripts\coverage-local.ps1 -Project MARS.OBS.Tests -NoGate

# Rebuild merged report and gate from existing per-project reports
.\.github\scripts\coverage-local.ps1 -SkipTests
python .\.github\scripts\coverage-gaps.py --merged coverage-local/coverage-report/Cobertura.xml --top 40
```

Important parameters: `-Project`, `-MaxParallel`, `-ThresholdMethods`, `-NoGate`, `-SkipBuild`, `-SkipTests`. Reports are written to ignored `coverage-local/`.

The script compares `tests/*.Tests` with the `tests` matrix in `.github/workflows/ci.yml`; a mismatch is an error. `MARS.ClientUi.Tests` is an intentional E2E-only exclusion, not a coverage project.

## Rules that keep the number honest

- CI coverage runs only after the complete test matrix succeeds. Do not run or interpret coverage from a partial test set as the CI result.
- Merge Cobertura reports with ReportGenerator; never add per-project percentages or method counts. Shared assemblies are instrumented by multiple test projects.
- The gate measures **methods**, not Cobertura line-rate/branch-rate. See `.github/scripts/coverage-gate.py` for the authoritative implementation.
- `--coverlet-include "[MARS.*]*"` is required to exclude third-party assemblies. Exclude test assemblies and migrations as configured by the canonical script.
- `coverlet.MTP` is used with Microsoft.Testing.Platform. VSTest collectors are not interchangeable. Test-app options go after `--`.
- An empty report set is an error, not zero percent.
- Do not hardcode current coverage percentages or project counts in guidance; they change as code and CI evolve.

## Prioritizing gaps

Use `coverage-gaps.py --merged <Cobertura.xml> --top 0 --format json` to rank classes by uncovered methods, then group by package.

1. Pure logic (validators, filters, mappers) usually yields the most coverage per test.
2. Services depending on interfaces can be exercised with Moq or `DependencyStubFactory`.
3. If construction starts network/IRC/websocket clients, introduce an interface/factory seam in production code. Do not make tests depend on live services.
4. `Program.Main` calling `app.Run()` is blocking; extract app construction if it must be tested.

## Known testing traps

- YoutubeExplode 6.5.3 can hang a test host merely by loading its assembly. Keep the concrete integration isolated behind `IYouTubeApi`; do not reference its concrete client in unit tests. The integration wrapper is intentionally excluded from public-contract reflection tests.
- EF tests must use real PostgreSQL via Testcontainers and service migrations. InMemory/SQLite do not reproduce PostgreSQL semantics.
- Npgsql `timestamptz` requires UTC. `MarsUtcDates` normalizes writes; values read from PostgreSQL have `DateTimeKind.Utc`. Convert to local time only at presentation boundaries.
- `SaveChangesAsync` reports changed rows, not necessarily the number of input records.
- TwitchLib response DTOs with internal setters cannot reliably be fabricated through a mocked `ITwitchAPI`; test failure paths or isolate response parsing behind a seam.
- `RabbitMqConsumerBase.Deserialize<T>` lets malformed JSON escape to retry/DLQ. A JSON `null` is a separate case.
- A pre-cancelled `BackgroundService` token prevents the loop body from running. To test the body, trigger an observable signal or call the extracted worker step.
- For fire-and-forget work, wait for an observable condition with a bounded timeout, not an arbitrary fixed `Task.Delay`.
- Moq cannot use `It.IsAnyType` as a generic `Callback` parameter. Use a small custom logger implementation when capturing `ILogger.Log`.
- A mocked `ILogger<T>` may require an accessible public `T` for Castle proxy generation.
- Twitch reward IDs parsed as GUIDs must be valid GUID strings in fixtures.

For test fixtures, PostgreSQL factories, and cancellation tokens, also read `backend-test-helpers`. For MTP command syntax and output parsing, read `dotnet-test-run`.
