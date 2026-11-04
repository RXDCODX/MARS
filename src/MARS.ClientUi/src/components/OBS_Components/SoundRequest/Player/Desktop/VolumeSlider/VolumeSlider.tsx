import { memo } from "react";
import { useShallow } from "zustand/react/shallow";

import { usePlayerStore } from "../../stores/usePlayerStore";
import { ElasticSlider } from "../ElasticSlider";

/**
 * Обёртка над ElasticSlider для громкости
 * Подписывается напрямую на volume и actions из стора
 * Громкость можно менять всегда, поэтому disabled не нужен
 */
function VolumeSliderComponent() {
  const { volume, actions } = usePlayerStore(
    useShallow(state => ({
      volume: state.volume,
      actions: state.actions,
    }))
  );

  const handleChange = (value: number) => {
    if (actions?.handleVolumeChange) {
      actions.handleVolumeChange(value);
    }
  };

  return (
    <ElasticSlider
      value={volume}
      onChange={handleChange}
      min={0}
      max={100}
      step={1}
      disabled={!actions}
    />
  );
}

// Экспортируем мемоизированную версию
export const VolumeSlider = memo(VolumeSliderComponent);
