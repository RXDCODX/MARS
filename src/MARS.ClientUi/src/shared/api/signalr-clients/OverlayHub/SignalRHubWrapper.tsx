import * as signalR from "react-signalr";

import { logger } from "@/shared/logger";

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
      onError={async error => { console.log(error); }}
      onClosed={async event => { console.log(event); }}
      onOpen={async event => { console.log(event); }}
      logger={logger}
      withCredentials={false}
      url={import.meta.env.VITE_BASE_PATH + "hubs/overlay"}
      logMessageContent
    >
      {children}
    </OverlayHubSignalRContext.Provider>
  );
}
