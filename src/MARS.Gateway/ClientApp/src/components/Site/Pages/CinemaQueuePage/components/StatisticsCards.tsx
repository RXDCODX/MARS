import { Card, Flex } from "antd";

import { CinemaQueueStatistics } from "@/shared/api";
import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "../CinemaQueuePage.module.scss";

interface StatisticsCardsProperties {
  statistics: CinemaQueueStatistics;
}

const StatisticsCards: React.FC<StatisticsCardsProperties> = ({
  statistics,
}) => {
  const colors = useSiteColors();

  const cardStyles = {
    backgroundColor: colors.background.card,
    borderColor: colors.border.primary,
    boxShadow: colors.shadow.light,
  };

  const statCards = [
    {
      id: "stat-total",
      value: statistics.totalItems,
      color: "var(--site-text-primary)",
      label: "Total Items",
    },
    {
      id: "stat-pending",
      value: statistics.pendingItems,
      color: "var(--site-text-info)",
      label: "Pending",
    },
    {
      id: "stat-in-progress",
      value: statistics.inProgressItems,
      color: "var(--site-text-warning)",
      label: "In Progress",
    },
    {
      id: "stat-completed",
      value: statistics.completedItems,
      color: "var(--site-text-success)",
      label: "Completed",
    },
    {
      id: "stat-cancelled",
      value: statistics.cancelledItems,
      color: "var(--site-text-danger)",
      label: "Cancelled",
    },
    {
      id: "stat-postponed",
      value: statistics.postponedItems,
      color: "var(--site-text-accent)",
      label: "Postponed",
    },
  ];

  return (
    <Flex className={styles.statisticsRow} gap={16} wrap="wrap">
      {statCards.map(stat => (
        <div
          key={stat.id}
          style={{ flex: "0 0 calc(16.666% - 14px)", minWidth: 120 }}
        >
          <Card style={cardStyles} id={stat.id}>
            <div style={{ textAlign: "center" }}>
              <h3 style={{ marginBottom: 4, color: stat.color }}>
                {stat.value}
              </h3>
              <p style={{ margin: 0, color: "var(--site-text-secondary)" }}>
                {stat.label}
              </p>
            </div>
          </Card>
        </div>
      ))}
    </Flex>
  );
};

export default StatisticsCards;
