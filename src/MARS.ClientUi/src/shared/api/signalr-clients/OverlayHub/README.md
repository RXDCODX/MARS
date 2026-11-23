# OverlayHub (`src/shared/api/signalr-clients/OverlayHub`)

SignalR-клиент хаба оверлея.

## Файлы

- `SignalRContext.ts` — `OverlayHubSignalRConnectionBuilder`: `HubConnectionBuilder` с URL `VITE_BASE_PATH + "hubs/overlay"`, автоматическим переподключением (задержка 5000 мс) и логгером.
- `SignalRHubWrapper.tsx` — `OverlayHubSignalRContext` и компонент `OverlayHubSignalRHubWrapper`, оборачивающий детей в провайдер `react-signalr` с `withCredentials={false}` и отладочными коллбэками.

## Использование

```tsx
import { OverlayHubSignalRHubWrapper } from "@/shared/api/signalr-clients/OverlayHub/SignalRHubWrapper";

<OverlayHubSignalRHubWrapper>{children}</OverlayHubSignalRHubWrapper>
```

Обе сущности реэкспортируются из `@/shared/api`.