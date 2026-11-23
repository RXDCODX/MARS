import { Button } from "antd";
import { Volume2, VolumeX } from "lucide-react";
import { memo, useCallback } from "react";
import { useShallow } from "zustand/react/shallow";

import { usePlayerStore } from "../../../stores/usePlayerStore";
import styles from "../../SoundRequestPlayerDesktop.module.scss";

function MuteButtonComponent() {
  const { isMuted, loading, actions } = usePlayerStore(
    useShallow(state => ({
      isMuted: state.playerState?.isMuted ?? false,
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleClick = useCallback(() => {
    if (actions?.handleMute) {
      actions.handleMute();
    }
  }, [actions]);

  return (
    <Button
      type={isMuted ? "default" : "primary"}
      className={styles.tbBtn}
      onClick={handleClick}
      disabled={loading || !actions}
      title={isMuted ? "Звук выкл." : "Звук вкл."}
      data-testid="button-mute"
    >
      {isMuted ? <VolumeX /> : <Volume2 />}
    </Button>
  );
}

export const MuteButton = memo(MuteButtonComponent);
