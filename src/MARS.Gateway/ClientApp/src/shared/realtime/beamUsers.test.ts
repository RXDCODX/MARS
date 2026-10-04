import { describe, expect, it } from "vitest";

import { normalizeBeamUser, normalizeBeamUsers } from "./beamUsers";

/**
 * Участники MikuMikuBeam на проводе.
 *
 * Форма снята из `MikuMikuBeamHandler`, а не из `data-contracts.ts`: сервер
 * кладёт в список только `TwitchId`. Экран же написан под `TwitchUser` и читал
 * `displayName`, `profileImageUrl` и `chatColor` — на проводе их нет.
 */
const wireUsers = [{ twitchId: "785975641" }, { twitchId: "123456789" }];

describe("участники луча MikuMikuBeam", () => {
  it("берёт то, что на проводе: идентификатор Twitch", () => {
    expect(normalizeBeamUser(wireUsers[0])).toEqual({
      twitchId: "785975641",
      displayName: "785975641",
      initial: "7",
      chatColor: "#FFFFFF",
      profileImageUrl: undefined,
    });
  });

  it("не падает на участнике без ника — это и был источник белого экрана", () => {
    // Раньше здесь вызывалось `user.displayName.charAt(0)` на `undefined`.
    // Падение происходило в рендере, а `ErrorBoundary` в приложение не
    // смонтирован, то есть валился весь документ.
    expect(() => normalizeBeamUser(wireUsers[0])).not.toThrow();
  });

  it("подставляет цвет по умолчанию, когда на проводе его нет", () => {
    expect(normalizeBeamUser(wireUsers[0])?.chatColor).toBe("#FFFFFF");
    expect(
      normalizeBeamUser({ twitchId: "1", chatColor: "#00FF00" })?.chatColor
    ).toBe("#00FF00");
  });

  it("отбрасывает участника без идентификатора", () => {
    expect(normalizeBeamUser({})).toBeNull();
    expect(normalizeBeamUser({ twitchId: "" })).toBeNull();
    expect(normalizeBeamUser(null)).toBeNull();
    expect(normalizeBeamUser("785975641")).toBeNull();
  });

  it("собирает весь список и не падает на мусоре", () => {
    expect(normalizeBeamUsers(wireUsers)).toHaveLength(2);
    expect(normalizeBeamUsers([wireUsers[0], null, {}, "x"])).toHaveLength(1);
  });

  it("пустой список означает «никого не было», а не «что-то сломалось»", () => {
    expect(normalizeBeamUsers(undefined)).toEqual([]);
    expect(normalizeBeamUsers({ twitchId: "1" })).toEqual([]);
    expect(normalizeBeamUsers([])).toEqual([]);
  });

  it("у участника всегда есть непустой заголовок для заглушки аватарки", () => {
    normalizeBeamUsers(wireUsers).forEach(user => {
      expect(user.initial.length).toBeGreaterThan(0);
    });
  });
});
