import { Button, Typography } from "antd";
import { Plus, RefreshCw } from "lucide-react";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const WaifuRollHeader: React.FC = () => {
  const mode = useWaifuRollStore(s => s.mode);
  const startCreate = useWaifuRollStore(s => s.startCreate);
  const loadWaifus = useWaifuRollStore(s => s.loadWaifus);
  const loadHusbands = useWaifuRollStore(s => s.loadHusbands);

  const handleRefresh = () => {
    if (mode === "waifu") {
      void loadWaifus();
    } else {
      void loadHusbands();
    }
  };

  return (
    <div className={styles.header} data-testid="waifu-roll-header">
      <div className={styles.headerLeft}>
        <Typography.Title
          level={3}
          className={styles.title}
          data-testid="page-title"
        >
          WaifuRoll Manager
        </Typography.Title>
      </div>
      <div style={{ display: "flex", gap: "0.5rem" }}>
        <Button
          icon={<RefreshCw size={16} />}
          onClick={handleRefresh}
          data-testid="button-refresh"
        >
          Обновить
        </Button>
        {mode === "waifu" && (
          <Button
            type="primary"
            icon={<Plus size={16} />}
            onClick={startCreate}
            data-testid="button-create"
          >
            Создать вайфу
          </Button>
        )}
      </div>
    </div>
  );
};

export default WaifuRollHeader;
