import type {
  CreateWaifuRequest,
  HusbandDto,
  UpdateHusbandRequest,
  UpdateWaifuRequest,
  WaifuDto,
} from "@/shared/api/types/data-contracts";

export type WaifuRollMode = "waifu" | "husband";

export interface LoadListOptions {
  showToast?: boolean;
}

export interface WaifuFormValues {
  shikiId: string;
  name: string;
  age: number;
  anime: string;
  manga: string;
  imageUrl: string;
  audioId: string;
}

export interface HusbandFormValues {
  waifuBrideId: string;
  isPrivated: boolean;
  waifuRollId: string;
  whenPrivated: string;
  lastWeddingCongratulatedMonths: number;
}

export function createDefaultWaifuFormValues(): WaifuFormValues {
  return {
    shikiId: "",
    name: "",
    age: 0,
    anime: "",
    manga: "",
    imageUrl: "",
    audioId: "",
  };
}

export function createDefaultHusbandFormValues(): HusbandFormValues {
  return {
    waifuBrideId: "",
    isPrivated: false,
    waifuRollId: "",
    whenPrivated: "",
    lastWeddingCongratulatedMonths: 0,
  };
}

export function waifuDtoToFormValues(waifu: WaifuDto): WaifuFormValues {
  return {
    shikiId: waifu.shikiId,
    name: waifu.name,
    age: waifu.age,
    anime: waifu.anime ?? "",
    manga: waifu.manga ?? "",
    imageUrl: waifu.imageUrl,
    audioId: waifu.audioId ?? "",
  };
}

export function husbandDtoToFormValues(husband: HusbandDto): HusbandFormValues {
  return {
    waifuBrideId: husband.waifuBrideId ?? "",
    isPrivated: husband.isPrivated,
    waifuRollId: husband.waifuRollId ?? "",
    whenPrivated: husband.whenPrivated
      ? new Date(husband.whenPrivated).toISOString().slice(0, 16)
      : "",
    lastWeddingCongratulatedMonths: husband.lastWeddingCongratulatedMonths ?? 0,
  };
}

export function waifuFormValuesToCreateRequest(
  values: WaifuFormValues
): CreateWaifuRequest {
  return {
    shikiId: values.shikiId,
    name: values.name,
    age: values.age,
    anime: values.anime || undefined,
    manga: values.manga || undefined,
    imageUrl: values.imageUrl,
    audioId: values.audioId || undefined,
  };
}

export function waifuFormValuesToUpdateRequest(
  values: WaifuFormValues
): UpdateWaifuRequest {
  return {
    name: values.name,
    age: values.age,
    anime: values.anime || undefined,
    manga: values.manga || undefined,
    imageUrl: values.imageUrl,
    audioId: values.audioId || undefined,
  };
}

export function husbandFormValuesToUpdateRequest(
  values: HusbandFormValues
): UpdateHusbandRequest {
  return {
    waifuBrideId: values.waifuBrideId || undefined,
    isPrivated: values.isPrivated,
    waifuRollId: values.waifuRollId || undefined,
    whenPrivated: values.whenPrivated
      ? new Date(values.whenPrivated).toISOString()
      : undefined,
    lastWeddingCongratulatedMonths:
      values.lastWeddingCongratulatedMonths || undefined,
  };
}
