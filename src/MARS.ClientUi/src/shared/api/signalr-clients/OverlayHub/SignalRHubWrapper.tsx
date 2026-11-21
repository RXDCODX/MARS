import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";
import { signalrDebugEnabled } from "@/shared/api/realtime/signalrDebug";

export const OverlayHubSignalRContext = signalR.createSignalRContext({});

interface OverlayHubProperties {
  children: React.ReactNode;
}

export function OverlayHubSignalRHubWrapper({
  children,
}: OverlayHubProperties) {
  return (
    <OverlayHubSignalRContext.Provider
      automaticReconnect={true}
      onError={async error => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.error("[SignalR][OverlayHub] error", error);
        }
      }}
      onClosed={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.warn("[SignalR][OverlayHub] closed", event);
        }
      }}
      onOpen={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.info("[SignalR][OverlayHub] open", event);
        }
      }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/overlay"}
      logMessageContent
    >
      {children}
    </OverlayHubSignalRContext.Provider>
  );
}
