# AGENTS.md

MARS — набор .NET 10 микросервисов с RabbitMQ и YARP Gateway. Монолит находится в отдельном репозитории `MARS_old`; его пути и старые инструкции здесь неприменимы.

## Быстрый старт

Основные команды собраны в `scripts/windows` (PowerShell 5.1) и `scripts/unix` (bash 3.2+). Имена скриптов совпадают; карта — `scripts/README.md`.

```powershell
.\scripts\windows\verify.ps1
.\scripts\windows\test.ps1 -Project MARS.Shared.Tests -FilterClass "*HealthCheck*"
```

```bash
./scripts/unix/verify.sh
./scripts/unix/test.sh --project MARS.Shared.Tests --filter-class '*HealthCheck*'
```

Полезные прямые команды для точечной проверки:

```bash
dotnet build src/MARS.X/MARS.X.csproj -c Release
dotnet test tests/MARS.X.Tests/MARS.X.Tests.csproj -c Release
dotnet csharpier format . && dotnet csharpier check .
```

Для миграций найди фактический `.csproj`, который содержит `DbContext` и его `*DbContextFactory`. Фабрика может находиться в отдельном проекте `Infrastructure` сервиса, например под `Data/DesignTime/`; не предполагай фиксированный суффикс или расположение по имени проекта. Скрипты `scripts/windows/migrate.ps1` и `scripts/unix/migrate.sh` принимают имя сервиса и находят проект по наличию design-time factory.

- Для Gateway UI: `cd src/MARS.ClientUi && corepack enable && yarn install --immutable && npx tsc -b --noEmit && yarn test && yarn build`.
- UI хранилища: Razor Pages + vanilla JavaScript в `src/MARS.MediaStorage/MARS.MediaStorage.Host/Pages/Storage` и `StorageUiAssets`; отдельная Node/npm-сборка не нужна. Проверяй `MARS.MediaStorage.Host` и `MARS.MediaStorage.Tests`.
- Для локального покрытия и анализа пробелов используй skill `mars-coverage`; каноническая реализация — `.github/scripts/coverage-local.ps1`.
- Тестовые проекты используют Microsoft.Testing.Platform. Не передавай VSTest `--filter`: фильтры MTP идут после `--` (`--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`). Подробности — в `dotnet-test-run`.
- Параллельный прогон решения локально по умолчанию выключен: Testcontainers и фоновые потоки могут ломать завершение тестового хоста. Используй скрипты репозитория и их штатные настройки.

## Рабочий процесс

1. Перед первой правкой создай отдельную ветку: `feat/*`, `fix/*`, `chore/*` или `docs/*`.
2. Работай небольшими логическими шагами. Для изменения поведения используй TDD: Red → Green → Refactor.
3. Перед отправкой запусти `scripts/windows/verify.ps1` или `scripts/unix/verify.sh` (они согласуют порядок форматирования, сборки и тестов). Для точечной проверки используй `dotnet-test-run`; для затронутого frontend запусти его typecheck/tests/build.
4. Запушь только свою ветку и открой PR в `main`. Не пушь напрямую в `main`, не мержи PR и не удаляй ветку.
5. PR должен объяснять: **Что**, **Зачем**, **Как**, **Проверка**, **Миграция и откат** (если применимо), **Сквозные правки**. Не заполняй разделы формально: укажи реальные команды и тесты.
6. Перед завершением проверь полный diff и остаточные ссылки на переименованные/удалённые символы, ключи, порты и файлы.

## Архитектурные инварианты

- **Обязательное правило для всех новых и затрагиваемых рефакторингом проектов:** перед созданием, разделением, объединением, перемещением или переименованием `.csproj` загрузи и соблюдай skill `mars-project-structure`. Он задаёт обязательную схему имён, критерии границ проектов и направление зависимостей; правило применяется и при рефакторинге существующих проектов, а не только при создании новых.
- Идентичность проектов, их границы и направление зависимостей определяет `mars-project-structure`; соглашения реализации C#/.NET и frontend — `mars-code-conventions`.
- Общие версии NuGet задаются в корневом `Directory.Packages.props`; в `.csproj` у `PackageReference` не должно быть `Version` (CPM / NU1008).
- Общие свойства сборки и анализаторы задаются в `Directory.Build.props`. Не копируй их в отдельные проекты без необходимости.
- Тестовый проект `tests/MARS.<Service>.Tests` ссылается на необходимые production-проекты сервиса (Domain, Application, Api, Infrastructure и другие — там, где они существуют); не создавай отсутствующие логические разделения только ради тестовых ссылок. Новый тестовый проект обязательно добавляется в матрицу `tests` в `.github/workflows/ci.yml`. `MARS.TestKit` — библиотека, не тестовый проект.
- Любая работа с БД в тестах выполняется против PostgreSQL из Testcontainers и настоящих миграций. Не используй EF InMemory или SQLite. Остальные внешние зависимости изолируй заглушками; тесты не должны требовать внешней сети или запущенного compose-стенда. См. `backend-test-helpers`.
- В тестах передавай `TestContext.Current.CancellationToken` во все async API, где есть соответствующая перегрузка.
- Протоколы `.proto` и межсервисные контракты находятся в `src/MARS.Shared/MARS.Shared.Contracts`. gRPC, RabbitMQ и YARP имеют централизованные точки регистрации — см. skill `mars-infrastructure`.
- Не добавляй секреты в репозиторий, launch-профили, примеры конфигурации или логи. Настоящие `.env.*` файлы не коммитятся.
- Скрипты в `scripts/windows` и `scripts/unix` должны иметь одинаковые имена и эквивалентное поведение. Паритет проверяется тестами.

## Стиль и качество

- Следуй существующему стилю C# и TypeScript в ближайшем проекте; не вводи локальные конвенции без причины.
- Методы должны иметь ясные имена, небольшую ответственность и явные зависимости. Не применяй механически правило «один return» там, где оно ухудшает читаемость или поток управления.
- Ошибки не проглатывай: сохраняй контекст, логируй на правильной границе и возвращай согласованный результат/статус.
- Не создавай скрытые сетевые, файловые или контейнерные побочные эффекты в конструкторах. Внешние клиенты должны быть заменяемыми интерфейсами/фабриками.
- Не добавляй временные обходы для тестов, которые не отражают production-поведение.

## CI и форматирование

- Источники истины: `.github/workflows/ci.yml`, `.github/workflows/auto-format.yml`, `.github/workflows/release-microservices.yml`. Не копируй сюда матрицы, даты замеров покрытия или подробное описание шагов CI.
- CI проверяет PR в `main`; новый тестовый проект без записи в матрице не будет проверяться.
- `auto-format.yml` запускается на push в `main` и на PR в `main`; для PR из того же репозитория он может amend'ить последний коммит ветки. Для fork PR запушить форматирование нельзя. Форматируй локально до пуша: `dotnet csharpier format .`, чтобы не полагаться на переписывание коммита ботом.
- Порог покрытия и условия запуска определяются актуальными workflow и `.github/scripts/coverage-gate.py`; не делай выводов по старым цифрам из заметок.

## Skills

Подробные процедуры загружай по необходимости, не дублируй их в этом файле:

| Skill | Когда использовать |
|---|---|
| `mars-project-structure` | Обязательные имена проектов, границы, создание/разделение/объединение/перемещение/переименование csproj |
| `dotnet-test-run` | Запуск/фильтрация тестов и разбор вывода MTP |
| `backend-test-helpers` | Написание .NET-тестов, фикстуры, PostgreSQL, CancellationToken |
| `mars-coverage` | Локальное покрытие, CI gate, анализ непокрытых методов и известные ловушки |
| `mars-code-conventions` | Соглашения реализации C#/.NET, тестирования и frontend внутри существующих проектов |
| `mars-infrastructure` | PostgreSQL, миграции, RabbitMQ, gRPC, YARP, Docker, порты и локальный запуск |
| `rabbitmq-pubsub-events` | Типизированные pub/sub события Wolverine: контракты, публикация, листенеры, имена exchange/очередей, DLQ |
| `mars-repo-maintenance` | Сквозные изменения, скрипты, env-файлы, CI-матрицы и безопасный sweep |
| `gateway-route-add` | Добавление внешнего маршрута YARP |
| `frontend-type-check` | Проверка и сборка UI хранилища (npm) |
| `build-for-good-ux` | UX-состояния, обратная связь и доступность |
| `make-no-mistake` | Финальная самопроверка по отдельному запросу |
| `graphify` | Запросы к графу знаний репозитория |

Skills для opencode и mimocode хранятся зеркально в `.opencode/skills/` и `.mimocode/skills/`. При изменении существующего skill обновляй обе копии.

## Источники истины и устаревшие материалы

При расхождении доверяй коду и конфигурации: `README.md`, `MARS.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `docker-compose*.yml`, актуальным workflow и `src/MARS.Shared`.

- `.mimocode/plans/` — перенесённые из монолита исторические планы; их пути и команды не считать актуальной спецификацией.
- `src/MIGRATION_CHECKLIST.md` — исторический статус переноса, не архитектурный контракт.
- Для задач по графу сначала используй skill `graphify`; обновляй граф после изменений кода, если он присутствует в рабочем checkout.
