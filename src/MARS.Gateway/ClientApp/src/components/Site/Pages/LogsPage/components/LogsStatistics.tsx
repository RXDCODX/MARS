import { Spin } from "antd";
import { BarChart3 } from "lucide-react";

import styles from "../LogsPage.module.scss";
import { LogsStatisticsProps as LogsStatisticsProperties } from "../LogsPage.types";

const LogsStatistics: React.FC<LogsStatisticsProperties> = ({
  statistics,
  isLoading,
}) => {
  if (isLoading) {
    return (
      <div className={styles.statsCard}>
        <div className={styles.loadingSpinner}>
          <Spin tip="Загрузка статистики..." />
        </div>
      </div>
    );
  }

  if (!statistics) {
    return (
      <div className={styles.statsCard}>
        <div className={styles.emptyState}>
          <BarChart3 className={styles.icon} />
          <h4>Статистика недоступна</h4>
          <p>Не удалось загрузить статистику логов</p>
        </div>
      </div>
    );
  }

  const formatDate = (dateString?: string) => {
    if (!dateString) return "Не указано";
    return new Date(dateString).toLocaleString("ru-RU", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      timeZone: "UTC",
    });
  };

  return (
    <div className={styles.statsCard}>
      <div className={styles.statsGrid}>
        <div className={styles.statItem}>
          <div className={styles.statNumber}>
            {statistics.totalLogs.toLocaleString()}
          </div>
          <div className={styles.statLabel}>Всего логов</div>
        </div>

        <div className={styles.statItem}>
          <div className={styles.statNumber} style={{ color: "#ffc107" }}>
            {statistics.warningLogs.toLocaleString()}
          </div>
          <div className={styles.statLabel}>Предупреждения</div>
        </div>

        <div className={styles.statItem}>
          <div className={styles.statNumber} style={{ color: "#dc3545" }}>
            {statistics.errorLogs.toLocaleString()}
          </div>
          <div className={styles.statLabel}>Ошибки</div>
        </div>

        <div className={styles.statItem}>
          <div className={styles.statNumber} style={{ color: "#721c24" }}>
            {statistics.criticalLogs.toLocaleString()}
          </div>
          <div className={styles.statLabel}>Критические</div>
        </div>

        <div className={styles.statItem}>
          <div className={styles.statNumber} style={{ fontSize: "1.2rem" }}>
            {formatDate(statistics.oldestLogDate)}
          </div>
          <div className={styles.statLabel}>Самая старая запись</div>
        </div>

        <div className={styles.statItem}>
          <div className={styles.statNumber} style={{ fontSize: "1.2rem" }}>
            {formatDate(statistics.newestLogDate)}
          </div>
          <div className={styles.statLabel}>Самая новая запись</div>
        </div>
      </div>

      {statistics.oldestLogDate && statistics.newestLogDate && (
        <div style={{ marginTop: "1rem", textAlign: "center" }}>
          <small style={{ color: "#999" }}>
            Период: {formatDate(statistics.oldestLogDate)} -{" "}
            {formatDate(statistics.newestLogDate)}
          </small>
        </div>
      )}
    </div>
  );
};

export default LogsStatistics;
