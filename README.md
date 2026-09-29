# MARS — микросервисы

Новая архитектура MARS: 14 микросервисов на .NET 10, обмен через RabbitMQ,
единая точка входа — Gateway на `:9155`.

Монолитная архитектура вынесена в отдельный репозиторий [`MARS_old`](https://github.com/RXDCODX/MARS_old).

## Структура

```
.
├── src/
│   ├── MARS.Shared/              общий код: RabbitMQ, HTTP-клиенты, auth, health-checks
│   ├── MARS.Gateway/             YARP: единая точка входа + Swagger-агрегатор
│   ├── MARS.TwitchCore/          единственный владелец Twitch IRC/EventSub
│   ├── MARS.WaifuGacha/          роллы, кулдауны, супруги, авто-приветствия
│   ├── MARS.Telegram/  MARS.Discord/  MARS.Commands/
│   ├── MARS.SoundRequest/  MARS.TTS/  MARS.OBS/  MARS.Alerts/
│   ├── MARS.Scoreboard/  MARS.CinemaQueue/  MARS.MediaStorage/
│   ├── MARS.Admin/               админ-API (закрыт X-Api-Key)
│   └── MARS.Microservices.Tests/ тесты
├── infrastructure/               Prometheus, Grafana, Loki
├── docker-compose.yml            14 сервисов + Postgres, RabbitMQ, Grafana, Jaeger, Loki, Seq
├── docker-compose.dev.yml        dev-переопределение с dotnet watch
├── .env.example                  шаблон переменных (копируется в .env)
└── .github/workflows/            публикация образов в ghcr.io по тегу
```

## Быстрый старт

```bash
cp .env.example .env      # при необходимости заменить секреты
docker compose up -d --build
```

Стек поднимается на 21 сервис. Проверка:

```bash
docker compose ps                          # все должны быть healthy
curl http://localhost:9155/health         # Gateway
```

## Переменные окружения

Все учётные данные берутся из `.env` (Docker Compose читает его автоматически):

| Переменная | Назначение |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | База данных |
| `RABBITMQ_USER` / `RABBITMQ_PASSWORD` | Брокер. **Не оставлять guest/guest** — RabbitMQ пускает guest только с loopback, и соединения падают с ACCESS_REFUSED |
| `SERVICE_API_KEY` | Общий ключ межсервисного обмена, заголовок `X-Api-Key`. **Обязателен в проде** |
| `GRAFANA_PASSWORD` | Админ Grafana |
| `SEQ_ADMIN_PASSWORD` | Админ Seq. Без него seq не стартует |
| `TWITCH_CLIENT_ID` / `TWITCH_SECRET` / `TWITCH_OAUTH` | Twitch API. Пустые значения допустимы: чат не подключится, остальное работает |
| `APPINSIGHTS_CONNECTION_STRING` | Application Insights, пусто — отключено |

`.env` не попадает в git, `.env.example` — попадает.

## Доступ к admin-API

Admin-API закрыт ключом `SERVICE_API_KEY`. Без ключа — 401.

```bash
curl -H "X-Api-Key: $SERVICE_API_KEY" http://localhost:9155/api/RootState
```

## Сборка и тесты

```bash
dotnet build MARS.slnx --configuration Release
src/MARS.Microservices.Tests/bin/Release/net10.0/MARS.Microservices.Tests.exe
```

## Публикация образов

Workflow `.github/workflows/release-microservices.yml` по тегу `v*` собирает
каждый сервис отдельной задачей и пушит в `ghcr.io/rxdcodx/mars-<service>`.

Образ получает теги: релизный тег, `sha-<commit>` и `latest`
(только для семантических версий `vX.Y.Z`).

```bash
git tag v1.0.0 && git push origin v1.0.0
```

Перевыпуск под тем же тегом — через `workflow_dispatch` с ручным вводом тега.

## Архитектурные решения

- **Один владелец Twitch-подключения.** Только `MARS.TwitchCore` держит IRC/EventSub.
  Остальные сервисы общаются с ним через RabbitMQ, а не открывают второе соединение
  (Twitch отключает более старое).
- **Хабы в `MARS.Shared`.** `TelegramusHub`/`TunaHub` лежат в общем проекте, поэтому
  `MARS.OBS` не ссылается на `MARS.Alerts` (иначе publish падал бы с NETSDK1152
  из-за дублей `appsettings.json`).
- **`IMarsSchemaReady<T>`** — миграции применяются синхронно до `app.Run()`,
  иначе фоновые сервисы успевают обратиться к несуществующим таблицам (42P01).
- **Retry/DLQ в шине.** `RabbitMqConsumerBase` ограничивает число попыток и
  складывает poison-сообщения в `<queue>.dlq` вместо бесконечного requeue.

Неймспейсы остаются `MARS.*` независимо от имён папок в `src/`.
