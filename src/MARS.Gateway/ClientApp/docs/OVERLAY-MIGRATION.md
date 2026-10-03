# Миграция компонентов на `HubAdapter`: что уже сделано и что мешает

Документ описывает результат первой попытки переноса подписок с
`useSignalREffect` на `useOverlayEvent` и то, что остановило перенос целиком.
Список невосстановленных подписок получен автоматически из исходников, а не
по памяти.

## Что сделано и остаётся в проекте

- `src/shared/realtime/overlayHub.ts` — реестр адаптера стал наблюдаемым.
  Раньше `setOverlayAdapter` менял значение молча: компонент, смонтированный
  раньше `start()`, получал `null` во время рендера и больше не перерисовывался,
  то есть терял подписку навсегда. Теперь есть `subscribeToOverlayAdapter`, и
  значение читается через `useSyncExternalStore`.
- `src/shared/realtime/useOverlayAdapter.ts` — один хук на чтение адаптера для
  подписки и для вызова, чтобы они не разошлись по разным соединениям.
- `src/shared/realtime/useHubInvoke.ts` — вызов метода хаба с проверкой имени
  по карте `HubInvocationMap`.
- `src/shared/realtime/hubAdapter.ts` — карта вызовов дополнена реальными именами
  команд оверлея.
- `tests/.../useOverlayEvent.test.ts` — три теста на гонку старта: подписка на
  адаптер, появившийся после монтирования, и отписка от прежнего при смене.

Компоненты **не** переведены: см. следующий раздел.

## Почему перенос остановлен

Автоматический перенос 22 подписок выполнил замену вызовов, и компилятор тут же
назвал то, что невозможно увидеть иначе.

### 1. Имена подписок разошлись с контрактом

Работали только потому, что резолвер SignalR регистронезависим:

| В коде было | В контракте | Файл |
|---|---|---|
| `adhd` | `Adhd` | `ADHDLayout/ADHDController.tsx` |
| `explosion` | `Explosion` | `ADHDLayout/ExplosionVideo.tsx` |
| `fumofriday` | `FumoFriday` | `FumoFriday/FumoFridayController.tsx` |
| `alert`, `alerts` | `Alert`, `Alerts` | `PyroAlerts/PyroAlerts.tsx` |
| `deletemessage` | `DeleteMessage` | `ChatVertical/ChatVertical.tsx` |

Само по себе это полезно: с типизированным хуком расхождение видно на сборке.

### 2. Форма события не совпадает с кодом компонентов

Сервер отдаёт **одно** proto-сообщение на событие. Клиент написан под другую
форму — местами два аргумента:

| Подписка | Ожидает компонент | Приходит |
|---|---|---|
| `NewMessage` | `(id: string, message: ChatMessage)` | `{ id, messageJson }` |
| `Highlite` | `(message: ChatMessage, color: string)` | `{ messageJson, color, faceUrlJson }` |
| `DeleteMessage` | `(id: string)` | `{ id }` — совпадает |
| `AutoMessage` | `(message: string)` | `{ message }` — не совпадает |
| `Adhd` | `(seconds?: number)` | `{ seconds }` — не совпадает |

Расхождение прикрывалось тем, что полезная нагрузка объявлена как `unknown`:
компилятор не мог проверить форму, и всё, что оставалось, — поверить сигнатуре
на глаз. Стор это обходит распаковкой (`unpackWaifuRoll`), а компоненты — нет.

### 3. В proto полезная нагрузка лежит в `bytes`

`NewMessageEvent.message_json`, `HighliteEvent.message_json`,
`AllRefundEvent.user_json`, `GaoAlertEvent.gao_alert_json` объявлены как `bytes`.
В JSON-отображении protobuf это **base64-строка**, а не объект. Значит браузер
получает не `{ id, message }`, а `{ id, messageJson: "eyJ0ZXh0Ijoi…" }`, и перед
любым разбором нужно base64-декодировать и распарсить.

Это не проверено живьём и требует интеграционного теста на формате сообщения:
если `System.Text.Json` сериализует `ByteString` как объект, а не строку, то
декодировать придётся иначе. **Прежде чем чинить компоненты, это нужно
зафиксировать тестом** — иначе правки пойдут вслепую.

### 4. Восемь команд не имеют серверной реализации

Компоненты вызывают `invoke` для `MuteAll`, `UnmuteSessions`, `ObsFreeze`,
`ObsUnfreeze`, `ExplosionGo`, `MikuMikuDeleteTwitchMessages`, `LogError`,
`TwitchMsg`. Из них на сервере есть только `LogError` и `TwitchMsg`
(`TelegramusGrpcService`, gRPC). Остальные шесть не реализованы нигде: ни в
`OverlayHub`, ни в gRPC-сервисах. Это наследие монолита.

`MARS.Alerts` ссылается только на `MARS.Shared`, поэтому реализация потребовала
бы сервис-клиентов (OBS, Commands) и проверки прав — отдельная работа, не
входящая в перенос клиента.

Имена перечислены в `HubInvocationMap` явно, с этим предупреждением: иначе
несуществующий метод и опечатка выглядели бы одинаково.

## Порядок дальнейшей работы

1. Интеграционным тестом зафиксировать фактическую форму сообщения SignalR для
   `NewMessage`, `Highlite`, `AllRefund`, `GaoAlert` — с учётом `bytes`.
2. Описать `OverlayEventArgs` реальными типами вместо `unknown` и добавить
   распаковку по образцу `unpackWaifuRoll` из стора.
3. Перенести подписи компонентов на форму события.
4. Удалить `src/shared/api/signalr-clients`, `react-signalr`, моки в
   `vitest.setup.ts` и элемент `undefinedhubs/telegramus` из
   `OBSComponentsSmokeCoverage.test.ts`.

Остальные хабы (`ScoreboardHub`, `TunaHub`, `SoundRequestHub`, `LoggerHub`)
тоже не обслуживаются: на сервере существует ровно один SignalR-хаб,
`OverlayHub` в `MARS.Alerts` на `/hubs/overlay`. `AudioControllerHub` и
`VoiceRecognitionHub` в клиенте не используются вовсе — их файлы можно удалять
сразу.