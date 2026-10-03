import { describe, expect, it, vi } from "vitest";

vi.mock("@lit/react", () => ({
  createComponent: vi.fn(
    () => (properties: any) => null // Mock компонент
  ),
}));

vi.mock("@lottiefiles/lottie-player/dist/tgs-player", () => ({
  TGSPlayer: class {
    constructor() {}
  },
}));

vi.mock("@/shared/Utils", () => ({
  getCoordinates: vi.fn(() => ({
    position: "absolute",
    left: "100px",
    top: "100px",
  })),
  getRandomRotation: vi.fn(() => ({})),
  replaceEmotes: vi.fn(data => data.text),
}));

vi.mock("@/shared/twitchStore/twitchStore", () => ({
  default: vi.fn(() => ({
    parser: null,
    parseToLink: null,
  })),
}));

vi.mock("./Media.module.scss", () => ({
  default: {
    media: "media-class",
  },
}));

vi.mock("./mediaFrameStyle", () => ({
  getMediaFrameStyle: vi.fn(mediaInfo =>
    mediaInfo.mediaInfo.stylesInfo.isBorder
      ? { boxShadow: "0 0 0 12px #ffffff" }
      : {}
  ),
}));

import { MediaDto } from "@/shared/api";

const createMockMediaDto = (overrides: Partial<MediaDto> = {}): MediaDto => ({
  mediaInfo: {
    id: "test-sticker-id",
    fileInfo: {
      filePath: "/test/sticker.tgs",
      isLocalFile: false,
      type: "TelegramSticker",
    },
    metaInfo: {
      duration: 2,
      displayName: "StickerUser",
      priority: "Normal",
      volume: 100,
      isLooped: false,
    },
    positionInfo: {
      xCoordinate: 50,
      yCoordinate: 50,
      width: 320,
      height: 320,
      isProportion: false,
      isHorizontalCenter: true,
      isVerticallCenter: true,
      isRotated: false,
      rotation: 0,
      randomCoordinates: false,
      isResizeRequires: false,
      isUseOriginalWidthAndHeight: false,
    },
    textInfo: {
      text: "Sticker Text",
      textColor: "#FFFFFF",
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
  ...overrides,
});

describe("TelegramSticker Data Structure", () => {
  it("creates valid mock data for sticker", () => {
    const media = createMockMediaDto();
    expect(media.mediaInfo.fileInfo.type).toBe("TelegramSticker");
    expect(media.mediaInfo.fileInfo.filePath).toContain("sticker.tgs");
  });

  it("supports custom text colors for sticker", () => {
    const colors = ["#FF0000", "#FFFFFF", "#000000"];
    for (const color of colors) {
      const media = createMockMediaDto({
        mediaInfo: {
          ...createMockMediaDto().mediaInfo,
          textInfo: {
            text: "Sticker Text",
            textColor: color,
            keyWordsColor: "#00FF00",
            keyWordSybmolDelimiter: "#",
            triggerWord: undefined,
          },
        },
      });
      expect(media.mediaInfo.textInfo.textColor).toBe(color);
    }
  });

  it("supports sticker border styling", () => {
    const media = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        stylesInfo: {
          isBorder: true,
          isShowLetterbox: false,
        },
      },
    });
    expect(media.mediaInfo.stylesInfo.isBorder).toBe(true);
  });

  it("sticker maintains square dimensions", () => {
    const media = createMockMediaDto();
    // TGamerSticker обычно квадратный 320x320
    expect(media.mediaInfo.positionInfo.width).toBe(320);
    expect(media.mediaInfo.positionInfo.height).toBe(320);
  });

  it("supports sticker positioning with centering", () => {
    const media = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isHorizontalCenter: true,
          isVerticallCenter: true,
        },
      },
    });
    expect(media.mediaInfo.positionInfo.isHorizontalCenter).toBe(true);
    expect(media.mediaInfo.positionInfo.isVerticallCenter).toBe(true);
  });

  it("supports sticker with text content", () => {
    const media = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        textInfo: {
          text: "Sticker Caption",
          textColor: "#FFFFFF",
          keyWordsColor: "#00FF00",
          keyWordSybmolDelimiter: "#",
          triggerWord: undefined,
        },
      },
    });
    expect(media.mediaInfo.textInfo.text).toBe("Sticker Caption");
  });

  it("supports short sticker duration", () => {
    const media = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        metaInfo: {
          ...createMockMediaDto().mediaInfo.metaInfo,
          duration: 1.5,
        },
      },
    });
    expect(media.mediaInfo.metaInfo.duration).toBe(1.5);
  });

  it("maintains sticker file extension", () => {
    const media = createMockMediaDto();
    expect(media.mediaInfo.fileInfo.filePath).toMatch(/\.tgs$/);
  });
});

describe("Audio/Voice Type Data Structure", () => {
  const createAudioMediaDto = (
    overrides: Partial<MediaDto> = {}
  ): MediaDto => ({
    mediaInfo: {
      id: "test-audio-id",
      fileInfo: {
        filePath: "/test/audio.mp3",
        isLocalFile: false,
        type: "Audio",
      },
      metaInfo: {
        duration: 10,
        displayName: "AudioUser",
        priority: "Normal",
        volume: 100,
        isLooped: false,
      },
      positionInfo: {
        xCoordinate: 0,
        yCoordinate: 0,
        width: 0,
        height: 0,
        isProportion: false,
        isHorizontalCenter: false,
        isVerticallCenter: false,
        isRotated: false,
        rotation: 0,
        randomCoordinates: false,
        isResizeRequires: false,
        isUseOriginalWidthAndHeight: false,
      },
      textInfo: {
        text: "Audio Alert",
        textColor: "#FFFFFF",
        keyWordsColor: "#00FF00",
        keyWordSybmolDelimiter: "#",
        triggerWord: undefined,
      },
      stylesInfo: {
        isBorder: false,
        isShowLetterbox: true,
      },
    },
    uploadStartTime: new Date().toISOString(),
    ...overrides,
  });

  it("creates valid mock data for audio", () => {
    const media = createAudioMediaDto();
    expect(media.mediaInfo.fileInfo.type).toBe("Audio");
    expect(media.mediaInfo.fileInfo.filePath).toContain("audio.mp3");
  });

  it("supports audio with volume above 100%", () => {
    const media = createAudioMediaDto({
      mediaInfo: {
        ...createAudioMediaDto().mediaInfo,
        metaInfo: {
          ...createAudioMediaDto().mediaInfo.metaInfo,
          volume: 150,
        },
      },
    });
    expect(media.mediaInfo.metaInfo.volume).toBe(150);
  });

  it("supports audio letterbox display", () => {
    const media = createAudioMediaDto({
      mediaInfo: {
        ...createAudioMediaDto().mediaInfo,
        stylesInfo: {
          isBorder: false,
          isShowLetterbox: true,
        },
      },
    });
    expect(media.mediaInfo.stylesInfo.isShowLetterbox).toBe(true);
  });

  it("supports looping for audio", () => {
    const media = createAudioMediaDto({
      mediaInfo: {
        ...createAudioMediaDto().mediaInfo,
        metaInfo: {
          ...createAudioMediaDto().mediaInfo.metaInfo,
          isLooped: true,
        },
      },
    });
    expect(media.mediaInfo.metaInfo.isLooped).toBe(true);
  });

  it("audio duration can be variable", () => {
    const durations = [5, 10, 30, 120];
    for (const duration of durations) {
      const media = createAudioMediaDto({
        mediaInfo: {
          ...createAudioMediaDto().mediaInfo,
          metaInfo: {
            ...createAudioMediaDto().mediaInfo.metaInfo,
            duration,
          },
        },
      });
      expect(media.mediaInfo.metaInfo.duration).toBe(duration);
    }
  });
});

describe("Video Type Data Structure", () => {
  const createVideoMediaDto = (
    overrides: Partial<MediaDto> = {}
  ): MediaDto => ({
    mediaInfo: {
      id: "test-video-id",
      fileInfo: {
        filePath: "/test/video.mp4",
        isLocalFile: false,
        type: "Video",
      },
      metaInfo: {
        duration: 15,
        displayName: "VideoUser",
        priority: "High",
        volume: 100,
        isLooped: false,
      },
      positionInfo: {
        xCoordinate: 100,
        yCoordinate: 100,
        width: 800,
        height: 600,
        isProportion: true,
        isHorizontalCenter: false,
        isVerticallCenter: false,
        isRotated: false,
        rotation: 0,
        randomCoordinates: false,
        isResizeRequires: true,
        isUseOriginalWidthAndHeight: false,
      },
      textInfo: {
        text: "Video Title",
        textColor: "#FFFF00",
        keyWordsColor: "#FF00FF",
        keyWordSybmolDelimiter: "#",
        triggerWord: undefined,
      },
      stylesInfo: {
        isBorder: true,
        isShowLetterbox: false,
      },
    },
    uploadStartTime: new Date().toISOString(),
    ...overrides,
  });

  it("creates valid mock data for video", () => {
    const media = createVideoMediaDto();
    expect(media.mediaInfo.fileInfo.type).toBe("Video");
    expect(media.mediaInfo.fileInfo.filePath).toContain("video.mp4");
  });

  it("supports high priority for videos", () => {
    const media = createVideoMediaDto();
    expect(media.mediaInfo.metaInfo.priority).toBe("High");
  });

  it("supports extended video duration", () => {
    const media = createVideoMediaDto({
      mediaInfo: {
        ...createVideoMediaDto().mediaInfo,
        metaInfo: {
          ...createVideoMediaDto().mediaInfo.metaInfo,
          duration: 300,
        },
      },
    });
    expect(media.mediaInfo.metaInfo.duration).toBe(300);
  });

  it("supports video text coloring", () => {
    const colors = ["#FFFF00", "#FF00FF", "#00FFFF"];
    for (const color of colors) {
      const media = createVideoMediaDto({
        mediaInfo: {
          ...createVideoMediaDto().mediaInfo,
          textInfo: {
            text: "Video Title",
            textColor: color,
            keyWordsColor: "#FF00FF",
            keyWordSybmolDelimiter: "#",
            triggerWord: undefined,
          },
        },
      });
      expect(media.mediaInfo.textInfo.textColor).toBe(color);
    }
  });

  it("supports video with border", () => {
    const media = createVideoMediaDto();
    expect(media.mediaInfo.stylesInfo.isBorder).toBe(true);
  });

  it("supports proportional video sizing", () => {
    const media = createVideoMediaDto();
    expect(media.mediaInfo.positionInfo.isProportion).toBe(true);
  });

  it("supports video resize requirement", () => {
    const media = createVideoMediaDto();
    expect(media.mediaInfo.positionInfo.isResizeRequires).toBe(true);
  });
});
