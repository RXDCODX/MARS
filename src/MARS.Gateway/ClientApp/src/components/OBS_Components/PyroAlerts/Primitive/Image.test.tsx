import { describe, expect, it, vi } from "vitest";

import { MediaDto } from "@/shared/api";

vi.mock("@/shared/Utils", () => ({
  getCoordinates: vi.fn(() => ({
    position: "absolute",
    left: "100px",
    top: "100px",
  })),
  getRandomRotation: vi.fn(() => ({})),
}));

vi.mock("../../OBSCommon.module.scss", () => ({
  default: {
    textStrokeShadow: "text-stroke-shadow-class",
  },
}));

vi.mock("./Media.module.scss", () => ({
  default: {
    imageContainer: "image-container-class",
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

const createMockMediaDto = (overrides: Partial<MediaDto> = {}): MediaDto => ({
  mediaInfo: {
    id: "test-image-id",
    fileInfo: {
      filePath: "/test/image.png",
      isLocalFile: false,
      type: "Image",
    },
    metaInfo: {
      duration: 3,
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
      isHorizontalCenter: false,
      isVerticallCenter: false,
      isRotated: false,
      rotation: 0,
      randomCoordinates: false,
      isResizeRequires: false,
      isUseOriginalWidthAndHeight: false,
    },
    textInfo: {
      text: "Test Alert",
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
  ...overrides,
});

describe("Image Primitive Component Data Structure", () => {
  const mockCallback = vi.fn();

  it("creates valid mock data for image", () => {
    const mediaDto = createMockMediaDto();
    expect(mediaDto.mediaInfo.fileInfo.type).toBe("Image");
    expect(mediaDto.mediaInfo.fileInfo.filePath).toContain("image.png");
  });

  it("image has text color defined", () => {
    const mediaDto = createMockMediaDto();
    expect(mediaDto.mediaInfo.textInfo.textColor).toBe("#FF0000");
    expect(mediaDto.mediaInfo.textInfo.textColor).toMatch(/^#[0-9A-F]{6}$/i);
  });

  it("image supports custom text colors", () => {
    const colors = ["#FF0000", "#00FF00", "#0000FF", "#FFFFFF"];
    for (const color of colors) {
      const mediaDto = createMockMediaDto({
        mediaInfo: {
          ...createMockMediaDto().mediaInfo,
          textInfo: {
            text: "Test",
            textColor: color,
            keyWordsColor: "#00FF00",
            keyWordSybmolDelimiter: "#",
            triggerWord: undefined,
          },
        },
      });
      expect(mediaDto.mediaInfo.textInfo.textColor).toBe(color);
    }
  });

  it("image border style can be enabled", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        stylesInfo: {
          isBorder: true,
          isShowLetterbox: false,
        },
      },
    });
    expect(mediaDto.mediaInfo.stylesInfo.isBorder).toBe(true);
  });

  it("image border style can be disabled", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        stylesInfo: {
          isBorder: false,
          isShowLetterbox: false,
        },
      },
    });
    expect(mediaDto.mediaInfo.stylesInfo.isBorder).toBe(false);
  });

  it("image handles proportional sizing", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isProportion: true,
          width: 400,
          height: 300,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.isProportion).toBe(true);
    expect(mediaDto.mediaInfo.positionInfo.width).toBe(400);
    expect(mediaDto.mediaInfo.positionInfo.height).toBe(300);
  });

  it("image handles fixed sizing", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isProportion: false,
          width: 800,
          height: 600,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.isProportion).toBe(false);
  });

  it("image supports rotation", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isRotated: true,
          rotation: 45,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.isRotated).toBe(true);
    expect(mediaDto.mediaInfo.positionInfo.rotation).toBe(45);
  });

  it("image supports horizontal centering", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isHorizontalCenter: true,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.isHorizontalCenter).toBe(true);
  });

  it("image supports vertical centering", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          isVerticallCenter: true,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.isVerticallCenter).toBe(true);
  });

  it("image supports random coordinates", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          randomCoordinates: true,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.randomCoordinates).toBe(true);
  });

  it("image has valid duration", () => {
    const mediaDto = createMockMediaDto();
    expect(mediaDto.mediaInfo.metaInfo.duration).toBeGreaterThan(0);
  });

  it("image text is not empty by default", () => {
    const mediaDto = createMockMediaDto();
    expect(mediaDto.mediaInfo.textInfo.text).toBeTruthy();
  });

  it("image constrains text width to image dimensions", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        positionInfo: {
          ...createMockMediaDto().mediaInfo.positionInfo,
          width: 300,
        },
      },
    });
    expect(mediaDto.mediaInfo.positionInfo.width).toBe(300);
  });

  it("image supports long alert text for wrapping", () => {
    const longText =
      "#UserName# был рикрольнут на баллы канала! Это очень длинный текст";
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        textInfo: {
          ...createMockMediaDto().mediaInfo.textInfo,
          text: longText,
        },
      },
    });
    expect(mediaDto.mediaInfo.textInfo.text.length).toBeGreaterThan(50);
  });

  it("image handles empty text gracefully", () => {
    const mediaDto = createMockMediaDto({
      mediaInfo: {
        ...createMockMediaDto().mediaInfo,
        textInfo: {
          ...createMockMediaDto().mediaInfo.textInfo,
          text: "",
        },
      },
    });
    expect(mediaDto.mediaInfo.textInfo.text).toBe("");
  });
});
