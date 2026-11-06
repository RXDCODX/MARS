# План миграции: разделение MARS.Shared на 11 проектов (feat/shared-split)

## Состояние на момент начала выполнения
- Ветка `feat/shared-split` создана от `main` (~566 изменённых/перемещённых файлов).
- Split-скрипт уже запущен: 152 файлов перемещены в 11 новых папок, пространства имён переписаны.
- Старая папка `src/MARS.Shared` удалена полностью.
- Уже созданы 11 `.csproj` с зависимостями.
- Уже разделен `ServiceCollectionExtensions.cs`:
  - `AddMarsHttpClient` → `MARS.Platform.Clients.Http.Infrastructure/Extensions/HttpClientServiceCollectionExtensions.cs`
  - `AddMarsDbContext` + `AddMarsSchemaMigration` → `MARS.Platform.Persistence.Postgres.Infrastructure/Extensions/DbContextServiceCollectionExtensions.cs`
- Уже убраны три вызывающие циклы using-а (MarsSchemaMigrationHostedService → Hosting, SignalRServiceCollectionExtensions → Hosting, GrpcOptions → Grpc).

## Оставшиеся шаги

### 1. MARS.slnx — заменить MARS.Shared на 11 проектов
Удалить строку `<Project Path="src/MARS.Shared/MARS.Shared.csproj" />` и добавить 11 записей в папку `/src/`.

### 2. Все 18 сервисных .csproj — заменить ссылку на MARS.Shared
В каждом `src/MARS.<Service>/<Service>.csproj` заменить:
```xml
<ProjectReference Include="..\MARS.Shared\MARS.Shared.csproj" />
```
на ссылки на все 11 новых проектов:
```xml
<ProjectReference Include="..\MARS.Shared.Kernel\MARS.Shared.Kernel.csproj" />
<ProjectReference Include="..\MARS.Shared.Contracts\MARS.Shared.Contracts.csproj" />
<ProjectReference Include="..\MARS.Platform.Messaging.RabbitMq.Infrastructure\MARS.Platform.Messaging.RabbitMq.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Persistence.Postgres.Infrastructure\MARS.Platform.Persistence.Postgres.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Grpc.Infrastructure\MARS.Platform.Grpc.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Observability.Logging.Infrastructure\MARS.Platform.Observability.Logging.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Observability.Telemetry.Infrastructure\MARS.Platform.Observability.Telemetry.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Web.AspNetCore.Infrastructure\MARS.Platform.Web.AspNetCore.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Development.Infrastructure\MARS.Platform.Development.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Hosting.Infrastructure\MARS.Platform.Hosting.Infrastructure.csproj" />
<ProjectReference Include="..\MARS.Platform.Clients.Http.Infrastructure\MARS.Platform.Clients.Http.Infrastructure.csproj" />
```
(Самый простой и безопасный способ — все сервисы ссылаются на все 11, сервисы — листья.)

### 3. tests/MARS.Shared.Tests.csproj — обновить ссылку
Заменить:
```xml
<ProjectReference Include="..\..\src\MARS.Shared\MARS.Shared.csproj" />
```
на ссылки на все 11 новых проектов.

### 4. Все 20 Dockerfile — обновить COPY-строки
В каждом `src/MARS.<Service>/Dockerfile` заменить:
```dockerfile
COPY ["src/MARS.Shared/MARS.Shared.csproj", "MARS.Shared/"]
...
COPY ["src/MARS.Shared/", "MARS.Shared/"]
```
на копирование всех 11 проектов (сначала csproj для restore, затем папки для исходников).

### 5. docker-compose.dev.yml — обновить маунты obj/bin
Заменить пары `dev-<svc>-shared-obj` / `dev-<svc>-shared-bin` (ссылающиеся на `/src/src/MARS.Shared/...`) на маунты для 11 проектов на каждый сервис. Либо оставить общий подход с одним набором томов для новых путей.

### 6. tests/MARS.Gateway.Tests/ProjectLayeringTests.cs — убрать "MARS.Shared" из Pending
Удалить строку `"MARS.Shared"` из множества `Pending`. 11 новых проектов уже соответствуют грамматике (все Infrastructure, все с технологией, не в Pending), тест пройдёт.

### 7. tests/MARS.Shared.Tests/PublicContractTests.cs — 11 сборок
Заменить `PublicContractVerifier.VerifyAssembly("MARS.Shared")` на вызов для всех 11 новых сборок:
```csharp
var violations = new List<string>();
var inspected = 0;
foreach (var assembly in new[] {
    "MARS.Shared.Kernel",
    "MARS.Shared.Contracts",
    "MARS.Platform.Messaging.RabbitMq.Infrastructure",
    "MARS.Platform.Persistence.Postgres.Infrastructure",
    "MARS.Platform.Grpc.Infrastructure",
    "MARS.Platform.Observability.Logging.Infrastructure",
    "MARS.Platform.Observability.Telemetry.Infrastructure",
    "MARS.Platform.Web.AspNetCore.Infrastructure",
    "MARS.Platform.Development.Infrastructure",
    "MARS.Platform.Hosting.Infrastructure",
    "MARS.Platform.Clients.Http.Infrastructure",
}) {
    var result = PublicContractVerifier.VerifyAssembly(assembly);
    violations.AddRange(result.Violations);
    inspected += result.TypesInspected;
}
Assert.Empty(violations);
Assert.True(inspected > 0, "...");
```

### 8. Сборка и тесты
```bash
dotnet build MARS.slnx -c Release
dotnet test MARS.slnx -c Release
dotnet csharpier format .
```
Исправить все ошибки компиляции.

### 9. Поиск остатков MARS.Shared
PowerShell- sweep (исключая bin|obj|node_modules|.git|graphify-out|coverage-local) по строке `MARS.Shared` — убедиться, что остались только intentional comments (история, объяснения) и `MARS.Shared.Tests` / `MARS.Shared.Kernel` / `MARS.Shared.Contracts` (имена новых проектов — это ожидаемо).

### 10. graphify update .
Запустить обновление графа знаний после изменений кода.

### 11. Коммиты и PR
- Коммит 1: 11 csproj + разделение ServiceCollectionExtensions.cs (уже частично готово, но ещё не закоммичено).
- Коммит 2: MARS.slnx + csproj-ссылки сервисов + тестовый проект.
- Коммит 3: Dockerfile всех сервисов.
- Коммит 4: docker-compose.dev.yml.
- Коммит 5: ProjectLayeringTests.cs + PublicContractTests.cs.
- Коммит 6: сборка/тесты/format.
- Форматирование: `dotnet csharpier format .` перед пушем.
- Пуш в `feat/shared-split`, PR с требуемым шаблоном (Что / Зачем / Как / Проверка / Миграция и откат / Сквозные правки).

## Проверка завершения
- `dotnet build MARS.slnx -c Release` — зелёный.
- `dotnet test MARS.slnx -c Release` — зелёный.
- `dotnet csharpier check .` — пройден.
- `ProjectLayeringTests` — `"MARS.Shared"` удалён из Pending, все 11 новых проектов проходят грамматику.
- Нет ссылок на несуществующий проект `MARS.Shared` (кроме `MARS.Shared.Tests`, `MARS.Shared.Kernel`, `MARS.Shared.Contracts`).
