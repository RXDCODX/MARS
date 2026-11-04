import type PrizeType from "react-roulette-pro/dist/types/PrizeType";

import type { MikuMondayDto } from "@/shared/api";

export interface RoulettePrize extends PrizeType {
  isPlaceholder?: boolean;
}

export interface RouletteGroup {
  prizes: RoulettePrize[];
  prizeIndex: number;
  hasWinner: boolean;
  isReversed: boolean;
}

export type QueuedMikuMondayAlert = MikuMondayDto & { queueId: string };
