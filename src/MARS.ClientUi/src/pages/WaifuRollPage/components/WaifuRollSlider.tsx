import { Segmented } from "antd";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const WaifuRollSlider: React.FC = () => {
  const mode = useWaifuRollStore(s => s.mode);
  const setMode = useWaifuRollStore(s => s.setMode);

  return (
    <div className={styles.slider} data-testid="waifu-roll-slider">
      <Segmented
        value={mode}
        onChange={value => setMode(value as "waifu" | "husband")}
        options={[
          { label: "Вайфу", value: "waifu" },
          { label: "Мужи", value: "husband" },
        ]}
        data-testid="segmented-mode"
      />
    </div>
  );
};

export default WaifuRollSlider;
