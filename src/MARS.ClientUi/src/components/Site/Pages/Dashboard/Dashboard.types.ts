export interface DashboardProps {
  // Пока пустой интерфейс, можно расширить в будущем
}

export interface Metrics {
  cpuUsage: number;
  memoryUsage: number;
  activeConnections: number;
  requestsPerSecond: number;
  errorRate: number;
  uptime: number;
}
