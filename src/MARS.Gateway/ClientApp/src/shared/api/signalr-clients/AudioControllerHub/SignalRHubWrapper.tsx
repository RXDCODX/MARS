import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

export const AudioControllerHubSignalRContext = signalR.createSignalRContext(
  {}
);

interface AudioControllerHubProperties {
  children: React.ReactNode;
}

export function AudioControllerHubSignalRHubWrapper({
  children,
}: AudioControllerHubProperties) {
  return (
    <AudioControllerHubSignalRContext.Provider
      automaticReconnect={true}
      onError={error => Promise.try(() => console.log(error))}
      onClosed={event => console.log(event)}
      onOpen={event => console.log(event)}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/audio-controller"}
      logMessageContent
    >
      {children}
    </AudioControllerHubSignalRContext.Provider>
  );
}
