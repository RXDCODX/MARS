import { describe, expect, it } from "vitest";

import { parseTwitchEventAlertPayload } from "./twitchEventAlertPayload";

describe("parseTwitchEventAlertPayload", () => {
  it("parses a typed Twitch follow alert from the SignalR branch", () => {
    expect(
      parseTwitchEventAlertPayload({
        eventId: "event-1",
        eventType: "channel.follow",
        title: "Новый подписчик",
        message: "Viewer подписался на канал",
        userId: "user-1",
        userLogin: "viewer",
        userName: "Viewer",
        viewers: 0,
        occurredAt: "2026-11-02T13:00:00.0000000+00:00",
      })
    ).toEqual({
      eventId: "event-1",
      eventType: "channel.follow",
      title: "Новый подписчик",
      message: "Viewer подписался на канал",
      userId: "user-1",
      userLogin: "viewer",
      userName: "Viewer",
      viewers: 0,
      occurredAt: "2026-11-02T13:00:00.0000000+00:00",
    });
  });

  it("rejects malformed event payloads", () => {
    expect(parseTwitchEventAlertPayload(null)).toBeNull();
    expect(parseTwitchEventAlertPayload({})).toBeNull();
    expect(
      parseTwitchEventAlertPayload({
        eventId: "event-1",
        eventType: "channel.follow",
      })
    ).toBeNull();
  });

  it("defaults optional metadata without rejecting an otherwise valid alert", () => {
    expect(
      parseTwitchEventAlertPayload({
        eventId: "event-2",
        eventType: "channel.raid",
        title: "РЕЙД",
        message: "Streamer привёл 42 зрителей",
      })
    ).toEqual({
      eventId: "event-2",
      eventType: "channel.raid",
      title: "РЕЙД",
      message: "Streamer привёл 42 зрителей",
      userId: "",
      userLogin: "",
      userName: "",
      viewers: 0,
      occurredAt: "",
    });
  });
});
