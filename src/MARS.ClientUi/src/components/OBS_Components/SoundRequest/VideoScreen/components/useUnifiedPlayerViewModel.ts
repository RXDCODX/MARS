import { useShallow } from "zustand/react/shallow";

import type { PlayerState, QueueItem } from "@/shared/api";
import { PlayerStateVideoStateEnum } from "@/shared/api";

import { useVideoScreenStore } from "../store/useVideoScreenStore";

interface UseUnifiedPlayerViewModelResult {
  currentTrack: NonNullable<QueueItem["track"]>;
  queueItemId?: string;
  playerState: PlayerState;
  isMainPlayer: boolean;
  hasUserInteracted: boolean;
  localVolume: number;
  videoState: PlayerStateVideoStateEnum;
}

export function useUnifiedPlayerViewModel(): UseUnifiedPlayerViewModelResult {
  const { playerStateFromStore, hasUserInteracted, localVolume, isMainPlayer } =
    useVideoScreenStore(
      useShallow(state => ({
        playerStateFromStore: state.playerState,
        hasUserInteracted: state.hasUserInteracted,
        localVolume: state.localVolume,
        isMainPlayer: state.isMainPlayerContext,
      }))
    );

  const currentQueueItem = playerStateFromStore?.currentQueueItem;
  const queueItemId = currentQueueItem?.id;
  const currentTrack = currentQueueItem?.track as NonNullable<
    QueueItem["track"]
  >;

  const playerState =
    playerStateFromStore ??
    ({
      state: "Paused",
      volume: 100,
      isMuted: false,
      videoState: PlayerStateVideoStateEnum.Video,
    } as PlayerState);

  const videoState =
    playerStateFromStore?.videoState ?? PlayerStateVideoStateEnum.Video;

  return {
    currentTrack,
    queueItemId,
    playerState,
    isMainPlayer,
    hasUserInteracted,
    localVolume,
    videoState,
  };
}
