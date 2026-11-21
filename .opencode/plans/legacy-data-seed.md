# План: перенос данных из старой БД `prod` в микросервисы как начальные данные

## Решения (зафиксированы)
- **Подход:** новые `.cs`-миграции с литеральными `INSERT ... VALUES ... ON CONFLICT DO NOTHING` — самодостаточно, работает на любом стенде, идемпотентно. ~15k строк ≈ несколько МБ сгенерированного кода.
- **`EnvironmentVariables`** — пропускаем целиком (внутри реальные токены GitHub/GitLab/Redmine + 119 строк env машины).
- **Мёртвые таблицы** (Clips, LiveChannelState, ResendLinks, TwitchChannelInspectors, WTelegramAlloweedChannels) — пропускаем все.
- Существующие `SeedLegacy*` (2026-09-30) не трогать и не удалять — они остаются staging-ноопы; их роль в новой работе — готовая спецификация маппингов (переименования, касты, owned-типы, проекции, порядок FK).

## Контекст и доступы
- **Источник:** `prod` на нативном PostgreSQL (`localhost:5432`, user `postgres`/`postgres`). Строка — `Prod_Path` из `D:\VS\MARS_old\MARS.Projects\MARS.Server\appsettings.json`. Клиент: `H:\Applications\Postgre\bin\psql.exe` (`$env:PGPASSWORD='postgres'`). dbhub MCP базу `prod` не видит (он подключён к Docker-postgres с 12 базами `mars_*`, порт хоста 6017) — чтение prod через psql, целевые БД проверяем через dbhub (`execute_sql_mars_*`).
- **Целевые БД:** 10 баз: `mars_admin`, `mars_twitch`, `mars_chat`, `mars_media`, `mars_mediastorage`, `mars_cinema`, `mars_alerts`, `mars_scoreboard`, `mars_videos365`, `mars_waifu`. Базы `mars` (0 таблиц) и `mars_shikimori` (новый домен) — не трогаем.

## Общий механизм
Новая миграция на сервис: `ImportLegacy<Service>Data`. В `Up()` — по одному `migrationBuilder.Sql("""...""")` на таблицу:

```sql
INSERT INTO chat."PostedImageRecords" ("Id","Source","ImageId","DiscordChannelId","PostedAtUtc")
VALUES (...),(...),(...)
ON CONFLICT DO NOTHING;
```

- **Идемпотентность** — `ON CONFLICT DO NOTHING` построчно: не конфликтует ни с `HasData`-строками моделей, ни с runtime-данными сервисов, ни со старыми staging-сидами на чистом стенде.
- `Down()` — `NotSupportedException` (как в существующих сидах).
- После таблиц с **целочисленными авто-PK** обязательно поднять секвенции: `SELECT setval(pg_get_serial_sequence('schema."T"','Id'), COALESCE((SELECT max("Id") FROM schema."T"),0)+1, false);`.
- Экранирование при генерации: строки — `''`; NULL — литерал `NULL`; timestamptz — ISO в кавычках; `'-infinity'` сохранять как `'-infinity'::timestamptz` там, где так делает старый сид.

## Список переносов (снимок prod от 2026-11-20)

| БД | Таблицы (prod → target), ожидаемые строки |
|---|---|
| `mars_admin` | TwitchUsers→TwitchUsers (проекция 5 колонок, 6389), FollowersEntitys→FollowerInfos (119), SevenTvEmotes (проекция Name/LoadedAt, 402), RootState (33, merge) |
| `mars_twitch` | TwitchUsers (полные, 6389), FollowersEntitys (119), SevenTvEmotes (402), AutoMessages (7), HelloVideosUsers (8), Husbands (133), FumoUsers (17), RollCooldowns (11), TwitchLeaderboardUsers (5), RootState (33, merge) |
| `mars_chat` | TelegramUsers (11), ChannelProcessingStates (2), TelegramDiscordChannelBindings (6), TelegramDiscordChannelStates (12), BooruAutoPostConfigs (4), BooruScheduledPosts (810), PostedImageRecords (1399 — НОВОЕ, каст `"DiscordChannelId"::numeric`, у prod `varchar`), RootState (33, merge) |
| `mars_media` | SoundRequestBaseTrackInfos→Tracks (1579), SoundRequestQueueItems→QueueItems (1024), SoundRequestPlayerState→PlayerStates (1), RootState (33, merge) |
| `mars_mediastorage` | Alerts (88, owned-колонки по маппингу из SeedLegacyMediaStorage), RandomMemeOrder (1210) |
| `mars_cinema` | CinemaQueue (2) |
| `mars_alerts` | AdhdLayoutConfig (1) |
| `mars_scoreboard` | ScoreboardStates (1), ScoreboardLayouts (1), ScoreboardPlayers (2) |
| `mars_videos365` | Videos365 (381) |
| `mars_waifu` | Waifus (342), Fumos (70), MikuModules (215), MikuMondayTracks (28), MikuMondayActivations (9), Frogs (52), Husbands (133), HusbandCoolDowns (133), HusbandAutoHelloCooldowns (26), RollCooldowns (11), UserFumoCollections (10), UserMikuCollections (21), WaifuRollGuarantees (6), WaifuRollAudios (2), AutoHelloMessages (100), RootState (33, merge) |

**Не переносим:** TwitchToken, WTelegramSessions (токены), EnvironmentVariables (токены + env машины), мёртвые таблицы (Clips, LiveChannelState, ResendLinks, TwitchChannelInspectors, WTelegramAlloweedChannels), пустые (Replies, WaifuChatFacts, ChannelRewards, ServiceStates, StreamArchive*), logs.Logs, hangfire.*, TelegramUpdateReceiverOffset (дропнута 2026-10-18), RandomMemeType (покрыта HasData).

## Порядок работ
1. Ветка `feat/legacy-data-seed`.
2. Загрузить skills: `mars-infrastructure`, `mars-code-conventions`, `backend-test-helpers`, `dotnet-test-run`.
3. Одноразовый генератор (PowerShell + psql, во временке, в репо не коммитить).
4. В каждом сервисе `dotnet ef migrations add ImportLegacy<Service>Data`, вставить сгенерированный SQL.
5. Применить к dev-стенду, сверить счётчики.
6. Прогнать затронутые тест-проекты, добавить seed-smoke-тесты.
7. `dotnet csharpier format .`, `.\scripts\windows\verify.ps1`, `graphify update .`.
8. PR в `main`.

## Проверка
- Чистые таблицы `==` снимку; RootState-таблицы `>=` снимку (merge).
- Секвенции подняты (insert после сида работает).
- Тесты проходят (Testcontainers накатывает миграции).
- `verify.ps1` зелёный, csharpier check зелёный.

## Грабли
- `PostedImageRecords.DiscordChannelId`: prod varchar → target numeric, каст `::numeric`.
- `Fumos.WhenAdded/LastOrder`: текстовый '-infinity' → `'-infinity'::timestamptz`.
- `CinemaQueue.MediaUrl`: `coalesce("MediaUrl",'')`.
- Целочисленные авто-PK: обязательно `setval`.
- Тесты, предполагавшие пустые таблицы — починить.
- Не трогать старые `SeedLegacy*` миграции.
- Данные-токены не коммитить.
