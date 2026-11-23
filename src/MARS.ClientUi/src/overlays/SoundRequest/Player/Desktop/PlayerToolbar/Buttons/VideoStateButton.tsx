import { Button } from "antd";
import { Music, Video, VideoOff } from "lucide-react";
import { memo, useCallback, useMemo } from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateVideoStateEnum } from "@/shared/api";

import { usePlayerStore } from "../../../stores/usePlayerStore";
import styles from "../../SoundRequestPlayerDesktop.module.scss";

function VideoStateButtonComponent() {
  const { videoState, loading, actions } = usePlayerStore(
    useShallow(state => ({
      videoState:
        state.playerState?.videoState ?? PlayerStateVideoStateEnum.Video,
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleClick = useCallback(() => {
    if (actions?.handleToggleVideoState) {
      actions.handleToggleVideoState();
    }
  }, [actions]);

  const videoIcon = useMemo(() => {
    switch (videoState) {
      case PlayerStateVideoStateEnum.Video: {
        return <Video />;
      }
      case PlayerStateVideoStateEnum.NoVideo: {
        return <VideoOff />;
      }
      case PlayerStateVideoStateEnum.AudioOnly: {
        return <Music />;
      }
      default: {
        return <Video />;
      }
    }
  }, [videoState]);

  const videoTitle = useMemo(() => {
    switch (videoState) {
      case PlayerStateVideoStateEnum.Video: {
        return "Режим: Видео (переключить на Без видео)";
      }
      case PlayerStateVideoStateEnum.NoVideo: {
        return "Режим: Без видео (переключить на Только аудио)";
      }
      case PlayerStateVideoStateEnum.AudioOnly: {
        return "Режим: Только аудио (переключить на Видео)";
      }
      default: {
        return "Переключить режим отображения";
      }
    }
  }, [videoState]);

  return (
    <Button
      type="default"
      className={`${styles.tbBtn} ${
        videoState === PlayerStateVideoStateEnum.Video
          ? styles.videoMode
          : videoState === PlayerStateVideoStateEnum.NoVideo
            ? styles.noVideoMode
            : styles.audioOnlyMode
      }`}
      onClick={handleClick}
      disabled={loading || !actions}
      title={videoTitle}
      data-testid="button-video-state"
    >
      {videoIcon}
    </Button>
  );
}

export const VideoStateButton = memo(VideoStateButtonComponent);
