# ScoreboardHub (`src/shared/api/signalr-clients/ScoreboardHub`)

SignalR-клиент хаба табло.

## Файлы

- `SignalRContext.ts` — `ScoreboardHubSignalRConnectionBuilder`: `HubConnectionBuilder` с URL `VITE_BASE_PATH + "hubs/scoreboard"`, автоматическим переподключением (задержка 5000 мс) и логгером.
- `SignalRHubWrapper.tsx` — `ScoreboardHubSignalRContext` и компонент `ScoreboardHubSignalRHubWrapper`, оборачивающий детей в провайдер `react-signalr`.

## Использование

```tsx
import { ScoreboardHubSignalRHubWrapper } from "@/shared/api/signalr-clients/ScoreboardHub/SignalRHubWrapper";

<ScoreboardHubSignalRHubWrapper>{children}</ScoreboardHubSignalRHubWrapper>
```

Обе сущности реэкспортируются из `@/shared/api`. URL хаба строится из `VITE_BASE_PATH`.