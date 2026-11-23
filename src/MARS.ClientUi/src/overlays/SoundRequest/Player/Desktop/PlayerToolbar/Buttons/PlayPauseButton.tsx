import { Button } from "antd";
import { Pause, Play } from "lucide-react";
import { memo, useCallback } from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateStateEnum } from "@/shared/api";

import { usePlayerStore } from "../../../stores/usePlayerStore";
import styles from "../../SoundRequestPlayerDesktop.module.scss";

function PlayPauseButtonComponent() {
  const { isPlaying, loading, actions } = usePlayerStore(
    useShallow(state => ({
      isPlaying: state.playerState?.state === PlayerStateStateEnum.Playing,
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleClick = useCallback(() => {
    if (actions?.handleTogglePlayPause) {
      actions.handleTogglePlayPause();
    }
  }, [actions]);

  return (
    <Button
      type={isPlaying ? "default" : "primary"}
      danger={isPlaying}
      className={styles.tbBtn}
      onClick={handleClick}
      disabled={loading || !actions}
      title={isPlaying ? "Пауза" : "Воспроизвести"}
      data-testid="button-play-pause"
    >
      {isPlaying ? <Pause /> : <Play />}
    </Button>
  );
}

export const PlayPauseButton = memo(PlayPauseButtonComponent);
