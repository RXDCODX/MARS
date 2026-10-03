import { HubConnectionBuilder, IRetryPolicy } from "@microsoft/signalr";

import { resolveHubUrl } from "@/shared/realtime/hubUrl";
import { logger } from "@/shared/logger";

const policy: IRetryPolicy = { nextRetryDelayInMilliseconds: () => 5000 };

const hubUrl = resolveHubUrl("hubs/scoreboard");

export const ScoreboardHubSignalRConnectionBuilder = new HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect(policy)
  .configureLogging(logger);
