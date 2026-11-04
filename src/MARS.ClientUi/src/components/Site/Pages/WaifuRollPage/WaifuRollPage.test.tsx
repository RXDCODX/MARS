import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("react-router-dom", () => ({
  Link: ({ children, to, ...properties }: any) =>
    createElement("a", { href: to, ...properties }, children),
}));

vi.mock("@/shared/Utils/ToastModal", () => ({
  useToastModal: () => ({
    showToast: vi.fn(),
  }),
}));

vi.mock("./store/useWaifuRollStore", () => ({
  useWaifuRollStore: (selector: any) =>
    selector({
      mode: "waifu",
      waifus: [],
      husbands: [],
      audios: [],
      searchQuery: "",
      sortDirection: "desc",
      isLoading: false,
      isSubmitting: false,
      isDeleting: false,
      error: "",
      showForm: false,
      formMode: "create",
      waifuFormValues: {
        shikiId: "",
        name: "",
        age: 0,
        anime: "",
        manga: "",
        imageUrl: "",
        audioId: "",
      },
      husbandFormValues: {
        waifuBrideId: "",
        isPrivated: false,
        waifuRollId: "",
        whenPrivated: "",
        lastWeddingCongratulatedMonths: 0,
      },
      editingTwitchId: undefined,
      confirmDeleteId: undefined,
      confirmUnmergeId: undefined,
      setMode: vi.fn(),
      setSearchQuery: vi.fn(),
      toggleSortDirection: vi.fn(),
      startCreate: vi.fn(),
      startEditWaifu: vi.fn(),
      startEditHusband: vi.fn(),
      cancelForm: vi.fn(),
      setWaifuFormValues: vi.fn(),
      setHusbandFormValues: vi.fn(),
      confirmDelete: vi.fn(),
      cancelDelete: vi.fn(),
      confirmUnmerge: vi.fn(),
      cancelUnmerge: vi.fn(),
      switchToHusbandAndEdit: vi.fn(),
      loadWaifus: vi.fn().mockResolvedValue(null),
      loadHusbands: vi.fn().mockResolvedValue(null),
      loadAudios: vi.fn().mockResolvedValue(null),
      submitWaifu: vi.fn(),
      submitHusband: vi.fn(),
      deleteWaifu: vi.fn(),
      deleteHusband: vi.fn(),
      unmergeHusband: vi.fn(),
      deleteAudio: vi.fn(),
      uploadAudio: vi.fn(),
    }),
}));

describe("WaifuRollPage", () => {
  it("contains page title", async () => {
    const { default: WaifuRollPage } = await import("./WaifuRollPage");
    const markup = renderToStaticMarkup(createElement(WaifuRollPage));
    expect(markup).toContain("WaifuRoll Manager");
  });

  it("has data-testid attributes", async () => {
    const { default: WaifuRollPage } = await import("./WaifuRollPage");
    const markup = renderToStaticMarkup(createElement(WaifuRollPage));
    expect(markup).toContain('data-testid="waifu-roll-page"');
    expect(markup).toContain('data-testid="waifu-roll-header"');
    expect(markup).toContain('data-testid="page-title"');
    expect(markup).toContain('data-testid="waifu-roll-slider"');
    expect(markup).toContain('data-testid="segmented-mode"');
  });

  it("shows waifu list by default", async () => {
    const { default: WaifuRollPage } = await import("./WaifuRollPage");
    const markup = renderToStaticMarkup(createElement(WaifuRollPage));
    expect(markup).toContain('data-testid="waifu-list"');
    expect(markup).toContain("Вайфу не найдены");
  });

  it("does not show audio manager on page", async () => {
    const { default: WaifuRollPage } = await import("./WaifuRollPage");
    const markup = renderToStaticMarkup(createElement(WaifuRollPage));
    expect(markup).not.toContain('data-testid="audio-manager"');
  });
});
