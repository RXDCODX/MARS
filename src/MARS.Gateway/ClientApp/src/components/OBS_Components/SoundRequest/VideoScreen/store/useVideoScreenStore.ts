import { HubConnection } from "@microsoft/signalr";
import { create } from "zustand";

import type { PlayerState, QueueItem } from "@/shared/api";
import {
  PlayerStateStateEnum,
  SoundRequestHubSignalRConnectionBuilder,
} from "@/shared/api";

import { parseDurationToSeconds } from "../utils/parseDuration";

interface VideoScreenStoreState {
  playerState: PlayerState | null;
  hasUserInteracted: boolean;
  currentProgressSeconds: number;
  connection: HubConnection | null;
  isInitialized: boolean;
  localVolume: number;
  isVolumeManuallyChanged: boolean;
  isMainPlayerContext: boolean;
  getCurrentTimeRef: (() => number) | null;

  init: (defaultHasUserInteracted: boolean) => Promise<void>;
  dispose: () => Promise<void>;
  markUserInteraction: () => void;
  reportProgress: (playedSeconds: number) => Promise<void>;
  notifyEnded: (track: QueueItem["track"]) => Promise<void>;
  notifyStarted: (track: QueueItem["track"]) => Promise<void>;
  notifyError: (track: QueueItem["track"]) => Promise<void>;
  setLocalVolume: (volume: number) => void;
  syncPlaybackState: (
    nextState: PlayerStateStateEnum.Playing | PlayerStateStateEnum.Paused
  ) => Promise<void>;
  setIsMainPlayerContext: (isMainPlayer: boolean) => void;
  registerTimeGetter: (getter: (() => number) | null) => void;
}

function extractQueueItemId(state: PlayerState | null): string | undefined {
  return state?.currentQueueItem?.id;
}

async function invokeConnection(
  connection: HubConnection | null,
  methodName: string,
  errorMessage: string,
  ...arguments_: unknown[]
): Promise<boolean> {
  if (!connection) {
    return false;
  }

  try {
    await connection.invoke(methodName, ...arguments_);
    return true;
  } catch (error) {
    console.error(errorMessage, error);
    return false;
  }
}

export const useVideoScreenStore = create<VideoScreenStoreState>(
  (set, get) => ({
    playerState: null,
    hasUserInteracted: false,
    currentProgressSeconds: 0,
    connection: null,
    isInitialized: false,
    localVolume: 100,
    isVolumeManuallyChanged: false,
    isMainPlayerContext: true,
    getCurrentTimeRef: null,

    init: async defaultHasUserInteracted => {
      const currentState = get();

      if (currentState.isInitialized) {
        if (defaultHasUserInteracted && !currentState.hasUserInteracted) {
          set({ hasUserInteracted: true });
        }
        return;
      }

      set({
        hasUserInteracted: defaultHasUserInteracted,
        isInitialized: true,
      });

      if (currentState.connection) {
        await currentState.connection.stop().catch(() => {});
      }

      const connection = SoundRequestHubSignalRConnectionBuilder.build();

      connection.on("PlayerStateChange", (playerState: PlayerState) => {
        const state = get();
        const previousPlayerState = state.playerState;
        const newProgressSeconds = parseDurationToSeconds(
          playerState.currentTrackProgress
        );
        const currentQueueItemId = extractQueueItemId(playerState);
        const previousQueueItemId = extractQueueItemId(previousPlayerState);
        const isNewTrack =
          currentQueueItemId && currentQueueItemId !== previousQueueItemId;
        const shouldResetProgress = isNewTrack || !previousPlayerState;

        set({
          playerState,
          currentProgressSeconds:
            (shouldResetProgress && newProgressSeconds >= 0) ||
            newProgressSeconds > 0
              ? newProgressSeconds
              : state.currentProgressSeconds,
          localVolume: state.isVolumeManuallyChanged
            ? state.localVolume
            : playerState.volume,
        });
      });

      try {
        await connection.start();
        set({ connection });
      } catch (error) {
        console.error(
          "[VideoScreenStore] Не удалось подключиться к SignalR",
          error
        );
      }
    },

    dispose: async () => {
      const { connection } = get();
      if (connection) {
        await connection.stop().catch(() => {});
      }

      set({
        playerState: null,
        hasUserInteracted: false,
        currentProgressSeconds: 0,
        connection: null,
        isInitialized: false,
        localVolume: 100,
        isVolumeManuallyChanged: false,
      });
    },

    markUserInteraction: () => {
      set({ hasUserInteracted: true });
    },

    reportProgress: async playedSeconds => {
      const { connection } = get();

      if (playedSeconds < 0) {
        return;
      }

      set({ currentProgressSeconds: playedSeconds });

      const progressSeconds = Math.max(0, Math.floor(playedSeconds));
      await invokeConnection(
        connection,
        "TrackProgress",
        "[VideoScreenStore] Не удалось отправить прогресс трека",
        progressSeconds
      );
    },

    notifyEnded: async track => {
      const { connection } = get();

      if (!track || !connection) {
        return;
      }

      await invokeConnection(
        connection,
        "Ended",
        "[VideoScreenStore] Не удалось отправить событие завершения трека",
        track
      );
    },

    notifyStarted: async track => {
      const { connection } = get();

      if (!track || !connection) {
        return;
      }

      await invokeConnection(
        connection,
        "Started",
        "[VideoScreenStore] Не удалось отправить событие старта трека",
        track
      );
    },

    notifyError: async track => {
      const { connection } = get();

      if (!track || !connection) {
        return;
      }

      await invokeConnection(
        connection,
        "ErrorPlaying",
        "[VideoScreenStore] Не удалось отправить событие ошибки воспроизведения",
        track
      );
    },

    setLocalVolume: volume => {
      if (volume < 0 || volume > 100) {
        return;
      }

      set({
        localVolume: volume,
        isVolumeManuallyChanged: true,
      });
    },

    syncPlaybackState: async nextState => {
      const state = get();

      if (!state.playerState) {
        return;
      }

      if (state.playerState.state === nextState) {
        return;
      }

      const previousState = state.playerState;

      set({
        playerState: {
          ...previousState,
          state: nextState,
        },
      });

      const newState: PlayerState = {
        ...previousState,
        state: nextState,
      };

      const isSynced = await invokeConnection(
        state.connection,
        "FrontStateChange",
        "[VideoScreenStore] Не удалось синхронизировать play/pause состояние",
        newState
      );

      if (!isSynced && state.connection) {
        set({ playerState: previousState });
      }
    },

    setIsMainPlayerContext: isMainPlayer => {
      set({ isMainPlayerContext: isMainPlayer });
    },
    registerTimeGetter: getter => {
      set({ getCurrentTimeRef: getter });
    },
  })
);
