import { act, createElement } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, waitFor } from "@testing-library/react";

// Моки зависимостей, как в прежнем файле: без них компонент не монтируется в jsdom.
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

vi.mock("@/shared/stores/twitchStore", () => ({
  default: vi.fn(() => ({
    parser: null,
    parseToLink: null,
  })),
}));

vi.mock("@/shared/utils/Announce/Announce", () => ({
  default: () => createElement("div", { "data-testid": "announce" }),
}));

vi.mock("@lottiefiles/lottie-player/dist/tgs-player", () => ({
  TGSPlayer: class {
    constructor() {}
  },
}));

vi.mock("react-textfit", () => ({
  Textfit: ({ children }: { children: React.ReactNode }) =>
    createElement("div", { "data-testid": "textfit" }, children),
}));

vi.mock("@/shared/utils/BigTexts/BigTextBlockForAudio", () => ({
  BigTextBlockForAudio: () =>
    createElement("div", { "data-testid": "big-text-audio" }),
}));

import PyroAlerts from "./PyroAlerts";
import { setOverlayAdapter } from "@/shared/api/realtime/overlayHub";
import { FakeHubAdapter } from "@/shared/api/realtime/FakeHubAdapter";
import fixture from "./__fixtures__/reward-alert.wire.json";

const EXPECTED_TEXT = "Pyro активировал Привет всем";

describe("PyroAlerts рендерит алерт по событию хаба", () => {
  let adapter: FakeHubAdapter;

  beforeEach(() => {
    adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);
  });

  afterEach(() => {
    setOverlayAdapter(null);
    cleanup();
  });

  it("рендерит точный текст алерта из события Alert", async () => {
    render(createElement(PyroAlerts));

    act(() => adapter.emit("Alert", fixture));

    await waitFor(() => {
      expect(document.body.textContent).toContain(EXPECTED_TEXT);
    });
  });

  it("рендерит алерт из пакета Alerts", async () => {
    render(createElement(PyroAlerts));

    act(() => adapter.emit("Alerts", { media: [fixture.media] }));

    await waitFor(() => {
      expect(document.body.textContent).toContain(EXPECTED_TEXT);
    });
  });

  it("собирает URL локального файла из MediaStorage без двойного слеша", async () => {
    const localFile = {
      media: {
        ...fixture.media,
        mediaInfo: {
          ...fixture.media.mediaInfo,
          fileInfo: {
            ...fixture.media.mediaInfo.fileInfo,
            type: "Image",
            filePath: "/Alerts/uploaded_mems/local.png",
            isLocalFile: true,
          },
        },
      },
    };

    render(createElement(PyroAlerts));

    act(() => adapter.emit("Alert", localFile));

    await waitFor(() => {
      const img = document.querySelector("img");
      expect(img).not.toBeNull();
      expect(img?.getAttribute("src")).toBe("/Alerts/uploaded_mems/local.png");
    });
  });

  it("рендерит высокоприоритетный алерт", async () => {
    const highPriority = {
      media: {
        ...fixture.media,
        mediaInfo: {
          ...fixture.media.mediaInfo,
          metaInfo: { ...fixture.media.mediaInfo.metaInfo, priority: "High" },
        },
      },
    };

    render(createElement(PyroAlerts));

    act(() => adapter.emit("Alert", highPriority));

    await waitFor(() => {
      expect(document.body.textContent).toContain(EXPECTED_TEXT);
    });
  });
});
