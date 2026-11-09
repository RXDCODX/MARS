---
name: mars-project-structure
description: Enforce MARS project naming, architectural boundaries, dependency direction, and safe project creation, splitting, merging, moving, or renaming.
---

# MARS project naming and structure

This skill is **mandatory** whenever a task creates, splits, merges, moves, renames, or materially refactors a project/assembly. Apply it to new projects and to every existing project whose structure or responsibility is being refactored. Do not use legacy project names as templates for new work.

## 1. Canonical project-name grammar

Use this exact shape for architectural projects:

```text
<Solution>.<Project>.<LogicalDivision>[.<Submodule>]
```

For MARS:

- `<Solution>` is always `MARS`.
- `<Project>` identifies the owning service, product, bounded context, or shared technical area, for example `Telegram`, `Alerts`, `MediaStorage`, `Shared`, or `Platform`.
- `<LogicalDivision>` is one of the six divisions defined below: `Domain`, `Api`, `Models`, `Application`, `Infrastructure`, or `Extensions`.
- `<Submodule>` is optional and is a single PascalCase segment that narrows the responsibility to a protocol, technology, provider, or cohesive feature, for example `Grpc`, `Rest`, `Postgres`, `GooglePhotos`, or `TwitchRewards`.

Do not add further naming levels after the optional submodule. If a name appears to need several qualifiers (for example, `Infrastructure.Persistence.Postgres`), simplify the project name to the actual distinguishing boundary (for example, `Infrastructure.Postgres`) and express finer organization through source folders and namespaces.

The project segment identifies **who owns the code**. The logical division identifies **what architectural responsibility it has**. The optional submodule identifies **which concrete protocol, technology, provider, or feature it serves**.

### Canonical examples

| Responsibility | Canonical name |
|---|---|
| Telegram domain entities | `MARS.Telegram.Domain` |
| Telegram application use cases | `MARS.Telegram.Application` |
| Google Photos use cases owned by Telegram | `MARS.Telegram.Application.GooglePhotos` |
| Telegram REST endpoints | `MARS.Telegram.Api.Rest` |
| Telegram gRPC endpoints | `MARS.Telegram.Api.Grpc` |
| Telegram REST exchange models | `MARS.Telegram.Models.Rest` |
| Telegram gRPC exchange models | `MARS.Telegram.Models.Grpc` |
| Telegram PostgreSQL implementation and migrations | `MARS.Telegram.Infrastructure.Postgres` |
| Google Photos adapter owned by Telegram | `MARS.Telegram.Infrastructure.GooglePhotos` |
| Telegram MTProto adapter | `MARS.Telegram.Infrastructure.MTProto` |
| Telegram C# extension methods, when independently reusable | `MARS.Telegram.Extensions` |
| Telegram executable composition root | `MARS.Telegram.Host` |
| Alerts integration with the external Telegram system | `MARS.Alerts.Infrastructure.Telegram` |
| Shared inter-service exchange models | `MARS.Shared.Models` |
| Shared PostgreSQL technical implementation | `MARS.Platform.Infrastructure.Postgres` |
| Shared RabbitMQ technical implementation | `MARS.Platform.Infrastructure.RabbitMq` |

The examples describe valid naming, not a requirement to create every listed project.

### Naming rules

- Use PascalCase for every segment and established technology/protocol spelling, for example `Postgres`, `RabbitMq`, `Grpc`, `Rest`, and `MTProto`.
- Do not repeat the owning project name as its own submodule. For example, use `MARS.Telegram.Infrastructure.MTProto`, not `MARS.Telegram.Infrastructure.Telegram`, when the latter would merely repeat the owner. A provider name is valid when it identifies a genuinely external system, such as `MARS.Alerts.Infrastructure.Telegram`.
- Do not use vague names such as `Common`, `Core`, `Utils`, `Helpers`, or `Misc` as substitutes for a defined responsibility.
- Do not create new naming categories such as `Integrations`, `Persistence`, `Messaging`, or `Contracts` between the project and submodule. These are source-level areas or concerns within one of the six logical divisions, not additional naming levels in this convention.
- Use `Platform` only for reusable cross-cutting technical capabilities that are not owned by one product or bounded context. Product-specific behavior and provider policies stay under their owning project, even when another product uses the same technology.
- Use `Shared` only for genuinely shared code or exchange models. It is not a dumping ground; keep its contents cohesive and minimal.
- Do not rename externally observable service identities just because a project or assembly name changes. Compose service names, environment keys, ports, metrics, and log labels may be deployed contracts; preserve them unless the task explicitly includes a compatibility migration.

### The six logical divisions

#### `Domain`

Contains the core domain model owned by the project:

- Domain entities and value objects.
- Domain enums, invariants, and business rules that belong to the model.
- Domain events where needed.

Domain entities may be persisted directly by an ORM. This does **not** make the Domain project a database project: database-specific mapping, migrations, provider configuration, and persistence implementation belong to Infrastructure.

Keep Domain independent of database frameworks, API frameworks, transport libraries, and concrete infrastructure. Do not put API DTOs or raw third-party protocol models in Domain.

#### `Api`

Contains inbound interaction endpoints:

- REST controllers and endpoints.
- gRPC services/endpoints.
- WebSocket or other inbound endpoint implementations.

API projects receive and validate transport input, translate it to application calls, and translate results to transport responses. They must not implement business scenarios or directly own persistence and provider-specific operations.

Use a submodule when the endpoint protocol or host boundary is distinct, for example `Api.Rest` and `Api.Grpc`.

#### `Models`

Contains models used to exchange data across an application boundary:

- Request and response DTOs.
- REST/gRPC transport models.
- Inter-service messages and event payload models.
- Other explicit data-exchange shapes.

Models describe data shape, not business behavior. Keep them independent of application handlers, infrastructure implementations, persistence entities, and host startup. Do not copy a Domain entity into Models automatically; define a separate exchange model only when the external contract requires a different shape or compatibility lifecycle.

Models for a third-party provider's private API belong inside the corresponding Infrastructure implementation, not in the owning project's general Models project. Put genuinely cross-project exchange models in a cohesive shared project such as `MARS.Shared.Models`.

#### `Application`

Contains application behavior and use cases:

- Business scenarios and workflows.
- Commands, queries, handlers, and orchestration.
- Application-level validation and coordination.
- Interfaces/ports that application scenarios require from external systems.
- Application-specific result and input types that are not external transport contracts.

Application depends on Domain. It must not depend on concrete Infrastructure, API endpoints, host startup, database providers, or third-party SDKs.

Use a feature submodule when the feature forms a meaningful independent boundary, for example `Application.GooglePhotos` or `Application.TwitchRewards`. A feature submodule may define the ports its own scenarios require; the matching Infrastructure submodule implements those ports.

#### `Infrastructure`

Contains technical implementations and adapters required by the project:

- Implementations of Application-owned ports.
- Database access, ORM configuration, migrations, and persistence adapters.
- Adapters to external services and provider APIs.
- Implementations based on NuGet libraries and other technical dependencies.
- Broker, filesystem, cache, network, and protocol implementations.

Use one submodule per meaningful provider, technology, or replaceable implementation, for example `Infrastructure.Postgres`, `Infrastructure.GooglePhotos`, `Infrastructure.MTProto`, or `Infrastructure.RabbitMq`.

Infrastructure may reference **both Application and Domain**:

- Reference Application to implement application-owned interfaces and use application contracts.
- Reference Domain directly when required to persist, map, load, or otherwise implement technical behavior for domain entities.
- A direct Infrastructure-to-Domain reference is valid and must not be treated as an architectural violation.
- Infrastructure must not force database-specific or provider-specific dependencies into Domain or Application.

Infrastructure projects should not depend on sibling Infrastructure projects merely to reuse implementation details. Prefer having the composition root connect independent adapters through Application ports. Extract genuinely reusable technical components into an appropriately owned `MARS.Platform.Infrastructure.<Submodule>` project when that creates a real independent boundary.

Keep business decisions and use-case orchestration in Application. An adapter may contain technical protocol handling, provider-specific translation, retries, and error mapping, but it must not become the owner of the overall business workflow.

#### `Extensions`

Contains C# extension methods for types and APIs.

Create a separate Extensions project only when the extension methods form a cohesive and independently reusable library. Extensions that are specific to one API, Infrastructure adapter, or host should normally stay inside that owning project (for example, DI registration extensions can live alongside the implementation they register).

Do not use Extensions as a general-purpose `Common`, `Utils`, or miscellaneous helper project. Do not place ordinary services, business logic, models, or dependency-injection composition roots here merely because they are exposed through extension methods.

### Special executable project: `Host`

An executable composition root is a special project type, not a seventh logical division. Name it `MARS.<Project>.Host` when it is useful to keep startup and composition separate from API and library projects.

Host is responsible for:

- Application startup and shutdown.
- Dependency-injection composition and implementation registration.
- Configuration binding and environment-specific setup.
- Middleware and endpoint wiring.
- Selecting and composing the required API, Application, and Infrastructure projects.

Host may reference the projects it composes. It must not become a home for business scenarios or provider implementations. A small service may keep its host and API in one executable when there is no meaningful independent boundary; do not split solely to satisfy a naming template.

## 2. Project identity and source folders

For every .NET project, keep all of these aligned:

- Project directory name.
- `.csproj` file name (without extension).
- `AssemblyName`.
- `RootNamespace`.

They must all equal the full canonical project name. Do not use a short assembly name or a different root namespace to work around an incorrect project name.

Source subfolders are **not automatically extra project-name segments**. They organize code and namespaces inside a project. Use PascalCase names and group files by cohesive responsibility, for example:

```text
src/
  MARS.Telegram.Infrastructure.Postgres/
    MARS.Telegram.Infrastructure.Postgres.csproj
    Data/
    Configurations/
    Migrations/
    Repositories/
```

Folders such as `Persistence`, `Messaging`, `Clients`, `Authentication`, or `Mapping` are allowed when they clarify internal organization. They do not need to appear in the project name.

Create another project only when code needs a separate compilation, dependency, deployment, ownership, or reuse boundary—not merely because a folder exists.

## 3. Decide project boundaries before moving files

A project is an architectural and compilation boundary, not a folder-shaped container. Split a responsibility into its own project only when at least one meaningful reason exists:

- It has a distinct dependency policy that must be enforced by the compiler.
- It has a cohesive public contract and can be built, tested, reused, or versioned independently.
- It has a separate runtime/deployment boundary.
- A provider-specific adapter must be replaceable without bringing its dependencies into Application or Domain.
- Ownership or lifecycle is materially different and a project reference is the right enforcement mechanism.

Do **not** create a project merely because:

- A folder contains many files or classes.
- A feature has a distinct name but no independent boundary.
- Every project is expected to contain all six logical divisions.
- A technology, provider, or protocol can be represented by a folder inside an existing project.
- A generic `Core`, `Common`, `Utils`, or `Extensions` project seems convenient.

Do not split mechanically. Small projects may keep several responsibilities in fewer assemblies; larger projects may have multiple projects under one logical division when each has a clear boundary. A logical division may be represented by a folder/namespace instead of an assembly. The architecture should express actual boundaries, not an idealized count of projects.

### When to use feature-specific Application and Infrastructure projects

A pair such as:

```text
MARS.Telegram.Application.GooglePhotos
MARS.Telegram.Infrastructure.GooglePhotos
```

is appropriate when the feature has enough independent behavior or dependencies to justify separate assemblies.

- Application.GooglePhotos owns the use cases and required ports/interfaces.
- Infrastructure.GooglePhotos implements the ports and contains Google-specific technical code.
- The Application project must not reference Infrastructure.
- Infrastructure may reference Application and Domain directly.
- The two projects are connected by the Host/composition root.

If the feature is small and has no independent compilation or dependency boundary, use folders such as `Application/GooglePhotos` and `Infrastructure/GooglePhotos` inside their parent projects instead. Never split a feature solely to make the project names symmetrical.

## 4. Dependency direction

Keep dependencies pointing toward the inner policy/model, with the explicitly allowed Infrastructure-to-Domain dependency:

- `Domain` has no project references to Application, Infrastructure, Api, Models, Host, database providers, brokers, or web/transport frameworks.
- `Models` should remain plain exchange shapes and must not depend on Application handlers, Infrastructure, persistence entities, or Host. Avoid referencing Domain from Models merely to reuse entities; map between the domain and exchange shapes at the boundary.
- `Application` may depend on Domain and on a genuinely required stable exchange/shared model project. It must not depend on concrete Infrastructure, Api, Host, database providers, or third-party SDKs.
- `Api` may depend on Application and the Models needed for its transport boundary. It must not depend on concrete Infrastructure or contain business workflows.
- `Infrastructure` may depend directly on Application and Domain. It implements Application-owned ports where applicable and may use Domain entities directly for persistence and mapping. It may also depend on provider libraries. Avoid references to sibling Infrastructure projects.
- `Host` is the composition root and may reference the API, Application, Models, and Infrastructure projects it composes. Inner projects must never reference Host.
- `Extensions` is not a general architectural layer. Its references must remain limited to the types it extends and must not introduce cycles or become a route for bypassing the dependency rules.

Application feature projects may depend on the root Application project when they need shared application contracts or services, but the root Application project must not depend back on feature projects. Prefer Domain and clearly owned Application ports for communication between independently deployable adapters and use cases.

Avoid project-reference cycles. If two projects need each other's implementation details, redesign the boundary or extract the smallest stable interface/model to the correct inner owner instead of adding a cycle.

Do not move product-owned behavior into `MARS.Platform` merely to reuse it. Platform projects may expose general-purpose technical capabilities; products own their business policies and provider-specific workflows.

## 5. Create, split, merge, move, or rename workflow

Before changing files:

1. Read the target project's actual layout, project references, namespaces, tests, solution entry, and relevant startup/composition code. Inspect the actual `.csproj` files; do not infer their contents from directory names.
2. Identify the owning project and responsibility. Decide whether the code belongs to Domain, Api, Models, Application, Infrastructure, Extensions, or the special Host composition root.
3. Draw the intended project/reference graph. Explain why each new project is an independent boundary and why each reference points in the chosen direction. Include direct Infrastructure-to-Domain references when persistence or domain mapping requires them.
4. Assign every resulting project a canonical name using the grammar above. Use no more than one optional submodule segment. When refactoring an existing project, bring the touched project into the canonical scheme in the same change; do not copy a legacy suffix order into new projects.
5. Preserve the distinction between domain entities, external exchange models, application use cases/ports, inbound API endpoints, and technical adapter implementations.
6. Prefer folders over new projects when there is no meaningful assembly boundary.

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

Existing names may predate this grammar. They are not templates for new projects. When a project is materially refactored, renamed, split, or moved, normalize the affected project names and paths to the canonical order as part of that work.

Examples:

| Legacy/overly nested name | Preferred name |
|---|---|
| `MARS.Telegram.Infrastructure.Integrations.GooglePhotos` | `MARS.Telegram.Infrastructure.GooglePhotos` |
| `MARS.Telegram.Infrastructure.Integrations.Telegram` | `MARS.Telegram.Infrastructure.MTProto` (when it implements MTProto) |
| `MARS.Telegram.Infrastructure.Persistence.Postgres` | `MARS.Telegram.Infrastructure.Postgres` |
| `MARS.Alerts.Application.Twitch.Rewards` | `MARS.Alerts.Application.TwitchRewards` |
| `MARS.Platform.Infrastructure.Messaging.RabbitMq` | `MARS.Platform.Infrastructure.RabbitMq` |
| `MARS.Platform.Infrastructure.Persistence.Postgres` | `MARS.Platform.Infrastructure.Postgres` |

Normalize only projects in the scope of the current refactor. Do not perform an unrelated repository-wide rename just to apply this skill, and do not introduce new exceptions. If an unavoidable external compatibility constraint prevents a rename, document the exact constraint and migration path in the PR; do not silently declare the legacy name canonical.

## 6. Verification checklist

Before finishing a project-structure change, verify:

- [ ] Every new or refactored architectural project follows `MARS.<Project>.<LogicalDivision>[.<Submodule>]`; Host uses the documented special form.
- [ ] No project name has more than one optional submodule segment.
- [ ] Directory, `.csproj` filename, `AssemblyName`, and `RootNamespace` match.
- [ ] Each project has one clear responsibility and a justified boundary; folders are used where a separate assembly is unnecessary.
- [ ] Domain remains independent of technical frameworks and infrastructure.
- [ ] Application contains use cases and ports, not concrete provider implementations.
- [ ] Infrastructure references Application and/or Domain as required, with no Infrastructure-to-Infrastructure coupling unless a separately justified shared technical boundary is extracted.
- [ ] API endpoints delegate business behavior to Application; Models remain exchange shapes.
- [ ] Host composes dependencies without owning business logic.
- [ ] Project references follow the dependency rules and contain no cycles.
- [ ] Solution, tests, CI matrix, Docker/publish configuration, scripts, DI, reflection scanning, and documentation are updated where applicable.
- [ ] No stale references to old names, namespaces, or paths remain.
- [ ] Relevant build and test commands pass; run the repository verification script when the scope permits.
