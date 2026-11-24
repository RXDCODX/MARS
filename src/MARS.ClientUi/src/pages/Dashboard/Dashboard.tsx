import {
  Badge,
  Card,
  Col,
  List,
  Progress,
  Row,
  Space,
  Spin,
  Statistic,
  Tag,
  Typography,
} from "antd";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";

import styles from "./Dashboard.module.scss";

interface ServiceEntry {
  name: string;
  displayName: string;
  status: string;
  cpuPercent?: number;
  memoryBytes?: number;
  gcHeapBytes?: number;
  uptimeSeconds?: number;
}

interface InfraEntry {
  name: string;
  status: string;
}

interface Overview {
  services: ServiceEntry[];
  infrastructure: InfraEntry[];
  activeAlerts: number;
  metricsAvailable: boolean;
  connections: Record<string, boolean>;
}

interface EventItem {
  id: string;
  type: string;
  sourceService: string;
  occurredAtUtc: string;
  actorDisplayName?: string;
  payloadJson: string;
}

const formatBytes = (bytes?: number): string => {
  if (!bytes || bytes <= 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
};

const formatUptime = (seconds?: number): string => {
  if (!seconds || seconds <= 0) return "—";
  const d = Math.floor(seconds / 86400);
  const h = Math.floor((seconds % 86400) / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const parts: string[] = [];
  if (d > 0) parts.push(`${d}д`);
  if (h > 0) parts.push(`${h}ч`);
  parts.push(`${m}м`);
  return parts.join(" ");
};

const statusColor = (status: string): string => {
  switch (status.toLowerCase()) {
    case "running":
    case "online":
      return "green";
    case "stopped":
    case "offline":
      return "default";
    case "starting":
      return "blue";
    case "error":
      return "red";
    default:
      return "default";
  }
};

const Dashboard: React.FC = () => {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [events, setEvents] = useState<EventItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

  const fetchOverview = async () => {
    try {
      const res = await fetch("/api/Dashboard/overview");
      const data = await res.json();
      if (data.success && data.result) {
        setOverview(data.result);
        setError(null);
      } else {
        setError(data.errorMessage ?? "Ошибка получения обзора");
      }
    } catch {
      setError("Сервер недоступен");
    } finally {
      setLoading(false);
    }
  };

  const fetchEvents = async () => {
    try {
      const res = await fetch("/api/Dashboard/events?limit=40");
      const data = await res.json();
      if (data.success && Array.isArray(data.result)) {
        setEvents(data.result);
      }
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    fetchOverview();
    fetchEvents();
    const ov = setInterval(fetchOverview, 15000);
    const ev = setInterval(fetchEvents, 10000);
    return () => {
      clearInterval(ov);
      clearInterval(ev);
    };
  }, []);

  // SignalR live feed
  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl("/hubs/dashboard")
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on("ReceiveEvent", (evt: EventItem) => {
      setEvents(prev => [evt, ...prev].slice(0, 60));
    });

    conn
      .start()
      .then(() => {
        connectionRef.current = conn;
      })
      .catch(() => {
        // hub недоступен — остаёмся на поллинге
      });

    return () => {
      conn.stop();
    };
  }, []);

  const servicesOnline = overview
    ? overview.services.filter(s => s.status.toLowerCase() === "running").length
    : 0;

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spin size="large" />
        <p>Загрузка дашборда...</p>
      </div>
    );
  }

  return (
    <div className={styles.dashboard}>
      <div className={styles.header}>
        <Typography.Title level={2} style={{ margin: 0 }}>
          MARS Dashboard
        </Typography.Title>
        <Typography.Text type="secondary">
          Мониторинг микросервисов и бизнес-событий
        </Typography.Text>
        {error && <Tag color="error">{error}</Tag>}
      </div>

      {/* KPI strip */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} sm={6}>
          <Card size="small">
            <Statistic title="Сервисов онлайн" value={servicesOnline} suffix={`/ ${overview?.services.length ?? 0}`} />
          </Card>
        </Col>
        <Col xs={12} sm={6}>
          <Card size="small">
            <Statistic title="Инфраструктура" value={overview?.infrastructure.filter(i => i.status === "online").length ?? 0} suffix={`/ ${overview?.infrastructure.length ?? 0}`} />
          </Card>
        </Col>
        <Col xs={12} sm={6}>
          <Card size="small">
            <Statistic title="Активные алерты" value={overview?.activeAlerts ?? 0} />
          </Card>
        </Col>
        <Col xs={12} sm={6}>
          <Card size="small">
            <Statistic title="Событий в ленте" value={events.length} />
          </Card>
        </Col>
      </Row>

      {/* Connections */}
      {overview?.connections && Object.keys(overview.connections).length > 0 && (
        <Card title="Подключения" size="small" style={{ marginBottom: 16 }}>
          <Space wrap>
            {Object.entries(overview.connections).map(([k, v]) => (
              <Tag key={k} color={v ? "success" : "error"}>
                {k}: {v ? "онлайн" : "офлайн"}
              </Tag>
            ))}
          </Space>
        </Card>
      )}

      {/* Services grid */}
      <Card title="Сервисы" size="small" style={{ marginBottom: 16 }}>
        <Row gutter={[12, 12]}>
          {(overview?.services ?? []).map(svc => (
            <Col xs={24} sm={12} md={8} lg={6} key={svc.name}>
              <Card size="small" title={svc.displayName || svc.name}>
                <Space direction="vertical" size={2} style={{ width: "100%" }}>
                  <Tag color={statusColor(svc.status)}>{svc.status}</Tag>
                  {svc.cpuPercent != null && (
                    <Typography.Text style={{ fontSize: 12 }}>
                      CPU: {svc.cpuPercent.toFixed(1)}%
                    </Typography.Text>
                  )}
                  {svc.memoryBytes != null && (
                    <Typography.Text style={{ fontSize: 12 }}>
                      RAM: {formatBytes(svc.memoryBytes)}
                    </Typography.Text>
                  )}
                  {svc.gcHeapBytes != null && (
                    <Typography.Text style={{ fontSize: 12 }}>
                      GC: {formatBytes(svc.gcHeapBytes)}
                    </Typography.Text>
                  )}
                  {svc.uptimeSeconds != null && (
                    <Typography.Text style={{ fontSize: 12 }} type="secondary">
                      Uptime: {formatUptime(svc.uptimeSeconds)}
                    </Typography.Text>
                  )}
                  {svc.cpuPercent != null && (
                    <Progress percent={Math.min(100, svc.cpuPercent)} size="small" showInfo={false} />
                  )}
                </Space>
              </Card>
            </Col>
          ))}
        </Row>
        {!overview?.metricsAvailable && (
          <Typography.Text type="secondary" style={{ fontSize: 12, marginTop: 8, display: "block" }}>
            Метрики Prometheus недоступны (возможно, локальный запуск без compose).
          </Typography.Text>
        )}
      </Card>

      {/* Infrastructure */}
      <Card title="Инфраструктура" size="small" style={{ marginBottom: 16 }}>
        <Space wrap>
          {(overview?.infrastructure ?? []).map(i => (
            <Tag key={i.name} color={i.status === "online" ? "success" : "error"}>
              {i.name}: {i.status}
            </Tag>
          ))}
        </Space>
      </Card>

      {/* Business event feed */}
      <Card title="Лента бизнес-событий" size="small">
        <List
          size="small"
          dataSource={events}
          renderItem={(item: EventItem) => (
            <List.Item>
              <Space direction="vertical" size={0} style={{ width: "100%" }}>
                <Space>
                  <Tag color="blue">{item.type}</Tag>
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    {item.sourceService}
                  </Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    {new Date(item.occurredAtUtc).toLocaleTimeString()}
                  </Typography.Text>
                  {item.actorDisplayName && (
                    <Badge count={item.actorDisplayName} style={{ backgroundColor: "#52c41a" }} />
                  )}
                </Space>
              </Space>
            </List.Item>
          )}
        />
        {events.length === 0 && (
          <Typography.Text type="secondary">Событий пока нет</Typography.Text>
        )}
      </Card>
    </div>
  );
};

export default Dashboard;