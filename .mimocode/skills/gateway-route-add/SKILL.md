---
name: gateway-route-add
description: Add or change an endpoint that is reachable through the YARP gateway. Covers the three places that must change together — Yarp:Routes rule, Clusters entry, and ServiceEndpoints property — plus the tests and compose/prometheus touchpoints.
---

# Gateway Route (MARS, YARP)

Наружу опубликован **только Gateway** (`9155:8080`). Новый эндпоинт не виден
клиенту, пока не появился в `Yarp:Routes` в `src/MARS.Gateway/appsettings.json`.

## Шаг 1. Эндпоинт в сервисе

Контроллер в `src/MARS.X/Controllers/`. Возвращать
`ActionResult<OperationResult<T>>`; `OperationResult<T?>`, если метод может вернуть
`null`. Бизнес-ошибка — `Ok(result)` с `Success = false`, а не `BadRequest(...)`.

Путь маршрута контроллера задаёт атрибут `[Route("api/[controller]")]`, а значит
`MediaStorageController` ⇒ `/api/MediaStorage/…`. Именно этот префикс и должен
стоять в правиле YARP — иначе правило не поймает ничего.

## Шаг 2. Правило в `Yarp:Routes`

```json
"media-storage-report": {
  "ClusterId": "media-storage",
  "Match": { "Path": "/api/MediaStorageReport/{**remainder}" }
}
```

- `ClusterId` — имя кластера, а не путь. Один кластер на сервис; несколько
  контроллеров сервиса ссылаются на **один и тот же** `ClusterId`.
- `{**remainder}` обязателен: без него подмаршруты не дойдут.
- Имя правила kebab-case. Оно не влияет на маршрутизацию, только на читаемость и
  логи.
- Пути хабов SignalR — `/hubs/<name>/{**remainder}`, статика UI — `/storage-ui/{**remainder}`.

## Шаг 3. Кластер в `Yarp:Clusters`

Кластер нужен **один раз на сервис**. Для существующего сервиса шаг уже сделан —
проверь, что `ClusterId` из шага 2 реально есть:

```json
"media-storage": {
  "Destinations": {
    "destination1": { "Address": "http://media-storage:8080/" }
  }
}
```

`Address` = имя docker-сервиса + `:8080` **для всех** кластеров. Наружу проксируется
только Gateway — `docker-compose.yml` публикует порт одного сервиса.

Для закрытых маршрутов `MARS.Admin` у правила есть
`"AuthorizationPolicy": "ServiceApiKey"`. Без неё админ-API открыт наружу.

## Шаг 4. `ServiceEndpoints` — только для нового сервиса

Swagger-агрегатор строит карту **рефлексией по строковым свойствам** класса
`src/MARS.Shared/Configuration/ServiceEndpoints.cs`, а не по своему списку.

- Новый сервис ⇒ добавить строковое свойство в класс **и** значение в секцию
  `ServiceEndpoints` в `appsettings.json` Gateway.
- Опечатка в имени свойства молча уронила бы маршрут — карта строится по именам.
- Пустое значение ⇒ сервис пропускается в карте (см. `Build_SkipsBlankEndpoints`).

`MARS.Videos365` — исключение: воркер без HTTP-поверхности, в `ServiceEndpoints`
его нет, и добавлять не нужно.

## Шаг 5. Тест

`tests/MARS.Gateway.Tests/SwaggerEndpointMapTests.cs` перечисляет ожидаемые имена
карты. **Новый сервис ⇒ новое имя в массив `expected`** в этом тесте, иначе тест
проверит старый набор и пропустит регрессию молча.

## Ошибки, которые выглядят как «работает»

| Симптом | Причина |
|---|---|
| 404 на маршрут, который есть в контроллере | нет правила в `Yarp:Routes` или в нём другой префикс |
| 404 при верном пути | `ClusterId` ссылается на несуществующий кластер |
| маршрут есть, Swagger не показывает сервис | не добавлено свойство в `ServiceEndpoints` (или опечатка) |
| админский маршрут доступен без ключа | забыт `AuthorizationPolicy: ServiceApiKey` |
| 413 при загрузке в хранилище | упёрлись в `Gateway:MaxRequestBodyBytes`; понижать нельзя |

## Перед сдачей

```bash
dotnet build src/MARS.Gateway/MARS.Gateway.csproj -c Release
dotnet test tests/MARS.Gateway.Tests/MARS.Gateway.Tests.csproj -c Release
dotnet csharpier .
```

Сервис без нового HTTP-эндпоинта (воркер, consumer) шаги 2–5 не выполняет.
Новый **сервис** дополнительно требует `MARS.slnx` (папки `/src/` и `/tests/`),
`tests/MARS.X.Tests`, `Dockerfile`, сервис и тома в `docker-compose.yml`, таргет в
`infrastructure/prometheus/prometheus.yml`, `ServiceEndpoints.cs`, `Yarp:Routes`
с кластером и матрицу release-workflow — полный список в `AGENTS.md`.