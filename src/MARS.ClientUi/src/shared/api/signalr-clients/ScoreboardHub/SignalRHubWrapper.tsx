import * as signalR from "react-signalr";

import { logger } from "@/shared/lib/logger";
import { signalrDebugEnabled } from "@/shared/api/realtime/signalrDebug";

export const ScoreboardHubSignalRContext = signalR.createSignalRContext({});

interface ScoreboardHubProperties {
  children: React.ReactNode;
}

export function ScoreboardHubSignalRHubWrapper({
  children,
}: ScoreboardHubProperties) {
  return (
    <ScoreboardHubSignalRContext.Provider
      automaticReconnect={true}
      onError={async error => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.error("[SignalR][ScoreboardHub] error", error);
        }
      }}
      onClosed={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.warn("[SignalR][ScoreboardHub] closed", event);
        }
      }}
      onOpen={async event => {
        if (signalrDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.info("[SignalR][ScoreboardHub] open", event);
        }
      }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/scoreboard"}
      logMessageContent
    >
      {children}
    </ScoreboardHubSignalRContext.Provider>
  );
}
