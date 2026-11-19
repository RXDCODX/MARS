import { act, createElement } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, waitFor } from "@testing-library/react";

vi.mock("@/shared/api", () => ({
  MediaType: {
    Image: "Image",
    Gif: "Gif",
    Video: "Video",
    Audio: "Audio",
    Voice: "Voice",
    TelegramSticker: "TelegramSticker",
  },
  MediaAlertPriority: {
    High: "High",
    Normal: "Normal",
    Low: "Low",
  },
}));

vi.mock("@/shared/twitchStore/twitchStore", () => ({
  default: vi.fn(() => ({ parser: null, parseToLink: null })),
}));

vi.mock("@/shared/Utils/Announce/Announce", () => ({
  default: () => createElement("div", { "data-testid": "announce" }),
}));

vi.mock("react-textfit", () => ({
  Textfit: ({ children }: { children: React.ReactNode }) =>
    createElement("div", { "data-testid": "textfit" }, children),
}));

vi.mock("@/shared/Utils/BigTexts/BigTextBlockForAudio", () => ({
  BigTextBlockForAudio: () =>
    createElement("div", { "data-testid": "big-text-audio" }),
}));

vi.mock("@lottiefiles/lottie-player/dist/tgs-player", () => ({
  TGSPlayer: class {
    constructor() {}
  },
}));

import RandomMem from "./RandomMem";
import { setOverlayAdapter } from "@/shared/api/realtime/overlayHub";
import { FakeHubAdapter } from "@/shared/api/realtime/FakeHubAdapter";
import fixture from "../PyroAlerts/__fixtures__/reward-alert.wire.json";

const EXPECTED_TEXT = "Pyro активировал Привет всем";

describe("RandomMem рендерит алерт по событию хаба", () => {
  let adapter: FakeHubAdapter;

  beforeEach(() => {
    adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);
  });

  afterEach(() => {
    setOverlayAdapter(null);
    cleanup();
  });

  it("рендерит точный текст из события RandomMem", async () => {
    render(createElement(RandomMem));

    act(() => adapter.emit("RandomMem", fixture));

    await waitFor(() => {
      expect(document.body.textContent).toContain(EXPECTED_TEXT);
    });
  });
});