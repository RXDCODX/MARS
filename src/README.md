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

| Сервис | Хостовый порт (dev) | Описание |
|--------|------|----------|
| Gateway | 6001 | YARP reverse proxy, Swagger aggregator |
| TwitchCore | 6002 | IRC, EventSub, токены, пользователи, награды |
| WaifuGacha | 6003 | Waifu/Fumo/Frog/Miku rolls, Shikimori |
| Telegram | 6004 | Telegram бот, WTelegram, Google Photos |
| Discord | 6005 | Discord gateway, голосовые каналы |
| Commands | 6006 | Кросс-платформенная система команд |
| SoundRequest | 6007 | Аудиоплеер, Spotify/SoundCloud/YouTube |
| TTS | 6008 | Text-to-Speech через AudioController |
| OBS | 6009 | Управление OBS (freeze, screenshot) |
| Alerts | 6010 | gRPC TelegramusService — шина алертов |
| Scoreboard | 6011 | Табло очков для стрима |
| CinemaQueue | 6012 | Очередь просмотра видео |
| MediaStorage | 6013 | Хранение медиафайлов |
| Admin | 6014 | Логирование, ServiceManager, конфигурация |
| AudioController | 30695 | Управление громкостью микрофона и микшером (Windows-хост, вне Docker) |
| Videos365 | 6015 | Конвейер публикации видео в Telegram |
| Shikimori | 6016 | Клиент Shikimori, рейт-лимитер, персонажи |

Порты — из схемы `MARS_PORT_BASE + номер` для **стенда разработки** (база 6001);
внутри контейнера каждый сервис слушает `8080`. Боевой стенд наружу публикует
Gateway (7001) и прокси Telegram (1443). Схема и её охрана — в корневом
`README.md` и `AGENTS.md`.

Сверх этого dev-стенд публикует вторые порты сервисов с gRPC (`6028…6032`, все на
`:8081`) и `matoi` (`6027`) — шлюз к booru; в бою наружу выходят Gateway и
прокси Telegram.
Чтобы отлаживать сервис на хосте, а не в контейнере, есть профиль `MARS.Local`:
значения лежат в `scripts/local-launch.conf`, а сам профиль пересобирает скрипт
`local`.

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
├── MARS.Shared/
│   ├── MARS.Shared.Kernel/      # Базовые доменные типы
│   └── MARS.Shared.Contracts/   # Межсервисные контракты (.proto, gRPC, RabbitMQ)
├── MARS.Platform/               # Общие технические библиотеки
├── MARS.Admin/                  # Admin API, Application, Domain и Infrastructure
├── MARS.Alerts/                 # Alerts API, Application, Infrastructure и Overlay
├── MARS.Modules.Alerts/         # Отдельные модули Alerts
├── MARS.Telegram/               # Host, Api, Application, Domain и Infrastructure
├── MARS.Gateway/                # YARP proxy + Swagger aggregator
├── MARS.Twitch.Api/             # Twitch IRC/EventSub/users/rewards
├── MARS.WaifuGacha/             # Gacha systems
├── MARS.Discord/                # Discord gateway
├── MARS.Commands/               # Command router
├── MARS.SoundRequest/           # Audio player
├── MARS.TTS/                    # Text-to-speech
├── MARS.AudioController/        # Windows-host audio control
├── MARS.OBS/                    # OBS control
├── MARS.Scoreboard/             # Scoreboard
├── MARS.CinemaQueue/            # Cinema queue
├── MARS.MediaStorage/           # Media storage
└── MARS.Videos365/              # Video publishing pipeline
```

## Секреты

Создайте файлы в `secrets/` (не коммитятся):
- `db_password.txt` — пароль PostgreSQL
- `rabbitmq_password.txt` — пароль RabbitMQ
- `twitch_client_secret.txt` — Twitch OAuth secret
- `telegram_bot_token.txt` — Telegram bot token
- `discord_bot_token.txt` — Discord bot token
- `spotify_client_secret.txt` — Spotify client secret
