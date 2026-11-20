# Задача: починка синхронизации наград (ChannelRewardsSyncService)

## Симптом
В логах `mars-dev-twitch-core-1` (20.11.2026 22:42:37) при работе `ChannelRewardsSyncService.SyncOnce` падает сохранение:
```
SqlState: 23505
duplicate key value violates unique constraint "IX_ChannelRewards_TwitchRewardId"
Detail: ... (Detail redacted)
SchemaName: twitch
TableName: ChannelRewards
ConstraintName: IX_ChannelRewards_TwitchRewardId
```
Стек: `ChannelRewardsSyncService.SyncOnce` → `SaveChangesAsync` (строки 102 и 141).

## Локализация
- Файл: `src/MARS.Twitch.Api/Services/ChannelRewards/ChannelRewardsSyncService.cs`
- Метод: `SyncOnce(CancellationToken)`
- Таблица: `twitch.ChannelRewards` (колонка `TwitchRewardId`, уникальный индекс `IX_ChannelRewards_TwitchRewardId`).

## Причина
В ветке «создание» (строки 63–104) сервис:
1. Находит локальную запись без совпадения с remote (match == null).
2. Создаёт награду на Twitch через `CreateRewardAsync` → получает `id`.
3. Ставит `tracked.TwitchRewardId = id` и вызывает `SaveChangesAsync`.

Проблема: если `CreateRewardAsync` возвращает id, который **уже присвоен другой локальной строке** (например, повторный запуск синхронизации, частичный сбой прошлого цикла, или Twitch вернул id существующей награды), `SaveChangesAsync` падает на уникальном индексе. Также возможен сценарий, когда две локальные строки без `TwitchRewardId` ссылаются на одну и ту же remote-награду (по Title/Cost) — тогда обе попадают в ветку создания.

Дополнительно: в ветке «обновление» (строки 105–144) при `record.TwitchRewardId != match.Id` ставится `tracked.TwitchRewardId = match.Id` без проверки, что этот id уже не занят другой строкой.

## Что нужно сделать
1. Перед записью `TwitchRewardId` проверять, что ни одна другая строка в `ChannelRewards` уже не имеет этого значения (или использовать `ON CONFLICT`/upsert на уровне БД).
2. В ветке создания: если `CreateRewardAsync` вернул id, который уже существует локально — не падать, а привязать существующую строку (или обновить её) и не дублировать.
3. В ветке обновления: перед присвоением `tracked.TwitchRewardId = match.Id` убедиться, что id не занят другой строкой; при конфликте — логировать и пропустить, а не падать.
4. Обернуть `SaveChangesAsync` в try/catch с логированием конкретной строки (TwitchRewardId, Id, Title), чтобы сбой одной награды не ронял весь цикл (сейчас весь `SyncOnce` в try/catch, но детали теряются).
5. Добавить идемпотентность: повторный `SyncOnce` не должен создавать дубли или падать.

## Ограничения
- Следовать соглашениям репозитория: `TreatWarningsAsErrors=true`, CSharpier, тесты в `tests/MARS.TwitchCore.Tests` (или Twitch.Api.Tests).
- Не менять схему БД (уникальный индекс `IX_ChannelRewards_TwitchRewardId` остаётся).
- Не трогать миграции данных.

## Критерий готовности
- Повторный запуск `SyncOnce` на БД с уже синхронизированными наградами не падает и не создаёт дублей.
- Unit-тесты покрывают: создание новой награды, повторный запуск (idempotency), конфликт id (graceful handling).
- `dotnet build MARS.slnx -c Release` и `dotnet test` зелёные.