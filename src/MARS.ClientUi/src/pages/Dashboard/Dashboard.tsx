import {
  Badge,
  Button,
  Card,
  Col,
  Drawer,
  Input,
  List,
  Row,
  Select,
  Skeleton,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from "antd";
import {
  AlertCircle,
  Activity,
  Cpu,
  Database,
  HardDrive,
  RefreshCw,
  Server,
} from "lucide-react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { dashboardApi } from "./dashboardApi";
import type {
  DashboardActivity,
  DashboardActivityStats,
  DashboardMetricsHistory,
  DashboardOverview,
  DashboardService,
} from "./Dashboard.types";
import styles from "./Dashboard.module.scss";

const HOURS_OPTIONS = [
  { value: 1, label: "1 час" },
  { value: 6, label: "6 часов" },
  { value: 24, label: "24 часа" },
  { value: 168, label: "7 дней" },
];

const formatBytes = (bytes?: number | null): string => {
  if (!bytes || bytes <= 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.min(
    sizes.length - 1,
    Math.floor(Math.log(bytes) / Math.log(k))
  );
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
};

const formatUptime = (seconds?: number | null): string => {
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
    case "starting":
      return "blue";
    case "error":
      return "red";
    case "stopped":
    case "offline":
      return "default";
    default:
      return "default";
  }
};

const statusLabel = (status: string): string => {
  switch (status.toLowerCase()) {
    case "running":
      return "Работает";
    case "online":
      return "Онлайн";
    case "starting":
      return "Запуск";
    case "error":
      return "Ошибка";
    case "stopped":
      return "Остановлен";
    case "offline":
      return "Офлайн";
    default:
      return status || "Неизвестно";
  }
};

const activityTypeLabel = (type?: string | null): string => {
  const labels: Record<string, string> = {
    "chat-message": "Сообщение",
    "chat-message-deleted": "Удаление сообщения",
    "command-requested": "Команда",
    "command-executed": "Выполнение команды",
    "reward-redeemed": "Награда",
    "reward-refunded": "Возврат награды",
    "waifu-rolled": "Вайфу-ролл",
    "sound-requested": "Звуковой запрос",
    "cinema-item-added": "Кино: добавлено",
    "cinema-status-changed": "Кино: статус",
    "media-uploaded": "Медиа: загрузка",
    "media-deleted": "Медиа: удаление",
    "scoreboard-updated": "Табло",
    "configuration-changed": "Конфигурация",
    "service-action": "Действие сервиса",
  };

  return type ? (labels[type] ?? type) : "Активность";
};

const Dashboard: React.FC = () => {
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [overviewError, setOverviewError] = useState<string | null>(null);
  const [overviewLoading, setOverviewLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const [activities, setActivities] = useState<DashboardActivity[]>([]);
  const [activitiesError, setActivitiesError] = useState<string | null>(null);
  const [activitiesLoading, setActivitiesLoading] = useState(true);

  const [stats, setStats] = useState<DashboardActivityStats | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);

  const [metrics, setMetrics] = useState<DashboardMetricsHistory | null>(null);
  const [metricsError, setMetricsError] = useState<string | null>(null);

  const [hours, setHours] = useState(24);
  const [serviceFilter, setServiceFilter] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");
  const [platformFilter, setPlatformFilter] = useState("all");
  const [search, setSearch] = useState("");
  const [selectedActivity, setSelectedActivity] =
    useState<DashboardActivity | null>(null);

  const overviewFetching = useRef(false);
  const activitiesFetching = useRef(false);

  const fetchOverview = useCallback(async (signal?: AbortSignal) => {
    if (overviewFetching.current) return;
    overviewFetching.current = true;
    try {
      const data = await dashboardApi.overview(signal);
      setOverview(data);
      setOverviewError(null);
      setLastUpdated(new Date());
    } catch (error) {
      if (signal?.aborted) return;
      setOverviewError(
        error instanceof Error ? error.message : "Сервер недоступен"
      );
    } finally {
      setOverviewLoading(false);
      overviewFetching.current = false;
    }
  }, []);

  const fetchActivities = useCallback(
    async (signal?: AbortSignal) => {
      if (activitiesFetching.current) return;
      activitiesFetching.current = true;
      try {
        const data = await dashboardApi.activities(
          {
            hours,
            service: serviceFilter === "all" ? undefined : serviceFilter,
            type: typeFilter === "all" ? undefined : typeFilter,
            platform: platformFilter === "all" ? undefined : platformFilter,
            search: search || undefined,
            limit: 120,
          },
          signal
        );
        setActivities(data);
        setActivitiesError(null);
      } catch (error) {
        if (signal?.aborted) return;
        setActivitiesError(
          error instanceof Error
            ? error.message
            : "Не удалось загрузить активности"
        );
      } finally {
        setActivitiesLoading(false);
        activitiesFetching.current = false;
      }
    },
    [hours, serviceFilter, typeFilter, platformFilter, search]
  );

  const fetchStats = useCallback(
    async (signal?: AbortSignal) => {
      try {
        const data = await dashboardApi.activityStats(hours, signal);
        setStats(data);
        setStatsError(null);
      } catch (error) {
        if (signal?.aborted) return;
        setStatsError(
          error instanceof Error
            ? error.message
            : "Не удалось загрузить статистику"
        );
      }
    },
    [hours]
  );

  const fetchMetrics = useCallback(
    async (signal?: AbortSignal) => {
      try {
        const data = await dashboardApi.metricsHistory(hours, signal);
        setMetrics(data);
        setMetricsError(null);
      } catch (error) {
        if (signal?.aborted) return;
        setMetricsError(
          error instanceof Error ? error.message : "Метрики недоступны"
        );
      }
    },
    [hours]
  );

  useEffect(() => {
    const controller = new AbortController();
    fetchOverview(controller.signal);
    const timer = window.setInterval(() => fetchOverview(), 15000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [fetchOverview]);

  useEffect(() => {
    const controller = new AbortController();
    setActivitiesLoading(true);
    fetchActivities(controller.signal);
    const timer = window.setInterval(() => fetchActivities(), 5000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [fetchActivities]);

  useEffect(() => {
    const controller = new AbortController();
    fetchStats(controller.signal);
    const timer = window.setInterval(() => fetchStats(), 30000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [fetchStats]);

  useEffect(() => {
    const controller = new AbortController();
    fetchMetrics(controller.signal);
    const timer = window.setInterval(() => fetchMetrics(), 60000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [fetchMetrics]);

  const servicesOnline = useMemo(
    () =>
      overview?.services.filter(
        service => service.status.toLowerCase() === "running"
      ).length ?? 0,
    [overview]
  );

  const infraOnline = useMemo(
    () =>
      overview?.infrastructure.filter(
        item => item.status.toLowerCase() === "online"
      ).length ?? 0,
    [overview]
  );

  const serviceOptions = useMemo(
    () => [
      { value: "all", label: "Все сервисы" },
      ...(overview?.services.map(service => ({
        value: service.name,
        label: service.displayName || service.name,
      })) ?? []),
    ],
    [overview]
  );

  const typeOptions = useMemo(() => {
    const types = new Set<string>();
    activities.forEach(activity => {
      if (activity.activityType) types.add(activity.activityType);
    });
    Object.keys(stats?.byAction ?? {}).forEach(type => types.add(type));
    return [
      { value: "all", label: "Все типы" },
      ...Array.from(types).map(type => ({
        value: type,
        label: activityTypeLabel(type),
      })),
    ];
  }, [activities, stats]);

  const platformOptions = useMemo(() => {
    const platforms = new Set<string>();
    activities.forEach(activity => {
      if (activity.platform) platforms.add(activity.platform);
    });
    Object.keys(stats?.byPlatform ?? {}).forEach(platform =>
      platforms.add(platform)
    );
    return [
      { value: "all", label: "Все платформы" },
      ...Array.from(platforms).map(platform => ({
        value: platform,
        label: platform,
      })),
    ];
  }, [activities, stats]);

  const cpuAverage = useMemo(() => {
    const values =
      overview?.services
        .map(service => service.cpuPercent)
        .filter((value): value is number => typeof value === "number") ?? [];
    if (values.length === 0) return null;
    return values.reduce((sum, value) => sum + value, 0) / values.length;
  }, [overview]);

  const memoryTotal = useMemo(
    () =>
      overview?.services.reduce(
        (sum, service) => sum + (service.memoryBytes ?? 0),
        0
      ) ?? 0,
    [overview]
  );

  const serviceColumns = [
    {
      title: "Сервис",
      dataIndex: "displayName",
      key: "name",
      render: (_: unknown, record: DashboardService) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>
            {record.displayName || record.name}
          </Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 11 }}>
            {record.name}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: "Статус",
      dataIndex: "status",
      key: "status",
      render: (status: string) => (
        <Tag color={statusColor(status)}>{statusLabel(status)}</Tag>
      ),
    },
    {
      title: "CPU",
      dataIndex: "cpuPercent",
      key: "cpu",
      render: (value?: number | null) =>
        value == null ? "—" : `${value.toFixed(1)}%`,
    },
    {
      title: "RAM",
      dataIndex: "memoryBytes",
      key: "memory",
      render: (value?: number | null) => formatBytes(value),
    },
    {
      title: "GC",
      dataIndex: "gcHeapBytes",
      key: "gc",
      render: (value?: number | null) => formatBytes(value),
    },
    {
      title: "Uptime",
      dataIndex: "uptimeSeconds",
      key: "uptime",
      render: (value?: number | null) => formatUptime(value),
    },
  ];

  if (overviewLoading && !overview) {
    return (
      <div className={styles.dashboard}>
        <div className={styles.header}>
          <Typography.Title level={2} style={{ margin: 0 }}>
            MARS Dashboard
          </Typography.Title>
          <Typography.Text type="secondary">
            Загрузка мониторинга и бизнес-активностей...
          </Typography.Text>
        </div>
        <Skeleton active paragraph={{ rows: 8 }} />
      </div>
    );
  }

  return (
    <div className={styles.dashboard}>
      <div className={styles.header}>
        <Space
          align="start"
          style={{ width: "100%", justifyContent: "space-between" }}
          wrap
        >
          <Space direction="vertical" size={0}>
            <Typography.Title level={2} style={{ margin: 0 }}>
              MARS Dashboard
            </Typography.Title>
            <Typography.Text type="secondary">
              Мониторинг сервисов, ресурсов и бизнес-активностей
            </Typography.Text>
          </Space>
          <Space wrap>
            <Tag color={overviewError ? "error" : "success"}>
              {overviewError ? "Частичные данные" : "Данные актуальны"}
            </Tag>
            {lastUpdated && (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Обновлено: {lastUpdated.toLocaleTimeString()}
              </Typography.Text>
            )}
            <Button
              size="small"
              icon={<RefreshCw size={14} />}
              onClick={() => fetchOverview()}
              loading={overviewLoading}
            >
              Обновить
            </Button>
          </Space>
        </Space>
        {overviewError && (
          <Tag color="error" style={{ marginTop: 8 }}>
            {overviewError}
          </Tag>
        )}
      </div>

      <div
        role="status"
        aria-live="polite"
        style={{ position: "absolute", left: -9999 }}
      >
        Сервисов онлайн: {servicesOnline} из {overview?.services.length ?? 0}.
        Активных алертов: {overview?.activeAlerts ?? "неизвестно"}.
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Сервисы online"
              value={servicesOnline}
              suffix={`/ ${overview?.services.length ?? 0}`}
              prefix={<Server size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Инфраструктура"
              value={infraOnline}
              suffix={`/ ${overview?.infrastructure.length ?? 0}`}
              prefix={<Database size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Активные алерты"
              value={overview?.activeAlerts ?? "—"}
              prefix={<AlertCircle size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Активности"
              value={stats?.total ?? activities.length}
              prefix={<Activity size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Предупреждения"
              value={stats?.warnings ?? 0}
              prefix={<AlertCircle size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Ошибки"
              value={stats?.errors ?? 0}
              prefix={<AlertCircle size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="Средний CPU"
              value={cpuAverage == null ? "—" : `${cpuAverage.toFixed(1)}%`}
              prefix={<Cpu size={16} />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={8} md={6} lg={3}>
          <Card size="small">
            <Statistic
              title="RAM суммарно"
              value={formatBytes(memoryTotal)}
              prefix={<HardDrive size={16} />}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} lg={16}>
          <Card
            title="Здоровье сервисов"
            size="small"
            extra={
              <Input.Search
                size="small"
                placeholder="Поиск сервиса..."
                style={{ width: 220 }}
                allowClear
                onSearch={value => setSearch(value)}
              />
            }
          >
            <Table
              size="small"
              rowKey="name"
              dataSource={overview?.services ?? []}
              columns={serviceColumns}
              pagination={{ pageSize: 8, hideOnSinglePage: true }}
              scroll={{ x: 760 }}
            />
            {!overview?.metricsAvailable && (
              <Typography.Text
                type="secondary"
                style={{ fontSize: 12, display: "block", marginTop: 8 }}
              >
                Метрики Prometheus недоступны: показаны только статусы
                health-check.
              </Typography.Text>
            )}
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            <Card title="Инфраструктура" size="small">
              <Space wrap>
                {(overview?.infrastructure ?? []).map(item => (
                  <Tag
                    key={item.name}
                    color={
                      item.status.toLowerCase() === "online"
                        ? "success"
                        : "error"
                    }
                  >
                    {item.name}: {statusLabel(item.status)}
                    {item.latencyMs != null ? ` (${item.latencyMs} мс)` : ""}
                  </Tag>
                ))}
              </Space>
            </Card>
            <Card title="Подключения" size="small">
              <Space wrap>
                {overview?.connections &&
                Object.keys(overview.connections).length > 0 ? (
                  Object.entries(overview.connections).map(([name, value]) => (
                    <Tag key={name} color={value ? "success" : "error"}>
                      {name}: {value ? "онлайн" : "офлайн"}
                    </Tag>
                  ))
                ) : (
                  <Typography.Text type="secondary">
                    Нет данных о подключениях
                  </Typography.Text>
                )}
              </Space>
            </Card>
            {overview?.warnings && overview.warnings.length > 0 && (
              <Card title="Предупреждения" size="small">
                <Space direction="vertical" size={4} style={{ width: "100%" }}>
                  {overview.warnings.map((warning, index) => (
                    <Typography.Text
                      key={index}
                      type="warning"
                      style={{ fontSize: 12 }}
                    >
                      {warning}
                    </Typography.Text>
                  ))}
                </Space>
              </Card>
            )}
          </Space>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} lg={12}>
          <Card title="Активность по типам" size="small">
            {statsError ? (
              <Typography.Text type="danger">{statsError}</Typography.Text>
            ) : !stats ? (
              <Skeleton active paragraph={{ rows: 6 }} />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart
                  data={Object.entries(stats.byAction).map(([name, value]) => ({
                    name: activityTypeLabel(name),
                    value,
                  }))}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="name"
                    tick={{ fontSize: 11 }}
                    interval={0}
                    angle={-25}
                    textAnchor="end"
                    height={70}
                  />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="value" fill="#1677ff" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Активность по сервисам" size="small">
            {statsError ? (
              <Typography.Text type="danger">{statsError}</Typography.Text>
            ) : !stats ? (
              <Skeleton active paragraph={{ rows: 6 }} />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart
                  data={Object.entries(stats.byService).map(
                    ([name, value]) => ({
                      name,
                      value,
                    })
                  )}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="name"
                    tick={{ fontSize: 11 }}
                    interval={0}
                    angle={-25}
                    textAnchor="end"
                    height={70}
                  />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="value" fill="#52c41a" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} lg={16}>
          <Card title="Динамика активности" size="small">
            {statsError ? (
              <Typography.Text type="danger">{statsError}</Typography.Text>
            ) : !stats ? (
              <Skeleton active paragraph={{ rows: 6 }} />
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <AreaChart
                  data={stats.timeline.map(point => ({
                    time: new Date(point.timestampUtc).toLocaleTimeString([], {
                      hour: "2-digit",
                      minute: "2-digit",
                    }),
                    count: point.count,
                  }))}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="time" tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Area
                    type="monotone"
                    dataKey="count"
                    stroke="#722ed1"
                    fill="#722ed1"
                    fillOpacity={0.2}
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Активные пользователи" size="small">
            {statsError ? (
              <Typography.Text type="danger">{statsError}</Typography.Text>
            ) : !stats || stats.topActors.length === 0 ? (
              <Typography.Text type="secondary">
                Пока нет бизнес-активностей с автором
              </Typography.Text>
            ) : (
              <List
                size="small"
                dataSource={stats.topActors}
                renderItem={item => (
                  <List.Item>
                    <Space
                      style={{ width: "100%", justifyContent: "space-between" }}
                    >
                      <Typography.Text>{item.actorDisplayName}</Typography.Text>
                      <Badge count={item.count} color="#1677ff" />
                    </Space>
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24}>
          <Card title="Ресурсы за период" size="small">
            {metricsError ? (
              <Typography.Text type="danger">{metricsError}</Typography.Text>
            ) : !metrics || metrics.points.length === 0 ? (
              <Typography.Text type="secondary">
                История метрик недоступна
              </Typography.Text>
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <AreaChart
                  data={metrics.points.map(point => ({
                    time: new Date(point.timestampUtc).toLocaleTimeString([], {
                      hour: "2-digit",
                      minute: "2-digit",
                    }),
                    cpu: point.cpuPercent,
                    memory: point.memoryBytes
                      ? point.memoryBytes / 1024 / 1024
                      : null,
                  }))}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="time" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip />
                  <Area
                    type="monotone"
                    dataKey="cpu"
                    name="CPU, %"
                    stroke="#1677ff"
                    fill="#1677ff"
                    fillOpacity={0.15}
                  />
                  <Area
                    type="monotone"
                    dataKey="memory"
                    name="RAM, MB"
                    stroke="#52c41a"
                    fill="#52c41a"
                    fillOpacity={0.1}
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
      </Row>

      <Card
        title="Лента бизнес-активностей"
        size="small"
        extra={
          <Space wrap>
            <Select
              size="small"
              value={hours}
              onChange={value => setHours(value)}
              options={HOURS_OPTIONS}
              style={{ width: 110 }}
            />
            <Select
              size="small"
              value={serviceFilter}
              onChange={value => setServiceFilter(value)}
              options={serviceOptions}
              style={{ width: 170 }}
              showSearch
              optionFilterProp="label"
            />
            <Select
              size="small"
              value={typeFilter}
              onChange={value => setTypeFilter(value)}
              options={typeOptions}
              style={{ width: 170 }}
              showSearch
              optionFilterProp="label"
            />
            <Select
              size="small"
              value={platformFilter}
              onChange={value => setPlatformFilter(value)}
              options={platformOptions}
              style={{ width: 150 }}
            />
            <Input.Search
              size="small"
              placeholder="Поиск по тексту..."
              style={{ width: 220 }}
              allowClear
              onSearch={value => setSearch(value)}
            />
          </Space>
        }
      >
        {activitiesError ? (
          <Typography.Text type="danger">{activitiesError}</Typography.Text>
        ) : activitiesLoading && activities.length === 0 ? (
          <Skeleton active paragraph={{ rows: 6 }} />
        ) : activities.length === 0 ? (
          <Typography.Text type="secondary">
            За выбранный период бизнес-активностей нет
          </Typography.Text>
        ) : (
          <List
            size="small"
            dataSource={activities}
            renderItem={(item, index) => (
              <List.Item
                key={`${item.occurredAtUtc}-${item.service}-${index}`}
                actions={[
                  <Button
                    key="details"
                    type="link"
                    size="small"
                    onClick={() => setSelectedActivity(item)}
                  >
                    Подробнее
                  </Button>,
                ]}
              >
                <Space direction="vertical" size={0} style={{ width: "100%" }}>
                  <Space wrap size={4}>
                    <Tag color="blue">
                      {activityTypeLabel(item.activityType)}
                    </Tag>
                    <Tag color={statusColor(item.level)}>{item.level}</Tag>
                    <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                      {item.service}
                    </Typography.Text>
                    {item.platform && (
                      <Typography.Text
                        type="secondary"
                        style={{ fontSize: 11 }}
                      >
                        {item.platform}
                      </Typography.Text>
                    )}
                    <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                      {new Date(item.occurredAtUtc).toLocaleString()}
                    </Typography.Text>
                    {item.actorDisplayName && (
                      <Tag color="geekblue">{item.actorDisplayName}</Tag>
                    )}
                  </Space>
                  <Typography.Paragraph
                    style={{ margin: "4px 0 0", fontSize: 13 }}
                    ellipsis={{ rows: 2, expandable: false }}
                  >
                    {item.message}
                  </Typography.Paragraph>
                </Space>
              </List.Item>
            )}
          />
        )}
      </Card>

      <Drawer
        title="Подробности активности"
        open={selectedActivity !== null}
        onClose={() => setSelectedActivity(null)}
        width={520}
      >
        {selectedActivity && (
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            <Space wrap>
              <Tag color="blue">
                {activityTypeLabel(selectedActivity.activityType)}
              </Tag>
              <Tag color={statusColor(selectedActivity.level)}>
                {selectedActivity.level}
              </Tag>
              <Typography.Text type="secondary">
                {selectedActivity.service}
              </Typography.Text>
            </Space>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {new Date(selectedActivity.occurredAtUtc).toLocaleString()}
            </Typography.Text>
            {selectedActivity.actorDisplayName && (
              <Typography.Text>
                Автор: {selectedActivity.actorDisplayName}
              </Typography.Text>
            )}
            {selectedActivity.platform && (
              <Typography.Text>
                Платформа: {selectedActivity.platform}
              </Typography.Text>
            )}
            <Card size="small" title="Текст">
              <Typography.Paragraph
                style={{ whiteSpace: "pre-wrap", margin: 0 }}
              >
                {selectedActivity.message}
              </Typography.Paragraph>
            </Card>
            {selectedActivity.rawMessage &&
              selectedActivity.rawMessage !== selectedActivity.message && (
                <Card size="small" title="Исходная строка">
                  <Typography.Paragraph
                    style={{ whiteSpace: "pre-wrap", margin: 0, fontSize: 12 }}
                  >
                    {selectedActivity.rawMessage}
                  </Typography.Paragraph>
                </Card>
              )}
          </Space>
        )}
      </Drawer>
    </div>
  );
};

export default Dashboard;
