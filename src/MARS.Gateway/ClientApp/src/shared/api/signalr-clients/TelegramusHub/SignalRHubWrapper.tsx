import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

export const TelegramusHubSignalRContext = signalR.createSignalRContext({});

interface TelegramusHubProperties {
  children: React.ReactNode;
}

export function TelegramusHubSignalRHubWrapper({
  children,
}: TelegramusHubProperties) {
  return (
    <TelegramusHubSignalRContext.Provider
      automaticReconnect={true}
      onError={error => Promise.try(() => console.log(error))}
      onClosed={event => console.log(event)}
      onOpen={event => console.log(event)}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/telegramus"}
      logMessageContent
    >
      {children}
    </TelegramusHubSignalRContext.Provider>
  );
}
