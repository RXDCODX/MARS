import { Button } from "antd";
import { SkipForward } from "lucide-react";
import { memo, useCallback } from "react";
import { useShallow } from "zustand/react/shallow";

import { usePlayerStore } from "../../../stores/usePlayerStore";
import styles from "../../SoundRequestPlayerDesktop.module.scss";

function SkipButtonComponent() {
  const { loading, actions } = usePlayerStore(
    useShallow(state => ({
      loading: state.loading,
      actions: state.actions,
    }))
  );

  const handleClick = useCallback(() => {
    if (actions?.handleSkip) {
      actions.handleSkip();
    }
  }, [actions]);

  return (
    <Button
      type="default"
      className={styles.tbBtn}
      onClick={handleClick}
      disabled={loading || !actions}
      title="Следующий"
      data-testid="button-skip"
    >
      <SkipForward />
    </Button>
  );
}

export const SkipButton = memo(SkipButtonComponent);
