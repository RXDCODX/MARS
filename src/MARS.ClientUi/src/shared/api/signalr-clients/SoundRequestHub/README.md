# SoundRequestHub (`src/shared/api/signalr-clients/SoundRequestHub`)

SignalR-клиент хаба очереди звуковых запросов.

## Файлы

- `SignalRContext.ts` — `SoundRequestHubSignalRConnectionBuilder`: `HubConnectionBuilder` с URL `VITE_BASE_PATH + "hubs/soundrequest"`, автоматическим переподключением (задержка 5000 мс) и логгером.
- `SignalRHubWrapper.tsx` — `SoundRequestHubSignalRContext` и компонент `SoundRequestHubSignalRHubWrapper`, оборачивающий детей в провайдер `react-signalr`.

## Использование

```tsx
import { SoundRequestHubSignalRHubWrapper } from "@/shared/api/signalr-clients/SoundRequestHub/SignalRHubWrapper";

<SoundRequestHubSignalRHubWrapper>{children}</SoundRequestHubSignalRHubWrapper>
```

Обе сущности реэкспортируются из `@/shared/api`. URL хаба строится из `VITE_BASE_PATH`.