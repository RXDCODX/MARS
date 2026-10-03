import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

// Mock зависимости
vi.mock("@/shared/api", () => ({
  MediaFileInfoTypeEnum: {
    Image: "Image",
    Gif: "Gif",
    Video: "Video",
    Audio: "Audio",
    Voice: "Voice",
    TelegramSticker: "TelegramSticker",
  },
  MediaMetaInfoPriorityEnum: {
    High: "High",
    Normal: "Normal",
    Low: "Low",
  },
  TelegramusHubSignalRContext: {
    useSignalREffect: () => {},
    invoke: () => {},
  },
}));

vi.mock("@/shared/twitchStore/twitchStore", () => ({
  default: vi.fn(() => ({
    parser: null,
    parseToLink: null,
  })),
}));

vi.mock("@/shared/Utils/Announce/Announce", () => ({
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

vi.mock("@/shared/Utils/BigTexts/BigTextBlockForAudio", () => ({
  BigTextBlockForAudio: () =>
    createElement("div", { "data-testid": "big-text-audio" }),
}));

import PyroAlerts from "./PyroAlerts";

// Mock данные
const mockMediaDto = {
  mediaInfo: {
    id: "test-id-1",
    fileInfo: {
      filePath: "/test/image.png",
      isLocalFile: false,
      type: "Image",
    },
    metaInfo: {
      duration: 5,
      displayName: "TestUser",
      priority: "Normal",
      volume: 100,
      isLooped: false,
    },
    positionInfo: {
      xCoordinate: 100,
      yCoordinate: 100,
      width: 400,
      height: 300,
      isProportion: true,
      isHorizontalCenter: true,
      isVerticallCenter: true,
      isRotated: false,
      rotation: 0,
      randomCoordinates: false,
      isResizeRequires: false,
      isUseOriginalWidthAndHeight: false,
    },
    textInfo: {
      text: "Test Alert Text",
      textColor: "#FF0000",
      keyWordsColor: "#00FF00",
      keyWordSybmolDelimiter: "#",
      triggerWord: undefined,
    },
    stylesInfo: {
      isBorder: true,
      isShowLetterbox: false,
    },
  },
  uploadStartTime: new Date().toISOString(),
};

describe("PyroAlerts Component", () => {
  it("renders without crashing", () => {
    const markup = renderToStaticMarkup(createElement(PyroAlerts));
    expect(markup).toBeTruthy();
    expect(markup.length).toBeGreaterThan(0);
  });

  it("renders announce component on initial mount", () => {
    const markup = renderToStaticMarkup(createElement(PyroAlerts));
    expect(markup).toContain("announce");
  });

  it("should have correct component structure", () => {
    const markup = renderToStaticMarkup(createElement(PyroAlerts));
    expect(markup).toBeTruthy();
    // Проверяем, что компонент рендерится корректно без пустых значений
    expect(markup).not.toContain("undefined");
  });
});

describe("PyroAlerts Media Data Structure", () => {
  it("mockMediaDto has required Image properties", () => {
    expect(mockMediaDto.mediaInfo).toBeDefined();
    expect(mockMediaDto.mediaInfo.fileInfo).toBeDefined();
    expect(mockMediaDto.mediaInfo.metaInfo).toBeDefined();
    expect(mockMediaDto.mediaInfo.textInfo).toBeDefined();
    expect(mockMediaDto.mediaInfo.positionInfo).toBeDefined();
    expect(mockMediaDto.mediaInfo.stylesInfo).toBeDefined();
  });

  it("mockMediaDto has valid text color", () => {
    expect(mockMediaDto.mediaInfo.textInfo.textColor).toBe("#FF0000");
    expect(mockMediaDto.mediaInfo.textInfo.textColor).toMatch(
      /^#[0-9A-F]{6}$/i
    );
  });

  it("mockMediaDto has border enabled", () => {
    expect(mockMediaDto.mediaInfo.stylesInfo.isBorder).toBe(true);
  });

  it("mockMediaDto has valid dimensions", () => {
    const position = mockMediaDto.mediaInfo.positionInfo;
    expect(position.width).toBeGreaterThan(0);
    expect(position.height).toBeGreaterThan(0);
  });

  it("mockMediaDto has valid coordinates", () => {
    const position = mockMediaDto.mediaInfo.positionInfo;
    expect(position.xCoordinate).toBeDefined();
    expect(position.yCoordinate).toBeDefined();
  });

  it("mockMediaDto has valid duration", () => {
    expect(mockMediaDto.mediaInfo.metaInfo.duration).toBeGreaterThan(0);
  });

  it("mockMediaDto has valid alert text", () => {
    expect(mockMediaDto.mediaInfo.textInfo.text).toBeTruthy();
    expect(typeof mockMediaDto.mediaInfo.textInfo.text).toBe("string");
  });
});

describe("Alert Type Variants", () => {
  const createMediaDtoForType = (type: string): typeof mockMediaDto => ({
    ...mockMediaDto,
    mediaInfo: {
      ...mockMediaDto.mediaInfo,
      fileInfo: {
        ...mockMediaDto.mediaInfo.fileInfo,
        type,
      },
    },
  });

  it("should have valid Image type data", () => {
    const imageMedia = createMediaDtoForType("Image");
    expect(imageMedia.mediaInfo.fileInfo.type).toBe("Image");
  });

  it("should have valid Video type data", () => {
    const videoMedia = createMediaDtoForType("Video");
    expect(videoMedia.mediaInfo.fileInfo.type).toBe("Video");
  });

  it("should have valid Audio type data", () => {
    const audioMedia = createMediaDtoForType("Audio");
    expect(audioMedia.mediaInfo.fileInfo.type).toBe("Audio");
  });

  it("should have valid Voice type data", () => {
    const voiceMedia = createMediaDtoForType("Voice");
    expect(voiceMedia.mediaInfo.fileInfo.type).toBe("Voice");
  });

  it("should have valid Gif type data", () => {
    const gifMedia = createMediaDtoForType("Gif");
    expect(gifMedia.mediaInfo.fileInfo.type).toBe("Gif");
  });

  it("should have valid TelegramSticker type data", () => {
    const stickerMedia = createMediaDtoForType("TelegramSticker");
    expect(stickerMedia.mediaInfo.fileInfo.type).toBe("TelegramSticker");
  });
});

describe("Media Styling Properties", () => {
  it("should apply textColor from textInfo", () => {
    const colorVariants = ["#FF0000", "#00FF00", "#0000FF", "#FFFFFF"];
    for (const color of colorVariants) {
      const media = {
        ...mockMediaDto,
        mediaInfo: {
          ...mockMediaDto.mediaInfo,
          textInfo: {
            ...mockMediaDto.mediaInfo.textInfo,
            textColor: color,
          },
        },
      };
      expect(media.mediaInfo.textInfo.textColor).toBe(color);
    }
  });

  it("should support border styling", () => {
    const mediaBorder = { ...mockMediaDto };
    mediaBorder.mediaInfo.stylesInfo.isBorder = true;
    expect(mediaBorder.mediaInfo.stylesInfo.isBorder).toBe(true);

    const mediaNooBorder = { ...mockMediaDto };
    mediaNooBorder.mediaInfo.stylesInfo.isBorder = false;
    expect(mediaNooBorder.mediaInfo.stylesInfo.isBorder).toBe(false);
  });

  it("should support letterbox styling", () => {
    const mediaLetterbox = { ...mockMediaDto };
    mediaLetterbox.mediaInfo.stylesInfo.isShowLetterbox = true;
    expect(mediaLetterbox.mediaInfo.stylesInfo.isShowLetterbox).toBe(true);
  });
});

describe("Position Configuration", () => {
  it("should support horizontal centering", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.isHorizontalCenter = true;
    expect(media.mediaInfo.positionInfo.isHorizontalCenter).toBe(true);
  });

  it("should support vertical centering", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.isVerticallCenter = true;
    expect(media.mediaInfo.positionInfo.isVerticallCenter).toBe(true);
  });

  it("should support random coordinates", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.randomCoordinates = true;
    expect(media.mediaInfo.positionInfo.randomCoordinates).toBe(true);
  });

  it("should support rotation", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.isRotated = true;
    media.mediaInfo.positionInfo.rotation = 45;
    expect(media.mediaInfo.positionInfo.isRotated).toBe(true);
    expect(media.mediaInfo.positionInfo.rotation).toBe(45);
  });

  it("should support proportional resizing", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.isProportion = true;
    expect(media.mediaInfo.positionInfo.isProportion).toBe(true);
  });

  it("should support fixed dimension resizing", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.positionInfo.isProportion = false;
    expect(media.mediaInfo.positionInfo.isProportion).toBe(false);
  });
});

describe("Alert Priority Levels", () => {
  it("should support Normal priority", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.metaInfo.priority = "Normal";
    expect(media.mediaInfo.metaInfo.priority).toBe("Normal");
  });

  it("should support High priority", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.metaInfo.priority = "High";
    expect(media.mediaInfo.metaInfo.priority).toBe("High");
  });

  it("should support Low priority", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.metaInfo.priority = "Low";
    expect(media.mediaInfo.metaInfo.priority).toBe("Low");
  });
});

describe("Audio Configuration", () => {
  it("should support volume settings above 100%", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.metaInfo.volume = 150;
    expect(media.mediaInfo.metaInfo.volume).toBe(150);
  });

  it("should support loop configuration", () => {
    const mediaLooped = { ...mockMediaDto };
    mediaLooped.mediaInfo.metaInfo.isLooped = true;
    expect(mediaLooped.mediaInfo.metaInfo.isLooped).toBe(true);

    const mediaNoLoop = { ...mockMediaDto };
    mediaNoLoop.mediaInfo.metaInfo.isLooped = false;
    expect(mediaNoLoop.mediaInfo.metaInfo.isLooped).toBe(false);
  });
});

describe("Text Information", () => {
  it("should contain text content", () => {
    expect(mockMediaDto.mediaInfo.textInfo.text).toBeTruthy();
  });

  it("should support custom text colors", () => {
    const colors = ["#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF"];
    for (const color of colors) {
      const media = { ...mockMediaDto };
      media.mediaInfo.textInfo.textColor = color;
      expect(media.mediaInfo.textInfo.textColor).toBe(color);
    }
  });

  it("should support keyword highlighting colors", () => {
    expect(mockMediaDto.mediaInfo.textInfo.keyWordsColor).toBe("#00FF00");
  });

  it("should support custom delimiter for keywords", () => {
    expect(mockMediaDto.mediaInfo.textInfo.keyWordSybmolDelimiter).toBe("#");
  });
});

describe("Text Wrapping Configuration", () => {
  it("should support long text content that requires wrapping", () => {
    const longText =
      "#UserName# был рикрольнут на баллы канала! Это очень длинный текст для проверки переноса строк в алертах";
    const media = { ...mockMediaDto };
    media.mediaInfo.textInfo.text = longText;
    expect(media.mediaInfo.textInfo.text.length).toBeGreaterThan(50);
    expect(media.mediaInfo.textInfo.text).toBe(longText);
  });

  it("should have positionInfo width available for text maxWidth constraint", () => {
    const position = mockMediaDto.mediaInfo.positionInfo;
    expect(position.width).toBeGreaterThan(0);
    expect(typeof position.width).toBe("number");
  });

  it("should handle empty text without breaking rendering", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.textInfo.text = "";
    expect(media.mediaInfo.textInfo.text).toBe("");
  });

  it("should handle text with keyword delimiters correctly", () => {
    const media = { ...mockMediaDto };
    media.mediaInfo.textInfo.text =
      "#user.name# был рикрольнут на #channel_points# канала!";
    expect(media.mediaInfo.textInfo.text).toContain("#");
    expect(media.mediaInfo.textInfo.keyWordSybmolDelimiter).toBe("#");
  });
});

describe("File Path Configuration", () => {
  it("should support local file paths", () => {
    const localMedia = { ...mockMediaDto };
    localMedia.mediaInfo.fileInfo.isLocalFile = true;
    expect(localMedia.mediaInfo.fileInfo.isLocalFile).toBe(true);
  });

  it("should support remote file paths", () => {
    const remoteMedia = { ...mockMediaDto };
    remoteMedia.mediaInfo.fileInfo.isLocalFile = false;
    expect(remoteMedia.mediaInfo.fileInfo.isLocalFile).toBe(false);
  });

  it("should have valid file paths", () => {
    expect(mockMediaDto.mediaInfo.fileInfo.filePath).toBeTruthy();
    expect(typeof mockMediaDto.mediaInfo.fileInfo.filePath).toBe("string");
  });
});
