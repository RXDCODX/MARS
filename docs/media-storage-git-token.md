# Как получить токен для синхронизации хранилища

Сервис `MARS.MediaStorage` версионирует каталог `wwwroot` в репозитории
`RXDCODX/random-memes`. Для записи нужен токен GitHub с правом
`contents:write` на **этот** репозиторий.

Пока `MEDIA_GIT_ENABLED=false` (значение по умолчанию), сервис не выполняет ни
одной git-команды и токен не нужен.

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

Токен читается только из `.env` в корне репозитория. Он игнорируется git,
в образ не попадает и не сохраняется в `.git/config` — при обращении к remote
подставляется в URL, а после клона конфиг перезаписывается чистым адресом.

Открыть `.env` и заполнить:

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

`.env` создаётся копированием `.env.example`:

```powershell
Copy-Item .env.example .env
```

Файл `.env` уже находится в `.gitignore`; убедиться, что токен не уехал в
репозиторий, можно так:

```powershell
git check-ignore -v .env
git status --short | Select-String '\.env'
```

## Проверка, что токен рабочий

Проверить доступ **без** включения сервиса:

```powershell
gh auth status
# либо явно токеном из .env, без попадания его в историю команд:
$env:GH_TOKEN = (Get-Content .env | Select-String '^MEDIA_GIT_TOKEN=').Line.Split('=')[1]
gh api repos/RXDCODX/random-memes --jq .permissions
Remove-Item Env:\GH_TOKEN
```

Ожидается `push: true` в выводе.

Затем перезапустить сервис и посмотреть лог:

```powershell
docker compose up -d media-storage
docker compose logs -f media-storage
```

Ожидаемые строки:

```
Синхронизация git выключена        <- при MEDIA_GIT_ENABLED=false
Репозиторий media-storage готов: branch=master, remote=origin
```

Первое клонирование занимает несколько минут: в репозитории около 1 ГБ
объектов. Последующие запуски используют уже скачанные данные из тома
`mars-media-git`.

Если remote недоступен, сервис **не упадёт** — медиа продолжат отдаваться, а
в лог уйдёт запись:

```
Не удалось подготовить git-репозиторий: ...
```

## Ошибки и что они означают

| Симптом в логе | Причина | Что делать |
| --- | --- | --- |
| `HTTP 403` при `push` | нет права `contents:write` | перевыпустить токен с этим правом |
| `HTTP 404` при клоне | неверный URL или токен без доступа к приватному репозиторию | проверить `MEDIA_GIT_REPOSITORY_URL` и доступ |
| `Authentication failed` | токен с истёкшим сроком | выпустить новый |
| `remote: refusing to merge unrelated histories` | каталог не пуст и не совпадает с содержимым remote | сделать `docker compose down media-storage`, очистить тома `mars-media-git` и `mars-wwwroot`, затем запустить заново |

Последняя строка — **необратимая** операция: очистка `mars-wwwroot` удаляет
все медиа из контейнера. Файлы при этом остаются в `D:\VS\MARS_old\...\wwwroot`
и в репозитории `random-memes`, поэтому восстановимы, но лучше избегать.

## Отзыв токена

Если токен утёк: <https://github.com/settings/tokens> → найти токен →
**Revoke**. Для fine-grained: <https://github.com/settings/personal-access-tokens>.

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
- **1 коммит на операцию.** Массовый перенос 500 файлов создаёт один коммит, а
  не 500. Если нужна история по каждому файлу — это отдельная задача.
