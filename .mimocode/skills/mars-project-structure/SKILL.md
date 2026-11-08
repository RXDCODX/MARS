---
name: mars-project-structure
description: Enforce MARS project naming, boundaries, dependency direction, and safe project creation, splitting, merging, moving, or renaming.
---

# MARS project naming and structure

This skill is **mandatory** whenever a task creates, splits, merges, moves, renames, or materially refactors a project/assembly. Apply it to new projects and to every existing project whose structure or responsibility is being refactored. Do not treat legacy names as the convention for new work.

## 1. Canonical project-name grammar

Use this shape:

```text
<Repository>.<Scope>.<CapabilityOrArchitecturalRole>[.<Responsibility>...]
```

For MARS:

- `<Repository>` is always `MARS`.
- `<Scope>` identifies the owning platform, product, service, project, business capability, or bounded context. It may be a compound scope when needed, for example `Platform`, `Alerts`, `MediaStorage`, or `Modules.Alerts`.
- `<CapabilityOrArchitecturalRole>` is the primary boundary represented by the project. Prefer a recognized architectural role when the project is a layer; otherwise use a clear business capability.
- `<Responsibility>` and subsequent qualifiers narrow the project to a concrete responsibility, adapter, protocol, technology, or feature. Add them only when they communicate a real boundary; order them broad-to-specific.
- Omit unnecessary segments. Do not invent a layer or qualifier merely to make names longer.

The first segments must make ownership obvious; the role/capability must make the project's architectural purpose obvious; trailing qualifiers must explain what it is responsible for.

### Naming examples

| Responsibility | Canonical name |
|---|---|
| Alerts domain model | `MARS.Alerts.Domain` |
| Alerts use cases | `MARS.Alerts.Application` |
| Twitch rewards use cases owned by Alerts | `MARS.Alerts.Application.Twitch.Rewards` |
| Alerts integration-event contracts for Twitch | `MARS.Alerts.IntegrationEvents.Twitch` |
| Alerts persistence adapter for PostgreSQL | `MARS.Alerts.Infrastructure.Persistence.Postgres` |
| Alerts Twitch API/client adapter | `MARS.Alerts.Infrastructure.Integrations.Twitch` |
| Alerts overlay presentation | `MARS.Alerts.Presentation.Overlay` |
| Alerts HTTP entry point | `MARS.Alerts.Api` |
| Shared RabbitMQ infrastructure | `MARS.Platform.Infrastructure.Messaging.RabbitMq` |
| Shared PostgreSQL infrastructure | `MARS.Platform.Infrastructure.Persistence.Postgres` |
| Media Storage background host | `MARS.MediaStorage.Worker` |
| A module inside the Alerts product | `MARS.Modules.Alerts.Application.Adhd` |

Use `Platform` only for reusable, cross-cutting technical capabilities that are not owned by one product/bounded context. Product-specific code belongs under its product/bounded-context scope, even if it uses the same technology as another product.

### Roles and terminology

Use names that communicate the project's real boundary:

- `Domain`: business entities, value objects, domain invariants, and domain events.
- `Application`: use cases, commands/queries, handlers, orchestration, and application ports.
- `Contracts`: request/response contracts or shared DTOs at an explicit boundary.
- `IntegrationEvents`: versionable event contracts published for other services or bounded contexts. Do not put event consumers or transport implementations here.
- `Abstractions`: a genuinely shared set of ports/interfaces that deserves an independent reference boundary. Prefer keeping use-case-owned ports in `Application` when they are not independently shared.
- `Infrastructure`: concrete adapters and technical implementations of inward-facing ports.
- `Infrastructure.Persistence.<Technology>`: persistence adapter when it belongs to the broader infrastructure boundary.
- `Infrastructure.Integrations.<Provider>`: outbound integration with a third-party provider.
- `Persistence`: use as the primary role only when persistence itself is a standalone boundary; do not create both `Persistence` and `Infrastructure.Persistence` for the same responsibility.
- `Presentation`: UI or presentation adapters that are not better described by a concrete host type.
- `Api`: an HTTP/gRPC-facing executable entry point or API boundary. Use a more specific qualifier only when multiple API hosts exist.
- `Worker`: background-processing executable/host.
- `Host`: a composition root when it is neither an API nor a worker.
- `Tests`: testing projects. Follow the existing stable service test-project convention described below.

`Infrastructure` describes implementation location; `Integrations` describes communication with external systems; `IntegrationEvents` describes the messages/contracts exchanged. They are not interchangeable.

Use PascalCase segments and the established spelling of technologies and protocols in this repository (for example, `Postgres`, `RabbitMq`, `AspNetCore`). Avoid inconsistent acronym casing, underscores, abbreviations that are not established, and vague names such as `Common`, `Utils`, `Helpers`, or `Misc`.

## 2. Project identity and source folders

For every .NET project, keep all of these aligned:

- Project directory name.
- `.csproj` file name (without extension).
- `AssemblyName`.
- `RootNamespace`.

They must all equal the full canonical project name. Do not use a short assembly name or a different root namespace to work around an incorrect project name.

Source subfolders are **not automatically extra project-name segments**. They form namespaces below the project root, and their names must be PascalCase and describe cohesive code areas. Create another project only when that code needs a separate compilation, dependency, deployment, ownership, or reuse boundary—not just because a folder exists.

Example:

```text
src/
  MARS.Alerts.Infrastructure.Persistence.Postgres/
    MARS.Alerts.Infrastructure.Persistence.Postgres.csproj
    Data/
    Repositories/
```

The namespaces follow the folder path under the project's `RootNamespace`.

## 3. Decide project boundaries before moving files

A project is an architectural and compilation boundary, not a folder-shaped container. Split a project only when at least one meaningful reason exists:

- A responsibility has a distinct dependency policy or must prevent references to another part of the system.
- A capability has a cohesive public contract and can be built, tested, reused, or versioned independently.
- A runtime/deployment boundary requires a separate host or executable.
- An integration/persistence adapter needs to remain replaceable without bringing its provider dependencies into the application/domain layer.
- Ownership or lifecycle is materially different and a project reference is the right enforcement mechanism.

Do **not** create a project merely because:

- A folder contains many files or classes.
- A namespace or feature has a distinct name but no independent boundary.
- Every architecture role is expected to exist in every product.
- A generic `Core`, `Common`, `Services`, or `Utils` project seems convenient.

Do not split mechanically. Small products may keep several responsibilities in fewer projects; large products may have multiple projects for one role when each has a clear, cohesive responsibility. The architecture should express actual boundaries, not an idealized count of projects.

## 4. Dependency direction

Keep references pointing inward:

- `Domain` must not depend on `Application`, `Infrastructure`, API/hosts, databases, brokers, or web frameworks.
- `Application` may depend on `Domain` and explicit contracts/abstractions it owns or consumes. It must not depend on concrete infrastructure or a host.
- `Infrastructure` implements ports owned by inner layers and may depend on those inner layers plus provider libraries. It must not depend on another product's API/host.
- `Contracts` and `IntegrationEvents` must stay free of infrastructure, application handlers, database entities, and host dependencies. They describe the boundary; they do not implement it.
- `Api`, `Presentation`, `Worker`, and `Host` are outer layers/composition roots. They may compose the required application and infrastructure projects, but inner layers must never reference them.
- Avoid project-reference cycles. If two projects need each other's implementation details, redesign the boundary or extract the minimal stable contract instead of adding a cycle.

Do not move product-owned behavior into `MARS.Platform` merely to reuse it. Platform projects may expose general-purpose technical capabilities; products own their business policies and provider-specific workflows.

## 5. Create, split, merge, move, or rename workflow

Before changing files:

1. Read the target product's existing project layout, project references, namespaces, tests, and the solution entry. Inspect the actual `.csproj` files; do not infer their contents from directory names.
2. Identify the owning scope and responsibility. Decide whether this is a product/bounded-context project, a module within a product, or a cross-cutting `Platform` capability.
3. Draw the intended project/reference graph. Explain why each new project is an independent boundary and why each reference points in the chosen direction.
4. Assign every resulting project a canonical name using the grammar above. When refactoring an existing project, bring the touched project into the canonical scheme in the same change; do not copy a legacy suffix order into new projects.
5. Preserve the distinction between domain events, integration-event contracts, integration clients/consumers, and persistence implementations.

When moving or splitting code, update all affected references and registrations, including as applicable:

- `ProjectReference` and package references.
- Namespace declarations/usages, `AssemblyName`, and `RootNamespace`.
- `MARS.slnx` and solution folder organization.
- Tests, test project references, and CI test matrices in `.github/workflows/ci.yml`.
- Docker build contexts, publish/compose configuration, container build scripts, code generation, analyzer configuration, and repository scripts that refer to project paths or assembly names.
- Dependency-injection/composition-root registrations and reflection/assembly scanning.
- Documentation and architecture diagrams.

Search for old project names, paths, namespaces, and assembly names after the change. Preserve externally observable deployment/service identities, environment keys, ports, metrics, and log labels unless the task explicitly includes changing those contracts.

### Existing test-project stability

Keep the repository's established one-test-project-per-service naming convention, currently `tests/MARS.<Service>.Tests`, and add new test projects to the `tests` matrix in `.github/workflows/ci.yml`. `MARS.TestKit` is a library, not a test project. Do not casually rename existing test projects: branch-protection and CI check names may depend on them. If a test-project rename is necessary, explicitly account for those external check-name dependencies.

### Legacy names during refactoring

Existing names may predate this grammar. They are not templates for new projects. When a project is materially refactored, renamed, split, or moved, normalize the affected project names and paths to the canonical order as part of that work. Examples of the intended normalization:

| Legacy ordering | Canonical ordering |
|---|---|
| `MARS.Platform.Persistence.Postgres.Infrastructure` | `MARS.Platform.Infrastructure.Persistence.Postgres` |
| `MARS.Platform.Messaging.RabbitMq.Infrastructure` | `MARS.Platform.Infrastructure.Messaging.RabbitMq` |
| `MARS.Modules.Alerts.Adhd.Application` | `MARS.Modules.Alerts.Application.Adhd` |
| `MARS.Alerts.Twitch.Rewards.Application` | `MARS.Alerts.Application.Twitch.Rewards` |
| `MARS.Alerts.Overlay.Application` | `MARS.Alerts.Application.Overlay` |

Do not perform an unrelated repository-wide rename just to apply this skill. Normalize every project in the scope of the current project refactor, and do not introduce new exceptions. If an unavoidable external compatibility constraint prevents a rename, document the exact constraint and migration path in the PR; do not silently declare the legacy name canonical.

## 6. Verification checklist

Before finishing a project-structure change, verify:

- [ ] Every new or refactored project follows the canonical naming grammar.
- [ ] Directory, `.csproj` filename, `AssemblyName`, and `RootNamespace` match.
- [ ] Each project has one clear responsibility and a justified boundary.
- [ ] Project references follow inward dependency direction and contain no cycles.
- [ ] Contracts/events contain contracts only; integrations and persistence contain implementations.
- [ ] Solution, tests, CI matrix, Docker/publish configuration, scripts, DI, reflection scanning, and documentation are updated where applicable.
- [ ] No stale references to old names, namespaces, or paths remain.
- [ ] Relevant build and test commands pass; run the repository verification script when the scope permits.
