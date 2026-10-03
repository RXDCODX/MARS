import { Button, Flex } from "antd";
import { Pause, Play, SkipForward, Square } from "lucide-react";
import { memo, useCallback } from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateStateEnum } from "@/shared/api";

import { usePlayerStore } from "../../stores/usePlayerStore";

function PlayerControlsComponent() {
  const { isPlaying, isStopped, loading, actions } = usePlayerStore(
    useShallow(state => ({
      isPlaying: state.playerState?.state === PlayerStateStateEnum.Playing,
      isStopped: state.playerState?.state === PlayerStateStateEnum.Stopped,
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleTogglePlayPause = useCallback(() => {
    if (actions?.handleTogglePlayPause) {
      actions.handleTogglePlayPause();
    }
  }, [actions]);

  const handleStop = useCallback(() => {
    if (actions?.handleStop) {
      actions.handleStop();
    }
  }, [actions]);

  const handleSkip = useCallback(() => {
    if (actions?.handleSkip) {
      actions.handleSkip();
    }
  }, [actions]);

  return (
    <Flex
      gap={8}
      style={{ width: "100%" }}
      data-testid="player-controls-mobile"
    >
      <Button
        onClick={handleTogglePlayPause}
        disabled={loading || !actions}
        type={isPlaying ? "default" : "primary"}
        size="large"
        style={{ flex: 1, height: 50 }}
        title={isPlaying ? "Пауза" : "Воспроизвести"}
      >
        {isPlaying ? <Pause size={20} /> : <Play size={20} />}
      </Button>
      <Button
        onClick={handleStop}
        disabled={loading || !actions}
        type="default"
        size="large"
        danger={isStopped}
        style={{ flex: 1, height: 50 }}
        title="Стоп"
      >
        <Square size={20} />
      </Button>
      <Button
        onClick={handleSkip}
        disabled={loading || !actions}
        type="primary"
        size="large"
        style={{ flex: 1, height: 50 }}
        title="Пропустить трек"
      >
        <SkipForward size={20} />
      </Button>
    </Flex>
  );
}

export const PlayerControls = memo(PlayerControlsComponent);
