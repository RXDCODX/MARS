---
name: mars-repo-maintenance
description: Keep MARS cross-cutting configuration, scripts, environment templates, CI matrices and generated files consistent during repository-wide changes.
---

# MARS repository maintenance

## Scripts

- Windows scripts live in `scripts/windows`; Unix scripts live in `scripts/unix`. Every public script must exist on both platforms with the same command name and equivalent behavior.
- Update `scripts/README.md` when adding or changing public commands. Keep Unix executable bits set.
- Script parity and behavior are tested by `ScriptsParityTests`. Preserve MTP filter names, safe defaults, and explicit opt-in for destructive/publishing actions.
- Coverage wrappers must delegate to or mirror `.github/scripts/coverage-local.ps1`; avoid a second independent implementation of the CI coverage pipeline.
- Release scripts derive image lists from the release workflow matrix. Publishing must require explicit opt-in.
- Stack/e2e scripts must use an explicit `--env-file`, readiness wait and the Playwright container settings expected by CI. Never silently start the stack with empty credentials.

## Cross-cutting change checklist

When renaming/removing a key, service, port, project, package, route or infrastructure component, search beyond the files in the immediate diff. Update only the applicable locations, then search for the old identifier again.

| Change | Check all relevant locations |
|---|---|
| Environment variable | both environment templates and `.env.example`, compose service environment, `appsettings*.json`, configuration mapping/code, README env table, `scripts/local-launch.conf` |
| Connection string | `AddMarsDefaults`, `AddMarsDbContext`, appsettings and compose; key names must match |
| Published port | environment templates, correct compose file, port allocation test, `ports` script, host launch config if used |
| gRPC port/endpoint | dev compose, `Grpc__HttpPort`/`Grpc__GrpcPort`, `Grpc:Endpoints`, port test and host launch config |
| Stack component | compose services/volumes, Grafana provisioning, environment templates, README and dashboards/alerts |
| New database | env templates, `infrastructure/db-init/01-databases.sh`, README DB table, design-time factory and service migrations |
| NuGet package | `Directory.Packages.props` and the consuming `.csproj` |
| New project/layer | `MARS.slnx`, project references, Dockerfile COPY before restore, compose watch/build paths, test project and layering tests |
| New service | all project/layer locations above, test matrix, release matrix, compose, Prometheus targets, Gateway routes/endpoints, env/ports and local launch config |
| New test project | `MARS.slnx`, references to service layers, `coverlet.MTP`, CI test matrix and coverage script exclusions if intentional |
| New script | both platforms, parity tests, `scripts/README.md`, executable bit for `.sh` |

## Safe stale-reference sweep

For a renamed/deleted identifier, search tracked and untracked files while excluding build output and generated/vendor directories. Example PowerShell:

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.json,*.yml,*.yaml,*.md,*.props,*.csproj,.env*,*.example,*.alloy,*.slnx |
  Where-Object { $_.FullName -notmatch '\\(obj|bin|node_modules|TestResults|\.git)\\' } |
  Select-String -Pattern 'OLD_IDENTIFIER' -CaseSensitive:$false |
  ForEach-Object { "$($_.Path.Replace((Get-Location).Path + '\\','')):$($_.LineNumber)" }
```

Do not rely on `git grep` alone: it may omit untracked files. Classify hits before deleting them:
- historical/explanatory comments can remain;
- documentation explaining the replacement can remain;
- executable references, stale keys, ports, routes, paths and image names must be fixed.

Never edit a real ignored `.env` file as part of an automated cleanup. Report the stale key and let the owner update their local secret-bearing file.

## Persistent state and one-time initialization

- Removing a Grafana provisioning file does not necessarily remove an existing provisioned datasource/dashboard from its volume. Inspect persistent state and use the documented reset procedure rather than assuming file deletion is enough.
- `infrastructure/db-init/01-databases.sh` runs only when the PostgreSQL data volume is initialized. Adding a database to templates does not modify an existing volume; migration/reset must be explicit and backed up.
- Keep real env files excluded from Docker build context (`.dockerignore`); otherwise secrets can enter image layers.

## Source of truth

Before editing, verify current workflow, compose, templates, tests and scripts. Do not trust historical incident notes or old `.mimocode/plans/` as current specification. Update generated files through their generator and validate with its `--check` mode.
