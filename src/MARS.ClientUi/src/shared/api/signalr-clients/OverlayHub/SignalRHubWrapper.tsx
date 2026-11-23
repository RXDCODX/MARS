import * as signalR from "react-signalr";

import { signalrDebugEnabled } from "@/shared/api/realtime/signalrDebug";
import { logger } from "@/shared/lib/logger";

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
          console.error("[SignalR][OverlayHub] error", error);
        }
      }}
      onClosed={async event => {
        if (signalrDebugEnabled()) {
          console.warn("[SignalR][OverlayHub] closed", event);
        }
      }}
      onOpen={async event => {
        if (signalrDebugEnabled()) {
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
