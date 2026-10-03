# Миграция компонентов на `HubAdapter`: что уже сделано и что мешает

Документ описывает результат первой попытки переноса подписок с
`useSignalREffect` на `useOverlayEvent` и то, что остановило перенос целиком.
Список невосстановленных подписок получен автоматически из исходников, а не
по памяти.

## Инвентаризация подписок

Из 22 подписок **переведены все 22**. Таблица ниже — что переведено и что при
этом оказалось не тем, за что принимали.

| Подписка | Файл | Что вскрылось при переносе |
|---|---|---|
| `Credits`, `MichaelJackson`, `PhonkEdit`, `explosion` | четыре компонента | без полезной нагрузки; имя `explosion` → `Explosion` |
| `adhd` | `ADHDLayout/ADHDController.tsx` | имя в нижнем регистре; число из поля `seconds` |
| `fumofriday` | `FumoFriday/FumoFridayController.tsx` | имя в нижнем регистре **и** поле `color` против читаемого `chatColor` |
| `AutoMessage` | `AutoMessageBillboard/…` | строка лежит в поле `message` |
| `AllRefund`, `GaoAlert` | два компонента | полезная нагрузка в `bytes` |
| `MikuMikuBeam` | `MikuMikuBeam/…` | `repeated bytes` — список списков |
| `NewMessage`, `DeleteMessage` | `ChatHorizontal`, `ChatVertical` | ждали два аргумента; `deletemessage` в нижнем регистре |
| `Highlite` | `HighliteMessage/Message.tsx` | ждал два аргумента |
| `alert`, `alerts` | `PyroAlerts/PyroAlerts.tsx` | имена в нижнем регистре; `alerts` несёт список |
| `RandomMem` | `RandomMem/RandomMem.tsx` | полезная нагрузка — `MediaPayload` |
| `MakeScreenParticles`, `MakeScreenEmojisParticles` | `ScreenParticles/Manager.tsx` | обе в `bytes` |
| `TiktokEdit` | `TikTokBigEdit/TikTokLayoutManager.tsx` | имя в нижнем регистре; ждал два аргумента |
| `TunaMusicInfo` | `SoundRequest/CurrentTrack/…` | хаба Tuna на сервере не было |

Отдельно переведены четыре прямых `.build()` и обёртки: `wrapper.tsx`,
`mikuMondayStore.ts`, `twitchStore.ts`, `VideoScreen.stories.tsx`.
`twitchStore` подписывался на `posttwitchinfo` и держал **пятое** соединение к
хабу оверлея при уже поднятом общем.

### Что ещё тянет `signalr-clients`

Каталог `src/shared/api/signalr-clients` и пакет `react-signalr` удалить
**нельзя**: на них остались четыре потребителя, и все — через бочку
`@/shared/api`, а не прямым путём, поэтому поиск по путям их не находит.

| Файл | Нужен хаб | Есть на сервере |
|---|---|---|
| `Scoreboard/AdminPanel/store/scoreboardStore.ts` | Scoreboard | нет |
| `SoundRequest/Player/hooks/useSoundRequestPlayer.ts` | SoundRequest | нет |
| `SoundRequest/VideoScreen/store/useVideoScreenStore.ts` | SoundRequest | нет |
| `Site/Pages/LogsPage/hooks/useLogsData.ts` | Logger | нет |

`AudioControllerHub` и `VoiceRecognitionHub` не используются нигде и могут быть
удалены сразу — они ни на что не подписаны.

Чтобы удалить каталог, нужны три серверных хаба: `ScoreboardHub`,
`SoundRequestHub`, `LoggerHub`. `MARS.Alerts` держит широковещатели `Tuna` и
`Telegramus`, так что по образцу `TunaHubRelay` это рядовая работа, но объём
её — отдельный шаг.

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
- `overlayPayload.ts` — разбор полезной нагрузки по измеренному формату.
- `src/tests/subscriptionLifecycle.test.tsx` — проверка того, что размонтированный
  компонент не остаётся подписчиком. Автоочистки в проекте не было: тесты
  протекали друг в друга, и проверка на получение события могла проходить
  засчёт компонента из соседнего теста.
- Переведены компоненты: `Credits`, `MichaelJackson`, `PhonkLayoutManager`,
  `ExplosionVideo`, `ADHDController`.

Компоненты переведены **не все** — см. таблицу и следующий раздел.

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

### 3. В proto полезная нагрузка лежит в `bytes` — и едет массивом байт

Это была главная неизвестная, и она измерена:
`tests/MARS.Alerts.Tests/Hubs/OverlayPayloadWireFormatTests.cs`.

Ожидание было обратным. По отображению protobuf поле `bytes` полагается
base64-строкой, и клиент, написанный на это предположение, получил бы вместо
сообщения строку, из которой `JSON.parse` вернул бы мусор. Фактически
`System.Text.Json` сериализует `ByteString` как **массив чисел**:

```json
{"newMessage":{"id":"42","messageJson":[123,34,116,101,120,116,34,…]}, …}
```

Значит браузеру надлежит собрать `Uint8Array`, декодировать UTF-8 и только потом
парсить JSON.

Измерением вскрылось и второе, чего не предполагал: **незаполненные ветки `oneof`
приезжают как `null`, а не отсутствуют.** Ключ есть у всех 36 веток, поэтому по
наличию ключа событие не опознать — различать надо по `eventCase` или по
единственному не-`null` значению. Побочно на каждое событие едет около
тридцати пяти ключей с `null`.

Фактическая форма события `NewMessage`, одним замером:

| Поле | Форма |
|---|---|
| `id` | строка |
| `messageJson` | массив байт |
| `adhd.seconds` | число |
| `gaoAlert` | имя ветки в camelCase |
| остальные 35 веток | `null` |

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

1. ~~Зафиксировать форму сообщения~~ — сделано, см. пункт 3: массив байт.
2. Описать `OverlayEventArgs` реальными типами вместо `unknown`: ветки с
   `bytes` требуют декодера, ветки без данных — пустого объеква, а событие
   опознаётся по `eventCase`.
3. Добавить распаковку по образцу `unpackWaifuRoll` из стора — с учётом формы
   из пункта 3, а не предположения о base64.
4. Перенести подписи компонентов на форму события.
5. Удалить `src/shared/api/signalr-clients`, `react-signalr`, моки в
   `vitest.setup.ts` и элемент `undefinedhubs/telegramus` из
   `OBSComponentsSmokeCoverage.test.ts`.

Остальные хабы (`ScoreboardHub`, `TunaHub`, `SoundRequestHub`, `LoggerHub`)
тоже не обслуживаются: на сервере существует ровно один SignalR-хаб,
`OverlayHub` в `MARS.Alerts` на `/hubs/overlay`. `AudioControllerHub` и
`VoiceRecognitionHub` в клиенте не используются вовсе — их файлы можно удалять
сразу.