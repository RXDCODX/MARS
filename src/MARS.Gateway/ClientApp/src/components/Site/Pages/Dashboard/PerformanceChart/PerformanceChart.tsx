import React from "react";

import styles from "./PerformanceChart.module.scss";

const PerformanceChart: React.FC = () => {
  // Имитация данных для графика
  const generateData = () => {
    const data = [];
    const now = new Date();
    for (let index = 23; index >= 0; index--) {
      const time = new Date(now.getTime() - index * 60 * 60 * 1000);
      data.push({
        time: time.toLocaleTimeString("ru-RU", {
          hour: "2-digit",
          minute: "2-digit",
        }),
        cpu: Math.random() * 100,
        memory: Math.random() * 100,
        requests: Math.floor(Math.random() * 500),
      });
    }
    return data;
  };

  const chartData = generateData();

  const getBarHeight = (value: number, max: number) => (value / max) * 100;

  const maxRequests = Math.max(...chartData.map(d => d.requests));

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h3>Производительность за 24 часа</h3>
        <div className={styles.legend}>
          <div className={styles.legendItem}>
            <div className={`${styles.legendColor} ${styles.cpu}`}></div>
            <span>CPU</span>
          </div>
          <div className={styles.legendItem}>
            <div className={`${styles.legendColor} ${styles.memory}`}></div>
            <span>Память</span>
          </div>
          <div className={styles.legendItem}>
            <div className={`${styles.legendColor} ${styles.requests}`}></div>
            <span>Запросы</span>
          </div>
        </div>
      </div>

      <div className={styles.chart}>
        <div className={styles.chartBars}>
          {chartData.map((data, index) => (
            <div key={index} className={styles.barGroup}>
              <div className={styles.barContainer}>
                <div
                  className={`${styles.bar} ${styles.cpu}`}
                  style={{ height: `${getBarHeight(data.cpu, 100)}%` }}
                  title={`CPU: ${data.cpu.toFixed(1)}%`}
                ></div>
                <div
                  className={`${styles.bar} ${styles.memory}`}
                  style={{ height: `${getBarHeight(data.memory, 100)}%` }}
                  title={`Память: ${data.memory.toFixed(1)}%`}
                ></div>
                <div
                  className={`${styles.bar} ${styles.requests}`}
                  style={{
                    height: `${getBarHeight(data.requests, maxRequests)}%`,
                  }}
                  title={`Запросы: ${data.requests}`}
                ></div>
              </div>
              <div className={styles.timeLabel}>{data.time}</div>
            </div>
          ))}
        </div>
      </div>

      <div className={styles.stats}>
        <div className={styles.stat}>
          <div className={styles.statLabel}>Среднее CPU</div>
          <div className={styles.statValue}>
            {(
              chartData.reduce((sum, d) => sum + d.cpu, 0) / chartData.length
            ).toFixed(1)}
            %
          </div>
        </div>
        <div className={styles.stat}>
          <div className={styles.statLabel}>Средняя память</div>
          <div className={styles.statValue}>
            {(
              chartData.reduce((sum, d) => sum + d.memory, 0) / chartData.length
            ).toFixed(1)}
            %
          </div>
        </div>
        <div className={styles.stat}>
          <div className={styles.statLabel}>Всего запросов</div>
          <div className={styles.statValue}>
            {chartData.reduce((sum, d) => sum + d.requests, 0).toLocaleString()}
          </div>
        </div>
      </div>
    </div>
  );
};

export default PerformanceChart;
