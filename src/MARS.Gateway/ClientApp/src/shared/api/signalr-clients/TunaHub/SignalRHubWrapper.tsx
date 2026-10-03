import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

export const TunaHubSignalRContext = signalR.createSignalRContext({});

interface TunaHubProperties {
  children: React.ReactNode;
}

export function TunaHubSignalRHubWrapper({ children }: TunaHubProperties) {
  return (
    <TunaHubSignalRContext.Provider
      automaticReconnect={true}
      onError={error => Promise.try(() => console.log(error))}
      onClosed={event => console.log(event)}
      onOpen={event => console.log(event)}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/tuna"}
      logMessageContent
    >
      {children}
    </TunaHubSignalRContext.Provider>
  );
}
