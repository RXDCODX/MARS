import { Button } from "antd";
import { Plus } from "lucide-react";
import { memo } from "react";
import { useShallow } from "zustand/react/shallow";

import { usePlayerStore } from "../../../stores/usePlayerStore";
import styles from "../../SoundRequestPlayerDesktop.module.scss";
import { useAddTrackModalStore } from "../stores/useAddTrackModalStore";

function AddTrackButtonComponent() {
  const open = useAddTrackModalStore(state => state.open);
  const loading = usePlayerStore(useShallow(state => state.loading));

  return (
    <Button
      type="primary"
      className={styles.tbBtn}
      onClick={open}
      disabled={loading}
      title="Добавить трек"
      style={{ marginLeft: "2rem" }}
      data-testid="button-add-track"
    >
      <Plus />
    </Button>
  );
}

export const AddTrackButton = memo(AddTrackButtonComponent);
