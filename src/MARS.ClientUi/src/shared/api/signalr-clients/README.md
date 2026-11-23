# SignalR clients (`src/shared/api/signalr-clients`)

React-обёртки (`react-signalr`) для SignalR-хабов MARS. По одной подпапке на хаб.

## Подпапки

- `OverlayHub/` — хаб оверлея (`hubs/overlay`).
- `ScoreboardHub/` — хаб табло (`hubs/scoreboard`).
- `SoundRequestHub/` — хаб очереди звуковых запросов (`hubs/soundrequest`).

## Содержимое каждой подпапки

- `SignalRContext.ts` — `HubConnectionBuilder` с URL хаба, политикой повторного подключения и логгером.
- `SignalRHubWrapper.tsx` — React-компонент-провайдер контекста (`react-signalr`), с коллбэками `onOpen`/`onClosed`/`onError`.

## Использование

```tsx
import { OverlayHubSignalRHubWrapper } from "@/shared/api/signalr-clients/OverlayHub/SignalRHubWrapper";

<OverlayHubSignalRHubWrapper>{children}</OverlayHubSignalRHubWrapper>
```

Обёртки экспортируются и через баррель `@/shared/api`. URL хаба строится из `VITE_BASE_PATH`.