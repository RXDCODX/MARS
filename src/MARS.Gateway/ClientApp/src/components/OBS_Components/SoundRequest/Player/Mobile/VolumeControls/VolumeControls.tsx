import { Button, Flex, Typography } from "antd";
import { Music, Video, VideoOff, Volume2, VolumeX } from "lucide-react";
import { memo, useCallback, useMemo } from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateVideoStateEnum } from "@/shared/api";

import { usePlayerStore } from "../../stores/usePlayerStore";
import styles from "./VolumeControls.module.scss";

function VolumeControlsComponent() {
  const { volume, isMuted, videoState, loading, actions } = usePlayerStore(
    useShallow(state => ({
      volume: state.volume,
      isMuted: state.playerState?.isMuted ?? false,
      videoState:
        state.playerState?.videoState ?? PlayerStateVideoStateEnum.Video,
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleMute = useCallback(() => {
    if (actions?.handleMute) {
      actions.handleMute();
    }
  }, [actions]);

  const handleToggleVideoState = useCallback(() => {
    if (actions?.handleToggleVideoState) {
      actions.handleToggleVideoState();
    }
  }, [actions]);

  const handleVolumeChange = useCallback(
    (value: number) => {
      if (actions?.handleVolumeChange) {
        actions.handleVolumeChange(value);
      }
    },
    [actions]
  );

  const videoIcon = useMemo(() => {
    switch (videoState) {
      case PlayerStateVideoStateEnum.Video: {
        return <Video size={18} />;
      }
      case PlayerStateVideoStateEnum.NoVideo: {
        return <VideoOff size={18} />;
      }
      case PlayerStateVideoStateEnum.AudioOnly: {
        return <Music size={18} />;
      }
      default: {
        return <Video size={18} />;
      }
    }
  }, [videoState]);

  const videoButtonType = useMemo(() => {
    switch (videoState) {
      case PlayerStateVideoStateEnum.Video: {
        return "primary";
      }
      case PlayerStateVideoStateEnum.NoVideo: {
        return "default";
      }
      case PlayerStateVideoStateEnum.AudioOnly: {
        return "primary";
      }
      default: {
        return "primary";
      }
    }
  }, [videoState]);

  return (
    <Flex
      align="center"
      gap={12}
      style={{
        padding: 12,
        borderRadius: 8,
        backgroundColor: "var(--site-bg-secondary)",
      }}
      data-testid="volume-controls-mobile"
    >
      <Button
        onClick={handleMute}
        disabled={loading || !actions}
        type={isMuted ? "default" : "primary"}
        aria-label={isMuted ? "Звук выкл." : "Звук вкл."}
        title={isMuted ? "Звук выкл." : "Звук вкл."}
        style={{ width: 45, height: 45, flexShrink: 0 }}
      >
        {isMuted ? <VolumeX size={18} /> : <Volume2 size={18} />}
      </Button>
      <Button
        onClick={handleToggleVideoState}
        disabled={loading || !actions}
        type={videoButtonType}
        aria-label="Переключить режим отображения"
        title="Переключить режим отображения"
        style={{ width: 45, height: 45, flexShrink: 0 }}
      >
        {videoIcon}
      </Button>
      <div style={{ flex: 1, padding: "0 8px" }}>
        <input
          type="range"
          aria-label="Громкость"
          min={0}
          max={100}
          value={volume}
          onChange={e => handleVolumeChange(Number(e.target.value))}
          disabled={loading || !actions}
          className={styles.slider}
          style={{
            background: `linear-gradient(to right, #52C41A 0%, #52C41A ${volume}%, #d9d9d9 ${volume}%, #d9d9d9 100%)`,
          }}
        />
      </div>
      <Typography.Text
        style={{
          minWidth: 45,
          textAlign: "right",
          fontWeight: 600,
          color: "var(--site-text-accent)",
          fontSize: 14,
        }}
      >
        {volume}%
      </Typography.Text>
    </Flex>
  );
}

export const VolumeControls = memo(VolumeControlsComponent);
