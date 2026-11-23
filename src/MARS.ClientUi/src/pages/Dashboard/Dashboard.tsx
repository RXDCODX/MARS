import { useEffect, useState } from "react";

import ActiveUsers from "./ActiveUsers/ActiveUsers";
import styles from "./Dashboard.module.scss";
import { DashboardProps as DashboardProperties } from "./Dashboard.types";
import MetricCard from "./MetricCard/MetricCard";
import { MetricCardProps as MetricCardProperties } from "./MetricCard/MetricCard.types";
import PerformanceChart from "./PerformanceChart/PerformanceChart";
import RecentLogs from "./RecentLogs/RecentLogs";
import SystemStatus from "./SystemStatus/SystemStatus";

const Dashboard: React.FC<DashboardProperties> = () => {
  const [metrics, setMetrics] = useState({
    cpuUsage: 0,
    memoryUsage: 0,
    activeConnections: 0,
    requestsPerSecond: 0,
    errorRate: 0,
    uptime: 0,
  });

  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Имитация загрузки данных
    const loadMetrics = async () => {
      setIsLoading(true);

      // Имитация API вызова
      await new Promise(resolve => setTimeout(resolve, 1000));

      setMetrics({
        cpuUsage: Math.random() * 100,
        memoryUsage: Math.random() * 100,
        activeConnections: Math.floor(Math.random() * 1000),
        requestsPerSecond: Math.floor(Math.random() * 500),
        errorRate: Math.random() * 5,
        uptime: Math.floor(Math.random() * 86_400), // секунды
      });

      setIsLoading(false);
    };

    loadMetrics();

    // Обновление метрик каждые 30 секунд
    const interval = setInterval(loadMetrics, 30_000);

    return () => clearInterval(interval);
  }, []);

  const formatUptime = (seconds: number) => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${hours}ч ${minutes}м`;
  };

  const metricCards: MetricCardProperties[] = [
    {
      title: "Использование CPU",
      value: `${metrics.cpuUsage.toFixed(1)}%`,
      icon: "🖥️",
      color: "blue",
      trend: "+2.5%",
      trendDirection: "up",
    },
    {
      title: "Использование памяти",
      value: `${metrics.memoryUsage.toFixed(1)}%`,
      icon: "💾",
      color: "green",
      trend: "-1.2%",
      trendDirection: "down",
    },
    {
      title: "Активные соединения",
      value: metrics.activeConnections.toString(),
      icon: "🔗",
      color: "purple",
      trend: "+15",
      trendDirection: "up",
    },
    {
      title: "Запросов в секунду",
      value: metrics.requestsPerSecond.toString(),
      icon: "📡",
      color: "orange",
      trend: "+8.3%",
      trendDirection: "up",
    },
    {
      title: "Ошибки (%)",
      value: `${metrics.errorRate.toFixed(2)}%`,
      icon: "⚠️",
      color: "red",
      trend: "-0.5%",
      trendDirection: "down",
    },
    {
      title: "Время работы",
      value: formatUptime(metrics.uptime),
      icon: "⏱️",
      color: "teal",
      trend: "Стабильно",
      trendDirection: "stable",
    },
  ];

  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.loadingSpinner}></div>
        <p>Загрузка метрик...</p>
      </div>
    );
  }

  return (
    <div className={styles.dashboard}>
      <div className={styles.header}>
        <h1>Панель управления MARS</h1>
        <p>Мониторинг состояния ASP.NET приложения</p>
      </div>

      <div className={styles.metricsGrid}>
        {metricCards.map((card, index) => (
          <MetricCard key={index} {...card} />
        ))}
      </div>

      <div className={styles.mainContent}>
        <div className={styles.leftColumn}>
          <SystemStatus />
          <PerformanceChart />
        </div>

        <div className={styles.rightColumn}>
          <ActiveUsers />
          <RecentLogs />
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
