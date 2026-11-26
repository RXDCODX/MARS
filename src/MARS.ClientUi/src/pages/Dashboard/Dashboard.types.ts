export interface DashboardService {
  name: string;
  displayName: string;
  status: string;
  cpuPercent?: number | null;
  memoryBytes?: number | null;
  gcHeapBytes?: number | null;
  uptimeSeconds?: number | null;
}

export interface DashboardInfra {
  name: string;
  status: string;
  latencyMs?: number | null;
  detail?: string | null;
}

export interface DashboardOverview {
  services: DashboardService[];
  infrastructure: DashboardInfra[];
  activeAlerts: number | null;
  metricsAvailable: boolean;
  metricsCoverage?: number;
  connections: Record<string, boolean>;
  generatedAtUtc?: string;
  environment?: string;
  warnings?: string[];
}

export interface DashboardActivity {
  occurredAtUtc: string;
  level: string;
  service: string;
  activityType?: string | null;
  platform?: string | null;
  actorDisplayName?: string | null;
  message: string;
  rawMessage?: string | null;
}

export interface DashboardActivityStats {
  total: number;
  warnings: number;
  errors: number;
  byAction: Record<string, number>;
  byService: Record<string, number>;
  byPlatform: Record<string, number>;
  timeline: Array<{ timestampUtc: string; count: number }>;
  topActors: Array<{ actorDisplayName: string; count: number }>;
  truncated?: boolean;
}

export interface DashboardMetricsPoint {
  timestampUtc: string;
  cpuPercent: number | null;
  memoryBytes: number | null;
  gcHeapBytes: number | null;
}

export interface DashboardMetricsHistory {
  points: DashboardMetricsPoint[];
}

export interface DashboardProps {
  // Пока пустой интерфейс, можно расширить в будущем
}
