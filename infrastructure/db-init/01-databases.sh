#!/usr/bin/env bash
#
# Создание изолированных баз данных MARS: одна база на сервис.
#
# Запускается один раз из контейнера postgres: каталог с этим файлом смонтирован
# в /docker-entrypoint-initdb.d, поэтому выполняется только при инициализации
# пустого тома. Скрипт идемпотентен — повторный прогон ничего не ломает.
#
# Модель доступа: на каждую базу — своя роль с входом, и она же владелец базы.
# Владелец нужен сервису по существу: EF Core создаёт схемы и таблицы при
# применении миграций, и сделать это может только владелец. Логины разделены,
# поэтому REVOKE CONNECT FROM PUBLIC — не формальность: без него любой, у кого
# есть учётка, подключился бы к чужой базе.
#
# Пароли приходят только из окружения контейнера. Ни один пароль и ни одна
# строка подключения не хранится в репозитории.
#
# Схемы намеренно не создаются: их создаёт EF Core согласно HasDefaultSchema()
# в коде. Дублирование списка схем здесь означало бы разойтись с ним при
# первом же изменении.

set -euo pipefail

log() { printf '[db-init] %s\n' "$*"; }
fail() { printf '[db-init] ОШИБКА: %s\n' "$*" >&2; exit 1; }

ADMIN_ROLE="${POSTGRES_USER:-mars}"

# "<база>|<роль>|<переменная с паролем>". Имя роли совпадает с именем базы:
# оно однозначно указывает сервис-владелец в pg_roles и в логах.
DATABASES=(
  "mars_twitch|mars_twitch|MARS_TWITCH_PASSWORD"
  "mars_waifu|mars_waifu|MARS_WAIFU_PASSWORD"
  "mars_chat|mars_chat|MARS_CHAT_PASSWORD"
  "mars_media|mars_media|MARS_MEDIA_PASSWORD"
  "mars_scoreboard|mars_scoreboard|MARS_SCOREBOARD_PASSWORD"
  "mars_cinema|mars_cinema|MARS_CINEMA_PASSWORD"
  "mars_mediastorage|mars_mediastorage|MARS_MEDIASTORAGE_PASSWORD"
  "mars_admin|mars_admin|MARS_ADMIN_PASSWORD"
  "mars_alerts|mars_alerts|MARS_ALERTS_PASSWORD"
  "mars_videos365|mars_videos365|MARS_VIDEOS365_PASSWORD"
"mars_shikimori|mars_shikimori|MARS_SHIKIMORI_PASSWORD"
)

# Проверяем пароли до любых изменений: частично созданный стек баз хуже, чем
# явная ошибка на старте.
for entry in "${DATABASES[@]}"; do
  pw_var="${entry##*|}"
  if [ -z "${!pw_var:-}" ]; then
    fail "не задана переменная ${pw_var} (пароль для ${entry%%|*})"
  fi
done

# Уничтожать существующие базы по умолчанию нельзя: initdb вызывается только на
# пустом томе, но скрипт можно запустить и руками. Сброс — явное решение.
RESET="${MARS_DB_INIT_RESET:-false}"
case "$RESET" in
  true | TRUE | 1 | yes | YES)
    log 'MARS_DB_INIT_RESET=true: существующие базы будут пересозданы'
    ;;
  *)
    RESET=false
    log 'существующие базы сохраняются (MARS_DB_INIT_RESET не задан)'
    ;;
esac

log "administrator role: ${ADMIN_ROLE}"

for entry in "${DATABASES[@]}"; do
  db="${entry%%|*}"
  rest="${entry#*|}"
  role="${rest%%|*}"
  pw_var="${rest##*|}"
  password="${!pw_var}"

  log "provisioning ${db} (role ${role})"

  # Отключаем живые подключения: иначе DROP DATABASE падает с
  # "database is being accessed by other users", пока сервис не остановлен.
  psql -v ON_ERROR_STOP=1 --quiet --username "$ADMIN_ROLE" --dbname postgres <<EOSQL
    SELECT pg_terminate_backend(pid)
    FROM pg_stat_activity
    WHERE datname = '${db}'
      AND pid <> pg_backend_pid();
EOSQL

  if [ "$RESET" = true ]; then
    psql -v ON_ERROR_STOP=1 --quiet --username "$ADMIN_ROLE" --dbname postgres <<EOSQL
      DROP DATABASE IF EXISTS "${db}";
EOSQL
  fi

  exists=$(psql -v ON_ERROR_STOP=1 --quiet --tuples-only --no-align \
    --username "$ADMIN_ROLE" --dbname postgres \
    -c "SELECT 1 FROM pg_database WHERE datname = '${db}'")

  if [ "$exists" = "1" ]; then
    log "  ${db} уже существует — создание пропущено, гранты обновляются"
  else
    # CREATE ROLE / CREATE DATABASE нельзя выполнять внутри DO-блока, поэтому
    # существование роли проверяем отдельно, а создаём в autocommit.
    # Пароль передаётся через -v: psql сам экранирует кавычки и обратные слэши
    # в значении :'pw'. Подстановка прямо в текст SQL сломалась бы на пароле
    # с одинарной кавычкой.
    psql -v ON_ERROR_STOP=1 --quiet --username "$ADMIN_ROLE" --dbname postgres \
      --set=pw="$password" <<EOSQL
      DO \$do\$
      BEGIN
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '${role}') THEN
          EXECUTE format('DROP ROLE %I', '${role}');
        END IF;
      END
      \$do\$;

      CREATE ROLE "${role}" WITH LOGIN PASSWORD :'pw'
        NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT NOREPLICATION;

      CREATE DATABASE "${db}" OWNER "${role}";
EOSQL
  fi

  # CONNECT выдан PUBLIC по умолчанию — именно поэтому сервис из другой базы
  # подключился бы к чужой без всякой аутентификации. Убираем и выдаём
  # обратно только владельцу. TEMP оставляем: временные таблицы сервису
  # не мешают, а лишний GRANT ничего не стоит.
  psql -v ON_ERROR_STOP=1 --quiet --username "$ADMIN_ROLE" --dbname postgres <<EOSQL
    REVOKE ALL ON DATABASE "${db}" FROM PUBLIC;
    GRANT CONNECT, TEMPORARY ON DATABASE "${db}" TO "${role}";
EOSQL

  # USAGE на public нужен ровно на время переноса данных из legacy: одноразовые
  # seed-миграции читают staging-таблицы из этой схемы и удаляют их после
  # проверки количества строк. Ни CREATE, ни других прав на public не даём —
  # сам дамп разворачивает администратор, а после переноса схема пуста и
  # миграции на чистом развёртывании ничего не делают.
  psql -v ON_ERROR_STOP=1 --quiet --username "$ADMIN_ROLE" --dbname "${db}" <<EOSQL
    GRANT USAGE ON SCHEMA public TO "${role}";
EOSQL

done

# Отчёт: состав баз и фактические ACL. У каждой базы в datacl должно быть
# ровно два элемента (владелец и CONNECT владельцу) и ни одного '=T/' (PUBLIC) —
# это проверка, что изоляция действительно применилась, а не только записана
# в скрипте.
psql -v ON_ERROR_STOP=1 --username "$ADMIN_ROLE" --dbname postgres <<'EOSQL'
\echo '--- MARS databases ---'
SELECT datname,
       pg_catalog.pg_get_userbyid(datdba) AS owner,
       coalesce(array_to_string(datacl, ' '), '(default)') AS acl
FROM pg_database
WHERE datname LIKE 'mars\_%'
ORDER BY datname;

\echo '--- roles ---'
SELECT rolname, rolcanlogin, rolsuper, rolinherit
FROM pg_roles
WHERE rolname LIKE 'mars\_%'
ORDER BY rolname;
EOSQL

log 'done'
