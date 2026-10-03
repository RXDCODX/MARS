import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/shared/api", () => ({
  RxdcodxViewers: class {
    public async rxdcodxViewersAllList() {
      return {
        data: {
          data: [],
        },
      };
    }
  },
  TelegramusHubSignalRContext: {
    useSignalREffect: () => {},
    invoke: () => {},
  },
}));

vi.mock("@/shared/twitchStore/twitchStore", () => ({
  useTwitchStore: () => ({
    getStreamerInfo: async () => null,
    getStreamerChatColor: async () => "#FFFFFF",
  }),
}));

vi.mock("@/shared/Utils/Announce/Announce", () => ({
  default: () => createElement("div", { "data-testid": "announce" }),
}));

import Credits from "./Credits";

describe("Credits render smoke", () => {
  it("SSR-renders without crashing", () => {
    const markup = renderToStaticMarkup(createElement(Credits));

    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });
});
