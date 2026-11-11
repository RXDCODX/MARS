---
name: mars-code-conventions
description: Apply MARS C#, .NET, testing, and frontend implementation conventions when writing or refactoring code within existing projects.
---

# MARS code conventions

## Scope and project boundaries

Use this skill for implementation and code organization **inside existing projects**: C#, .NET, tests, and the repository's frontends.

Project naming, project boundaries, architectural roles, project-reference direction, and operations that create, split, merge, move, or rename projects are defined exclusively by `mars-project-structure`. Load that skill whenever a change affects the Solution/project structure. Do not duplicate its project-level rules here.

## Source files and namespaces

- Within an existing .NET project, namespaces follow the source-folder path below the project's `RootNamespace`.
- Keep one public type per C# file, except for local/nested types where the existing convention permits them.
- Follow the established naming, nullable, async, and formatting patterns in the nearest relevant code. Do not introduce a local convention without a concrete reason.
- For `[Flags]` enums, assign explicit powers of two. Keep protobuf enum mappings explicit by name; numeric values may differ.

## .NET build and package settings

- Central Package Management is enabled. Add package versions to root `Directory.Packages.props`; consuming `.csproj` files use versionless `PackageReference`. Keep package declaration and use-site reference in sync.
- Common target framework, nullable/implicit usings, warning policy, and analyzer settings live in root `Directory.Build.props`. Project files should contain only project-specific differences.
- `TreatWarningsAsErrors` is enabled. Add narrow `NoWarn` exceptions only with a reason; preserve inherited values via `$(NoWarn)`.
- `xUnit1051` is treated as an error: pass `TestContext.Current.CancellationToken` to async APIs that expose a cancellation-token overload. Use named arguments where needed to avoid binding the token to another optional parameter.
- CSharpier is the only C# formatter. Run `dotnet csharpier format .`; use `dotnet csharpier check .` to verify.

## C# implementation conventions

These conventions were inherited from the original codebase and remain the local style where they fit the code being changed:

- Prefer the repository's `OperationResult<T>` response contract. Controllers return `ActionResult<OperationResult<T>>`; business failures are represented consistently by `Success = false`, not ad-hoc response shapes.
- Read-only EF Core queries use `AsNoTracking()`.
- Keep methods linear and results explicit. The legacy style prefers a default result variable and a single final return for result-returning methods; apply it when it improves clarity, but do not contort guard clauses or exception flow solely to satisfy a mechanical rule.
- Conditions should make the successful path easy to follow. Catch exceptions only at a boundary where they can be logged and translated meaningfully; do not turn every exception into a success-shaped message.
- Do not perform network, filesystem, IRC, websocket, or container startup as a hidden constructor side effect. Inject an interface/factory and keep integration boundaries replaceable.
- Preserve established DTO, API, and serialization contracts; do not duplicate transport-envelope transformations across UI components.

## Frontends

There is one separately bundled frontend: `src/MARS.ClientUi` (Yarn 4). MediaStorage UI is Razor Pages plus vanilla JavaScript inside `MARS.MediaStorage.Host`; it has no separate Node/npm project or build stage.
## Tests and supporting skills

- For database fixtures and test doubles, use `backend-test-helpers`.
- For running tests and Microsoft.Testing.Platform filtering, use `dotnet-test-run`.
- For coverage strategy and known runtime traps, use `mars-coverage`.
