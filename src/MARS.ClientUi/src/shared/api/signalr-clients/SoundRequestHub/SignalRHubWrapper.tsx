import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";
import { signalrDebugEnabled } from "@/shared/api/realtime/signalrDebug";

export const SoundRequestHubSignalRContext = signalR.createSignalRContext({});

interface SoundRequestHubProperties {
  children: React.ReactNode;
}

export function SoundRequestHubSignalRHubWrapper({
  children,
}: SoundRequestHubProperties) {
  return (
    <SoundRequestHubSignalRContext.Provider
      automaticReconnect={true}
      onError={async error => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.error("[SignalR][SoundRequestHub] error", error);
        }
      }}
      onClosed={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.warn("[SignalR][SoundRequestHub] closed", event);
        }
      }}
      onOpen={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.info("[SignalR][SoundRequestHub] open", event);
        }
      }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/soundrequest"}
      logMessageContent
    >
      {children}
    </SoundRequestHubSignalRContext.Provider>
  );
}
