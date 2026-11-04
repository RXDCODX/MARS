import { useCallback, useEffect } from "react";

import { PlayerStateVideoStateEnum } from "@/shared/api";

import { CommandCarousel, InfoBar, UnifiedPlayerView } from "./components";
import { useVideoScreenStore } from "./store/useVideoScreenStore";
import styles from "./VideoScreen.module.scss";

interface Properties {
  groupName?: string;
  className?: string;
}

export function VideoScreen({
  className,
  groupName = "mainplayer",
}: Properties) {
  const isMainPlayer = groupName === "mainplayer";

  const currentQueueItem = useVideoScreenStore(
    state => state.playerState?.currentQueueItem ?? null
  );
  const videoState = useVideoScreenStore(
    state => state.playerState?.videoState
  );
  const isVideoMode = videoState === PlayerStateVideoStateEnum.Video;
  const isAudioOnly = videoState === PlayerStateVideoStateEnum.AudioOnly;
  const hasUserInteracted = useVideoScreenStore(
    state => state.hasUserInteracted
  );

  useEffect(() => {
    void useVideoScreenStore.getState().init(import.meta.env.PROD);

    return () => {
      void useVideoScreenStore.getState().dispose();
    };
  }, []);

  useEffect(() => {
    useVideoScreenStore.getState().setIsMainPlayerContext(isMainPlayer);
  }, [isMainPlayer]);

  const handleUserInteraction = useCallback(() => {
    useVideoScreenStore.getState().markUserInteraction();
  }, []);

  useEffect(() => {
    if (import.meta.env.PROD) {
      return;
    }

    if (hasUserInteracted) {
      return;
    }

    document.addEventListener("click", handleUserInteraction, { once: true });
    document.addEventListener("touchstart", handleUserInteraction, {
      once: true,
      passive: true,
    });

    return () => {
      document.removeEventListener("click", handleUserInteraction);
      document.removeEventListener("touchstart", handleUserInteraction);
    };
  }, [handleUserInteraction, hasUserInteracted]);

  if (!currentQueueItem) {
    if (isAudioOnly) {
      return (
        <div
          className={styles.container}
          style={{ padding: 0 }}
          data-testid="obs-video-screen-empty"
        />
      );
    }

    if (isVideoMode) {
      return (
        <div
          className={styles.container}
          style={{ padding: 0 }}
          data-testid="obs-video-screen-commands"
        >
          <div className={styles.fullScreenCarousel}>
            <CommandCarousel />
          </div>
        </div>
      );
    }

    return (
      <div
        className={styles.container}
        style={{ padding: 0 }}
        data-testid="obs-video-screen-empty"
      >
        <div className={styles.emptyState}>
          <InfoBar />
        </div>
      </div>
    );
  }

  const videoSectionClassName = [
    styles.videoSection,
    styles.fullScreen,
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div
      className={styles.container}
      style={{ padding: 0 }}
      data-testid="obs-video-screen"
    >
      <div className={videoSectionClassName}>
        <UnifiedPlayerView />
      </div>
    </div>
  );
}
