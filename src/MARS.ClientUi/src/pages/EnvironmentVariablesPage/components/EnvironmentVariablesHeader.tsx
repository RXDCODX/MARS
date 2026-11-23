import { Database, History, KeyRound } from "lucide-react";

import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "../EnvironmentVariablesPage.module.scss";
import {
  selectLastUpdated,
  selectTotalVariables,
  useEnvironmentVariablesStore,
} from "../store/useEnvironmentVariablesStore";
import { formatDateTime } from "../utils/formatDateTime";

const EnvironmentVariablesHeader: React.FC = () => {
  const colors = useSiteColors();
  const totalVariables = useEnvironmentVariablesStore(selectTotalVariables);
  const lastUpdatedRaw = useEnvironmentVariablesStore(selectLastUpdated);

  return (
    <div className={styles.header}>
      <div>
        <h1 style={colors.utils.getTextStyle("primary")}>
          <Database size={32} />
          Управление переменными окружения
        </h1>
        <p style={colors.utils.getTextStyle("secondary")}>
          Просматривайте, редактируйте и перезагружайте переменные окружения
          MARS
        </p>
      </div>
      <div className={styles.headerMeta}>
        <span className={styles.metaItem}>
          <KeyRound size={16} />
          {totalVariables} переменных
        </span>
        <span className={styles.metaItem}>
          <History size={16} />
          Обновлено: {formatDateTime(lastUpdatedRaw)}
        </span>
      </div>
    </div>
  );
};

export default EnvironmentVariablesHeader;
