import {
  faBolt,
  faCheckCircle,
  faClock,
  faExclamationTriangle,
  faEyeSlash,
  faList,
  faPlay,
  faRedo,
  faRefresh,
  faSearch,
  faStop,
  faSyncAlt,
  faTimesCircle,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  Alert,
  Badge,
  Button,
  Card,
  Col,
  Drawer,
  Input,
  InputNumber,
  Progress,
  Row,
  Space,
  Spin,
  Switch,
  Table,
  Tooltip,
} from "antd";
import { useEffect, useMemo } from "react";
import { useNavigate } from "react-router-dom";

import { useServiceStore } from "@/shared/serviceStore";

import LogViewer from "./LogViewer";
import ServiceDetails from "./ServiceDetails";

const statusColors: Record<string, string> = {
  Running: "#198754",
  Stopped: "#6c757d",
  Starting: "#0dcaf0",
  Stopping: "#ffc107",
  Error: "#dc3545",
  Unknown: "#343a40",
};

const statusOrder = [
  "Running",
  "Stopped",
  "Starting",
  "Stopping",
  "Error",
  "Unknown",
];

function ServerViewerUI() {
  const {
    services,
    loading,
    error,
    actionLoading,
    logs,
    logsService,
    logsLoading,
    statusFilter,
    search,
    autoRefresh,
    progress,
    fetchServices,
    handleAction,
    handleShowLogs,
    handleCloseLogs,
    setStatusFilter,
    setSearch,
    setAutoRefresh,
    setProgress,
    showLogs,
    selectedService,
    setSelectedService,
    clearSelectedService,
  } = useServiceStore();

  const navigate = useNavigate();

  useEffect(() => {
    let interval: NodeJS.Timeout;
    let progressInt: NodeJS.Timeout;
    if (autoRefresh) {
      interval = setInterval(fetchServices, 5000);
      let p = 0;
      progressInt = setInterval(() => {
        p = (p + 20) % 100;
        setProgress(p);
      }, 1000);
    }
    return () => {
      if (interval) clearInterval(interval);
      if (progressInt) clearInterval(progressInt);
      setProgress(0);
    };
  }, [autoRefresh, fetchServices, setProgress]);

  useEffect(() => {
    fetchServices();
  }, [fetchServices]);

  const filteredServices = useMemo(
    () =>
      services.filter(
        s =>
          (!statusFilter || s.status === statusFilter) &&
          (!search ||
            s.displayName.toLowerCase().includes(search.toLowerCase()) ||
            s.name.toLowerCase().includes(search.toLowerCase()))
      ),
    [services, statusFilter, search]
  );

  const columns = [
    {
      title: "Имя",
      key: "name",
      render: (_: unknown, record: (typeof services)[0]) => (
        <Button
          type="link"
          style={{ padding: 0, fontWeight: 500 }}
          onClick={() => {
            setSelectedService(record.name);
            navigate(
              `/services/details?name=${encodeURIComponent(record.name)}`
            );
          }}
        >
          {record.displayName || record.name}
        </Button>
      ),
    },
    {
      title: "Описание",
      dataIndex: "description",
      key: "description",
    },
    {
      title: "Статус",
      key: "status",
      render: (_: unknown, record: (typeof services)[0]) => (
        <Badge
          color={statusColors[record.status] || "#6c757d"}
          style={{ borderRadius: 16, padding: "2px 10px" }}
        >
          <FontAwesomeIcon
            icon={
              record.status === "Running"
                ? faCheckCircle
                : record.status === "Error"
                  ? faExclamationTriangle
                  : record.status === "Stopped"
                    ? faTimesCircle
                    : record.status === "Starting"
                      ? faClock
                      : record.status === "Stopping"
                        ? faBolt
                        : faEyeSlash
            }
          />{" "}
          {record.status}
        </Badge>
      ),
    },
    {
      title: "Время старта",
      key: "startTime",
      render: (_: unknown, record: (typeof services)[0]) =>
        record.startTime ? new Date(record.startTime).toLocaleString() : "-",
    },
    {
      title: "Последняя активность",
      key: "lastActivity",
      render: (_: unknown, record: (typeof services)[0]) =>
        record.lastActivity
          ? new Date(record.lastActivity).toLocaleString()
          : "-",
    },
    {
      title: "Включен",
      key: "isEnabled",
      render: (_: unknown, record: (typeof services)[0]) => (
        <Tooltip
          title={record.isEnabled ? "Отключить сервис" : "Включить сервис"}
        >
          <Switch
            checked={record.isEnabled}
            onChange={() => handleAction(record.name, "toggle")}
            disabled={actionLoading === record.name + "toggle"}
          />
        </Tooltip>
      ),
    },
    {
      title: "Действия",
      key: "actions",
      render: (_: unknown, record: (typeof services)[0]) => (
        <Space>
          <Tooltip title="Старт">
            <Button
              size="small"
              type="primary"
              ghost
              disabled={actionLoading === record.name + "start"}
              onClick={() => handleAction(record.name, "start")}
            >
              <FontAwesomeIcon icon={faPlay} />
            </Button>
          </Tooltip>
          <Tooltip title="Стоп">
            <Button
              size="small"
              danger
              ghost
              disabled={actionLoading === record.name + "stop"}
              onClick={() => handleAction(record.name, "stop")}
            >
              <FontAwesomeIcon icon={faStop} />
            </Button>
          </Tooltip>
          <Tooltip title="Рестарт">
            <Button
              size="small"
              type="default"
              disabled={actionLoading === record.name + "restart"}
              onClick={() => handleAction(record.name, "restart")}
              style={{ borderColor: "#faad14", color: "#faad14" }}
            >
              <FontAwesomeIcon icon={faRedo} />
            </Button>
          </Tooltip>
        </Space>
      ),
    },
    {
      title: "Логи",
      key: "logs",
      render: (_: unknown, record: (typeof services)[0]) => (
        <Button
          size="small"
          type="primary"
          ghost
          onClick={() => handleShowLogs(record.name)}
        >
          <FontAwesomeIcon icon={faList} />
        </Button>
      ),
    },
  ];

  return (
    <div style={{ padding: "16px 0", position: "relative" }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          marginBottom: 12,
        }}
      >
        <h2 style={{ marginBottom: 0 }}>
          <FontAwesomeIcon icon={faList} /> Service Manager
        </h2>
        <Space>
          <Button
            type={autoRefresh ? "primary" : "default"}
            onClick={() => setAutoRefresh(!autoRefresh)}
            title={
              autoRefresh
                ? "Автообновление включено"
                : "Включить автообновление"
            }
            style={
              autoRefresh
                ? { background: "#52c41a", borderColor: "#52c41a" }
                : undefined
            }
          >
            <FontAwesomeIcon icon={faSyncAlt} spin={autoRefresh} />
            {autoRefresh ? " Автообновление" : " Вручную"}
          </Button>
          <Button
            type="primary"
            onClick={fetchServices}
            title="Обновить сейчас"
          >
            <FontAwesomeIcon icon={faRefresh} /> Обновить
          </Button>
        </Space>
      </div>
      <div
        style={{
          display: "flex",
          gap: 12,
          marginBottom: 12,
          flexWrap: "wrap",
          alignItems: "center",
        }}
      >
        <Input
          prefix={<FontAwesomeIcon icon={faSearch} />}
          placeholder="Поиск по имени..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          style={{ maxWidth: 300 }}
        />
        <Space.Compact>
          <Button
            size="small"
            type={statusFilter === "" ? "primary" : "default"}
            onClick={() => setStatusFilter("")}
          >
            Все
          </Button>
          {statusOrder.map(st => (
            <Button
              key={st}
              size="small"
              type={statusFilter === st ? "primary" : "default"}
              onClick={() => setStatusFilter(st)}
              style={{ minWidth: 90, textTransform: "none" }}
            >
              {st}
            </Button>
          ))}
        </Space.Compact>
      </div>
      {error && <Alert type="error" message={error} />}
      {autoRefresh && (
        <Progress
          percent={progress}
          showInfo={false}
          strokeWidth={3}
          style={{ marginBottom: 8 }}
        />
      )}
      <div style={{ maxHeight: "70vh", overflowY: "auto" }}>
        <Table
          columns={columns}
          dataSource={filteredServices}
          rowKey="name"
          size="small"
          bordered
          pagination={false}
          style={{
            background: "#fff",
            boxShadow:
              "0 1px 2px 0 rgba(0,0,0,0.03), 0 1px 6px -1px rgba(0,0,0,0.02), 0 2px 4px 0 rgba(0,0,0,0.02)",
          }}
        />
        {loading && (
          <div
            style={{
              position: "absolute",
              left: 0,
              top: 0,
              right: 0,
              bottom: 0,
              background: "rgba(255,255,255,0.6)",
              zIndex: 10,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Spin size="large" />
          </div>
        )}
      </div>
      <Drawer
        open={!!selectedService}
        onClose={clearSelectedService}
        placement="right"
        maskClosable={false}
        width={600}
        styles={{ body: { padding: 0 } }}
      >
        {selectedService && <ServiceDetails onClose={clearSelectedService} />}
      </Drawer>
      <Drawer
        open={showLogs}
        onClose={handleCloseLogs}
        placement="right"
        maskClosable={false}
        width={500}
        styles={{ body: { padding: 0 } }}
      >
        <LogViewer
          logs={logs}
          loading={logsLoading}
          onClose={handleCloseLogs}
          serviceName={logsService || undefined}
        />
      </Drawer>
    </div>
  );
}

export default ServerViewerUI;
