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
dotnet csharpier format . && dotnet csharpier check .

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

### Покрытие локально: один скрипт вместо ручной сборки отчётов

Задача `coverage` в CI считает покрытие по слитому отчёту и проверяет порог 95%
по методам, а разбираться с пробелами удобно локально. Скрипт повторяет её шаги
один в один — 17 тестовых проектов с `--coverlet` → слияние ReportGenerator →
`coverage-gate.py`:

```bash
# полный честный замер, как в CI (тесты гоняются параллельно)
.\.github\scripts\coverage-local.ps1

# быстрый ответ «покрыли ли мы сервис X»: числа ниже, чем в CI, и несравнимы
.\.github\scripts\coverage-local.ps1 -Project MARS.OBS.Tests -NoGate

# пересобрать только слияние и порог по уже прогнанным тестам
.\.github\scripts\coverage-local.ps1 -SkipTests
```

Ключевые `-Project`, `-MaxParallel`, `-ThresholdMethods`, `-NoGate`, `-SkipBuild`.
Скрипт сам сверяет список `tests/*.Tests` с матрицей `tests` в `ci.yml` и падает,
если они разошлись: иначе проект, забытый в матрице, молча выпадает и из CI, и
из локального числа. Отчёты складываются в `coverage-local/` (в `.gitignore`).

Инструменты живут в `.config/dotnet-tools.json` (`reportgenerator`, `csharpier`),
а не в системном наборе, поэтому перед слиянием скрипт зовёт
`dotnet tool restore` — ровно как шаг «Восстановить локальные инструменты» в CI.
На свежей клоне без этого шага `dotnet reportgenerator` просто не находится, и
замер падал бы на инструменте вместо покрытия. Пустой набор отчётов — тоже
ошибка, а не 0%: и слово об этом есть в самом скрипте, и в `coverage-gate.py`.

Что делать с пробелами дальше — `.github/scripts/coverage-gaps.py`, тот же
слитый `Cobertura.xml`:

```bash
python .\.github\scripts\coverage-gaps.py --merged coverage-local/coverage-report/Cobertura.xml --top 40
python .\.github\scripts\coverage-gaps.py --merged ... --package MARS.OBS --min-methods 1 --methods
```

### Контракт публичной поверхности

`tests/MARS.TestKit` — библиотека (не тестовый проект: в ней нет `[Fact]`, в
матрицу `tests` она не попадает, в `MARS.slnx` — попадает). Даёт два
использования, каждое в `tests/MARS.X.Tests/PublicContractTests.cs`:

- `PublicContractVerifier.VerifyAssembly` создаёт каждый публичный тип сборки из
  подставляемых значений, проверяет, что каждое читаемое и записываемое свойство
  возвращает положенное обратно, и прогоняет статические конструкторы. На
  моделях, DTO и options приходится основная масса методов в знаменателе, и
  проверять их по одному вручную — сотни одинаковых тестов.
- Границы намеренные: типы, чьи конструкторы требуют зависимостей, типы без
  публичного конструктора и наследники системных классов **не** создаются.
  Иначе проверка «сервис собирается» превратилась бы в проверку сети и файлов:
  `YouTubeResolver` создаёт клиент в конструкторе и намертво вешал прогон.
- `DependencyStubFactory.Build` собирает `ServiceProvider`, где интерфейсы —
  loose-заглушки Moq (`DefaultValue.Mock`, иначе свойство-интерфейс вернёт null
  и уронит конструктор). Нужен там, где сервис сам ищет зависимости рефлексией,
  — например, `MARS.Commands.Tests/CommandSuiteTests.cs` собирает все команды
  настоящей `CommandFactory`.

Точечные тесты сервисного поведения обязательны: контракт проверяет создаваемость
и данные, но не логику.

### Чем закрывать остаток покрытия

На 2026-10-03 покрытие слитого отчёта — **95.2% (2711 из 2848 методов)**, порог
`coverage` — 95%, и он пройден: `coverage-gate.py` на слитом `Cobertura.xml`
заканчивается с кодом 0. Начиналось с 21.7% (607 из 2796). Запас над порогом
тонкий — 5 методов, — поэтому любое исключение из тестового набора или падение
одного проекта уводит задачу за порог снова.

Дальше покрытие растёт теми же средствами. Разбирать остаток стоит по отчёту
`coverage-gaps.py --merged ... --top 0 --format json` (по числу непокрытых
методов в классе; группировка по пакету показывает, где искать) и в таком
порядке:

1. **Классы с чистой логикой** — валидаторы, фильтры, мапперы, схлопывания
   (`MessageValidationBuilder`, `TtsMessageFilterService`,
   `TelegramusNotifier`). Покрываются юнит-тестами с заглушками и дают
   десятки методов за один файл.
2. **Сервисы на интерфейсах** (`ISoundRequestService`, `IWaifuGachaClient`,
   хранилища). Собираются вручную или через `DependencyStubFactory`.
3. **Сервисы, создающие клиента в конструкторе** (`YouTubeResolver`,
   `TwitchConnectionManager`, Discord- и Telegram-шлюзы) — их нельзя даже
   собрать заглушками: конструктор уходит в сеть или поднимает IRC/websocket.
   Здесь нужен разрыв в production-коде (клиент через `IHttpClientFactory` или
   интерфейс), иначе тест будет проверять сеть, а не код. **Уже сделано для
   `MARS.Discord`**: `IYouTubeResolver` рядом с `YouTubeResolver`
   (`src/MARS.Discord/Services/YouTube/IYouTubeResolver.cs`), регистрация в
   `Program.cs` — `AddSingleton<IYouTubeResolver, YouTubeResolver>()`.
   **И для `MARS.Shikimori`**: `IShikimoriClient` + `ShikimoriSharpClient`
   (`src/MARS.Shikimori/Services/IShikimoriClient.cs`), регистрация —
   `AddSingleton<IShikimoriClient, ShikimoriSharpClient>()`. До разрыва тест
   контроллера проходил, когда Shikimori доступен, и падал бы в CI без сети,
   ничего не проверяя; после — модуль закрыт целиком и без сети. Модель та же,
   что у `IMediaCompressor`/`IFfmpegRunner`.
4. `Program.Main` в каждом сервисе (`app.Run()` блокирует): чтобы покрыть,
   нужен вынос сборки приложения в отдельный метод — на 16 файлов
   `Program.cs`. Пока этого не сделано, 16 методов останутся вне покрытия,
   и это ~0.6% знаменателя.

### Ловушки покрытия, найденные на живых прогонах

Все ловушки ниже проверены на этой машине, а не взяты из документации.

- **YoutubeExplode 6.5.3 вешает процесс при загрузке сборки.** Консольное
  приложение, которое только *использует* `YoutubeClient`, не печатает даже
  первую строку и не завершается. Значит тест, **сославшийся на сам
  `YoutubeExplodeApi`**, подвесит **весь** тестовый хост, а не только этот тест:
  ловить тут нечего, такая «проверка» была бы проверкой сети.
  Ловушку снимает разрыв, а не осторожность: `IYouTubeApi`
  (`src/MARS.TwitchCore/Services/YouTube/IYouTubeApi.cs` и
  `src/MARS.Discord/Services/YouTube/IYouTubeApi.cs`) — резолверы
  (`YouTubeResolver`) больше не знают про YoutubeExplode и проверяются заглушкой,
  а их собственные модели живут в `YouTubeModels.cs`/`BaseTrackInfo.cs`. Из
  YoutubeExplode остался ровно один класс — `YoutubeExplodeApi`, и он намеренно
  вне покрытия. В `PublicContractTests` он перечислен в `IgnoredTypes` с тем же
  объяснением.
- **Обходные EF-провайдеры в тестах не используются.** Раньше пути, недоступные
  InMemory и SQLite, приходилось оставлять непроверенными, и это стоило трёх
  дефектов, найденных при переводе (см. ниже). Сейчас любой тест, который
  обращается к базе, идёт против PostgreSQL из Testcontainers; `UseInMemoryDatabase`,
  `UseSqlite` и пакеты EF InMemory/Sqlite в `tests/` запрещены.
- **Npgsql отвергает `DateTime` с `Kind=Local` при записи в
  `timestamp with time zone`,** а `DateTime.Now` в коде сервисов встречается
  сотни раз. Приведение к UTC живёт в `MarsUtcDates.ConfigureUtcDates()` и
  назначается переопределением `ConfigureConventions` в каждом контексте;
  `UtcDatesConventionVerifier` проверяет, что правило не забыто. Локальное время
  именно *переводится* в UTC, а не переименовывается в него — переименование
  хранило бы момент со сдвигом на смещение машины, невидимое на стенде с TZ=UTC.
- **Значение, прочитанное из базы, приходит с `Kind=Utc`.** Код, который печатает
  время зрителю, обязан звать `ToLocalTime()` сам. Сравнение сохранённой даты с
  `DateTime.Now` — смешение UTC и локального: на машине не в UTC оно всегда даёт
  неверный ответ.
- **`SaveChangesAsync` возвращает число изменённых строк, а не записей.**
  PostgreSQL не считает обновлением строку, значения которой не изменились, и
  сервис, отдававший это число как «сколько сохранили», занижал счётчик и запись
  в журнале. Такое возвращаемое значение имеет смысл только вместе с числом
  обработанных записей, а не вместо него.
- **Ответы TwitchLib нельзя подделать через `ITwitchAPI`.** `Helix` — конкретный
  класс, а `GetUsersResponse`/`GetCustomRewards` имеют internal-сеттеры, которые
  System.Text.Json не заполняет. Собранный вручную `Helix` с заглушкой
  `IHttpCallHandler` отдаёт в ответ `null`, молча, без исключения. Тесты на
  сервисы Twitch API проверяют отказные ветки (нет токена, недоступен Twitch),
  а не успешный разбор ответа.
- **`RabbitMqConsumerBase.Deserialize<T>` не ловит исключение.** Битый JSON
  (`{ это не json`) доходит из `HandleMessageAsync` наружу и уходит в ретрай и
  DLQ — это правильное поведение, и тест на «пропуск сообщения» его не отрицает.
  `null`-строка разбирается в `null` и пропускается тихо. Тест обязан утверждать
  именно разницу: `Assert.Throws<TargetInvocationException>` при вызове через
  рефлексию (иначе исключение обернётся), а не «сообщение просто не дошло».
- **`BackgroundService.ExecuteAsync` с уже отменённым токеном ничего не делает.**
  Условие цикла `while (!stoppingToken.IsCancellationRequested)` проверяется до тела,
  поэтому предотменённый токен не исполняет даже первый проход. Чтобы покрыть
  тело цикла, нужен сигнал (логгер-заглушка, счётчик) либо вызов приватного шага
  воркера напрямую; ждать настоящий интервал (30 минут, сутки) в тесте нельзя.
- **Фоновые вызовы без `await` нужно дожидаться по признаку, а не по `Task.Delay`.**
  `TemporaryReward.TimerElapseNow` и подобные запускают обновление через
  `_ = ExecuteRewardStateAsync(...)`: сразу после вызова счётчик попыток ещё ноль.
  Работающая замена — опрос счётчика с предельным числом попыток и `Assert.Fail`
  по его исчерпании.
- **Moq не умеет `Callback` с `It.IsAnyType`.** Тип-матчер в generic-аргументе
  колбэка даёт `ArgumentException: Type matchers may not be used as the type for
  'Callback'`. Для перехвата `ILogger.Log` пишется свой `ILogger<T>`
  (как `WarningCollector` в `tests/MARS.CinemaQueue.Tests`).
- **`Mock<ILogger<T>>` требует публичный `T`.** Для `ILogger` с внутренним
  вложенным типом Castle не создаёт прокси: `type ... is not accessible`. Тип
  должен быть `public` либо сборка должна иметь `InternalsVisibleTo`.
- **Идентификаторы наград Twitch — GUID.** `TemporaryReward.EnsureRewardStateAsync`
  разбирает ответ как `Guid.Parse`, поэтому в подставном ответе нужен настоящий
  GUID: строка `created-1` роняет проверку на разборе, а не на логике.

### Telegram.Bot 22: заглушка клиента

`SendMessage` и `GetFile` в `Telegram.Bot` 22.10 — **расширяющие методы** над
интерфейсом, а `DownloadFile` и `SendRequest<T>` — методы интерфейса. Перечислять
необязательные параметры `SendMessage` ради проверки не нужно: заглушка
перехватывает `SendRequest` и читает из вызовов `SendMessageRequest`, как это
сделано в `tests/MARS.Admin.Tests/TelegramLoggerTests.cs`. `GetFile` подменяется
через `SendRequest(new GetFileRequest(fileId))` с ответом `TGFile`.


## Линтер / типы / CI

- **Триггер CI — push в `main`, PR в `main`, `workflow_dispatch`**
  (`.github/workflows/ci.yml`), отдельно от публикации образов
  (`release-microservices.yml`, триггер — тег `v*`). Задачи: `build`,
  `tests` (матрица по всем 17 тестовым проектам, `fail-fast: false`), `coverage`.
- **Отдельный статус на каждый тестовый проект** получается из матрицы:
  `tests / MARS.Gateway.Tests` — самостоятельный check в branch protection.
  Новый тестовый проект ⇒ запись в матрицу `tests` в `ci.yml`, иначе он не
  проверяется вообще и это молчаливо.
- **Задача `coverage` зелёная: порог 95% методов, фактически 95.2%** (2711 из
  2848, замер 2026-10-03; начинали с 21.7% — 607 из 2796). Запас тонкий, и
  теряется он не из-за одного нового непокрытого метода, а из-за исключения
  проекта из матрицы или падения тестов: `coverage` запускается только при
  `needs.tests.result == 'success'`, иначе падали бы две задачи по одной причине,
  а покрытие считалось бы по неполным данным.
- **Таблица по проектам в выводе `coverage-local.ps1` — это прогон, а не сборка.**
  `MARS.Shared` инструментируется в каждом из 17 прогонов, поэтому в строке
  `MARS.Commands.Tests` сидят ещё и методы `MARS.Shared`. Ориентир — только слитый
  `coverage-local/coverage-report/Cobertura.xml`, как его и считает гейт.
- Покрытие снимает `coverlet.MTP` (MTP v2). `coverlet.collector` и датаколлекторы
  VSTest с MTP несовместимы, `Microsoft.Testing.Extensions.CodeCoverage` требует
  MTP 18.x. Опции тест-приложения — после `--`.
- **`--coverlet-include "[MARS.*]*"` обязателен**: без него coverlet берёт и чужие
  сборки (HealthChecks, YARP, Serilog), отчёт распухает до тысяч строк. Плюс
  `--coverlet-exclude "[*.Test*]*"` и `-by-file "**/Migrations/**"`.
  `MARS.TestKit` исключён тем же правилом, что и тестовые сборки: он лежит в
  `tests/`, в образы сервисов не попадает, а измерять покрытие рефлексии и
  заглушек смысла нет.
- **Отчёты по проектам нельзя складывать**: `MARS.Shared` инструментируется в
  каждом тестовом проекте и посчитался бы 17 раз. Слияние делает ReportGenerator
  (объединением покрытых строк), порог по слитому `Cobertura.xml` считает
  `.github/scripts/coverage-gate.py`. Пустой набор отчётов → ошибка, не 0%.
- Порог считается **по методам**: в Cobertura покрытие методов есть только в узлах
  `<methods>`, а `line-rate`/`branch-rate` в `<coverage>` методов не содержат.
  У `<method>` нет атрибута `covered` — покрытым считается метод, у которого хотя
  бы одна вложенная `<line hits>` > 0. Методы без строк в знаменатель не идут.
- **Форматирование автофиксится, а не проверяется**: `.github/workflows/auto-format.yml`
  на `main` и PR в `main` гоняет `dotnet csharpier format .` и сам коммитит результат
  (`style: автоформатирование CSharpier`). На feature-ветках и в форках не
  наезжает. Локально то же: `dotnet csharpier format .`, проверка —
  `dotnet csharpier check .`.
- **`TreatWarningsAsErrors=true` во всех 32 проектах**: свойства заданы прямо в
  каждом `.csproj` (`src/` и `tests/`), общего `Directory.Build.props` в репозитории
  нет. Любое новое предупреждение компиляции роняет `build` в CI, поэтому
  `.editorconfig` поднимает только `ASP0014` до warning — остальное неявно
  становится ошибкой.
- **Исключения из этого правила живут в `NoWarn` каждого проекта**:
  `NU1902` (AngleSharp 1.2.0 транзитивно из `YoutubeExplode` 6.5.3, CVE
  GHSA-pgww-w46g-26qg — закрыть можно только апгрейдом YoutubeExplode) и
  `NU1903` (SQLitePCLRaw, CVE-2025-6965, исправлённой версии нет). В
  `MARS.WaifuGacha` к ним добавлен `CS9113` — непрочитанные параметры
  primary-конструктора в `WaifuRollException`, `AddNewWaifuService`,
  `MergeWaifuService` и `ShikimoriRateLimiter`.
  `NU1903` в тестах больше не нужен: SQLite вычищен из `tests/`, и пакет
  SQLitePCLRaw приходит только через провайдер EF устаревшей версии.
- **`xUnit1051` в `WarningsAsErrors`**: в тестах любой вызов, у которого есть
  перегрузка с `CancellationToken` (EF Core `SingleAsync`/`CountAsync`/
  `SaveChangesAsync`, `File.*Async`, сервисные `UploadAsync`/`ListAsync`/
  `MoveAsync`/`SyncAsync`/`RunAsync`), обязан получать
  `TestContext.Current.CancellationToken`. Если токен в сигнатуре не последний,
  передавать его именованным аргументом: `ListAsync(cancellationToken: …)`,
  `SyncAsync("msg", cancellationToken: …)`, `RunAsync(dir, args, cancellationToken: …)`
  — иначе токен молча уедет в `includeDeleted`/`allowEmptyCommit`/`stdin`.
- **CLI CSharpier 1.x — с подкомандами**: `format <path>` пишет, `check <path>`
  только проверяет, `dotnet csharpier <path>` без подкоманды в 1.x не существует
  (в 0.30.6 было наоборот). Локальный tool из `.config/dotnet-tools.json`
  (`rollForward: false`, команда переименована в `csharpier`). Конфига
  `.csharpierrc` в репозитории нет.
- **CSharpier 1.3.0 понимает C# 14 extension members** (`extension(Type this)`).
  На 0.30.6 они давали «Failed to compile so was not formatted» с exit code 1,
  из-за чего auto-format ронялся целиком. `.csharpierignore` в репозитории нет
  и не нужен: вернуть его можно только вместе с откатом версии tool'а.
- `dotnet-ef` в манифесте tools **нет** (только csharpier и reportgenerator) — стоит глобально.
  Предупреждение «tools version 10.0.8 is older than runtime 10.0.10» — норма, не чинить.
- Коммиты: conventional-коммиты с русским описанием (`feat:`, `chore:`, `docs:`).

## Git workflow: только `main`

Репозиторий живёт в одной ветке. Ни feature-веток, ни PR — это осознанное
решение владельца, а не недосмотр, и агент не должен предлагать их вернуть.

- **Единственная ветка — `main`.** Работа ведётся в ней же: правка, коммит,
  `git push origin main`. Ветки `feature/…`/`fix/…` не создаются, `gh pr create`
  не вызывается, в ответе возвращается ссылка на коммит.
- **GITHUB_TOKEN**: переменная окружения пользователя содержит GitHub token с
  доступом к репозиторию, доступна как `$env:GITHUB_TOKEN` (PowerShell).
- **Перед пушем обязателен зелёный `dotnet build MARS.slnx -c Release`
  и `dotnet test MARS.slnx -c Release`.** В `main` сразу попадает код, который
  дальше собирает CI, поэтому красная сборка здесь обходится дороже, чем
  inconvenience от отдельной ветки: откатить придётся revert'ом в `main`.
- **Пуш только fast-forward'ом.** Перед пушем `git fetch origin`; если `main`
  отстал — сначала `git pull --ff-only origin main`, иначе пуш упадёт без
  fast-forward и понадобится merge-коммит, которого в `main` быть не должно.
- **После каждого логического шага — коммит и пуш.** Шаг — это цикл Red/Green
  целиком (падающий тест + реализация), а не отдельный файл, поэтому промежуточный
  красный в `main` не попадает никогда. Смысл не в аккуратности, а в откате:
  неудачный шаг откатывается до заведомо рабочего состояния, а не разбирается
  в недоделанном diff'е на живом стенде. Пуш на каждом шаге означает полный прогон
  проверок на каждом шаге — это цена того, что в `main` не попадает непроверенный код.
- **Ветки, полностью вошедшие в `main`, удаляются** — локальные через
  `git branch -D`, удалённые через `git push origin --delete <имя>`. Проверять
  перед удалением: `git merge-base --is-ancestor <ветка> origin/main`.
  Именно `-D`, а не `-d`: `-d` сверяется с upstream-веткой, а не с `main`, и
  после fast-forward-пуша в `main` отказывается удалять уже смерженную ветку
  («not fully merged»). `origin/main` — единственный верный адресат сравнения.
- Коммит без тестов не допускается — см. TDD ниже.
- **Проверка прав на команды при переносе была выпилена, а не отсутствовала.**
  В `MARS_old` первоисточник — `MARS.Server/Services/CommandExecutor/Adapters/*`:
  Twitch сверял пишущего со стримером, Telegram — с одним захардкоженным ID,
  Discord — со списком `AdminIdsArray` из конфига, API считал админом всех.
  Копия `MARS.Projects/MARS.Microservices/MARS.Commands`, из которой вырос этот
  репозиторий, получила все четыре предиката как `_ => false` с комментарием
  «Полная реализация зависит от MARS.TwitchCore microservice», из-за чего 38
  админ-команд стали недостижимы. При переносе репозитория это выглядело как
  «прав нет», но отсутствовала именно заглушка.
- **Гейт живёт в `MARS.Commands`, а права разрешает сервис платформы.**
  Позиция гейта — как в монолите: после резолва алиаса и проверки платформы, до
  `ExecuteAsync`; алиас обязан быть разрешён до этого, иначе `adhdstart` прошёл бы
  как пользовательская команда. Личность знает только сервис платформы, поэтому он
  передаёт `is_admin` (у Twitch — `TwitchCommandPermissions.IsAdmin`, у Discord и
  Telegram — `AdminIdsArray` из их конфигов). Значение по умолчанию — `false`:
  неизвестный вызывающий админом не считается.
- **Модератор Twitch — администратор, это отличие от монолита.** В командном
  фреймворке монолита проходил только стример, но валидаторы наград и
  `TwitchTitleChangeCommand` допускали модератора («только модераторам и
  стримеру»). Стример-only сделал бы `title` недоступной тому, ради кого она
  писалась. `TelegramConfiguration.AdminIdsArray` в `MARS.Telegram` при этом
  используется **только** для рассылки уведомлений (`UpdateHandler`), а не как
  источник прав, — та же ловушка, что была в монолите.

## TDD (Test-Driven Development) — обязательно

1. **Red**: сначала падающий тест, описывающий требуемое поведение.
2. **Green**: минимальная реализация, чтобы тест прошёл.
3. **Refactor**: рефакторинг без изменения поведения.

Правила:

- Любая новая функциональность и любое исправление бага начинаются с теста.
- Тест лежит в `tests/MARS.X.Tests` — том же наборе, что и сервис в `src/MARS.X`.
- Новый тестовый проект ⇒ запись в матрицу `tests` в `ci.yml`, иначе он не проверяется.

## Стиль кода

Перенесено из монолита и остаётся обязательным. Автоматикой не проверяется:
`TreatWarningsAsErrors` включён, но `ASP0014` — единственный поднятый анализатор.

### C# — форма метода (single input / single output)

- В начале метода объявляется переменная результата с дефолтным состоянием.
- В конце метода — **один** `return result;`. Множественные `return` запрещены,
  ранний выход недопустим.
- Условия формулируются **позитивно**: `if` описывает успешный/ожидаемый ход,
  ошибки и отклонения — в `else`. Успешный путь линеен, без вложенных отрицаний.
- `catch` не возвращает сразу, а присваивает результату понятное сообщение об ошибке.
- Чек-лист перед завершением метода: результат объявлен; `return` один и в конце;
  все ветки корректируют результат; исключения превращены в сообщение.

```csharp
public async Task<OperationResult<Foo>> DoWorkAsync(string input)
{
    var result = OperationResult<Foo>.Fail("Стартовая ошибка");

    if (!string.IsNullOrWhiteSpace(input))
    {
        try
        {
            var entity = await db.Entities.FirstOrDefaultAsync(e => e.Name == input);
            if (entity is not null)
            {
                result = OperationResult<Foo>.Ok(entity);
            }
            else
            {
                result = OperationResult<Foo>.Fail("Не найдено");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DoWork failed for {Input}", input);
            result = OperationResult<Foo>.Fail($"Ошибка: {ex.Message}");
        }
    }
    else
    {
        result = OperationResult<Foo>.Fail("input не может быть пустым");
    }

    return result;
}
```

- **Контроллеры**: возвращают `ActionResult<OperationResult<T>>`, не сырой `IActionResult`.
  `OperationResult<T?>` — если метод может вернуть `null`. Бизнес-ошибка — это
  `Ok(result)` с `Success = false`, а не `BadRequest(...)`. Эталон:
  `src/MARS.CinemaQueue/Controllers/CinemaQueueController.cs`.
- **EF Core**: read-only запросы — с `AsNoTracking()` (в коде 30 мест, это норма).
- **1 тип = 1 файл**, кроме локальных и вложенных типов.
- **`CSharpier` — единственный форматтер**, `.csharpierrc` в репозитории нет,
  значит дефолтные настройки (printWidth 100, 4 пробела).
- **`Platform` и `CommandVisibility` — именно `[Flags]`, значения обязаны быть
  степенями двойки.** У `Platform` их не было: без явных присваиваний компилятор
  выдаёт `0,1,2,3,4,5`, и тогда `Twitch | Discord` = `3 | 4` = `7`, а `Platform.All`
  = `7` — то есть `[Flags]` врёт, а любая битовая проверка молча выдаёт мусор.
  Из-за этого `IsAvailableOnPlatform` сравнивал через `Enumerable.Contains`, и
  агрегат платформ вроде `[Platform.Twitch | Platform.Discord]` не означал
  «и там, и там». Значения исправлены; при добавлении платформы — только явными
  `= 1`, `= 2`, `= 4`, `= 8`. Числа нигде не сериализуются: платформа приходит из
  маршрута по имени. Внимание: у `Platform` в `MARS.Commands` и у `CommandPlatform`
  в `commands.proto` **разные значения**, сопоставлять только по имени.

### React/TypeScript (UI хранилища)

Действует только для `src/MARS.MediaStorage/ClientApp` — единственного фронтенда
в репозитории. Zustand, Chakra, Storybook и react-router из монолита сюда не переносились.

- Состояние — `useState`/`useReducer` компонента. Стор-библиотеки нет; не ссылаться
  на `useStore.getState()`, `useShallow` и `ToastModal` — их тут не существует.
- `useMemo` для дорогих вычислений (filter/sort по массивам), `useCallback` для
  функций, уходящих в зависимости хуков. Виртуализация списка — ручная, на
  `ROW_HEIGHT`/`OVERSCAN`, см. `App.tsx`.
- Импорты React — только именованные (`import { useState } from 'react'`).
- Цвета и стили — из `styles.css`, литералы в разметке не хардкодить.
- API-вызовы идут через `src/api.ts`: он сам разбирает конверт `OperationResult`
  и бросает исключение при `Success = false`. В компонентах `fetch` не вызывать.

## Границы пакетов и точка входа

- `src/MARS.Shared` — единственная общая библиотека. Всё, что нужно двум сервисам
  и не должно тянуть проектную ссылку, живёт здесь.
- `tests/MARS.X.Tests` ↔ `src/MARS.X` — один тестовый проект на сервис, набор ссылок
  повторяет набор зависимостей сервиса. Новый сервис ⇒ новый проект в `tests/` + запись
  в обе папки (`/src/` и `/tests/`) `MARS.slnx`. Общего `Directory.Build.props` нет,
  поэтому **новый проект обязан скопировать оба `PropertyGroup`** из соседнего
  (`net10.0-windows` + `TreatWarningsAsErrors`/`WarningsAsErrors`/`NoWarn`/
  `CSharpier_Bypass`), иначе он окажется единственным, где предупреждения не роняют
  сборку.
- Неймспейсы всегда `MARS.*`, независимо от имён папок.
- **Протоколы и контракты — только в `MARS.Shared`.** Все `.proto` лежат в
  `src/MARS.Shared/Protos`, а хосты и нотификаторы — в `src/MARS.Shared/Grpc`.
  Нельзя ссылаться `MARS.OBS` → `MARS.Alerts`: publish падает с NETSDK1152 из-за
  дублей `appsettings.json`.
- **Хабов SignalR в репозитории нет.** Вместо них gRPC: подписки — server-streaming
  (`Subscribe` в `TelegramusService`, `TunaService`, `ScoreboardService`,
  `SoundRequestService`, `VoiceRecognitionService`), вызовы клиент→сервер — unary.
  Сервер рассылает события подписчикам через `GrpcEventBroadcaster<T>` из
  `MARS.Shared`; сервисы, которые раньше держали хабы, — `MARS.Alerts`,
  `MARS.OBS`, `MARS.Scoreboard`, `MARS.SoundRequest`, `MARS.TTS`.
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

### Тесты ходят в живой PostgreSQL (Testcontainers)

Любая проверка работы с базой идёт только против PostgreSQL — того же, что на
стенде. Отдельной базы в репозитории нет: контейнер поднимает сам
Testcontainers (`Testcontainers.PostgreSql`), отдельный compose с postgres и
`.devcontainer` не нужны.

```csharp
internal sealed class WaifuTestDbContextFactory : PostgresTestDbContextFactory<WaifuDbContext>;
```

`PostgresTestDbContextFactory<TContext>` в `tests/MARS.TestKit/Postgres/` даёт
свою базу на тест и применяет **настоящие миграции** сервиса, а не
`EnsureCreated`. Устроено так:

- один контейнер `postgres:16` на процесс прогона (поднимается лениво, поэтому
  тестовые проекты без базы не платят за Docker; убирает его Ryuk сам);
- база на тест клонируется из шаблона, в котором миграции применены один раз на
  тип контекста — подъём базы на каждый тест превратил бы сотню тестов в
  десятки минут ожидания;
- `IDisposable`/`IAsyncDisposable` удаляют базу теста; `ClearAllPools()` перед
  `DROP DATABASE` обязателен, иначе пул держит подключение.

Требуется Docker: локально — Docker Desktop, в CI — раннер GitHub (он есть по
умолчанию). Сборка `Testcontainers.PostgreSql` в `.devcontainer` или отдельная
база в `docker-compose.yml` для тестов не нужны и были бы вторым источником
правды.

Что было заменено и почему это было нечестно (перевод 2026-10-03, 12 проектов):

| Было | Что проверяло на самом деле |
|---|---|
| `UseInMemoryDatabase` | только собранная модель; `ExecuteUpdateAsync` и `EF.Functions.ILike` падают, `Include` обязательной навигации теряет строки |
| `UseSqlite` + удерживаемое `SqliteConnection` | другой диалект: уникальность, пустые строки в датах, отсутствие `timestamptz`; `ILIKE` не переводится вовсе |
| `EnsureCreated` | схема из модели, а не из миграций: расхождение с production оставалось незамеченным до `RunMarsSchemaMigrationsAsync` |

Перевод вскрыл три production-дефекта, которых обходные провайдеры не показывали:
запись `DateTime` с `Kind=Local` в `timestamptz` (падал `AddToQueueAsync`),
смешение UTC и локального времени в проверке свежести токена и счётчик
фоловеров, возвращавший `SaveChangesAsync()` вместо числа обработанных записей.

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

## gRPC (вместо хабов SignalR)

- **Все `.proto` — в `src/MARS.Shared/Protos`.** Код генерируется `Grpc.Tools`
  (`<Protobuf Include="Protos\*.proto" GrpcServices="Both" ProtoRoot="Protos" />`)
  в `MARS.Shared`, у каждого proto свой `csharp_namespace`: `MARS.Shared.Grpc.*`
  (Media / Telegramus / Tuna / Scoreboard / SoundRequest / Voice / Commands). Одинаковый
  namespace для всех файлов не компилируется — имена сообщений (`SubscribeRequest`)
  начинают совпадать. Имена сообщений не совпадают и с C#-моделями: суффиксы
  `Payload`/`Snapshot`/`Kind`/`TtsUser` разводят их по разным пространствам.
- **Подписка — server-streaming `Subscribe`, вызовы клиент→сервер — unary.**
  Сервисы: `TelegramusService`, `TunaService` (MARS.Alerts, MARS.OBS),
  `ScoreboardService`, `SoundRequestService`, `VoiceRecognitionService`.
- **Ошибки — стандартными `grpc Status`, не своим полем.** `TunaGrpcService`
  и `SoundRequestGrpcService` уже бросают `RpcException(new Status(...))`; вторая
  система ошибок рядом с ними разошлась бы. Поэтому в `commands.proto` поля
  `error_code` нет: `NOT_FOUND`, `INVALID_ARGUMENT`, `PERMISSION_DENIED`,
  `UNAVAILABLE`, `DEADLINE_EXCEEDED` несут смысл сами.
- **Свой broadcaster недоступен извне** — он живёт в памяти сервиса, поэтому
  `ITelegramusNotifier` пишет только в свой собственный. Чтобы сервис мог
  вызвать оверлей другого, в `TelegramusService` есть unary-метод `Fire`
  (вброс `TelegramusEvent` подписчикам). Это делает `MARS.Alerts` не единственным
  писателем в оверлей, и `RickRollerService` при вызове через `Fire` не
  срабатывает — для команд вроде `/adhd` это то, что нужно.
- **Хостинг — `builder.AddMarsGrpcHosting()`**: поднимает `AddGrpc()` и два
  эндпоинта Kestrel — `0.0.0.0:8080` (`Http1`) и `0.0.0.0:8081` (`Http2`).
  gRPC без TLS работает **только** на явно Http2-эндпоинте: проверил на живом
  запуске, `Http1AndHttp2` и эндпоинт из `ASPNETCORE_HTTP_PORTS` отвечают на
  HTTP/2 с prior knowledge ошибкой `HTTP_1_1_REQUIRED`. Обратная сторона:
  `Listen*` в Kestrel полностью подавляет `ASPNETCORE_URLS`, поэтому оба порта
  объявляются кодом, а не конфигом.
- **Адресация клиентов — `http://<docker-service>:8081`** (внутренняя сеть).
  В `docker-compose.yml` у этих пяти сервисов `expose: ["8080", "8081"]`;
  наружу (Gateway, `9155:8080`) gRPC не выведен, YARP-маршрутов `/hubs/*` больше нет.
- **Рассылка — `GrpcEventBroadcaster<T>`** из `MARS.Shared`: у каждого подписчика
  свой ограниченный канал (по умолчанию 64 сообщения, `DropWrite`), поэтому медленный
  клиент роняет только свои события и никогда не блокирует отправителя.
  `BroadcastExceptAsync(subscriberId, …)` — замена `Clients.Others`, исключается
  подписчик, назвавший себя в запросе.
- **Тесты — TestServer + настоящий `GrpcChannel`**
  (`GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() }`).
  `ServerCallContext` в `Grpc.Core.Api` **не mockается**: все его члены не virtual,
  а `Grpc.Core.Testing.TestServerCallContext` лежит в неподключённом пакете.
  У блокирующего `ResponseStream.MoveNext` обязателен таймаут, иначе тест без
  ожидаемого события висит до конца прогона. Запрос подписки уходит лениво,
  при первом чтении потока: перед рассылкой ждать `broadcaster.SubscriberCount > 0`,
  иначе событие уйдёт в пустоту.
- Пакеты — только `Grpc.AspNetCore` и `Grpc.Tools` (`Directory.Packages.props`).
  Ниже 2.64.0 нельзя: `Microsoft.Extensions.Http.Resilience` предупреждает о
  конфликте с `Grpc.Net.ClientFactory`, а предупреждение видно в CI-сборке.
  `Grpc.Net.ClientFactory` отдельной строкой **не заводи**: он приезжает
  транзитивно из `Grpc.AspNetCore`, и его версия обязана совпадать с хостом —
  отдельная `PackageVersion` допустила бы расхождение с NU1605.
- **h2c работает без `AppContext.SetSwitch` — проверено на живом сокете.**
  Клиентские каналы регистрируются `GrpcClientExtensions.AddMarsGrpcClient<T>`.
  Переключатель `Http2UnencryptedSupport` на .NET 10 не нужен; проверка лежит в
  `tests/MARS.Shared.Tests/Grpc/H2cTransportTests.cs` и поднимает настоящий
  Kestrel с `Protocols = Http2` плюс настоящий `GrpcChannel`. `TestServer`
  для этой задачи не годится — он подменяет Kestrel целиком и не проверяет
  согласование протокола вовсе. Тест дополнительно фиксирует `HTTP/2` на
  стороне сервера: иначе ослабление конфига до `Http1AndHttp2` сделало бы его
  проходящим, ни разу не проверив h2c.
- **Адрес gRPC выводится из адреса REST подменой порта, а не свойством в
  `ServiceEndpoints`**: `GrpcClientExtensions.WithGrpcPort` меняет порт на
  `GrpcHostingExtensions.GrpcPort`. Причина — рефлексия
  `SwaggerEndpointMap.Build`, которая обходит все строковые свойства и приписывает
  `/swagger/v1/swagger.json`: свойство вида `CommandsGrpc` заставило бы агрегатор
  лезть за спекой на порт, обслуживающий только HTTP/2.

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
- **`net10.0-windows` у всех проектов не мешает Linux-образам.** TFM с
  `-windows` без `UseWindowsForms`/`UseWPF` собирается Linux-SDK без
  `EnableWindowsTargeting` и публикуется в `Microsoft.NETCore.App` — в
  `runtimeconfig.json` остаётся `"tfm": "net10.0"`, поэтому
  `mcr.microsoft.com/dotnet/aspnet:10.0` запускает сервис как обычно. Проверено
  на живом `docker build` и `docker run`. Если у проекта появится
  `UseWindowsForms`/`UseWPF`, сборка в Linux-образе упадёт — тогда TFM придётся
  откатить, а не чинить добавлением `EnableWindowsTargeting`.
- **Тесты гоняются и на Linux, а не только на Windows.** Матрица `tests` в CI
  работает на `ubuntu-latest`, и тест, написанный на виндовых путях или на
  `Path.GetInvalidFileNameChars()`, падает там, хотя локально зелёный. Две
  ловушки, пойманные на прогоне 2026-10-03:
  - `Path.GetInvalidFileNameChars()` на Linux знает только про NUL и «/».
    Набор запрещённых для имени файла поэтому берётся из константы
    (`ForbiddenFileNameChars` в `YouTubeResolver` обоих сервисов и в
    `TrashPathBuilder`): кавычка, `?`, `:`, `*`, `<`, `>`, `|`, `\` законны в
    Linux-контейнере, но ломают файл на Windows, который читает общий том
    `mars-wwwroot`.
  - `Path.DirectorySeparatorChar` на Linux — это `/`, и виндовый путь
    `C:\alerts\_converted\...` не пройдёт проверку кэша. Тестовые данные
    собираются через `Path.Combine`, а не пишутся константой.
  Проверять такие места локально можно честно и без CI:
  `docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/<проект>/<проект>.csproj` —
  тестовые проекты с `net10.0-windows` собираются и запускаются в Linux-контейнере.

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

## Навыки (skills)

Навыки лежат в двух зеркальных местах: `.opencode/skills/<имя>/SKILL.md` (его читает
opencode) и `.mimocode/skills/<имя>/SKILL.md` (его читает mimocode). **Правка одного
требует такой же правки второго** — иначе агенты получат разные инструкции.

| Skill | Когда нужен |
|---|---|
| `dotnet-test-run` | прогон тестов, фильтрация по классу/методу/трейту, разбор результата |
| `backend-test-helpers` | написание .NET-тестов: провайдер БД, фикстуры, `CancellationToken` |
| `gateway-route-add` | новый эндпоинт наружу: правило YARP + кластер + `ServiceEndpoints` |
| `frontend-type-check` | проверка и сборка `ClientApp` (npm, не yarn) |
| `build-for-good-ux` | состояния loading/error/empty, обратная связь, доступность |
| `make-no-mistake` | финальная самопроверка перед сдачей работы |
| `graphify` | запросы по графу знаний репозитория |

## Устаревшие источники — не использовать

`.mimocode/plans/` переехали из монолита и **не соответствуют этому репозиторию**.
Они ссылаются на несуществующие `MARS.Projects/MARS.Server`, `MARS.Tests`,
`mars.client`, `PostgresFixture`, `TwitchTestHelper`, `docker-compose.override.yml`,
`yarn build-api` и `yarn type-check`. Ничего из этого здесь не работает — проверяй по коду.

Навыки из `.mimocode/skills/` и `.opencode/skills/`, наоборот, **переписаны под этот
репозиторий** и применимы; доверять им можно, пока команда в них совпадает с `AGENTS.md`.

Достоверные источники: корневой `README.md`, `MARS.slnx`, `Directory.Packages.props`,
`global.json`, `docker-compose*.yml`, `.config/dotnet-tools.json` и код `src/MARS.Shared`.
`src/MIGRATION_CHECKLIST.md` — исторический статус переноса из монолита, не спецификация.
