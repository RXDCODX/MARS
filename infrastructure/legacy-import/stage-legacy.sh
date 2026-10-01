#!/usr/bin/env bash
#
# Развёртывание legacy-дампа в staging-схему public целевой базы.
#
# Одна старая база prod разъезжалась на десять сервисных. Дамп восстанавливается
# не целиком в каждую базу, а по списку нужных таблиц: иначе в сервисной базе
# окажется весь мусор монолита, а миграции всё равно не поймут, что переносить.
#
# Что происходит для каждой таблицы:
#   1. pg_dump вытаскивает DDL и данные одной таблицы из legacy-базы;
#   2. они разворачиваются в public целевой базы (схему создаёт EF, public
#      существует всегда);
#   3. владельцем таблицы становится роль сервиса — seed-миграция обязана
#      удалить staging после проверки количества строк, а DROP требует
#      владения объектом. Права на схему public (USAGE) выдаёт
#      infrastructure/db-init/01-databases.sh.
#
# Скрипт идемпотентен: повторный прогон перезаписывает staging с нуля, что
# нужно после неудачного прогона миграций. Перенос выполняют миграции
# (dotnet ef database update), а не этот скрипт.
#
# Переменные окружения:
#   PGHOST, PGPORT, PGPASSWORD     — подключение к legacy-базе ( postgres )
#   LEGACY_PGUSER                   — пользователь legacy-базы, по умолчанию $PGUSER
#   TARGET_PGUSER                   — администратор целевых баз, по умолчанию $PGUSER
#   LEGACY_DB                       — имя legacy-базы, по умолчанию prod
#
# Пример:
#   LEGACY_PGPASSWORD=… TARGET_PGPASSWORD=… \
#     bash infrastructure/legacy-import/stage-legacy.sh mars_twitch

set -euo pipefail

log() { printf '[stage] %s\n' "$*"; }
fail() { printf '[stage] ОШИБКА: %s\n' "$*" >&2; exit 1; }

LEGACY_DB="${LEGACY_DB:-prod}"
TARGET_PGUSER="${TARGET_PGUSER:-${PGUSER:-postgres}}"

# "<база>|<таблица> …". Порядок внутри базы не важен: это только staging,
# внешние ключи проверяются уже в seed-миграции при вставке в целевые схемы.
DATABASES=(
  "mars_twitch|AutoMessages ChannelRewards FollowersEntitys FumoUsers HelloVideosUsers Husbands RollCooldowns RootState SevenTvEmotes TwitchLeaderboardUsers TwitchToken TwitchUsers"
  "mars_waifu|AutoHelloMessages Frogs Fumos HusbandAutoHelloCooldowns HusbandCoolDowns Husbands MikuModules MikuMondayActivations MikuMondayTracks RollCooldowns RootState UserFumoCollections UserMikuCollections WaifuRollAudios WaifuRollGuarantees Waifus"
  "mars_chat|BooruAutoPostConfigs BooruScheduledPosts ChannelProcessingStates RootState TelegramDiscordChannelBindings TelegramDiscordChannelStates TelegramUpdateReceiverOffset TelegramUsers WTelegramSessions"
  "mars_media|SoundRequestBaseTrackInfos SoundRequestPlayerState SoundRequestQueueItems RootState"
  "mars_scoreboard|ScoreboardLayouts ScoreboardPlayers ScoreboardStates"
  "mars_cinema|CinemaQueue"
  "mars_mediastorage|Alerts RandomMemeOrder RandomMemeType"
  "mars_admin|EnvironmentVariables FollowersEntitys RootState ServiceStates SevenTvEmotes StreamArchiveConfigs StreamArchiveFileChunks StreamArchiveFiles TwitchUsers"
  "mars_alerts|AdhdLayoutConfig"
  "mars_videos365|Videos365"
)

target_args=("$@")
if [ "${#target_args[@]}" -eq 0 ]; then
  target_args=()
  for entry in "${DATABASES[@]}"; do
    target_args+=("${entry%%|*}")
  done
fi

workdir="$(mktemp -d)"
trap 'rm -rf "${workdir}"' EXIT

for db in "${target_args[@]}"; do
  known=false
  for entry in "${DATABASES[@]}"; do
    if [ "${entry%%|*}" = "$db" ]; then
      known=true
      break
    fi
  done

  [ "$known" = true ] || fail "нет списка таблиц для базы ${db}"

  tables=""
  for entry in "${DATABASES[@]}"; do
    if [ "${entry%%|*}" = "$db" ]; then
      tables="${entry#*|}"
      break
    fi
  done

  log "staging ${db}"

  # Seed-миграция удаляет staging сама, но только если она ещё не применялась.
  # Повторный прогон после успешного переноса оставил бы таблицы висеть в
  # public навсегда, поэтому предупреждаем: базу надо предварительно сбросить.
  applied_seeds=$(psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 -t -A -c \
    "SELECT count(*) FROM pg_namespace n
       JOIN pg_class c ON c.relnamespace = n.oid AND c.relkind = 'r'
     WHERE n.nspname NOT IN ('pg_catalog','information_schema','public')
       AND c.relname = '__EFMigrationsHistory'" \
    2>/dev/null || echo 0)

  if [ "${applied_seeds:-0}" != "0" ]; then
    seeds=$(psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 -t -A -c \
      "SELECT count(*) FROM \"${db#mars_}\".\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" LIKE '%Seed%'" 2>/dev/null || echo 0)

    if [ "${seeds:-0}" -gt 0 ] 2>/dev/null; then
      log "ВНИМАНИЕ: в ${db} уже применена seed-миграция."
      log "  staging-таблицы никто не заберёт и не удалит — сбросьте базу перед повтором."
    fi
  fi

  # Чистая public нужна для двух вещей: повторный прогон не должен натыкаться
  # на остатки прошлого, а seed-миграции ищут источник именно здесь. Сама схема
  # public в PostgreSQL удаляется только каскадом вместе с зависимостями, поэтому
  # пересоздаём её целиком — в сервисной базе кроме неё ничего и нет.
  #
  # Права роли выдаём здесь, а не полагаемся на 01-databases.sh: DROP SCHEMA
  # уничтожает и GRANT USAGE, который был выдан при инициализации. Без повторной
  # выдачи seed-миграция падала бы с "permission denied for schema public" уже на
  # первом чтении staging — то есть ровно там, где перенос должен был пройти.
  psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 --quiet <<EOSQL
    DROP SCHEMA IF EXISTS public CASCADE;
    CREATE SCHEMA public;
    GRANT USAGE ON SCHEMA public TO "${db}";
    REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL

  for table in $tables; do
    schema_dump="${workdir}/${table}-schema.sql"
    data_dump="${workdir}/${table}-data.sql"

    # Имя обязательно в двойных кавычках: в legacy почти все имена в смешанном
    # регистре ("Videos365"), а -t ищет по точному имени и без кавычек
    # сообщает "no matching tables were found".
    pg_dump -U "${PGUSER:-$TARGET_PGUSER}" -d "$LEGACY_DB" \
      --schema-only -t "public.\"${table}\"" > "$schema_dump.raw"
    pg_dump -U "${PGUSER:-$TARGET_PGUSER}" -d "$LEGACY_DB" \
      --data-only -t "public.\"${table}\"" > "$data_dump"

    # В legacy внешние ключи пересекали границы сервисов: CinemaQueue и FollowersEntitys
    # ссылаются на TwitchUsers, а MikuMondayTracks — на SoundRequestBaseTrackInfos.
    # Таблицы-родители в этот набор staging не попадают, и развёртывание DDL
    # падало бы на "relation public.TwitchUsers does not exist" ещё до переноса
    # данных. Staging — одноразовая сцена, её целостность всё равно проверяет
    # seed-миграция (а целевые схемы создают собственные FK), поэтому вырезаем
    # только ADD CONSTRAINT ... FOREIGN KEY, сохраняя первичные ключи, уникальность
    # и CHECK. Операторы занимают две строки, поэтому perl собирает утверждение
    # целиком, а не фильтрует построчно.
    perl -0pe 's/^ALTER TABLE ONLY\b.*?ADD CONSTRAINT\b.*?FOREIGN KEY\b.*?;\n//gms' \
      "$schema_dump.raw" > "$schema_dump"

    psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 --quiet \
      -f "$schema_dump" > /dev/null
    psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 --quiet \
      -f "$data_dump" > /dev/null

    # Владелец — роль сервиса: без неё миграция не сможет удалить staging.
    psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 --quiet \
      -c "ALTER TABLE public.\"${table}\" OWNER TO \"${db}\"" > /dev/null
  done

  psql -U "$TARGET_PGUSER" -d "$db" -v ON_ERROR_STOP=1 -A -F' | ' <<EOSQL
    SELECT c.relname AS table,
           c.reltuples::bigint AS estimated_rows,
           (xpath('/row/c/text()', query_to_xml(
                format('SELECT count(*) AS c FROM public.%I', c.relname), false, true, '')))[1]::text::bigint AS rows
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relkind = 'r'
    ORDER BY c.relname;
EOSQL
done

log 'staging готов: запустите dotnet ef database update для каждой базы'