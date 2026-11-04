# Data-test-id checklist for routes

This file contains the full list of route-level components where `data-test-id` can be added for automated testing.

## Common wrappers

| Area                | Component             | Suggested hook point                                  |
| ------------------- | --------------------- | ----------------------------------------------------- |
| Site layout         | `Layout`              | Root wrapper, header, main content, footer            |
| OBS wrapper         | `OBSComponentWrapper` | Root OBS container                                    |
| Routes listing page | `RoutesPage`          | Root container, route cards, route links, stats block |

## Main site routes

| Path                     | Route name           | Component                  |
| ------------------------ | -------------------- | -------------------------- |
| `/environment-variables` | Переменные окружения | `EnvironmentVariablesPage` |
| `/`                      | Главная страница     | `WelcomePage`              |
| `/about`                 | О проекте            | `AboutPage`                |
| `/docs`                  | Документация         | `DocsPage`                 |
| `/contacts`              | Контакты             | `ContactsPage`             |
| `/commands`              | Команды              | `CommandsPage`             |
| `/routes`                | Все маршруты         | `RoutesPage`               |
| `/media-info`            | Информация о медиа   | `MediaInfoListPage`        |
| `/media-info/create`     | Создание медиа       | `MediaInfoCreatePage`      |
| `/media-info/edit/:id`   | Редактирование медиа | `MediaInfoEditPage`        |
| `/cinema-queue`          | Очередь кинотеатра   | `CinemaQueuePage`          |
| `/random-meme`           | Random Meme Manager  | `RandomMemePage`           |
| `/random-meme/:id`       | Random Meme Details  | `RandomMemeDetailsPage`    |
| `/random-meme/edit/:id`  | Edit Random Meme     | `RandomMemeEditPage`       |

## Admin routes

| Path                       | Route name                | Component                   |
| -------------------------- | ------------------------- | --------------------------- |
| `/logs`                    | Панель логов              | `LogsPage`                  |
| `/admin`                   | Админ панель              | `ServerViewer`              |
| `/dashboard`               | Дашборд                   | `ServerViewer`              |
| `/main`                    | Главная панель            | `ServerViewer`              |
| `/services`                | Сервисы                   | `ServerViewer`              |
| `/services/details`        | Детали сервиса            | `ServiceDetailsPage`        |
| `/environment-variables`   | Переменные окружения      | `EnvironmentVariablesPage`  |
| `/root-state`              | RootState                 | `RootStatePage`             |
| `/twitch-users`            | Пользователи Twitch       | `TwitchUsersPage`           |
| `/twitch-users/create`     | Создание пользователя     | `TwitchUserCreatePage`      |
| `/twitch-users/:id/edit`   | Редактирование            | `TwitchUserEditPage`        |
| `/twitch-rewards`          | Twitch награды            | `TwitchRewardsPage`         |
| `/auto-messages`           | Автосообщения             | `AutoMessagesPage`          |
| `/telegram-discord-bridge` | Telegram ↔ Discord Bridge | `TelegramDiscordBridgePage` |
| `/tts-voice`               | Голоса TTS                | `TtsVoicePage`              |
| `/commands`                | Команды                   | `CommandsPage`              |
| `/cinema-queue`            | Очередь кинотеатра        | `CinemaQueuePage`           |
| `/spotify`                 | Spotify                   | `SpotifyAuthManager`        |

## Special routes

| Path                | Route name             | Component                   |
| ------------------- | ---------------------- | --------------------------- |
| `/telegram-copy`    | Telegram copy          | `TelegramClipboardCopyPage` |
| `/scoreboard-admin` | Админ панель скорборда | `ScoreboardAdminPanel`      |

## OBS component routes

| Path                 | Route name             | Component                  |
| -------------------- | ---------------------- | -------------------------- |
| `/all-refund`        | All Refund             | `AllRefundManager`         |
| `/tik-tok-manager`   | TikTok Manager         | `TikTokLayoutManager`      |
| `/phonk-manager`     | Phonk Manager          | `PhonkLayoutManager`       |
| `/MikuMonday`        | Miku Monday            | `MikuMonday`               |
| `/mikumikubeam`      | Miku Miku Beam         | `MikuMikuBeam`             |
| `/sr/*`              | SR: SoundRequest       | `ChoosePath`               |
| `/MichaelJackson`    | Michael Jackson        | `MichaelJackson`           |
| `/credits`           | Титры (RXDCODX)        | `Credits`                  |
| `/gaoalert`          | Гао алертс             | `GaoAlertController`       |
| `/adhd`              | ADHD Layout            | `ADHDController`           |
| `/automessage`       | Автосообщения          | `AutoMessageBillboard`     |
| `/automessage-test`  | Тест автосообщений     | `AutoMessageBillboardTest` |
| `/pyroalerts`        | Pyro Алерты            | `PyroAlerts`               |
| `/randommem`         | Случайные мемы         | `RandomMem`                |
| `/waifu`             | Waifu Алерты           | `WaifuAlerts`              |
| `/fumofriday`        | Fumo Friday            | `FumoFriday`               |
| `/highlite`          | Подсветка сообщений    | `HighliteMessage`          |
| `/confetti`          | Конфетти               | `Manager`                  |
| `/scoreboard`        | Скорборд               | `Scoreboard`               |
| `/chath`             | Чат горизонтальный     | `ChatHorizontal`           |
| `/chatv`             | Чат вертикальный       | `ChatVertical`             |
| `/afkscreen`         | AFK экран              | `AFKScreen`                |
| `/avatarka`          | PNG Tuber              | `PNGTuber`                 |
| `/avatarka-fire`     | Аватарка с огнем       | `AvatarWithFire`           |
| `/avatarka-fire-svg` | Аватарка с огнем (SVG) | `AvatarWithFireSvg`        |
| `/explosion`         | Explosion              | `ExplosionVideo`           |

## Practical recommendations

| Component type | Best place for `data-test-id`                              |
| -------------- | ---------------------------------------------------------- |
| Page root      | Top-level wrapper element of the page                      |
| Layout         | Root layout container, header, footer, main content region |
| Lists/cards    | Collection container and each item/card                    |
| Links/buttons  | The interactive element itself                             |
| Status blocks  | Empty state, loading state, error state                    |
| OBS overlays   | Root overlay container and primary visible content         |

## Notes

- If a route renders the same component from multiple paths, the component should expose a stable `data-test-id` based on the component name, while the route wrapper can carry a route-specific identifier.
- For parametric routes such as `:id` and `:changeId`, use a route-level id on the page root and additional ids on detail sections if needed.
- For `RoutesPage`, it is useful to separate ids for the type cards, route rows, and statistics so E2E tests can assert both content and counts.
