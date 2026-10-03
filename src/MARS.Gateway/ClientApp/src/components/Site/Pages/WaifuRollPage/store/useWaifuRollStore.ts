import { create } from "zustand";

import { defaultApiConfig } from "@/shared/api/api-config";
import { WaifuRoll } from "@/shared/api/http-clients/WaifuRoll";
import type {
  HusbandDto,
  WaifuDto,
  WaifuRollAudioDto,
} from "@/shared/api/types/data-contracts";
import {
  createErrorResult,
  createSuccessResult,
  type OperationResult,
} from "@/shared/types/OperationResult";

import {
  createDefaultHusbandFormValues,
  createDefaultWaifuFormValues,
  husbandDtoToFormValues,
  type HusbandFormValues,
  husbandFormValuesToUpdateRequest,
  type LoadListOptions,
  waifuDtoToFormValues,
  type WaifuFormValues,
  waifuFormValuesToCreateRequest,
  waifuFormValuesToUpdateRequest,
  type WaifuRollMode,
} from "../WaifuRollPage.types";

interface WaifuRollStoreState {
  mode: WaifuRollMode;
  waifus: WaifuDto[];
  husbands: HusbandDto[];
  audios: WaifuRollAudioDto[];
  searchQuery: string;
  sortDirection: "asc" | "desc";
  isLoading: boolean;
  isSubmitting: boolean;
  isDeleting: boolean;
  error: string;
  showForm: boolean;
  formMode: "create" | "edit";
  waifuFormValues: WaifuFormValues;
  husbandFormValues: HusbandFormValues;
  editingTwitchId: string | undefined;
  confirmDeleteId: string | undefined;
  confirmUnmergeId: string | undefined;

  setMode: (mode: WaifuRollMode) => void;
  setSearchQuery: (query: string) => void;
  toggleSortDirection: () => void;
  startCreate: () => void;
  startEditWaifu: (waifu: WaifuDto) => void;
  startEditHusband: (husband: HusbandDto) => void;
  cancelForm: () => void;
  setWaifuFormValues: (values: WaifuFormValues) => void;
  setHusbandFormValues: (values: HusbandFormValues) => void;
  confirmDelete: (id: string) => void;
  cancelDelete: () => void;
  confirmUnmerge: (id: string) => void;
  cancelUnmerge: () => void;
  switchToHusbandAndEdit: (twitchId: string) => void;

  loadWaifus: (options?: LoadListOptions) => Promise<OperationResult | null>;
  loadHusbands: (options?: LoadListOptions) => Promise<OperationResult | null>;
  loadAudios: (options?: LoadListOptions) => Promise<OperationResult | null>;
  submitWaifu: () => Promise<OperationResult | null>;
  submitHusband: () => Promise<OperationResult | null>;
  deleteWaifu: (shikiId: string) => Promise<OperationResult | null>;
  deleteHusband: (twitchId: string) => Promise<OperationResult | null>;
  unmergeHusband: (twitchId: string) => Promise<OperationResult | null>;
  deleteAudio: (id: string) => Promise<OperationResult | null>;
  uploadAudio: (file: File, name: string) => Promise<OperationResult | null>;
}

const api = new WaifuRoll(defaultApiConfig);

export const useWaifuRollStore = create<WaifuRollStoreState>((set, get) => ({
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
  waifuFormValues: createDefaultWaifuFormValues(),
  husbandFormValues: createDefaultHusbandFormValues(),
  editingTwitchId: undefined,
  confirmDeleteId: undefined,
  confirmUnmergeId: undefined,

  setMode: mode => set({ mode, showForm: false, error: "", searchQuery: "" }),

  setSearchQuery: query => set({ searchQuery: query }),

  toggleSortDirection: () =>
    set(state => ({
      sortDirection: state.sortDirection === "asc" ? "desc" : "asc",
    })),

  startCreate: () =>
    set({
      showForm: true,
      formMode: "create",
      waifuFormValues: createDefaultWaifuFormValues(),
      husbandFormValues: createDefaultHusbandFormValues(),
      editingTwitchId: undefined,
    }),

  startEditWaifu: waifu =>
    set({
      showForm: true,
      formMode: "edit",
      waifuFormValues: waifuDtoToFormValues(waifu),
      editingTwitchId: undefined,
    }),

  startEditHusband: husband =>
    set({
      showForm: true,
      formMode: "edit",
      husbandFormValues: husbandDtoToFormValues(husband),
      editingTwitchId: husband.twitchId,
    }),

  cancelForm: () => set({ showForm: false, editingTwitchId: undefined }),

  setWaifuFormValues: values => set({ waifuFormValues: values }),

  setHusbandFormValues: values => set({ husbandFormValues: values }),

  confirmDelete: id => set({ confirmDeleteId: id }),

  cancelDelete: () => set({ confirmDeleteId: undefined }),

  confirmUnmerge: id => set({ confirmUnmergeId: id }),

  cancelUnmerge: () => set({ confirmUnmergeId: undefined }),

  switchToHusbandAndEdit: twitchId => {
    const { husbands } = get();
    const husband = husbands.find(h => h.twitchId === twitchId);
    if (husband) {
      set({
        mode: "husband",
        showForm: true,
        formMode: "edit",
        husbandFormValues: husbandDtoToFormValues(husband),
        editingTwitchId: husband.twitchId,
        searchQuery: "",
      });
    }
  },

  loadWaifus: async options => {
    const showToast = options?.showToast ?? true;
    set({ isLoading: true, error: "" });
    try {
      const response = await api.waifuRollWaifusList();
      const result = response.data;
      if (result.success && result.data) {
        set({ waifus: result.data, isLoading: false });
        return createSuccessResult(result.message);
      }
      set({ isLoading: false, error: result.message ?? "Ошибка загрузки" });
      return showToast ? createErrorResult(result.message) : undefined;
    } catch {
      set({ isLoading: false, error: "Ошибка сети" });
      return showToast ? createErrorResult("Ошибка сети") : undefined;
    }
  },

  loadHusbands: async options => {
    const showToast = options?.showToast ?? true;
    set({ isLoading: true, error: "" });
    try {
      const response = await api.waifuRollHusbandsList();
      const result = response.data;
      if (result.success && result.data) {
        set({ husbands: result.data, isLoading: false });
        return createSuccessResult(result.message);
      }
      set({ isLoading: false, error: result.message ?? "Ошибка загрузки" });
      return showToast ? createErrorResult(result.message) : undefined;
    } catch {
      set({ isLoading: false, error: "Ошибка сети" });
      return showToast ? createErrorResult("Ошибка сети") : undefined;
    }
  },

  loadAudios: async options => {
    const showToast = options?.showToast ?? true;
    try {
      const response = await api.waifuRollAudiosList();
      const result = response.data;
      if (result.success && result.data) {
        set({ audios: result.data });
        return createSuccessResult(result.message);
      }
      return showToast ? createErrorResult(result.message) : undefined;
    } catch {
      return showToast ? createErrorResult("Ошибка сети") : undefined;
    }
  },

  submitWaifu: async () => {
    const { formMode, waifuFormValues } = get();
    set({ isSubmitting: true, error: "" });
    try {
      const response =
        formMode === "create"
          ? await api.waifuRollWaifusCreate(
              waifuFormValuesToCreateRequest(waifuFormValues)
            )
          : await api.waifuRollWaifusUpdate(
              waifuFormValues.shikiId,
              waifuFormValuesToUpdateRequest(waifuFormValues)
            );
      const result = response.data;
      set({ isSubmitting: false });
      if (result.success) {
        set({ showForm: false, editingTwitchId: undefined });
        await get().loadWaifus({ showToast: false });
        return createSuccessResult(result.message);
      }
      set({ error: result.message ?? "Ошибка сохранения" });
      return createErrorResult(result.message);
    } catch {
      set({ isSubmitting: false, error: "Ошибка сети" });
      return createErrorResult("Ошибка сети");
    }
  },

  submitHusband: async () => {
    const { husbandFormValues, editingTwitchId } = get();
    if (!editingTwitchId) {
      return createErrorResult("Не указан ID мужа для редактирования");
    }
    set({ isSubmitting: true, error: "" });
    try {
      const response = await api.waifuRollHusbandsUpdate(
        editingTwitchId,
        husbandFormValuesToUpdateRequest(husbandFormValues)
      );
      const result = response.data;
      set({ isSubmitting: false });
      if (result.success) {
        set({ showForm: false, editingTwitchId: undefined });
        await get().loadHusbands({ showToast: false });
        return createSuccessResult(result.message);
      }
      set({ error: result.message ?? "Ошибка сохранения" });
      return createErrorResult(result.message);
    } catch {
      set({ isSubmitting: false, error: "Ошибка сети" });
      return createErrorResult("Ошибка сети");
    }
  },

  deleteWaifu: async shikiId => {
    set({ isDeleting: true, confirmDeleteId: undefined });
    try {
      const response = await api.waifuRollWaifusDelete(shikiId);
      const result = response.data;
      set({ isDeleting: false });
      if (result.success) {
        await get().loadWaifus({ showToast: false });
        return createSuccessResult(result.message);
      }
      return createErrorResult(result.message);
    } catch {
      set({ isDeleting: false });
      return createErrorResult("Ошибка сети");
    }
  },

  deleteHusband: async twitchId => {
    set({ isDeleting: true, confirmDeleteId: undefined });
    try {
      const response = await api.waifuRollHusbandsDelete(twitchId);
      const result = response.data;
      set({ isDeleting: false });
      if (result.success) {
        await get().loadHusbands({ showToast: false });
        return createSuccessResult(result.message);
      }
      return createErrorResult(result.message);
    } catch {
      set({ isDeleting: false });
      return createErrorResult("Ошибка сети");
    }
  },

  unmergeHusband: async twitchId => {
    set({ confirmUnmergeId: undefined });
    try {
      const response = await api.waifuRollHusbandsUnmergeCreate(twitchId);
      const result = response.data;
      if (result.success) {
        await get().loadHusbands({ showToast: false });
        await get().loadWaifus({ showToast: false });
        return createSuccessResult(result.message);
      }
      return createErrorResult(result.message);
    } catch {
      return createErrorResult("Ошибка сети");
    }
  },

  deleteAudio: async id => {
    try {
      const response = await api.waifuRollAudiosDelete(id);
      const result = response.data;
      if (result.success) {
        await get().loadAudios({ showToast: false });
        return createSuccessResult(result.message);
      }
      return createErrorResult(result.message);
    } catch {
      return createErrorResult("Ошибка сети");
    }
  },

  uploadAudio: async (file, name) => {
    try {
      const response = await api.waifuRollAudiosCreate({ file }, { name });
      const result = response.data;
      if (result.success) {
        await get().loadAudios({ showToast: false });
        return createSuccessResult(result.message);
      }
      return createErrorResult(result.message);
    } catch {
      return createErrorResult("Ошибка загрузки аудио");
    }
  },
}));
