import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

export const LoggerHubSignalRContext = signalR.createSignalRContext({});

interface LoggerHubProperties {
  children: React.ReactNode;
}

export function LoggerHubSignalRHubWrapper({ children }: LoggerHubProperties) {
  return (
    <LoggerHubSignalRContext.Provider
      automaticReconnect={true}
      onError={error => Promise.try(() => console.log(error))}
      onClosed={event => console.log(event)}
      onOpen={event => console.log(event)}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/logger"}
      logMessageContent
    >
      {children}
    </LoggerHubSignalRContext.Provider>
  );
}
