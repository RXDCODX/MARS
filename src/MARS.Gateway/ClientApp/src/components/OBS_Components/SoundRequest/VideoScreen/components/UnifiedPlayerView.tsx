import { PlayerStateStateEnum, PlayerStateVideoStateEnum } from "@/shared/api";
import { useInjectStyles } from "@/shared/hooks/useInjectStyles";

import { CommandCarousel } from "./CommandCarousel";
import { InfoBar } from "./InfoBar";
import styles from "./UnifiedPlayerView.module.scss";
import { useReactCustomPlayer } from "./useReactCustomPlayer";
import { useUnifiedPlayerViewModel } from "./useUnifiedPlayerViewModel";

const TRANSPARENT_BACKGROUND_STYLES = `
  body {
    background-color: transparent !important;
    background: transparent !important;
  }

  #root {
    background-color: transparent !important;
    background: transparent !important;
  }

  html {
    background-color: transparent !important;
    background: transparent !important;
  }
`;

/**
 * Единый компонент плеера для всех режимов (Video, NoVideo, AudioOnly)
 * Использует один экземпляр ReactPlayer, меняя только UI обёртку
 * Это предотвращает ререндер плеера при переключении режимов
 */
function UnifiedPlayerViewComponent() {
  const {
    currentTrack,
    queueItemId,
    playerState,
    isMainPlayer,
    hasUserInteracted,
    localVolume,
    videoState,
  } = useUnifiedPlayerViewModel();

  const { playerContainerRef, playerElement } = useReactCustomPlayer({
    track: currentTrack,
    queueItemId,
    playerState,
    isMainPlayer,
    hasUserInteracted,
    localVolume,
    pauseSuppressionMs: 900,
    playerView: {
      width: "100%",
      height: "100%",
      controls: true,
    },
  });

  const isVideoMode = videoState === PlayerStateVideoStateEnum.Video;
  const isNoVideoMode = videoState === PlayerStateVideoStateEnum.NoVideo;
  const isNeedsTransparentBackground = !isVideoMode;
  const isNotPlaying =
    playerState?.state != undefined &&
    playerState.state !== PlayerStateStateEnum.Playing;

  // In video mode, show command carousel in the player area when paused/stopped
  const showCommandsInVideoArea = isVideoMode && isNotPlaying && currentTrack;

  useInjectStyles(
    isNeedsTransparentBackground ? TRANSPARENT_BACKGROUND_STYLES : "",
    "video-screen-transparent-bg"
  );

  const playerClassName = isVideoMode
    ? styles.videoPlayer
    : styles.hiddenPlayer;
  const containerClassName = isVideoMode
    ? styles.videoContainer
    : isNoVideoMode
      ? styles.noVideoContainer
      : styles.audioOnlyContainer;
  const showInfoBar = isMainPlayer && (isVideoMode || isNoVideoMode);

  return (
    <div className={containerClassName}>
      <div
        className={
          showCommandsInVideoArea ? styles.hiddenPlayer : playerClassName
        }
        ref={playerContainerRef}
      >
        {playerElement}
      </div>

      {showCommandsInVideoArea && (
        <div className={styles.videoPlayer}>
          <CommandCarousel />
        </div>
      )}

      {showInfoBar && (
        <div className={isNoVideoMode ? styles.infoBarBottom : undefined}>
          <InfoBar />
        </div>
      )}
    </div>
  );
}

export const UnifiedPlayerView = UnifiedPlayerViewComponent;
