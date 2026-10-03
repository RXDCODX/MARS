import { HubConnectionBuilder, IRetryPolicy } from "@microsoft/signalr";

import { logger } from "@/shared/logger";

const policy: IRetryPolicy = { nextRetryDelayInMilliseconds: () => 5000 };

const baseUrl = import.meta.env.VITE_BASE_PATH;

export const SoundRequestHubSignalRConnectionBuilder =
  new HubConnectionBuilder()
    .withUrl(baseUrl + "hubs/soundrequest")
    .withAutomaticReconnect(policy)
    .configureLogging(logger);
