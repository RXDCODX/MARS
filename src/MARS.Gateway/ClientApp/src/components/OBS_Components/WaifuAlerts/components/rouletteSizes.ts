export type RouletteSize =
  | "xs"
  | "s"
  | "m"
  | "l"
  | "xl"
  | "xxl"
  | "xxxl"
  | "xxxxl";

export type RouletteSizeWithFill = RouletteSize | "fill";

export interface RouletteSizePreset {
  width: number;
  height: number;
}

export const ROULETTE_SIZE_PRESETS: Record<RouletteSize, RouletteSizePreset> = {
  xs: { width: 100, height: 115 },
  s: { width: 140, height: 160 },
  m: { width: 175, height: 200 },
  l: { width: 205, height: 234 },
  xl: { width: 260, height: 290 },
  xxl: { width: 320, height: 350 },
  xxxl: { width: 350, height: 410 },
  xxxxl: { width: 380, height: 440 },
};
