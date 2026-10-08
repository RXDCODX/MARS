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

# клиент Gateway: оверлеи, админка, сайт (React/Vite, Yarn 4)
cd src/MARS.Gateway/ClientApp && corepack enable && yarn install --immutable
npx tsc -b --noEmit && yarn test && yarn build

# UI хранилища (React/Vite)
cd src/MARS.MediaStorage/ClientApp && npm ci && npm run typecheck
```

### Готовые скрипты: `scripts/windows` и `scripts/unix`

Те же команды собраны в `scripts/` — отдельно для Windows (PowerShell 5.1) и unix
(bash 3.2+), с одинаковыми именами: `build`, `test`, `verify`, `format`,
`frontend`, `e2e`, `coverage`, `release`, `stack`, `migrate`, `clean`, `sweep`,
`pr`. Карта и отличия платформ — `scripts/README.md`.

```powershell
.\scripts\windows\verify.ps1                 # гейт перед пушем: формат → сборка → тесты
.\scripts\windows\test.ps1 -Project MARS.Shared.Tests -FilterClass "*HealthCheck*"
```

```bash
./scripts/unix/verify.sh
./scripts/unix/test.sh --project MARS.Shared.Tests --filter-class '*HealthCheck*'
```

Правила, которые скрипты уже соблюдают, а руками забываются:

- **Фильтры.** `test` принимает только MTP-имена (`-FilterClass`,
  `-FilterMethod`, `-FilterNamespace`, `-FilterTrait`; `--filter-class` и т.д.) и
  никогда не передаёт VSTest-овский `--filter`, который молча находит ноль
  тестов. Это проверяется `ScriptsParityTests`;
- **Параллелизм по умолчанию выключен.** Каждый проект с базой поднимает свой
  контейнер Testcontainers, и параллельный прогон решения на одной машине
  заканчивается `[FATAL ERROR] Foreground threads were left running, forcing
  process exit` при нуле красных тестов и коде 1. Поэтому `test` и `verify`
  гоняют проекты по очереди, а CI по-прежнему делает по одному проекту на задачу;
- **`--use-test-host` / `-UseTestHost`** обходит интеграцию `dotnet test` с MTP,
  когда она находит ноль тестов при зелёной сборке (см. ловушку ниже);
- **Покрытие.** `coverage.ps1` только делегирует
  `.github/scripts/coverage-local.ps1` — своя реализация того же пути разошлась
  бы с CI молча; `coverage.sh` повторяет его шаги на unix, а тест сверяет
  значения фильтров с каноническим скриптом;
- **Публикация.** `release` берёт список образов из матрицы
  `release-microservices.yml`, поэтому новый сервис не нужно вписывать в скрипт,
  и `-Push`/`--push` обязателен явно. Тег версии ставится отдельным действием
  `Tag`: пуш тега `v*` и есть триггер публикации;
- **Стенд.** `stack` и `e2e` держат файл окружения и всегда передают
  `--env-file`: дефолтного `.env` в репозитории нет, и без флага compose взял бы
  значения по умолчанию из `${ПЕРЕМЕННАЯ:-}`, то есть поднял стенд на пустых
  паролях. Среда выбирается тем же переключателем, что и compose-файл: без него
  `.env.production`, с ним `.env.development` — разъехаться могут только два
  переключателя подряд, а их один. Файл создаётся из `.env.<среда>.example`, а
  пока таких шаблонов нет — из `.env.example` с предупреждением. Плюс `--wait` у
  `compose up` и `--network host` + `--shm-size=1g` у контейнера с Playwright;
- **Скрипты не коммитят, не мержат и не публикуют образы без явного указания** —
  это тоже проверяется тестом.

Новый скрипт обязателен на обеих платформах с одинаковым именем: расхождение
одиноко проверено `tests/MARS.Gateway.Tests/ScriptsParityTests.cs` (набор имён,
поиск корня от каталога скрипта, отсутствие путей машины, синтаксис
MTP-фильтров, фильтры покрытия, чтение матрицы публикации).

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

### Ловушки покрытия, найденные на живых прогонах

Все ловушки ниже проверены на этой машине, а не взяты из документации.

- **`dotnet test` может найти ноль тестов при рабочем самом тесте.** На SDK
  10.0.401 интеграция `dotnet test` с Microsoft.Testing.Platform отдавала
  «Запущено ноль тестов» и код 5, тогда как запуск того же dll через
  `dotnet exec` выполнял все тесты. Если сборка и сам тест-проект собираются, а
  прогон не находит ни одного теста — это оно, а не сломанный тест. Обход:
  `dotnet exec tests/<проект>/bin/Release/net10.0-windows/<проект>.dll`.
- **`NodeJS.Timeout` вместо `number` в браузерном коде.** `vite/client`, на
  который ссылается `vite-env.d.ts`, подтягивает `@types/node`, и
  `globalThis.setTimeout` начинает резолвиться в ноду. `types` в `tsconfig`
  ограничивает только автоподключение и от этого не спасает. Тип таймера берётся
  у функции: `ReturnType<typeof setTimeout>`. Тип узла работает и компилируется —
  ловушка в том, что он остаётся в коде: `NodeJS.Timeout` на таймере в
  браузерном модуле означает, что тип приехал из `@types/node`, и в сборке,
  где подключений не будет, такой код перестанет собираться. В `ClientApp`
  таких мест 13, и новые добавлять нельзя.
- **Глобалы сторонних скриптов не объявлены.** `YT`, `onYouTubeIframeAPIReady`
  и `webkitAudioContext` живут в `src/types/browser-globals.d.ts`. Объявлять
  через `var`, а не `const`: только `var` становится свойством глобального
  объекта, и только тогда работает `globalThis.YT`.
- **`Array.from({length}).fill(x).flat()` теряет тип.** Массив выходит
  `unknown[]`, и каждое обращение к полям элемента падает с «is of type
  unknown». Форма `Array.from({ length }, () => x)` тип сохраняет.
- **Конверт `OperationResult` надо разворачивать в транспортном слое.** Клиент
  написан под тело с полем `data`, а сервисы отвечают `{ success, result }`, и 52
  места вызова в 12 файлах читали `result.data.data`, получая `undefined`.
- **Тип `description` у antd `Alert` перекрыт значением из ARIA-описания.**
  Передать текст или узел нельзя, обход собран в `ServerViewer/ErrorAlert.tsx`.
  Поля `extra` у `Alert` нет вовсе — действие передаётся через `action`.
- **Сортировка заголовков antd обрезана до пяти уровней.** `level={6}` не
  существует.
- **Строковые литералы вместо членов перечисления не расширяются.** Тест с
  `type: "Image"` не собирается, пока не подставит `MediaFileInfoTypeEnum.Image`.
- **Файлы `bell.wav`, `mute.png`, `svadba.mp3` в репозитории отсутствуют** и не
  были ни в одном коммите: они остались в монолите. `/Alerts/*` теперь
  обслуживается маршрутом `alerts-media` и раздачей wwwroot в
  `MARS.MediaStorage`; без самих файлов ответ честный 404, а не HTML-заглушка
  клиента.

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
  (`release-microservices.yml`, триггер — тег `v*`). Задачи: `build`, `tests`
  (матрица по 17 тестовым проектам, `fail-fast: false`), `frontend`, `e2e`,
  `coverage`. Работа идёт через PR (см. «Git workflow»), поэтому ветку
  проверяет PR-триггер, а push-триггер срабатывает уже после merge в `main`.
- **Отдельный статус на каждый тестовый проект** получается из матрицы:
  `tests / MARS.Gateway.Tests` — самостоятельный check в branch protection.
  Новый тестовый проект ⇒ запись в матрицу `tests` в `ci.yml`, иначе он не
  проверяется вообще и это молчаливо.
- **`MARS.ClientUi.Tests` в матрицу `tests` не входит** — его гоняет задача
  `e2e`, потому что проект не ссылается ни на один проект из `src/` и измерять в
  нём нечего: coverlet выдал бы пустой отчёт и испортил счёт по методам в общем
  гейте. Исключение из сверки списка `tests/` с матрицей — в
  `.github/scripts/coverage-local.ps1` (`$coverageExcluded`); без него скрипт
  бросает исключение с честным сообщением «проект не в матрице».
- **`e2e` поднимает стенд сам**: `cp .env.production.example .env.production` (файл
  окружения в git не попадает), `docker compose --env-file .env.production up -d
  --build --wait`, гоняет тесты в контейнере с `--network host` и
  `--shm-size=1g`, гасит стенд при `always()`. Без
  `--network host` контейнер не увидит Gateway на localhost раннера, а 64 МБ
  `/dev/shm` недостаточно Chromium — он падает с «Target crashed» без внятной
  причины.
- **`frontend` гоняет клиент**: `corepack enable && yarn install --immutable`,
  `npx tsc -b --noEmit`, `yarn test` и `yarn build`. `SKIP_STORYBOOK_VITEST=1`
  обязателен, иначе vitest поднимает сюжеты Storybook, а тот ставит Playwright и
  качает Chromium.
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
  на `main` и PR в `main` гоняет `dotnet csharpier format .` и **ложит результат в
  последний коммит** (`git commit --amend`), отдельного коммита от бота не
  остаётся. На feature-ветках и в форках не наезжает. Локально то же:
  `dotnet csharpier format .`, проверка — `dotnet csharpier check .`.
  **Мы работаем теперь только в ветках, поэтому на наш код автоформат не приходит
  совсем** — форматировать локально обязательно, см. «Ветки и auto-format» в
  разделе «Git workflow». Три последствия ниже описывают правку `main` и к
  веткам не применяются:
  1. **Ветка переписывается, коммит меняет хеш.** Локальный `main` после
     автоформата разъезжается с `origin`, и `git pull --ff-only` упадёт.
     Перед пушем — `git fetch origin && git reset --hard origin/main`.
     Пушить надо уже с этого состояния.
  2. **Push сделанным `GITHUB_TOKEN` не запускает workflow** (защита GitHub от
     рекурсии), поэтому после amend проверки на новом хеше не появились бы сами, а
     обязательные статусы в branch protection не закрылись бы никогда. Workflow
     запускает их сам: `gh workflow run ci.yml --ref <ветка>`, и на это есть
     `permissions: actions: write`. С секретом `AUTO_FORMAT_TOKEN` (PAT) push идёт
     им, проверки запускаются сами, а перезапуск пропускается — так у PR появляются
     и статусы проверок самого PR, которых у `workflow_dispatch` не бывает.
  3. **Force-push привязан к SHA, с которого начался прогон** (`--force-with-lease`
     со значением из `env.START_SHA`). Ожидаемое значение из `FETCH_HEAD` после
     `git fetch` годилось бы только на вид: fetch обновляет tracking ref текущей
     головой ветки, lease против него совпадает всегда, и перезапись сносила бы
     чужой коммит молча. Воспроизведено на живом git: force-push проходил, хотя
     ветку только что увеличили. Ветка уехала вперёд → push не проходит, workflow
     предупреждает, следующий прогон форматирует поверх.
- **Коммит, который переписал автоформат, видно по `Co-authored-by`.** Автор
  прежний, коммитер — `github-actions[bot]`; amend проверяет, что автор пережил, и
  падает, если нет. Проверки этих договоров — в
  `tests/MARS.Gateway.Tests/AutoFormatWorkflowTests.cs`.
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

## Git workflow: ветка + PR

Любая работа идёт в отдельной ветке и завершается Pull Request'ом с описанием.
Пуш в `main` напрямую запрещён.

- **Ветка на задачу, не на файл.** Одна задача (один законченный Red/Green-цикл
  или один логический блок) — одна ветка. Имена: `feat/…`, `fix/…`, `chore/…`,
  `docs/…` + короткий kebab-case смысл, по-английски:
  `feat/matoi-booru-gateway`, `fix/random-art-empty-page`.
  Ветку создают **до** первой правки, иначе первый же коммит ляжет в `main`:
  `git switch -c feat/<имя>`.
- **Пуш только в свою ветку:** `git push -u origin <ветка>`. `git push origin main`
  не делается никогда — даже одной строки. Исключение, которое надо проговорить
  отдельно: если в `main` уже лежит твоя незакоммиченная работа (см. ниже про
  auto-format), она сначала переносится в ветку, а не выталкивается в `main`.
- **Перед пушем обязателен зелёный `dotnet build MARS.slnx -c Release` и
  `dotnet test MARS.slnx -c Release`.** Раньше это было нужно потому, что код
  сразу попадал в `main` и дальше его собирал CI; теперь CI собирает PR, но
  локальный прогон остаётся обязательным — он единственный, кто отличает
  «красный новый тест» от «красный сломанный соседний проект».
- **Перед пушем — `dotnet csharpier format .`.** Это теперь не опция: с переходом
  на ветки `auto-format.yml` **перестал наезжать** (см. ниже), и неотформатированный
  код дойдёт до `main` без автоправки.
- **Коммит — это логический шаг, а не файл.** Шаг — цикл Red/Green целиком
  (падающий тест + реализация), поэтому промежуточный красный не коммитится.
  Коммиты в ветке — линейная история, без `fixup!`/`squash` задним числом:
  так ревью видно, где Red, а где Green.
- **PR создаётся через `gh pr create`** после пуша ветки, с заполненным телом
  (шаблон ниже). Голый PR без описания — не считается сделанной работой.
- **Агент не мержит.** `gh pr merge` не вызывается: merge и удаление ветки —
  решение владельца. В ответе возвращается ссылка на PR.
- **GITHUB_TOKEN**: переменная окружения пользователя содержит GitHub token с
  доступом к репозиторию, доступна как `$env:GITHUB_TOKEN` (PowerShell);
  `gh` её подхватывает сам.

### Описание PR — обязательный состав

Тело PR читает человек, который не помнит контекст задачи. Пустые или
формальные разделы хуже отсутствующих: они выглядят как заполненные.

```markdown
## Что
Одно предложение: какое поведение изменилось.

## Зачем
Проблема или требование, из которого это следует. Ссылка на исходный
дефект/задачу, если была.

## Как
Ключевые решения и почему именно так. Для перехода на внешний источник —
что он делает, чем платим (лимиты, авторизация, кэш).

## Проверка
Чем проверялось: имя тест-проекта и класс/метод теста, либо ручной сценарий
на стенде с командой, которой он выполнен.

## Миграция и откат
Что нужно владельцу: новые переменные в `.env.production` (или
`.env.development`), пересоздание тома, перезапуск стенда. Как откатить: revert
коммита или откат версии образа.

## Сквозные правки
Что ещё обязано поменяться рядом и уже изменено: оба `.env.*.example`, compose,
`Directory.Packages.props`, `ci.yml`, `ServiceEndpoints`, README, миграции.
```

Раздел «Сквозные правки» — не формальность: больше всего ошибок этого репозитория
приходит именно от хвостов в несвязанных файлах (см. таблицу ниже).

### Что теперь делает CI и почему это важно знать

- Триггер `ci.yml` — PR в `main`, поэтому **каждый коммит в ветке прогоняет
  `build`, `tests`, `frontend`, `e2e`, `coverage`**. Промежуточный коммит с
  зелёной сборкой локально, но красным в CI (например, `e2e`, гоняющий стенд),
  не повод продолжать ветку — сначала разобраться.
- Матрица `tests` даёт отдельный статус на каждый тестовый проект, и в branch
  protection эти статусы обязательны. Новый тестовый проект без записи в
  матрицу не проверяется **молча** — PR останется зелёным и неполным.
- `coverage` запускается только при `needs.tests.result == 'success'`. Упал один
  тестовый проект — упали бы две задачи по одной причине, а покрытие посчиталось
  бы по неполным данным.

### Ветки и auto-format: правило переписывает прежнее

Прежнее правило описывало, что делать с переписанным коммитом в `main`.
С переходом на ветки оно **перестало быть актуальным и опасным**, и применять
его нельзя:

- **`auto-format.yml` больше не наезжает на нашу работу.** Он запускается на
  `main` и в PR в `main`, то есть уже **после** merge. Ветку он не трогает,
  поэтому `dotnet csharpier format .` — наша собственная обязанность до пуша.
- **Три прежних последствия (переписанный коммит, `GITHUB_TOKEN` без workflow,
  `--force-with-lease` по `FETCH_HEAD`) к веткам не применяются.** `force-push`
  своей ветки допустим, но только с явным `--force-with-lease=<sha>` от
  последнего запушенного коммита: `--force-with-lease` без значения опирается на
  tracking ref и молча сносит чужую работу (воспроизведено на живом git).
- **После merge `main` может переписать автоформат**, и тогда открытый PR
  покажет конфликт. Это нормально: подтягивать `origin/main` и перебазировать
  свою ветку, а не пушить в `main`.

### Ветки, вошедшие в `main`, удаляются

- Локально — `git branch -D <ветка>`, удалённо — `git push origin --delete <имя>`.
  Проверять перед удалением: `git merge-base --is-ancestor <ветка> origin/main`.
- Именно `-D`, а не `-d`: `-d` сверяется с upstream-веткой, а не с `main`, и
  после merge отказывается удалять уже смерженную ветку («not fully merged»).
  `origin/main` — единственный верный адресат сравнения.
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

### Фронтенды

В репозитории **два** фронтенда, и они разные. Путать их нельзя: наборы
зависимостей, проверок и ловушек у них не совпадают.

| | `src/MARS.Gateway/ClientApp` | `src/MARS.MediaStorage/ClientApp` |
|---|---|---|
| Что это | оверлеи, админка и сайт | страница хранилища |
| Состояние | `zustand` | `useState`/`useReducer` компонента |
| Библиотеки | antd, `antd-style`, Storybook, react-router | Chakra, zustand, Storybook, react-router |
| Инструменты | Yarn 4, `yarn install --immutable` | Yarn 4 |

Общее для обоих: импорты React только именованные, `useMemo` для дорогих
вычислений, `useCallback` для функций, уходящих в зависимости хуков, цвета и
стили из файлов стилей, а не литералы в разметке.

**`src/MARS.MediaStorage/ClientApp`:** стор-библиотеки нет; не ссылаться на
`useStore.getState()`, `useShallow` и `ToastModal` — их тут не существует.
Виртуализация списка ручная, на `ROW_HEIGHT`/`OVERSCAN`, см. `App.tsx`.
Проверка его типа команд — `cd src/MARS.MediaStorage/ClientApp && npm ci`.

**`src/MARS.Gateway/ClientApp`:**

- **Пакеты — Yarn 4, а не npm.** В репозитории лежит `yarn.lock`, `package.json`
  объявляет `packageManager: yarn@4.18.0`, а `.yarnrc.yml` разрешает
  `enableScripts`. `npm ci` требует `package-lock.json`, которого нет, и не
  выполняет postinstall. Локально и в CI — `corepack enable &&
  yarn install --immutable`; в образе то же самое в `ClientApp.Dockerfile`.
  Второй lock-файл рядом с первым означает два источника правды: собирать надо
  тем же инструментом, каким собирается образ.
- **Проверки:** `npx tsc -b --noEmit`, `yarn test` (436 тестов), `yarn build`.
- **Хабы — через `HubAdapter`.** Прямые `new HubConnection(...)` и `.build()`
  из react-signalr выпилены; реестр адаптеров — в `src/shared/realtime`
  (`createHubRegistry`, `hubConnection.ts`, по реестру на хаб: overlay, tuna,
  scoreboard, soundrequest). У каждого хаба свой реестр: один адаптер на два
  означал бы, что события табло едут в оверлейный сокет, где их никто не слушает.
  `createHubConnection` идемпотентен — иначе два компонента, смонтированные
  одновременно, или StrictMode открыли бы два соединения на один хаб.
- **Адрес хаба собирается `resolveHubUrl`, а не склейкой строки.**
  `${import.meta.env.VITE_BASE_PATH}hubs/overlay` при незаданной переменной даёт
  `undefinedhubs/overlay`, и та же ошибка уехала бы в боевое окружение.
- **Отправка от клиента.** `HubAdapter.invoke` типизирован картой
  `HubInvocationMap`; вызовы без контракта идут через `send`. На сервере
  реализованы не все: `MuteAll`, `UnmuteSessions`, `ObsFreeze`, `ObsUnfreeze`,
  `ExplosionGo`, `MikuMikuDeleteTwitchMessages` есть в карте, но методов хаба
  не имеют — это известно и помечено в `hubAdapter.ts`, опечатку не спутать с
  несуществующим методом.
- **Конверт `OperationResult` разворачивается один раз, в транспортном слое**
  (`http-client.ts`): полезная нагрузка кладётся и в `result`, и в `data`,
  потому что 52 места вызова в 12 файлах читают `result.data.data`. Знание о
  форме ответа не должно размазываться по компонентам. Отказ
  (`success === false`) превращается в исключение с текстом сервиса.
- **Перечисления сериализуются именами.** Конвертер добавлен в `AddMarsDefaults`
  через `Configure<JsonOptions>`, а не через `AddControllers()`: у
  `MARS.Videos365` контроллеров нет, и добавлять MVC в общих настройках
  означало бы менять состав сервиса, а не формат ответа.
- **Маршруты для навигационных тестов порождаются из кода**
  (`routes.generated.json` из `allRoutes`), а не пишутся руками. Проверка
  сверяет файл с кодом и падает при расхождении; обновление — только под
  `UPDATE_ROUTES_MANIFEST=1`. Файл, который тест сначала пишет, а потом
  сравнивает, всегда равен себе — проверки в нём нет.

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
  тестовые проекты без базы не платят за Docker; удаление — по правилу
  «Контейнеры тестов обязаны удаляться после прогона» ниже, Ryuk на это не
  рассчитываем);
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

### Контейнеры тестов обязаны удаляться после прогона

Ryuk (resource reaper) — **страховка, а не механизм удаления**, и полагаться на
него нельзя. Проверено на этой машине, 2026-10-04:

- **Ryuk может не запуститься вовсе.** После прогонов в `docker ps -a` лежит
  `testcontainers/ryuk:0.14.0` в состоянии `Created` (метки
  `org.testcontainers=true`, `org.testcontainers.ryuk=true`): контейнер создали,
  но не подняли — сборщик мусора не отработал ни разу, и контейнеры, за которые
  он отвечал, остались в Docker. Значит «Ryuk сам уберёт» — не основание, удаление
  обязано быть в коде теста. `TESTCONTAINERS_RYUK_DISABLED=true` в коде и в CI
  запрещён: он не ускоряет прогон, а гарантированно оставляет мусор.
- **Анонимный том `postgres:16` не удаляется вообще.** Образ объявляет `VOLUME
  /var/lib/postgresql/data`, и этот том создаёт демон **до** контейнера, поэтому в
  нём остаётся одна метка `com.docker.volume.anonymous` — меток контейнера
  (`org.testcontainers.*`) там нет, а Ryuk удаляет ресурсы по меткам и такой том
  не видит. Проверено: `docker run` + `docker rm -f` увеличивают
  `docker volume ls -f dangling=true` на единицу (39 → 40). Удалить такой том
  Testcontainers не умеет, поэтому `PostgresContainerScope` монтирует PGDATA в
  **tmpfs** (`WithTmpfsMount`): монтирования нет вовсе, и прогон не создаёт том
  (`docker inspect` показывает пустой `Mounts`).
- **Пул Npgsql переживает удаление контейнера.** Соединение, отданное в пул,
  выдаётся снова без проверки, и `OpenAsync` проходит успешно при мёртвом сервере:
  тест «после удаления контейнера база не отвечает» без `ClearAllPools()` врал бы в
  сторону «жив». Ловушка поймана на живом прогоне, до `ClearAllPools()` тест падал
  именно этим.

Правила:

- Удаляет ресурс **тот же код, который его поднял**: `DisposeAsync` фикстуры или
  `IAsyncLifetime.DisposeAsync`, а не «тест закончился». Вызывать удаление
  обязательно и при падении теста, и при отмене, и **с `CancellationToken.None`**:
  под уже отменённым `TestContext.Current.CancellationToken` удаление не
  выполняется, и контейнер остаётся именно в том прогоне, который разбирают.
- Удаление **дожидается**: `StopAsync`/`DeleteAsync`, а не `Stop`. Остановленный,
  но не удалённый контейнер остаётся в `docker ps -a` и держит имя.
- Контейнер, общий на процесс (`MarsPostgres`), удаляется последним — после того,
  как дропнуты базы тестов, потому что базу удаляет не он, а
  `PostgresTestDbContextFactory.DisposeAsync` (`ClearAllPools()` перед
  `DROP DATABASE`). Точка «после выполнения тестов» — выход из процесса:
  `MarsPostgres` вешает `AppDomain.CurrentDomain.ProcessExit` и удаляет контейнер
  там, а Ryuk остаётся страховкой на случай убийства процесса.
- Новый контейнер в тестах — через обёртку в `MARS.TestKit`
  (`PostgresContainerScope` либо `PostgresTestDbContextFactory`-подобную фабрику),
  а не `new PostgreSqlBuilder(...)` в теле теста; отдельный Testcontainers-пакет
  тестовому проекту не нужен, всё живёт в TestKit.
- Общий контейнер процесса **нельзя удалять посреди прогона**: параллельные тесты
  уже ходят в него. Проверять удаление надо на своём контейнере — так и сделан
  `tests/MARS.Shared.Tests/Postgres/PostgresContainerScopeTests.cs`.
- Никаких ручных `docker run` в тестах и никаких `WithCleanUp(false)`: ресурсы
  стенда и ресурсы тестов должны различаться по меткам, иначе автоочистка съест
  чужое.

Проверка после прогона:

```bash
# ни одного контейнера с метками testcontainers, кроме ничего
docker ps -a --filter "label=org.testcontainers" --format "{{.Names}}\t{{.Status}}"
# дельта, а не ноль: на этой машине в списке уже 40 чужих висящих томов
docker volume ls -f dangling=true -q
```

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
  наружу (Gateway, `10155:8080`) gRPC не выведен, YARP-маршрутов `/hubs/*` больше нет.
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
service name + `:8080` для всех. Наружу опубликован только Gateway (`10155:8080`).

Swagger-агрегатор строит карту рефлексией по строковым свойствам `ServiceEndpoints`
(`MARS.Shared/Configuration/ServiceEndpoints.cs`), а не по своему списку: опечатка в имени
свойства молча уронила бы маршрут. Новый эндпоинт ⇒ правило в `Yarp:Routes` + кластер +
свойство в `ServiceEndpoints`.

`src/README.md` устарел: там перечислены порты 5001–5013 (они есть только в
`docker-compose.dev.yml`), нет `MARS.Videos365`, и написано, что CPM отключён —
это неверно. `MARS_GATEWAY` использует `Gateway:MaxRequestBodyBytes` (256 МБ по умолчанию):
лимит применяется до проксирования, уменьшать его нельзя без проверки загрузок в хранилище.

**Клиент раздаётся отдельным контейнером `client-ui`**, и в маршрутах это
обязательно отражается: `spa` с `Order: 1000` и `/{**remainder}` стоит последним,
иначе catch-all перехватит `/api/*`, `/hubs/overlay`, `/storage-ui/*`, `/memory/*`
и `/Alerts/*`. Список защищённых префиксов проверяется тестом
`tests/MARS.Gateway.Tests/OverlayHubRouteTests.cs` — опечатка в маршруте не
роняет сборку, YARP просто отдаёт 404.

Контейнер объявлен отдельным блоком **без** `<<: *service-defaults`: наследование
притащило бы .NET-окружение и curl-healthcheck на `:8080`, а клиент слушает 80.
Публикации у него нет — наружу выходит только Gateway.

## Docker

- `docker-compose.yml` — production-образы. `docker-compose.dev.yml` подключается
  **явно** (`-f docker-compose.yml -f docker-compose.dev.yml`) и использует
  `target: dev` c `dotnet watch`. Он переименован из `docker-compose.override.yml`
  намеренно, чтобы обычный `docker compose up` не подхватывал hot-reload молча.
- Каждый `Dockerfile` ставит `curl` в базовый stage — compose-healthcheck'и его требуют
  (в `mcr.microsoft.com/dotnet/aspnet` его нет, иначе exit code 127 → unhealthy).
  `MARS.MediaStorage` дополнительно ставит `git` для синка wwwroot.
- **Healthcheck postgres обязан ходить по TCP, и это не украшение.** На пустом томе
  entrypoint поднимает временный сервер только на unix-сокете, выполняет
  `docker-entrypoint-initdb.d` и гасит его, и только потом поднимает настоящий,
  уже слушающий порт. `pg_isready` без `-h` стучится в сокет и зеленеет в этом
  первом окне: на живой машине разрыв составил **109 секунд** (21:41:05
  `ready to accept connections` → 21:42:54 `listening on IPv4 0.0.0.0, port 5432`).
  Все сервисы compose запускает по `service_healthy`, то есть в это окно, и падают
  на `Connection refused`. Дальше срабатывает `restart: unless-stopped`, стенд в
  итоге здоров, но `--wait` уже отдался «dependency failed to start: container … is
  unhealthy» — то есть стек чинил себя сам, а проверка падала вхолостую. В CI тома
  всегда свежие, значит повторялось на каждом прогоне. У `rabbitmq` то же milder:
  `rabbitmq-diagnostics -q ping` отвечает про живость ноды, а не про порт 5672, и
  добавлена `check_port_connectivity`. Обе правки охраняются
  `tests/MARS.Gateway.Tests/ComposeReadinessTests.cs`.
- **На Docker Desktop для Windows rabbitmq на свежем томе может упасть с
  `Error when reading /var/lib/rabbitmq/.erlang.cookie: eacces`.** Это особенность
  машины, а не репозитория, и в CI (Linux) её нет. `restart: unless-stopped`
  вытаскивает брокер со второй попытки, а отличить это от настоящего дефекта можно
  по тому, что попытка вторая и логи чистые.
- Том `mars-wwwroot` общий для `obs`, `alerts`, `media-storage`: конвейер
  «алерт → файл» пересекает эти три контейнера, потеря тома рвёт его.
- Данные git-метаданных `media-storage` вынесены в отдельный том: пустой volume поверх
  `/app/wwwroot/.git` замаскировал бы каталог и сломал клонирование.
- **Окружений два, и каждое соответствует своему `ASPNETCORE_ENVIRONMENT`**:
  `.env.development` идёт с `docker-compose.dev.yml`
  (`Development` → `appsettings.Development.json`), `.env.production` — с
  `docker-compose.yml` (`Production` → `appsettings.json`). Реальные файлы в
  git не попадают (правило `.gitignore` широкое: `.env.*`), шаблоны
  `.env.*.example` и перечень ключей `.env.example` попадают, а compose получает
  нужный **флагом `--env-file`** — файла по умолчанию (`.env`) в репозитории нет,
  и без флага compose поднимет стенд на `${ПЕРЕМЕННАЯ:-}`, то есть на пустых
  паролях. Переключатель один (`-Dev` у скриптов) намеренно: два переключателя
  подряд разъезжаются молча. Паритет ключей шаблонов с `docker-compose.yml`,
  отсутствие секретов в боевом шаблоне и выбор файла окружения по профилю
  проверяет `tests/MARS.Gateway.Tests/EnvironmentFilesTests.cs`.
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
оставить хвосты в остальных. Собственный diff этого не показывает: шаблоны `.env.*.example`
просто не входит в список изменённых, а стенд при этом работает. Так потерялись
`SEQ_ADMIN_PASSWORD`, `Loki__Url` и `LogsDb` при выпиливании Seq, и `.env.example`
при переходе Jaeger→Tempo.

Меняй **вместе** (проверено на двух заменах — обе оставили хвост):

| Что меняешь | Обязательно тронуть |
|---|---|
| Переменную окружения | **оба** `.env.*.example` и `.env.example`, `docker-compose.yml` (env сервиса или `x-service-env`), `src/*/appsettings*.json`, код `configuration["…"]`, таблица env в `README.md` |
| `ConnectionStrings__X` | `AddMarsDefaults` **и** `AddMarsDbContext` (имена обязаны совпасть), оба `appsettings*.json`, compose |
| Компонент стека (образ/контейнер) | сервис и тома в `docker-compose.yml`, файл в `infrastructure/grafana/datasources/`, оба `.env.*.example`, README, комментарии |
| Публикуемый порт | compose, README, `docker-compose.dev.yml` |
| Имя метрики в `MarsMetrics` | PromQL в `mars-overview.json`, проверка в `/metrics` |
| БД нового сервиса | `MARS_*_PASSWORD` в обоих `.env.*.example`, список `dbs` в `infrastructure/db-init/01-databases.sh`, таблица БД в README, `Data/DesignTime/*DbContextFactory` |
| Пакет NuGet | `Directory.Packages.props` **и** `.csproj` (иначе NU1008) |
| Новый сервис | `MARS.slnx` (папки `/src/` и `/tests/`), `tests/MARS.X.Tests`, `Dockerfile`, compose, таргеты в `infrastructure/prometheus/prometheus.yml`, `ServiceEndpoints.cs`, `Yarp:Routes` + кластер, матрица release-workflow |
| Новый тестовый проект | `MARS.slnx` (папка `/tests/`), `PackageReference` `coverlet.MTP`, матрица `tests` в `.github/workflows/ci.yml` |
| Новый скрипт в `scripts/` | файл на **обеих** платформах с тем же именем (`X.ps1` и `X.sh`), `scripts/README.md` в карту, бит `+x` для `.sh` (`git update-index --chmod=+x`), `ScriptsParityTests` не должен падать |
| Контейнер в тестах | удаление в `DisposeAsync`/`IAsyncLifetime`, обёртка в `MARS.TestKit`, проверка `docker ps -a --filter "label=org.testcontainers"` после прогона |

Перед завершением прогони sweep по **старому** имени, переменной или порту:

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.json,*.yml,*.yaml,*.md,*.props,*.csproj,.env*,*.example,*.alloy,*.slnx -LiteralPath . |
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
- **`01-databases.sh` выполняется только на пустом томе.** Добавил строку в
  `.env.production.example` и `.env.development.example` — базы не появятся, пока
  не пересоздан `mars_postgres_data`. Правка шаблона не чинит запущенный стенд:
  compose читает файл, переданный `--env-file`, а он в git не попадает. Пустая
  `MARS_*_PASSWORD` останавливает скрипт до любых изменений.
- **Файлы окружений в git не входят, но sweep их видит.** Мёртвая переменная в
  `.env.production` всплывёт поиском — удалять её вручную, молча не правь файл с
  секретами. И `.dockerignore` ловит их правилом `**/.env.*`: без него реальные
  файлы окружений попадут в контекст сборки образа вместе с токенами.

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

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
