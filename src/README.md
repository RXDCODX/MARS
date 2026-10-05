# MARS.Microservices

Микросервисная архитектура MARS — стриминговая платформа с интеграцией Twitch, Telegram, Discord.

## Архитектура

```
Frontend (mars.client) → Gateway (YARP) → 14 микросервисов
                                         ↓
                                    PostgreSQL (8 schemas)
                                    RabbitMQ (event bus)
```

## Сервисы

| Сервис | Порт | Описание |
|--------|------|----------|
| Gateway | 10155 | YARP reverse proxy, Swagger aggregator |
| TwitchCore | 5001 | IRC, EventSub, токены, пользователи, награды |
| WaifuGacha | 5002 | Waifu/Fumo/Frog/Miku rolls, Shikimori |
| Telegram | 5003 | Telegram бот, WTelegram, Google Photos |
| Discord | 5004 | Discord gateway, голосовые каналы |
| Commands | 5005 | Кросс-платформенная система команд |
| SoundRequest | 5006 | Аудиоплеер, Spotify/SoundCloud/YouTube |
| TTS | 5007 | Text-to-Speech через AudioController |
| OBS | 5008 | Управление OBS (freeze, screenshot) |
| Alerts | 5009 | gRPC TelegramusService — шина алертов |
| Scoreboard | 5010 | Табло очков для стрима |
| CinemaQueue | 5011 | Очередь просмотра видео |
| MediaStorage | 5012 | Хранение медиафайлов |
| Admin | 5013 | Логирование, ServiceManager, конфигурация |

## Быстрый старт

```bash
# Infrastructure only (для локальной разработки)
docker-compose up -d postgres rabbitmq

# All services
docker-compose up -d

# Development mode (hot-reload)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

## Генерация API клиентов

```bash
cd mars.client

# 1. Собрать спеки через Gateway
yarn fetch:specs

# 2. Сгенерировать TypeScript клиенты
yarn build:api
```

## Структура

```
MARS.Microservices/
├── Directory.Packages.props     # Отключает CPM (ManagePackageVersionsCentrally=false)
├── MARS.Shared/                 # Общая библиотека
│   ├── Messaging/               # RabbitMQ event bus + consumer base
│   ├── Middleware/               # Exception, logging, correlation ID
│   ├── Telemetry/               # OpenTelemetry, metrics
│   ├── Logging/                 # Serilog extensions
│   ├── HealthChecks/            # Health check extensions
│   └── Extensions/              # DI helpers, internal endpoints
├── MARS.Gateway/                # YARP proxy + Swagger aggregator
├── MARS.TwitchCore/             # Twitch IRC/EventSub/users/rewards
├── MARS.WaifuGacha/             # Gacha systems
├── MARS.Telegram/               # Telegram bot + services
├── MARS.Discord/                # Discord gateway
├── MARS.Commands/               # Command router
├── MARS.SoundRequest/           # Audio player
├── MARS.TTS/                    # Text-to-speech
├── MARS.OBS/                    # OBS control
├── MARS.Alerts/                 # Alert bus (gRPC TelegramusService)
├── MARS.Scoreboard/             # Scoreboard
├── MARS.CinemaQueue/            # Cinema queue
├── MARS.MediaStorage/           # Media storage
└── MARS.Admin/                  # Admin/logging
```

## Секреты

Создайте файлы в `secrets/` (не коммитятся):
- `db_password.txt` — пароль PostgreSQL
- `rabbitmq_password.txt` — пароль RabbitMQ
- `twitch_client_secret.txt` — Twitch OAuth secret
- `telegram_bot_token.txt` — Telegram bot token
- `discord_bot_token.txt` — Discord bot token
- `spotify_client_secret.txt` — Spotify client secret
