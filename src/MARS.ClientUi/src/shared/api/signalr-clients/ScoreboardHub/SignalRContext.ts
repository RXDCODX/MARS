import { HubConnectionBuilder, IRetryPolicy } from "@microsoft/signalr";

import { logger } from "@/shared/logger";

const policy: IRetryPolicy = { nextRetryDelayInMilliseconds: () => 5000 };

const baseUrl = import.meta.env.VITE_BASE_PATH;

export const ScoreboardHubSignalRConnectionBuilder = new HubConnectionBuilder()
  .withUrl(baseUrl + "hubs/scoreboard")
  .withAutomaticReconnect(policy)
  .configureLogging(logger);
