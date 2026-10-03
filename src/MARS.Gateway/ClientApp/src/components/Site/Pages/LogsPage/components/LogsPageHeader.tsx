import { FileText } from "lucide-react";

import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "../LogsPage.module.scss";

const LogsPageHeader: React.FC = () => {
  const colors = useSiteColors();

  return (
    <div className={styles.pageHeader}>
      <h1 style={colors.utils.getTextStyle("primary")}>
        <FileText size={32} className="me-3" />
        Логи приложения
      </h1>
      <p style={colors.utils.getTextStyle("secondary")}>
        Просмотр и анализ логов системы MARS в реальном времени
      </p>
    </div>
  );
};

export default LogsPageHeader;
