---
name: mars-repo-architecture
description: Apply MARS architectural dependency, namespace, C#, and frontend conventions. Use mars-project-structure for mandatory project naming and project-boundary changes.
---

# MARS architecture and code conventions

## Projects and namespaces

- The mandatory project naming grammar, scope taxonomy, project-boundary criteria, dependency direction, and create/split/merge/move/rename workflow are defined in `mars-project-structure`. Load and follow that skill before any project-structure change.
- Project directory name, `.csproj` file name, `AssemblyName`, and `RootNamespace` are the same identity. Namespaces follow the source-folder path below the project root.
- Keep service dependencies pointed inward. Infrastructure projects must not reference another service's API/host. Avoid project-reference cycles; see `mars-project-structure` for the required dependency graph.
- `MARS.Shared` is not a dumping ground: keep its internal boundaries and place cross-service contracts/protobuf definitions in `MARS.Shared.Contracts`.
- One test project per service: `tests/MARS.<Service>.Tests`, referencing every layer. A new test project also needs a `tests` matrix entry in `.github/workflows/ci.yml`. `MARS.TestKit` is a library, not a test project.
- Keep test project names stable: branch-protection checks depend on them, even when production projects move to `MARS.Modules.*`.
- One public type per file, except local/nested types, follows the existing C# convention.
- A long assembly namespace does not imply renaming external service identity: compose service names, environment keys, metrics and log labels may be deployed contracts.

## Dependencies and build settings

- Central Package Management is enabled. Add package versions to root `Directory.Packages.props`; `.csproj` uses versionless `PackageReference`. Keep package declaration and use-site reference in sync.
- Common target framework, nullable/implicit usings, warning policy and analyzer settings live in root `Directory.Build.props`. Project files should contain only project-specific differences.
- `TreatWarningsAsErrors` is enabled. Add narrow `NoWarn` exceptions only with a reason; preserve inherited values via `$(NoWarn)`.
- `xUnit1051` is treated as an error: pass `TestContext.Current.CancellationToken` to async APIs that expose a cancellation-token overload. Use named arguments where needed to avoid binding the token to another optional parameter.
- CSharpier is the only C# formatter. Run `dotnet csharpier format .`; use `dotnet csharpier check .` to verify.

## C# conventions

These conventions were inherited from the original codebase and remain the local style where they fit the code being changed:

- Prefer the repository's `OperationResult<T>` response contract. Controllers return `ActionResult<OperationResult<T>>`; business failures are represented consistently by `Success = false`, not ad-hoc response shapes.
- Read-only EF Core queries use `AsNoTracking()`.
- Keep methods linear and results explicit. The legacy style prefers a default result variable and a single final return for result-returning methods; apply it when it improves clarity, but do not contort guard clauses or exception flow solely to satisfy a mechanical rule.
- Conditions should make the successful path easy to follow. Catch exceptions only at a boundary where they can be logged and translated meaningfully; do not turn every exception into a success-shaped message.
- Do not perform network, filesystem, IRC, websocket, or container startup as a hidden constructor side effect. Inject an interface/factory and keep integration boundaries replaceable.
- For `[Flags]` enums, assign explicit powers of two. Keep protobuf enum mappings explicit by name; numeric values may differ.
- Preserve established DTO, API and serialization contracts; do not duplicate transport-envelope transformations across UI components.

## Frontends

There are two distinct frontends. Confirm the package manager and scripts from the target directory before running commands.

- `src/MARS.ClientUi`: Yarn 4, `corepack enable`, `yarn install --immutable`, then `npx tsc -b --noEmit`, `yarn test`, `yarn build`. Do not introduce a second lockfile.
- `src/MARS.MediaStorage/ClientApp`: use the package manager and scripts in that directory's current manifest/lockfile; current repository commands are `npm ci` and `npm run typecheck`.
- Keep browser timers typed as `ReturnType<typeof setTimeout>`, not Node-specific `NodeJS.Timeout`.
- React imports are named; use existing state-management patterns and shared style files. Do not copy stores/hooks/components from one frontend into the other without checking that they exist there.
- Gateway UI hub connections go through the existing `HubAdapter` registry and `resolveHubUrl`; do not create ad-hoc SignalR connections.
- Keep generated navigation manifests synchronized using their documented update command; do not edit generated output independently.

## Tests

For database fixtures and test doubles, use `backend-test-helpers`. For running tests and MTP filtering, use `dotnet-test-run`. For coverage strategy and known runtime traps, use `mars-coverage`.
