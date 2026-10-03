import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

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
      onError={error => Promise.try(() => console.log(error))}
      onClosed={event => console.log(event)}
      onOpen={event => console.log(event)}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/soundrequest"}
      logMessageContent
    >
      {children}
    </SoundRequestHubSignalRContext.Provider>
  );
}
