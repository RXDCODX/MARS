# MARS — микросервисы

Новая архитектура MARS: 14 микросервисов на .NET 10, обмен через RabbitMQ,
единая точка входа — Gateway на `:9155`.

Монолитная архитектура вынесена в отдельный репозиторий [`MARS_old`](https://github.com/RXDCODX/MARS_old).

## Структура

```
.
├── src/
│   ├── MARS.Shared/              общий код: RabbitMQ, gRPC, HTTP-клиенты, auth, health-checks
│   ├── MARS.Gateway/             YARP: единая точка входа + Swagger-агрегатор
│   │   └── ClientApp/            клиент: оверлеи, админка и сайт (React + Vite)
│   ├── MARS.TwitchCore/          единственный владелец Twitch IRC/EventSub
│   ├── MARS.WaifuGacha/          роллы, кулдауны, супруги, авто-приветствия
│   ├── MARS.Telegram/  MARS.Discord/  MARS.Commands/
│   ├── MARS.SoundRequest/  MARS.TTS/  MARS.OBS/  MARS.Alerts/
│   ├── MARS.Scoreboard/  MARS.CinemaQueue/  MARS.MediaStorage/
│   ├── MARS.Videos365/              конвейер публикации видео в Telegram
│   ├── MARS.Shikimori/             клиент Shikimori, рейт-лимитер, персонажи
│   └── MARS.Admin/               админ-API (закрыт X-Api-Key)
├── tests/                         по одному тестовому проекту на каждый сервис из src/
│   ├── MARS.Shared.Tests/  MARS.Gateway.Tests/  MARS.TwitchCore.Tests/
│   ├── MARS.MediaStorage.Tests/  MARS.WaifuGacha.Tests/
│   └── MARS.Admin.Tests/  MARS.Alerts.Tests/  MARS.CinemaQueue.Tests/
│       MARS.Commands.Tests/  MARS.Discord.Tests/  MARS.OBS.Tests/
│       MARS.Scoreboard.Tests/  MARS.SoundRequest.Tests/  MARS.Telegram.Tests/
│       MARS.TTS.Tests/  MARS.Videos365.Tests/  MARS.Shikimori.Tests/
│   ├── MARS.TestKit/             общие проверки контракта публичной поверхности (не тесты)
│   └── MARS.ClientUi.Tests/      навигация по клиенту в браузере (Playwright)
├── scripts/                       готовые команды: сборка, тесты, стенд, релиз
│   ├── windows/                   PowerShell 5.1
│   └── unix/                      bash 3.2+
├── Directory.Packages.props       версии всех NuGet-пакетов репозитория (CPM)
├── infrastructure/               Prometheus, Grafana, Loki, Alloy, db-init
├── docker-compose.yml            16 сервисов + Postgres, RabbitMQ, Grafana, Tempo, Loki, Alloy
├── docker-compose.dev.yml        dev-переопределение с dotnet watch
├── .env.development.example      шаблон переменных для стенда разработки
├── .env.production.example       шаблон переменных для боевого стенда
├── .env.example                  перечень ключей со значениями стенда
└── .github/workflows/            CI (сборка/тесты/покрытие), автоформат, публикация образов
```

## Быстрый старт

```bash
# боевой стенд: docker-compose.yml, ASPNETCORE_ENVIRONMENT=Production
cp .env.production.example .env.production   # заполнить секреты
docker compose --env-file .env.production up -d --build

# стенд разработки: docker-compose.dev.yml, ASPNETCORE_ENVIRONMENT=Development
cp .env.development.example .env.development
docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

Файл по умолчанию `.env` compose не читает: переменные приходят только из
переданного `--env-file`. Без флага стенд поднимется на значениях по умолчанию,
то есть на пустых паролях, и `db-init` остановится на первой проверке. Тот же
выбор делают скрипты — по переключателю `-Dev`:

```powershell
.\scripts\windows\stack.ps1 -Action Up          # боевой стенд
.\scripts\windows\stack.ps1 -Action Up -Dev     # стенд разработки
```

Стек поднимается на 22 сервиса. Проверка:

```bash
docker compose ps                          # все должны быть healthy
curl http://localhost:9155/health         # Gateway
curl http://localhost:9155/               # клиент: оверлеи, админка и сайт
```

Наружу открыт только Gateway на 9155. Клиент собирается отдельно (Node → Vite →
nginx) и живёт в контейнере `client-ui`, который публикует пустую раздачу на
внутришней сети; маршрут `spa` в `appsettings.json` отдаёт его с корня.

## Наблюдаемость

Grafana — единственный интерфейс: метрики, логи и трейсы в одном месте.

| Панель | Источник | Порт | Чем наполняется |
|---|---|---|---|
| Метрики | Prometheus | `9090` | скрапит `/metrics` с 14 сервисов (`prometheus-net` в `UseMarsDefaults`) |
| Логи | Loki | `3100` | Alloy читает `stdout` контейнеров и пушит в Loki push-API |
| Трейсы | Tempo | `3200` | сервисы шлют OTLP на `Otlp__Endpoint` (`tempo:4317`) |
| UI | Grafana | **`30000`** | Explore → Prometheus / Loki / Tempo |

Grafana слушает 30000, а не 3000: порт 3000 держит контейнер `cryptpad` из
другого проекта, и `docker compose up` падал с
`Bind for 0.0.0.0:3000 failed`. Внутри сети Grafana остаётся на 3000.

Метрики идут через prometheus-net, а не через OpenTelemetry: `WithMetrics(...)`
в `OpenTelemetryExtensions` был удалён, потому что в нём не было экспортёра —
инструменты создавались и никуда не уходили. OTel отвечает только за трейсы.

Проверка после `docker compose up -d`:

```bash
curl -s localhost:30000/api/health
curl -s "localhost:3100/loki/api/v1/labels"        # container, project
curl -s localhost:3200/ready                       # Tempo
curl -s "localhost:9090/api/v1/targets" | head -c 200
```

### Tempo вместо Jaeger

Tempo заменил Jaeger (`grafana/tempo:3.1.0`): health-check datasource Jaeger
в Grafana 10.x отдавал 500 (`[plugin.unavailable]`), а Tempo — штатный источник
трейсов у Grafana. Конфиг — `infrastructure/tempo/tempo.yaml`, монолитный режим
(`-target=all` по умолчанию), локальный backend, `block_retention: 24h`.

Две особенности, которые стоит знать:

- **Трейсы находятся в поиске не сразу.** Jaeger держал их в памяти и отдавал
  мгновенно; Tempo сначала пишет в WAL, потом собирает блок. Первый запрос
  `/api/search` может вернуть 0 — это норма, через минуту трейс находится.
- **Корреляция trace → logs не включена намеренно.** Обе её механики сейчас
  не работают: `service.name` в трейсах — это `MARS.TwitchCore`, а лейбл
  `container` в Loki — `twitch-core`; и `TraceId` кладётся в свойства Serilog,
  но шаблон Console не рендерит `{Properties}`, поэтому trace ID не доходит до
  stdout. Включать корреляцию, которая молча возвращает пустоту, не стоит —
  см. комментарий в `infrastructure/grafana/datasources/tempo.yml`.

В Tempo 3.x **нет** блоков `ingester` и `compactor` (их заменили `live_store` и
`backend_scheduler`); конфиг из документации Tempo 2.x на 3.1 не подходит.
Проверить конфиг без запуска стека:

```bash
docker run --rm -v "$PWD/infrastructure/tempo/tempo.yaml:/etc/tempo/tempo.yaml:ro" \
  grafana/tempo:3.1.0 "-config.file=/etc/tempo/tempo.yaml"
```

Без флага `-config.file` Tempo стартует и падает на `unknown backend ""` —
дефолтного пути конфига в образе нет.

Логи доставляет **Grafana Alloy**, а не сами приложения: сервисы только пишут в
`stdout` через Serilog, а агент читает их через Docker-демон. Конфигурация —
`infrastructure/alloy/config.alloy`, она фильтрует контейнеры по compose-проекту
`mars`, поэтому в Loki не попадает мусор с машины, а лейбл `container` содержит
имя сервиса (`twitch-core`, `gateway`, …). Новый сервис в доставке логов
не требует ничего — ни правок конфигурации, ни новых зависимостей.

Если логи не появляются: `docker compose logs alloy`, потом UI Alloy на
`http://localhost:12345` → вкладка Graph → компонент `loki.source.docker.mars`.

Alloy читает Docker-демон, поэтому у него смонтирован `/var/run/docker.sock`
в режиме `:ro`. Логи Alloy в собственный Loki он тоже доставляет — если нужно
отключить, добавь в `config.alloy` правило `drop` по контейнеру.

Данные наблюдаемости переживают рестарт: у Loki том `loki_data`, у Alloy —
`alloy_data` (позиции чтения), у Tempo — `tempo_data` (WAL и блоки), у
Prometheus — `prometheus_data`.

Образ Alloy запинен на `grafana/alloy:v1.20.1` — в Docker Hub тег с префиксом
`v`, без него образ не находится.

Seq из стека убран: это был второй интерфейс логов рядом с Grafana со своим
паролем и портом. Вместе с ним из `AddMarsLogging` убран синк
`Serilog.Sinks.Seq`, логи сервисов по-прежнему идут в `stdout`.

## Базы данных

На каждый сервис — своя база и своя роль с таким же именем. Базы и роли создаёт
`infrastructure/db-init/01-databases.sh`, который официальный entrypoint
контейнера `postgres` выполняет при инициализации пустого тома
(`/docker-entrypoint-initdb.d`).

| База | Схемы | Сервис |
|---|---|---|
| `mars_twitch` | `twitch` | MARS.TwitchCore |
| `mars_waifu` | `waifu` | MARS.WaifuGacha |
| `mars_chat` | `chat` | MARS.Telegram |
| `mars_media` | `media` | MARS.SoundRequest |
| `mars_scoreboard` | `scoreboard` | MARS.Scoreboard |
| `mars_cinema` | `cinema` | MARS.CinemaQueue |
| `mars_mediastorage` | `mediastorage` | MARS.MediaStorage |
| `mars_admin` | `admin` | MARS.Admin |
| `mars_alerts` | `alerts` | MARS.Alerts |
| `mars_videos365` | `videos365` | MARS.Videos365 |
| `mars_shikimori` | `shikimori` | MARS.Shikimori |

Схемы создаёт EF Core при применении миграций — скрипт их не трогает, чтобы не
расходиться с `HasDefaultSchema()` в коде.

Роль владеет своей базой: это нужно, потому что EF Core создаёт схемы и таблицы
при применении миграций, и сделать это может только владелец. Логины разделены,
поэтому `REVOKE ALL ON DATABASE … FROM PUBLIC` — не формальность: без него любой,
у кого есть учётка, подключился бы к чужой базе.

У сервисов без своей базы (Gateway, Commands, Discord, OBS, TTS) строки
подключения нет вовсе, и проверка `postgresql` в health check не
регистрируется: иначе readiness был бы зелёным по чужой базе. Имя строки
передаётся в `AddMarsDefaults` и `AddMarsDbContext` явно и обязано совпадать —
расхождение даёт «зелёный» readiness при недоступной базе, из которой сервис
читает данные.

Скрипт идемпотентен: повторный прогон ничего не ломает. Пересоздать базы с
потерей данных можно только явно — `MARS_DB_INIT_RESET=true`.

## Переменные окружения

**Окружений два, и каждое соответствует своему `ASPNETCORE_ENVIRONMENT`:**
`.env.development` идёт с `docker-compose.dev.yml`
(`Development` → `appsettings.Development.json`), `.env.production` — с
`docker-compose.yml` (`Production` → `appsettings.json`). Реальные файлы в git не
попадают (правило `.gitignore` широкое: `.env.*`), шаблоны `.env.*.example` —
попадают. Файл окружения передаётся compose флагом `--env-file`, а не читается
автоматически: дефолтного `.env` в репозитории нет, и без флага стенд поднялся бы
на `${ПЕРЕМЕННАЯ:-}`, то есть на пустых паролях.

Переключатель один — `-Dev`: он выбирает и compose-файл, и файл окружения
разом, потому что секреты среды обязаны соответствовать той же среде, что и
`appsettings`. Скрипты `stack`/`e2e` зовут `docker compose` с флагом сами.

| Переменная | Назначение |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Суперпользователь контейнера `postgres`. Ни один сервис под ним не подключается |
| `MARS_*_PASSWORD` (по одной на базу из таблицы выше) | Пароли ролей сервисов. Скрипт инициализации останавливается, если хотя бы один пуст |
| `RABBITMQ_USER` / `RABBITMQ_PASSWORD` | Брокер. **Не оставлять guest/guest** — RabbitMQ пускает guest только с loopback, и соединения падают с ACCESS_REFUSED |
| `SERVICE_API_KEY` | Общий ключ межсервисного обмена, заголовок `X-Api-Key`. **Обязателен в проде** |
| `GRAFANA_PASSWORD` | Админ Grafana |
| `TWITCH_CLIENT_ID` / `TWITCH_SECRET` / `TWITCH_OAUTH` | Twitch API. Пустые значения допустимы: чат не подключится, остальное работает |
| `APPINSIGHTS_CONNECTION_STRING` | Application Insights, пусто — отключено |
| `SHIKIMORI_CLIENT_NAME` / `SHIKIMORI_CLIENT_ID` / `SHIKIMORI_CLIENT_SECRET` | Реквизиты приложения Shikimori API для MARS.Shikimori. Пустые значения допустимы: сервис стартует, но запросы к Shikimori будут отклонены как анонимные |
| `SHIKIMORI_SITE` | Адрес сайта Shikimori, `https://shikimori.one` по умолчанию |
| `CONFIG365_*` | Конвейер MARS.Videos365. Пустая конфигурация допустима: воркер пишет предупреждение и не запускается |
| `TELEGRAM_BOT_TOKEN` / `TELEGRAM_ADMIN_ID` | Уведомления администраторам. Пустое значение допустимо: сервис стартует, уведомление пропускается с предупреждением в логе. Второй адресат — `TELEGRAM_ADMIN_ID_2` и так далее |

Пароли ролей попадают в строки подключения и потому видны в `docker inspect` и
`docker compose config`. В коде уже есть поддержка `Password_FILE=`
(см. `secrets/README.md`) — для боевого развёртывания замените механизм на
docker secrets.

Шаблоны окружений (`.env.*.example`) и перечень ключей `.env.example` в git
попадают, реальные файлы (`.env`, `.env.<среда>`) — нет.

## Доступ к admin-API

Admin-API закрыт ключом `SERVICE_API_KEY`. Без ключа — 401.

```bash
curl -H "X-Api-Key: $SERVICE_API_KEY" http://localhost:9155/api/RootState
```

## Сборка и тесты

Версии NuGet-пакетов вынесены в корневой `Directory.Packages.props`: в `.csproj`
остаётся только имя пакета. Новую зависимость добавляем и туда, и в `.csproj`.

```bash
dotnet build MARS.slnx --configuration Release
dotnet test MARS.slnx
```

Тесты лежат в `tests/`, по одному проекту на сервис (`MARS.Gateway.Tests` →
`src/MARS.Gateway`). Отдельный проект вместо общего на все сервисы нужен, чтобы
набор ссылок теста совпадал с набором зависимостей проверяемого сервиса и его
сломанная сборка роняла только свои тесты. Отдельный проект прогоняется и сам:

```bash
dotnet test tests/MARS.MediaStorage.Tests/MARS.MediaStorage.Tests.csproj
```

### Тесты и база данных

Любая проверка работы с базой идёт против **живой PostgreSQL**: контейнер
поднимает сам Testcontainers (`Testcontainers.PostgreSql`), база на тест
создаётся в нём, схема берётся из настоящих миграций сервиса. Отдельной базы в
репозитории нет — ни в `docker-compose.yml`, ни в `.devcontainer`, ни в CI:
всё это создаёт и убирает код фикстуры.

```csharp
internal sealed class WaifuTestDbContextFactory
    : PostgresTestDbContextFactory<WaifuDbContext>;
```

Требуется Docker: локально Docker Desktop, в CI раннер GitHub (он есть по
умолчанию). Обходные EF-провайдеры (`UseInMemoryDatabase`, `UseSqlite`) в тестах
не используются и пакеты их вычищены: они проверяли собранную модель, а не
работу с базой, и на них молча оставались непроверенными запросы, доступные
только Npgsql, реальные миграции и `ExecuteUpdateAsync`.

Контейнер после прогона не остаётся: удаляет его код (`PostgresContainerScope` +
выход из процесса), а не Ryuk — тот является страховкой и может не стартовать.
Данные postgres смонтированы в tmpfs, поэтому прогон не создаёт и висящий том.
Проверка после прогона:

```bash
docker ps -a --filter "label=org.testcontainers" --format "{{.Names}}\t{{.Status}}"
docker volume ls -f dangling=true -q
```

### Клиент: проверка типов и навигационные тесты

Клиент — отдельное дерево со своим инструментарием. Node и Yarn 4: в репозитории
лежит `yarn.lock`, а `package.json` объявляет `packageManager`, поэтому `npm ci`
здесь нерабочий — он требует `package-lock.json` и не выполняет postinstall,
которые `.yarnrc.yml` разрешает.

```bash
cd src/MARS.Gateway/ClientApp
corepack enable
yarn install --immutable
npx tsc -b --noEmit      # проверка типов
yarn test                 # 436 тестов
yarn build                # сборка для образа
```

Маршруты для навигационных тестов порождаются из кода, а не пишутся руками:
`routes.generated.json` собирается из того же `allRoutes`, которым создаётся
роутер. Проверка сверяет файл с кодом и падает при расхождении; обновить его
осознанно:

```bash
UPDATE_ROUTES_MANIFEST=1 npx vitest run src/tests/routesManifest.test.ts
```

`tests/MARS.ClientUi.Tests` открывает каждый маршрут в настоящем браузере и
проверяет, что подъём состояния не дал ошибок в консоли, что страница не пустая и
что хабы отвечают на рукопожатие. Ему нужен поднятый стенд, и в матрицу `tests`
в `ci.yml` он не входит: его гоняет отдельная задача `e2e`, потому что проект не
ссылается ни на один проект из `src/` и измерять в нём нечего — coverlet выдал
бы пустой отчёт и испортил счёт по методам в общем гейте. Исключение из сверки
с матрицей — в `.github/scripts/coverage-local.ps1`.

### Фильтрация тестов — ловушка

`global.json` включает Microsoft.Testing.Platform, а проекты на xunit.v3 имеют
`OutputType=Exe`. Фильтр VSTest-вида `--filter` молча находит ноль тестов и
заканчивается кодом 8, поэтому опции тест-приложения передаются после `--`:

```bash
# правильно:
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-class "*HealthCheck*"
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-method "*Namespace.Class.Method"
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-namespace "*Media*"

# неправильно: ноль тестов, код 8
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj --filter "*HealthCheck*"
```

### Покрытие кода

Собирается `coverlet.MTP` (обычный `coverlet.collector` с MTP не работает):

```bash
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- \
  --coverlet --coverlet-output-format cobertura \
  --coverlet-include "[MARS.*]*" --coverlet-exclude "[*.Test*]*" \
  --coverlet-exclude-by-file "**/Migrations/**"
```

`--coverlet-include "[MARS.*]*"` обязателен: без него coverlet инструментирует и
чужие сборки (HealthChecks, YARP, Serilog), отчёт распухает до тысяч строк, и
доля кода репозитория в нём становится нечитаемой. `-by-file "**/Migrations/**"`
исключает сгенерированные миграции EF.

Отчёты по проектам нельзя просто складывать: `MARS.Shared` инструментируется в
каждом из 17 тестовых проектов и посчитался бы 17 раз. Слияние делает ReportGenerator
(объединением покрытых строк), а порог по нему считает
`.github/scripts/coverage-gate.py`:

```bash
dotnet reportgenerator -reports:"reports/**/*.cobertura*.xml" \
  -targetdir:coverage-report -reporttypes:"HtmlInline_AzurePipelines_Dark;Cobertura;TextSummary"

python .github/scripts/coverage-gate.py --merged coverage-report/Cobertura.xml \
  --reports-dir reports --threshold-methods 95
```

Порог считается **по методам**: в Cobertura покрытие методов есть только в узлах
`<methods>`, а `line-rate`/`branch-rate` в `<coverage>` методов не содержат.
Метод с хотя бы одной покрытой строкой считается покрытым; методы без строк
(абстрактные, внешние, сгенерированные) в знаменатель не идут, но их количество
выводится отдельно. Пустой набор отчётов — ошибка, а не 0%: иначе сломанная
выгрузка артефакта дала бы зелёный статус.

### Покрытие локально

Прогон вручную по семнадцати проектам расходится с CI: забытый проект или
забытая опция coverlet дают число, которому нельзя верить. Скрипт повторяет шаги
задачи `coverage` один в один — сборка, 17 прогонов с `--coverlet`, слияние
ReportGenerator и `coverage-gate.py`:

```powershell
# полный честный замер, как в CI
.\.github\scripts\coverage-local.ps1

# ответ на вопрос «покрыли ли мы сервис X»
.\.github\scripts\coverage-local.ps1 -Project MARS.OBS.Tests -NoGate

# пересобрать слияние и порог по уже прогнанным тестам
.\.github\scripts\coverage-local.ps1 -SkipTests
```

Отчёты складываются в `coverage-local/`: покрытие каждого проекта в
`coverage-local/reports`, слитый отчёт в `coverage-local/coverage-report`
(каталог в `.gitignore`). Скрипт сверяет список `tests/*.Tests` с матрицей
`tests` в `ci.yml` и падает, если они разошлись.

`reportgenerator` и `csharpier` лежат в локальном манифесте
`.config/dotnet-tools.json`, а не в системном наборе; скрипт сам зовёт
`dotnet tool restore` перед слиянием — так же, как это делает задача `coverage`
в CI. Пустой набор отчётов считается ошибкой, а не нулевым покрытием.

Разбирать остаток удобно по тому же слитому `Cobertura.xml`:

```bash
python .\.github\scripts\coverage-gaps.py --merged coverage-local/coverage-report/Cobertura.xml --top 40
```

Текущее покрытие репозитория — **95.2% методов** (2711 из 2848, замер 2026-10-03).
Порог в CI стоит 95%, задача `coverage` зелёная; начиналось с 21.7% (607 из 2796).

## Скрипты

Все команды выше собраны в `scripts/` — отдельно для Windows и unix, с
одинаковыми именами (`build`, `test`, `verify`, `format`, `frontend`, `e2e`,
`coverage`, `release`, `stack`, `migrate`, `clean`, `sweep`, `pr`):

```powershell
# гейт перед пушем: формат → сборка Release → тесты
.\scripts\windows\verify.ps1

# один тестовый проект, один класс
.\scripts\windows\test.ps1 -Project MARS.Shared.Tests -FilterClass "*HealthCheck*"
```

```bash
./scripts/unix/verify.sh
./scripts/unix/test.sh --project MARS.Shared.Tests --filter-class '*HealthCheck*'
```

Скрипты ничего не добавляют к командам — они зовут те же `dotnet` и `docker`,
но с проверками, которые забывают чаще всего, и с внятными сообщениями. Своим
кодом они берут состав репозитория из него же: список тестовых проектов — из
`tests/`, список образов для публикации — из матрицы `release-microservices.yml`.
Карта скриптов и отличия платформ — в [`scripts/README.md`](scripts/README.md),
договорённости между платформами проверяет
`tests/MARS.Gateway.Tests/ScriptsParityTests.cs`.

## Непрерывная интеграция

`.github/workflows/ci.yml` — на каждый push в `main` и каждый PR:

1. `build` — `dotnet build MARS.slnx -c Release`, отдельной задачей, чтобы ошибка
   компиляции не ждала 17 матричных прогонов;
2. `tests` — матрица по всем 17 тестовым проектам с `fail-fast: false`. Имя задачи
   `tests / MARS.Gateway.Tests` становится **отдельным статусом в GitHub**, так что
   в branch protection можно требовать любой набор проверок, а не только сводный
   «всё зелёное». Каждый проект отдаёт свой cobertura и TRX артефактами;
3. `coverage` — слияние отчётов ReportGenerator, HTML-отчёт и проверка порога.
   Запускается только при полностью зелёных тестах: иначе падало бы две задачи по
   одной причине, а покрытие считалось бы по неполным данным.

Новый тестовый проект обязан попасть в матрицу `tests` в `ci.yml`.

`.github/workflows/auto-format.yml` — на каждый push в `main` и PR в `main`
прогоняет `dotnet csharpier format .` и **ложит результат в последний коммит
ветки** (`git commit --amend`), а не добавляет отдельный коммит от бота. История
не растёт: остаётся один коммит, у которого автор прежний, коммитер —
`github-actions[bot]`, а в сообщении появляется строка
`Co-authored-by: github-actions[bot]`. Ветка при этом переписывается, поэтому
коммит меняет хеш — сильно вытянутая ветка просто повторит форматирование.

Форматирование наезжает только на `main` и на PR в него, чтобы не трогать
незавершённую работу в feature-ветках. Для PR из форка push невозможен (токен
принудительно read-only) — workflow не падает, форматирование приедет после
merge в `main`. Если ветку увеличил человек параллельно, force-push не
проходит: workflow предупреждает, следующий прогон форматирует поверх чужого
коммита.

Проверки после переписанного коммита запускаются отдельно: push, сделанный
`GITHUB_TOKEN`, не запускает workflow (защита GitHub от рекурсии), поэтому шаг
делает `gh workflow run ci.yml --ref <ветка>`. Если в репозитории задан секрет
`AUTO_FORMAT_TOKEN` (PAT), push идёт им, проверки запускаются сами, а ручной
перезапуск пропускается — так у PR появляются и статусы проверок самого PR.

**Локальная копия `main` после автоформата разъезжается с `origin`.** Это
ожидаемо: коммит переписан, и обычный `git pull --ff-only` упадёт. Перед
следующим пушем:

```bash
git fetch origin && git reset --hard origin/main
```

Локально то же самое:

```bash
dotnet csharpier format .   # отформатировать
dotnet csharpier check .    # только проверить
```

То же обёртками: `scripts/windows/format.ps1` и `scripts/unix/format.sh`
(`-Action format|check`, `--action format|check`). Перед пушем формат обязателен
в любом случае: с переходом на ветки `auto-format.yml` больше не наезжает на
незавершённую работу.

## Публикация образов

Workflow `.github/workflows/release-microservices.yml` по тегу `v*` собирает
каждый сервис отдельной задачей и пушит в `ghcr.io/rxdcodx/mars-<service>`.

Образ получает теги: релизный тег, `sha-<commit>` и `latest`
(только для семантических версий `vX.Y.Z`).

```bash
git tag v1.0.0 && git push origin v1.0.0
```

Перевыпуск под тем же тегом — через `workflow_dispatch` с ручным вводом тега.

Локально то же самое можно сделать без правки тега руками:
`scripts/windows/release.ps1 -Action Tag -Version v1.0.0 -Push` или
`./scripts/unix/release.sh --action tag --version v1.0.0 --push`. Скрипт берёт
список образов из матрицы этого же workflow, проверяет чистоту дерева и то, что
тег не занят, и ставит аннотированный тег. Сборка образов без публикации —
`release.ps1 -Action Images` / `release.sh --action images`.

## Архитектурные решения

- **Один владелец Twitch-подключения.** Только `MARS.TwitchCore` держит IRC/EventSub.
  Остальные сервисы общаются с ним через RabbitMQ, а не открывают второе соединение
  (Twitch отключает более старое).
- **Контракты gRPC в `MARS.Shared`.** Все `.proto` лежат в `src/MARS.Shared/Protos`,
  а хосты и нотификаторы — в `src/MARS.Shared/Grpc`, поэтому `MARS.OBS` не
  ссылается на `MARS.Alerts` (иначе publish падал бы с NETSDK1152 из-за дублей
  `appsettings.json`).
- **gRPC — на отдельном порту 8081.** Сервисы с gRPC поднимают `AddMarsGrpcHosting()`:
  `8080` остаётся HTTP/1.1 (REST, health, метрики), `8081` — HTTP/2 без TLS, потому
  что Kestrel обслуживает h2c только на эндпоинте с явно заданным протоколом
  `Http2`. Клиенты подключаются к `http://<service>:8081` внутри docker-сети;
  наружу gRPC не выведен.
- **`IMarsSchemaReady<T>`** — миграции применяются синхронно до `app.Run()`,
  иначе фоновые сервисы успевают обратиться к несуществующим таблицам (42P01).
- **Retry/DLQ в шине.** `RabbitMqConsumerBase` ограничивает число попыток и
  складывает poison-сообщения в `<queue>.dlq` вместо бесконечного requeue.

Неймспейсы остаются `MARS.*` независимо от имён папок в `src/`.
