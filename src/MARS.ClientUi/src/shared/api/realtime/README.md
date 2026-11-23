# Realtime (`src/shared/api/realtime`)

Ручной слой работы с SignalR: адаптеры, жизненный цикл соединений, реестры, хуки и разбор payload'ов хабов. Не генерируется.

## Группы модулей

### Жизненный цикл соединения

- `hubAdapter.ts` — типы `HubAdapter`, `HubHandlers`, `HubStatus`.
- `hubConnection.ts` — `HubConnection`: идемпотентный `start`, `acquire`, текущий адаптер.
- `hubConnections.ts` — готовые подключения `scoreboardConnection`, `soundRequestConnection`.
- `hubRegistry.ts` — `createHubRegistry()` (наблюдатели, `useSyncExternalStore`).
- `overlayHub.ts` — реестр адаптера оверлейного хаба.
- `hubUrl.ts` — `resolveHubUrl()` (абсолютный URL хаба из базового пути).
- `signalrDebug.ts` — флаг отладки SignalR.

### Адаптеры SignalR

- `SignalRHubAdapter.ts` — фабрики адаптеров для scoreboard и sound request хабов.
- `FakeHubAdapter.ts` — подделка адаптера для тестов.

### События и payload'ы оверлея

- `overlayEvents.ts` — имена событий и типы аргументов/обработчиков оверлея.
- `overlayPayload.ts`, `overlaySubscription.ts` — разбор и подписка на payload'ы.
- `mediaPayload.ts`, `mediaTextInfo.ts`, `chatMessage.ts`, `beamUsers.ts`, `highliteMessage.ts` — преобразование конкретных событий.

### Хуки

- `useOverlayHubLifecycle.ts` — жизненный цикл оверлейного хаба.
- `useOverlayEvent.ts` — подписка компонента на событие оверлея.
- `useOverlayAdapter.ts` — получение адаптера оверлея через `useSyncExternalStore`.
- `useHubInvoke.ts` — вызов методов хаба.

## Тесты

Тесты лежат рядом с модулями (`*.test.ts`). Запуск: `yarn test` из `src/MARS.ClientUi`.

## Использование

```ts
import { useOverlayEvent } from "@/shared/api/realtime/useOverlayEvent";
import { resolveHubUrl } from "@/shared/api/realtime/hubUrl";
```