export interface TwitchEventAlertPayload {
  eventId: string;
  eventType: string;
  title: string;
  message: string;
  userId: string;
  userLogin: string;
  userName: string;
  viewers: number;
  occurredAt: string;
}

/** Reads the typed SignalR branch emitted by MARS.Alerts. */
export function parseTwitchEventAlertPayload(
  payload: unknown
): TwitchEventAlertPayload | null {
  if (payload === null || typeof payload !== "object" || Array.isArray(payload)) {
    return null;
  }

  const value = payload as Record<string, unknown>;
  const eventId = value.eventId;
  const eventType = value.eventType;
  const title = value.title;
  const message = value.message;

  if (
    typeof eventId !== "string" ||
    eventId.length === 0 ||
    typeof eventType !== "string" ||
    typeof title !== "string" ||
    typeof message !== "string"
  ) {
    return null;
  }

  return {
    eventId,
    eventType,
    title,
    message,
    userId: typeof value.userId === "string" ? value.userId : "",
    userLogin: typeof value.userLogin === "string" ? value.userLogin : "",
    userName: typeof value.userName === "string" ? value.userName : "",
    viewers: typeof value.viewers === "number" ? value.viewers : 0,
    occurredAt: typeof value.occurredAt === "string" ? value.occurredAt : "",
  };
}
