# AGENTS.md

14 микросервисов на .NET 10, обмен через RabbitMQ, единая входная точка — YARP-Gateway.
Монолит вынесен в отдельный репозиторий `MARS_old`; его путей здесь нет.

## Команды

```bash
dotnet build MARS.slnx -c Release
dotnet test MARS.slnx

# один сервис / один набор тестов
dotnet build src/MARS.MediaStorage/MARS.MediaStorage.csproj -c Release
dotnet test tests/MARS.MediaStorage.Tests/MARS.MediaStorage.Tests.csproj -c Release

# EF: scaffold миграции (фабрика в src/MARS.X/Data/DesignTime/*)
dotnet ef migrations add Name --project src/MARS.TwitchCore/MARS.TwitchCore.csproj

# форматирование (локальный tool)
dotnet csharpier . && dotnet csharpier --check .

# UI хранилища (React/Vite)
cd src/MARS.MediaStorage/ClientApp && npm ci && npm run typecheck
```

### Фильтрация тестов — ловушка

`global.json` включает `"test": { "runner": "Microsoft.Testing.Platform" }`, а проекты
на xunit.v3 имеют `OutputType=Exe`. VSTest-выражение `--filter` **молча находит ноль тестов**
и выходит с кодом 8:

```bash
# правильно:
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-class "*HealthCheck*"
dotnet test ... -- --filter-method "*Namespace.Class.Method"
dotnet test ... -- --filter-namespace "*Media*"
dotnet test ... -- --filter-trait "key=value"
```

Вывод тестов локализован: `итог`, `сбой`, `успешно`, `пропущено`, `Пройден!`.

### Покрытие одного проекта локально

```bash
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- \
  --coverlet --coverlet-output-format cobertura \
  --coverlet-include "[MARS.*]*" --coverlet-exclude "[*.Tests]*" \
  --coverlet-exclude-by-file "**/Migrations/**"
```

Слияние и порог — как в CI: `dotnet reportgenerator -reports:"..." -targetdir:coverage-report`
и `python .github/scripts/coverage-gate.py --merged coverage-report/Cobertura.xml --threshold-methods 95`.

## Линтер / типы / CI

- **Триггер CI — push в `main`, PR в `main`, `workflow_dispatch`**
  (`.github/workflows/ci.yml`), отдельно от публикации образов
  (`release-microservices.yml`, триггер — тег `v*`). Задачи: `build`,
  `tests` (матрица по всем 16 тестовым проектам, `fail-fast: false`), `coverage`.
- **Отдельный статус на каждый тестовый проект** получается из матрицы:
  `tests / MARS.Gateway.Tests` — самостоятельный check в branch protection.
  Новый тестовый проект ⇒ запись в матрицу `tests` в `ci.yml`, иначе он не
  проверяется вообще и это молчаливо.
- **Задача `coverage` падает всегда: порог 95% методов, фактически 4.7%**
  (108 из 2321). Это задуманный ориентир, а не поломка сборки: `tests` и `build`
  зелёные. `coverage` запускается только при `needs.tests.result == 'success'` —
  иначе падали бы две задачи по одной причине, а покрытие считалось бы по неполным
  данным.
- Покрытие снимает `coverlet.MTP` (MTP v2). `coverlet.collector` и датаколлекторы
  VSTest с MTP несовместимы, `Microsoft.Testing.Extensions.CodeCoverage` требует
  MTP 18.x. Опции тест-приложения — после `--`.
- **`--coverlet-include "[MARS.*]*"` обязателен**: без него coverlet берёт и чужие
  сборки (HealthChecks, YARP, Serilog), отчёт распухает до тысяч строк. Плюс
  `--coverlet-exclude "[*.Tests]*"` и `-by-file "**/Migrations/**"`.
- **Отчёты по проектам нельзя складывать**: `MARS.Shared` инструментируется в
  каждом тестовом проекте и посчитался бы 16 раз. Слияние делает ReportGenerator
  (объединением покрытых строк), порог по слитому `Cobertura.xml` считает
  `.github/scripts/coverage-gate.py`. Пустой набор отчётов → ошибка, не 0%.
- Порог считается **по методам**: в Cobertura покрытие методов есть только в узлах
  `<methods>`, а `line-rate`/`branch-rate` в `<coverage>` методов не содержат.
  У `<method>` нет атрибута `covered` — покрытым считается метод, у которого хотя
  бы одна вложенная `<line hits>` > 0. Методы без строк в знаменатель не идут.
- **Форматирование автофиксится, а не проверяется**: `.github/workflows/auto-format.yml`
  на `main` и PR в `main` гоняет `dotnet csharpier .` и сам коммитит результат
  (`style: автоформатирование CSharpier`). На feature-ветках и в форках не
  наезжает. Локально то же: `dotnet csharpier .`, проверка — `dotnet csharpier --check .`.
- `TreatWarningsAsErrors` **не включён** (нет `Directory.Build.props`), предупреждения
  компиляции не роняют сборку. `.editorconfig` поднимает только `ASP0014` до warning.
- Форматирование: `dotnet csharpier <file>` — локальный tool из `.config/dotnet-tools.json`
  (`rollForward: false`). Конфига `.csharpierrc` в репозитории нет.
- `dotnet-ef` в манифесте tools **нет** (только csharpier и reportgenerator) — стоит глобально.
  Предупреждение «tools version 10.0.8 is older than runtime 10.0.10» — норма, не чинить.
- Коммиты: conventional-коммиты с русским описанием (`feat:`, `chore:`, `docs:`).

## Границы пакетов и точка входа

- `src/MARS.Shared` — единственная общая библиотека. Всё, что нужно двум сервисам
  и не должно тянуть проектную ссылку, живёт здесь.
- `tests/MARS.X.Tests` ↔ `src/MARS.X` — один тестовый проект на сервис, набор ссылок
  повторяет набор зависимостей сервиса. Новый сервис ⇒ новый проект в `tests/` + запись
  в обе папки (`/src/` и `/tests/`) `MARS.slnx`.
- Неймспейсы всегда `MARS.*`, независимо от имён папок.
- **Хабы — только в `MARS.Shared`** (`TelegramusHub`, `TunaHub`). Нельзя ссылаться
  `MARS.OBS` → `MARS.Alerts`: publish падает с NETSDK1152 из-за дублей `appsettings.json`.
- `MARS.Videos365` — асимметричный сервис: только воркер, без контроллеров, без кластера
  и маршрута в YARP, без записи в `ServiceEndpoints`. Матрица release-workflow его тоже
  не публикует. Не ищи в нём HTTP-поверхности.

## Секреты пакетов (CPM)

Версии живут **только** в корневом `Directory.Packages.props`; в `.csproj` остаётся голое
`PackageReference` без `Version` — иначе NU1008. Новую зависимость добавлять в оба файла.

Каждый `Dockerfile` копирует в контекст только `.csproj` и обязан иметь
`COPY ["Directory.Packages.props", "./"]` **до** `dotnet restore` — иначе restore в образе
не найдёт версий.

## Базы данных: изоляция по сервису

На каждый сервис — своя база, своя роль и своё имя строки подключения. Схемы создаёт
EF Core при применении миграций; `infrastructure/db-init/01-databases.sh` создаёт только
базы и роли и выполняется официальным entrypoint'ом postgres **на пустом томе**
(идемпотентно; пересоздать с потерей данных — только `MARS_DB_INIT_RESET=true`).

Имя строки передаётся в два места и **обязано совпадать**:

```csharp
builder.AddMarsDefaults("MARS.TwitchCore", "TwitchDb");
builder.Services.AddMarsDbContext<TwitchDbContext>(builder.Configuration, "twitch", "TwitchDb");
```

Расхождение даёт зелёный readiness при недоступной базе, из которой сервис читает данные.
Сервисы без базы (Gateway, Commands, Discord, OBS, TTS) передают `null` — тогда проверка
`postgresql` не регистрируется вовсе.

## Миграции применяются синхронно

В каждом `Program.cs` вызов `app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult()`
стоит **до** `app.Run()`:

```csharp
app.UseMarsDefaults();
app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();
app.Run();
```

Иначе фоновые сервисы успевают обратиться к несуществующим таблицам (42P01). Не переносить
в `IHostedService` и не выносить после `app.Run()`.

`MARS.Admin` — единственный сервис с нестандартной схемой: логи не хранятся в БД.
Эндпоинтов `/api/Logs` и `/hubs/logger` не существует. Логи всех сервисов уходят
в **Loki**, доставляет их **Grafana Alloy** из `stdout` контейнеров
(`infrastructure/alloy/config.alloy`) — сами приложения никуда логи не шлют,
`AddMarsLogging` пишет только в Console и OTLP→Tempo. Не ищи в коде синка Loki:
его нет, и `Serilog.Sinks.Seq` тоже выпилен. Правки лог-пайплайна идут отсюда.

Seq в стеке больше нет, отдельного UI логов нет: всё смотрится в Grafana
(Explore → Loki). **Grafana опубликован на `30000`, не на 3000** — порт 3000
занят контейнером `cryptpad` из чужого проекта, и `docker compose up` на
grafana падал с `Bind for 0.0.0.0:3000 failed`; внутри сети остаётся 3000.
Датасорсы Grafana — по одному на файл, с явным `uid`; дашборд `mars-overview`
лежит в папке `MARS`. В `dashboards.yml` **не включай обратно
`foldersFromFilesStructure`** — флаг перебивает `folder: MARS`, и дашборд из
корня каталога уезжает в General. Дашборд обязан иметь корневой `title`:
Grafana отклоняет файл с «Dashboard title cannot be empty».

Образы наблюдаемости запинены (`prom/prometheus:v2.48.0`,
`grafana/loki:2.9.0`, `grafana/grafana:10.2.0`, `grafana/tempo:3.1.0`,
`grafana/alloy:v1.20.1` — у Alloy тег **с префиксом `v`**, у Tempo **без**).

### Tempo, а не Jaeger

Трейсы принимает Tempo (`tempo:4317`, HTTP API `tempo:3200`), Jaeger из стека
выпилен. Три ловушки, все проверены на живой сборке:

- **В Tempo 3.x нет блоков `ingester` и `compactor`** — их заменили `live_store`
  и `backend_scheduler`. Конфиг из документации Tempo 2.x на 3.1 не подходит.
  Минимальный набор: `distributor.receivers.otlp.protocols.grpc.endpoint` плюс
  `storage.trace` (`backend: local`, `local.path`, `wal.path`).
- **Без флага `-config.file` Tempo стартует и падает** на
  `unknown backend ""`: дефолтного пути конфига в образе нет. В `compose`
  флаг задан явно; проверять конфиг можно одним `docker run`.
- **Трейсы находятся в поиске не сразу** (WAL → блок, около минуты). Jaeger
  держал их в памяти и отдавал мгновенно — нулевой `/api/search` сразу после
  отправки это норма, а не поломка.
- **Корреляция trace → logs в `tempo.yml` НЕ включена** намеренно: обе механики
  сейчас не работают. `tags` сопоставляет `service.name` (`MARS.TwitchCore`) с
  лейблом Loki `container` (`twitch-core`) — значения не совпадают; а
  `filterByTraceID` требует trace ID в тексте лога, но `ActivityEnricher` кладёт
  его в свойства Serilog, а шаблон Console в `SerilogExtensions` не рендерит
  `{Properties}`. Включать корреляцию, молча возвращающую пустоту, нельзя.

Панели дашборда строятся на реальных именах prometheus-net
(`http_requests_received_total`, `http_request_duration_seconds`,
`process_cpu_seconds_total`, `system_runtime_dotnet_*` — последние **без**
суффиксов `_sum`/`_total`). Имена метрик, которые отдаёт сам OTel, в дашборде
не появятся: у OTel-пайплайна в `OpenTelemetryExtensions` есть только трейсы,
экспортёра метрик нет — метрики идут через prometheus-net и мост.

### Имена метрик — точка ловушек

`MarsMetrics` задаёт инструменты с точками (`mars.rabbitmq.consumed`), а
prometheus-net валидирует имя как `^[a-zA-Z_][a-zA-Z0-9_]*$` и **бросает
`ArgumentException`**. `OpenTelemetryPrometheusBridge.ToPrometheusName` нормализует
имя; без неё исключение уходит в вызывающий код — в `RabbitMqConsumerBase` падение
счётчика после успешного `BasicAckAsync` попадает в `catch` обработки сообщения и
засчитывается как ошибка с ретраем и DLQ. Регрессия закрыта тестом
`tests/MARS.Shared.Tests/Telemetry/OpenTelemetryPrometheusBridgeTests.cs`.
Новый инструмент в `MarsMetrics` с точкой в имени — норма, но проверь, что его
видно в `/metrics`.

## RabbitMQ

- Обмен — `mars.events`. Очереди и все routing key'ы централизованы в
  `src/MARS.Shared/Messaging/RabbitMqConfig.cs` — новое событие добавляется туда,
  а не литералом в сервисе.
- **Один владелец Twitch-подключения.** Только `MARS.TwitchCore` держит IRC/EventSub
  (Twitch отключает более старое из двух). Остальные публикуют `twitch.chat.send`,
  `twitch.reward.*` и читают результат.
- Routing key награды строится `RabbitMqConfig.RewardKey(name)` — только lowercase
  буквы/цифры. Используется и publishers, и тестами; ручной key разойдётся.
- `RabbitMqConsumerBase` ограничивает попытки и складывает poison-сообщения в
  `<queue>.dlq`. Не заменять на бесконечный requeue.
- `twitch.reward.redeemed` разбирает `TwitchMediaAlerts`; биндинг по `twitch.reward.#`
  заставил бы `RewardAlertConsumer` получать те же сообщения повторно.

## Gateway (YARP)

Маршруты и кластеры — `src/MARS.Gateway/appsettings.json`. Cluster address = docker
service name + `:8080` для всех. Наружу опубликован только Gateway (`9155:8080`).

Swagger-агрегатор строит карту рефлексией по строковым свойствам `ServiceEndpoints`
(`MARS.Shared/Configuration/ServiceEndpoints.cs`), а не по своему списку: опечатка в имени
свойства молча уронила бы маршрут. Новый эндпоинт ⇒ правило в `Yarp:Routes` + кластер +
свойство в `ServiceEndpoints`.

`src/README.md` устарел: там перечислены порты 5001–5013 (они есть только в
`docker-compose.dev.yml`), нет `MARS.Videos365`, и написано, что CPM отключён —
это неверно. `MARS_GATEWAY` использует `Gateway:MaxRequestBodyBytes` (256 МБ по умолчанию):
лимит применяется до проксирования, уменьшать его нельзя без проверки загрузок в хранилище.

## Docker

- `docker-compose.yml` — production-образы. `docker-compose.dev.yml` подключается
  **явно** (`-f docker-compose.yml -f docker-compose.dev.yml`) и использует
  `target: dev` c `dotnet watch`. Он переименован из `docker-compose.override.yml`
  намеренно, чтобы обычный `docker compose up` не подхватывал hot-reload молча.
- Каждый `Dockerfile` ставит `curl` в базовый stage — compose-healthcheck'и его требуют
  (в `mcr.microsoft.com/dotnet/aspnet` его нет, иначе exit code 127 → unhealthy).
  `MARS.MediaStorage` дополнительно ставит `git` для синка wwwroot.
- Том `mars-wwwroot` общий для `obs`, `alerts`, `media-storage`: конвейер
  «алерт → файл» пересекает эти три контейнера, потеря тома рвёт его.
- Данные git-метаданных `media-storage` вынесены в отдельный том: пустой volume поверх
  `/app/wwwroot/.git` замаскировал бы каталог и сломал клонирование.
- `.env` в git не попадает, `.env.example` попадает; compose читает `.env` автоматически.
  `guest/guest` для RabbitMQ недопустим (брокер пускает guest только с loopback).

## UI хранилища

`src/MARS.MediaStorage/ClientApp` (React + Vite) собирается **внутри образа**
(`node:24-alpine` stage → `ui-dist/`, gitignored) и отдаётся на `/storage-ui`.
Статику класть в `wwwroot` нельзя: это git-версионируемый том хранилища, ребилд породил бы
коммит с минифицированными файлами, а файлы попали бы в таблицу записей как «медиа».

## Сквозная правка: не забудь остальные места

Самая частая ошибка здесь — переименовать или выпилить что-то в «своих» файлах и
оставить хвосты в остальных. Собственный diff этого не показывает: `.env.example`
просто не входит в список изменённых, а стенд при этом работает. Так потерялись
`SEQ_ADMIN_PASSWORD`, `Loki__Url` и `LogsDb` при выпиливании Seq, и `.env.example`
при переходе Jaeger→Tempo.

Меняй **вместе** (проверено на двух заменах — обе оставили хвост):

| Что меняешь | Обязательно тронуть |
|---|---|
| Переменную окружения | `.env.example`, `docker-compose.yml` (env сервиса или `x-service-env`), `src/*/appsettings*.json`, код `configuration["…"]`, таблица env в `README.md` |
| `ConnectionStrings__X` | `AddMarsDefaults` **и** `AddMarsDbContext` (имена обязаны совпасть), оба `appsettings*.json`, compose |
| Компонент стека (образ/контейнер) | сервис и тома в `docker-compose.yml`, файл в `infrastructure/grafana/datasources/`, `.env.example`, README, комментарии |
| Публикуемый порт | compose, README, `docker-compose.dev.yml` |
| Имя метрики в `MarsMetrics` | PromQL в `mars-overview.json`, проверка в `/metrics` |
| БД нового сервиса | `MARS_*_PASSWORD` в `.env.example`, список `dbs` в `infrastructure/db-init/01-databases.sh`, таблица БД в README, `Data/DesignTime/*DbContextFactory` |
| Пакет NuGet | `Directory.Packages.props` **и** `.csproj` (иначе NU1008) |
| Новый сервис | `MARS.slnx` (папки `/src/` и `/tests/`), `tests/MARS.X.Tests`, `Dockerfile`, compose, таргеты в `infrastructure/prometheus/prometheus.yml`, `ServiceEndpoints.cs`, `Yarp:Routes` + кластер, матрица release-workflow |
| Новый тестовый проект | `MARS.slnx` (папка `/tests/`), `PackageReference` `coverlet.MTP`, матрица `tests` в `.github/workflows/ci.yml` |

Перед завершением прогони sweep по **старому** имени, переменной или порту:

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.json,*.yml,*.yaml,*.md,*.props,*.csproj,*.env,*.example,*.alloy,*.slnx -LiteralPath . |
  Where-Object { $_.FullName -notmatch '\\(obj|bin|node_modules|\.opencode|\.mimocode|TestResults|\.git)\\' } |
  Select-String -Pattern "СТАРОЕ_ИМЯ" -CaseSensitive:$false |
  ForEach-Object { "$($_.Path.Replace((Get-Location).Path + '\','')):$($_.LineNumber)" }
```

`git grep` для этого **не годится**: он ищет только по индексированным файлам, а
новые в индекс не попали. `git grep --untracked` на этой машине возвращает 0
совпадений вместо того, чтобы добавить неотслеживаемые, — не рассчитывай на него.

Совпадения после sweep бывают трёх видов, и это нормально:

- объясняющий комментарий («раньше здесь был…») — оставить;
- «почему так» в `README.md` / `AGENTS.md` — оставить;
- **рабочая ссылка**: env-переменная, имя сервиса, порт, путь, имя образа — удалить.

### Ловушки, которые sweep не ловит

- **Grafana не удаляет датасорсы, которых больше нет в файлах** — провижининг только
  добавляет и обновляет. После удаления `jaeger.yml` запись `jaeger` осталась в
  `mars_grafana_data`, а удалить через API нельзя (403 на провижиненный). Лечится
  только пересозданием тома: `docker compose stop grafana`, `docker rm -f mars-grafana-1`,
  `docker volume rm mars_grafana_data`. С дашбордами то же самое, и они ещё не
  переезжают в папку — `DELETE` даёт 400/404.
- **`01-databases.sh` выполняется только на пустом томе.** Добавил строку в
  `.env.example` — базы не появятся, пока не пересоздан `mars_postgres_data`.
  Правка `.env.example` не чинит запущенный стек: compose читает `.env`, а он в git
  не попадает. Пустая `MARS_*_PASSWORD` останавливает скрипт до любых изменений.
- **`.env` в git не входит, но sweep его видит.** Мёртвая переменная в `.env`
  всплывёт поиском — удалять её вручную, молча не правь файл с секретами.

## Устаревшие источники — не использовать

`.mimocode/skills/` и `.mimocode/plans/` переехали из монолита и **не соответствуют этому
репозиторию**. Они ссылаются на несуществующие `MARS.Projects/MARS.Server`, `MARS.Tests`,
`mars.client`, `PostgresFixture`, `TwitchTestHelper`, `docker-compose.override.yml`,
`yarn build-api`, `yarn type-check` и `--configuration Release` «как в CI». Ничего из этого
здесь не работает — проверяй по коду.

Достоверные источники: корневой `README.md`, `MARS.slnx`, `Directory.Packages.props`,
`global.json`, `docker-compose*.yml`, `.config/dotnet-tools.json` и код `src/MARS.Shared`.
`src/MIGRATION_CHECKLIST.md` — исторический статус переноса из монолита, не спецификация.
