import styles from "./MetricCard.module.scss";
import { MetricCardProps as MetricCardProperties } from "./MetricCard.types";

const MetricCard: React.FC<MetricCardProperties> = ({
  title,
  value,
  icon,
  color,
  trend,
  trendDirection,
}) => {
  const getTrendIcon = () => {
    switch (trendDirection) {
      case "up": {
        return "↗️";
      }
      case "down": {
        return "↘️";
      }
      case "stable": {
        return "→";
      }
      default: {
        return "→";
      }
    }
  };

  const getTrendColor = () => {
    switch (trendDirection) {
      case "up": {
        return styles.trendUp;
      }
      case "down": {
        return styles.trendDown;
      }
      case "stable": {
        return styles.trendStable;
      }
      default: {
        return styles.trendStable;
      }
    }
  };

  return (
    <div className={`${styles.card} ${styles[color]}`}>
      <div className={styles.header}>
        <div className={styles.icon}>{icon}</div>
        <div className={styles.trend}>
          <span className={`${styles.trendValue} ${getTrendColor()}`}>
            {getTrendIcon()} {trend}
          </span>
        </div>
      </div>

      <div className={styles.content}>
        <h3 className={styles.title}>{title}</h3>
        <div className={styles.value}>{value}</div>
      </div>
    </div>
  );
};

export default MetricCard;
