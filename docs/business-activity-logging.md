# Бизнес-логирование в MARS

Это руководство описывает действующую реализацию MARS Dashboard и единого
журнала бизнес-активностей. Источник истины для поведения — код
`MARS.Shared.Kernel.Observability`, `MARS.Platform.Infrastructure.Observability.Logging`,
`MARS.Admin.Api` и тесты затронутых сервисов.

## 1. Где находится реализация

| Зона ответственности | Проект и файлы |
|---|---|
| Абстракция и модель | `src/MARS.Shared/MARS.Shared.Kernel/Observability/IBusinessActivityLogger.cs`, `BusinessActivity.cs`, `BusinessActivityOutcome.cs`, `BusinessActivityLevel.cs` |
| Реализация на Serilog и регистрация | `src/MARS.Platform/MARS.Platform.Infrastructure.Observability.Logging/BusinessActivityLogger.cs`, `SerilogExtensions.cs`, `LogLevelEnricher.cs` |
| Подключение в сервисах | `src/MARS.Platform/MARS.Platform.Infrastructure.Hosting/Extensions/WebApplicationBuilderExtensions.cs` через `AddMarsDefaults` |
| Чтение журналов | `src/MARS.Admin/MARS.Admin.Api/Services/LokiLogQueryService.cs`, `src/MARS.Admin/MARS.Admin.Application/Services/Logging/ILogQueryService.cs` |
| Dashboard API/UI | `src/MARS.Admin/MARS.Admin.Api/Controllers/DashboardController.cs`, `src/MARS.ClientUi/src/pages/Dashboard/Dashboard.tsx`, `dashboardApi.ts` |
| Конфигурация Alloy/Loki | `infrastructure/alloy/config.alloy`, `infrastructure/loki/loki-config.yml` |

Новый сервис получает `IBusinessActivityLogger` автоматически через
`AddMarsDefaults`. Прямые зависимости на `MARS.Admin`, Loki или PostgreSQL из
сервисов не добавляются.

## 2. Что считается бизнес-активностью

Бизнес-активность — значимое изменение состояния или пользовательское действие:
сообщение в чате, команда, награда, вайфу-ролл, звуковой запрос, изменение
табло, административное действие. Обычные технические записи, health checks,
heartbeat-ы и прогресс воспроизведения в этот журнал не пишутся.

Текущее покрытие на момент PR #161:

| Сервис | Покрытые действия |
|---|---|
| `MARS.Twitch` | отправка сообщения в чат, отказ отправки пустого сообщения |
| `MARS.Commands` | запрос команды, ошибка выполнения команды |
| `MARS.WaifuGacha` | успешный и неуспешный ролл вайфу |
| `MARS.Scoreboard` | обновление табло |

Остальные сервисы подключаются к этому API при изменении их кода. Новые
бизнес-действия в затронутом сервисе должны сопровождаться записью через
`IBusinessActivityLogger`.

## 3. Формат записи

В консоли, файле и Loki запись имеет вид:

```text
[MARS.Twitch][Nickname] отправил сообщение: "привет"
[MARS.Commands][Nickname] вызвал команду !roll
[MARS.Scoreboard][system] обновил табло: Match title
```

`ServiceName` и trace ID добавляются общей настройкой Serilog. Автор передаётся
в `ActorDisplayName`, а не вручную встраивается в имя сервиса.

Пример использования:

```csharp
businessActivity.Log(
    new BusinessActivity
    {
        Category = "chat",
        Action = "message-sent",
        Outcome = BusinessActivityOutcome.Succeeded,
        ActorType = "service",
        ActorDisplayName = "MARS.Twitch",
        Platform = "Twitch",
        SubjectType = "channel",
        SubjectId = channel,
        Summary = $"отправил сообщение в чат: \"{message.Message}\"",
    }
);
```

Стабильные поля:

- `Category` — широкая предметная область: `chat`, `command`, `reward`, `waifu`,
  `scoreboard`.
- `Action` — стабильное действие в kebab-case: `message-sent`,
  `command-requested`, `waifu-rolled`.
- `Outcome` — `Accepted`, `Succeeded`, `Rejected`, `Failed`, `Cancelled`.
- `Level` — по умолчанию `Information`. Обычный бизнес-отказ пишется как
  `Information + Rejected`, чтобы не создавать операционные алерты. Системная
  ошибка пишется как `Error + Failed`.

## 4. Правила безопасности и кардинальности

- Секреты, токены, verification codes, OAuth codes, значения environment variables,
  произвольные request DTO и полные файловые пути не логируются.
- Тексты сообщений и пользовательские данные логируются только при явной
  необходимости и только для внутренней панели. Текст обрезается до 2000 символов.
- В Loki labels попадают только низкокардинальные поля: `level`,
  `activity_action`, `activity_platform`, `activity_outcome`, `container`,
  `service.name`.
- `actor_id`, `actor_display_name`, `subject_id`, correlation ID и тексты не
  становятся labels.
- Бизнес-активность записывается один раз в авторитетной точке перехода
  состояния, а не на каждом REST/gRPC/SignalR входе.

## 5. Loki и метки

Сервисы отправляют логи через Serilog по OTLP gRPC в Alloy; инфраструктура
читается Alloy из stdout контейнеров. Alloy превращает атрибуты записи в метки
Loki. После изменений конфигурации Alloy нужно перезапустить.

Метки, используемые MARS Dashboard:

- ресурсные: `container`, `service.name`;
- атрибуты записи: `level`, `activity_action`, `activity_platform`,
  `activity_outcome`.

Также `LogLevelEnricher` добавляет `level` ко всем записям, поэтому технические
логи можно фильтровать без разбора текста.

## 6. Dashboard API

Внутренние endpoints:

- `GET /api/Dashboard/overview` — статусы сервисов, инфраструктура, подключения,
  активные алерты, метрики.
- `GET /api/Dashboard/activities` — бизнес-активности из Loki.
- `GET /api/Dashboard/activity-stats` — статистика по действиям, сервисам,
  платформам, timeline и активным авторам.
- `GET /api/Dashboard/metrics/history` — история CPU/RAM/GC из Prometheus.
- `GET /api/ServiceManager/service/{serviceName}/logs` — технические и
  бизнес-логи сервиса из Loki.

Legacy endpoints `/api/Dashboard/events` и `/api/Dashboard/event-stats` сохранены
для совместимости со старыми записями `EventLogs`.

## 7. Доступ

Dashboard — внутренняя панель. Gateway подставляет межсервисный ключ только для
маршрутов с метаданными `MARS.ServiceApiKey=true`. Ключ не отправляется в
браузер и не должен попадать в клиентский бандл. Доступ контролируется на уровне
внутренней сети, VPN или ingress.

## 8. Адрес Loki

Адрес задаётся конфигурацией:

- `Loki:Url` или `Loki:BaseUrl`;
- при пустом значении и наличии `MARS_PORT_LOKI` используется
  `http://localhost:{port}`;
- иначе используется `http://loki:3100`.

Loki хранит логи до 168 часов. Dashboard ограничивает период выборки до 7 дней.
Статистика активностей рассчитывается по последним 5000 записям периода.

## 9. Проверка после изменений

- Открыть `/` и `/dashboard` без `401`.
- Убедиться, что в собранном JS нет сервисного API-ключа.
- Вызвать бизнес-действие и увидеть его в ленте в формате
  `[MARS.Service][Actor] ...`.
- При отключении Loki/Prometheus/Alertmanager соответствующие секции показывают
  «недоступно», остальные продолжают работать.
- Проверить тёмную тему и мобильный экран.

## 10. Связанные документы

- [Межсервисные события: Wolverine + RabbitMQ](messaging-wolverine.md) — typed
  pub/sub. Бизнес-логи не дублируют RabbitMQ-события и не используют
  универсальный event с полем `kind`.
- [README проекта](../README.md) — стек наблюдаемости и порты.
