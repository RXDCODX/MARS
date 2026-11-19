import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

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
      onError={async error => { console.log(error); }}
      onClosed={async event => { console.log(event); }}
      onOpen={async event => { console.log(event); }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/scoreboard"}
      logMessageContent
    >
      {children}
    </ScoreboardHubSignalRContext.Provider>
  );
}
