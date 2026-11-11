# Как получить токен для синхронизации хранилища

Сервис `MARS.MediaStorage` версионирует каталог `wwwroot` в репозитории
`RXDCODX/random-memes`. Для записи нужен токен GitHub с правом
`contents:write` на **этот** репозиторий.

Пока `MEDIA_GIT_ENABLED=false` (значение по умолчанию), commit/push не выполняются.
Локальная проверка Git-рабочей копии всё равно обязательна, но токен для неё не нужен.

## Вариант 1 — Fine-grained токен (рекомендуется)

1. Открыть <https://github.com/settings/personal-access-tokens/new>.
2. Заполнить:
   - **Token name** — например `mars-media-storage`;
   - **Expiration** — 90 дней (максимум 1 года);
   - **Repository access** — `Only select repositories` → выбрать
     `RXDCODX/random-memes`.
3. **Repository permissions** → **Contents** → `Read and write`.
   Больше ничего ставить не нужно: сервис только коммитит и пушит, ветки и
   pull request'ы не создаёт.
4. Нажать **Generate token**, скопировать значение — оно показывается один раз.

Токен с минимальным доступом безопаснее classic: при утечке он не даст доступа
к остальным репозиториям.

## Вариант 2 — classic PAT

Подойдёт, если fine-grained недоступен.

1. Открыть <https://github.com/settings/tokens/new>.
2. **Note** — `mars-media-storage`.
3. **Expiration** — выбрать срок.
4. Отметить только галочку **`repo`** (у classic нет granular-прав; `repo`
   даёт доступ ко всем приватным репозиториям, поэтому токен нужно считать
   чувствительным).
5. **Generate token** → скопировать.

## Куда положить

Токен читается только из файла окружения в корне репозитория — того, который
соответствует среде: `.env.production` для боевого стенда, `.env.development` для
стенда разработки. Он игнорируется git, в образ не попадает и не сохраняется в
`.git/config` или аргументах процесса. Для HTTPS push Git передаёт временный
Authorization header через переменные окружения дочернего процесса.

Открыть нужный файл и заполнить:

```dotenv
MEDIA_GIT_ENABLED=true
MEDIA_GIT_REPOSITORY_URL=https://github.com/RXDCODX/random-memes.git
MEDIA_GIT_BRANCH=master
MEDIA_GIT_USERNAME=
MEDIA_GIT_TOKEN=github_pat_ЗДЕСЬ_ТОКЕН
```

`MEDIA_GIT_USERNAME` для fine-grained и classic токенов GitHub можно оставить
пустым: сервис подставит служебное имя пользователя. Заполнять его нужно
только если в вашей организации токен требует явного логина.

Файл среды создаётся копированием своего шаблона:

```powershell
Copy-Item .env.production.example .env.production    # боевой стенд
Copy-Item .env.development.example .env.development # стенд разработки
```

Реальные файлы окружений уже находятся в `.gitignore` (правило широкое —
`.env.*`, поэтому новое окружение не уедет в коммит молча); убедиться, что токен
не уехал в репозиторий, можно так:

```powershell
git check-ignore -v .env.production
git status --short | Select-String '\.env'
```

## Проверка, что токен рабочий

Проверить доступ **без** включения сервиса:

```powershell
gh auth status
# либо явно токеном из файла среды, без попадания его в историю команд:
$envFile = '.env.production'   # или '.env.development' для стенда разработки
$env:GH_TOKEN = (Get-Content $envFile | Select-String '^MEDIA_GIT_TOKEN=').Line.Split('=')[1]
gh api repos/RXDCODX/random-memes --jq .permissions
Remove-Item Env:\GH_TOKEN
```

Ожидается `push: true` в выводе.

Перед запуском убедись, что `MEDIA_STORAGE_HOST_PATH` указывает на уже
клонированный `RXDCODX/random-memes`, а не на произвольную папку. Например,
для Windows:

```powershell
git clone --branch master https://github.com/RXDCODX/random-memes.git D:/MARS/Media/wwwroot
```

Если файлы алертов уже лежат в другой папке, сначала сохрани их резервную копию,
клонируй репозиторий в новую директорию, затем перенеси медиа внутрь клона и
проверь `git status`. Не запускай контейнеры, пока origin и путь не сверены.

Затем перезапустить сервис и посмотреть лог:

```powershell
docker compose --env-file .env.production up -d media-storage
docker compose --env-file .env.production logs -f media-storage
```

Перед запуском MediaStorage локально проверяет, что `MEDIA_STORAGE_HOST_PATH`
уже является рабочей копией `MEDIA_GIT_REPOSITORY_URL`. Ожидаемые строки:

```
Рабочая копия media-storage проверена: remote=origin. Git-синхронизация выключена (MediaGit:Enabled=false).
Рабочая копия media-storage проверена: branch=master, remote=origin
```

Автоматического клонирования нет. Если каталог пустой, не является корнем Git
или у `origin` другой URL, MediaStorage завершит запуск с ошибкой. Контейнеры
`alerts` и `obs` ждут healthy-состояния MediaStorage и не начнут писать файлы
в неправильный каталог.

Проверка не обращается к GitHub по сети. Если GitHub временно недоступен, само
приложение стартует при корректной локальной рабочей копии; ошибка возникнет
только при попытке commit/push.

## Ошибки и что они означают

| Симптом в логе | Причина | Что делать |
| --- | --- | --- |
| `не является Git-репозиторием` | В `MEDIA_STORAGE_HOST_PATH` нет рабочей копии Git | Клонировать `RXDCODX/random-memes` в указанный каталог |
| `Неверный Git remote` | `origin` указывает на другой репозиторий | Проверить `git remote get-url origin` и `MEDIA_GIT_REPOSITORY_URL` |
| `ожидается ветка 'master'` | При включённой синхронизации выбрана другая ветка или detached HEAD | Переключиться на `MEDIA_GIT_BRANCH` |
| `HTTP 403` при `push` | Нет права `contents:write` | Перевыпустить токен с этим правом |
| `Authentication failed` | Токен истёк или неверен | Выпустить новый и обновить файл окружения |

## Отзыв токена

Если токен утёк: <https://github.com/settings/tokens> → найти токен →
**Revoke**. Для fine-grained: <https://github.com/settings/personal-access-tokens>.

Подробнее о разнице между корнем дискового хранилища, URL файлов и Git remote см. в
[документе о локальном каталоге MediaStorage](media-storage-local-path.md).

## Ограничения, о которых стоит знать заранее

- **Лимит 100 МБ на файл.** Два файла уже превышают рекомендацию GitHub в
  50 МБ (`assets/videos/streamer-BVAgjXLy.webm` — 63.7 МБ,
  `assets/videos/fitness-small-BcjC-D7N.webm` — 57.2 МБ). Пока они меньше
  жёсткого предела, push проходит, но рост этих файлов упрётся в отказ.
- **Загрузка ограничена 95 МБ** (`MediaStorage:MaxUploadSizeMb`). Файл крупнее
  нельзя закоммитить, поэтому принимать его в хранилище бессмысленно: он остался
  бы на диске, а push падал бы навсегда. При превышении файл удаляется и в
  отчёт попадает причина.
- **Тело запроса ограничено в двух местах.** И в `MARS.MediaStorage`
  (`FormOptions.MultipartBodyLengthLimit`), и в `MARS.Gateway`
  (`Gateway:MaxRequestBodyBytes`, дефолт 256 МБ). Лимит gateway применяется
  **до** проксирования: при его дефолтных 30 МБ загрузка файла на 63 МБ
  отбивалась с 413, хотя сервис её принимал. Если поднимется лимит загрузки —
  поднимать надо оба.
- **Git LFS не настроен.** Если медиа разрастутся, единственный выход —
  перевести крупные файлы в LFS.
- **1 коммит на операцию, только затронутые файлы.** Массовый перенос 500
  файлов создаёт один коммит с этими 500 путями; изменения, которые параллельно
  внесли Alerts или OBS в другие файлы общего wwwroot, в него не попадут.
- **Ручная выборочная синхронизация.** В UI MediaStorage можно выделить
  проиндексированные активные файлы и нажать «Синхронизировать в Git». Будут
  добавлены только выбранные пути.
- **Нет постоянного двустороннего pull.** При запуске сервис проверяет уже
  существующую локальную рабочую копию, а операции синхронизации создают коммит
  и выполняют push. Изменения, внесённые напрямую в GitHub после старта,
  автоматически не скачиваются в локальную папку.
