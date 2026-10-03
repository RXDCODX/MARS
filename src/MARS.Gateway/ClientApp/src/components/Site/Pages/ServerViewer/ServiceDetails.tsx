import {
  Alert,
  Badge,
  Button,
  Card,
  Checkbox,
  Col,
  Input,
  Row,
  Select,
  Spin,
  Table,
} from "antd";
import axios from "axios";
import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";

import { useServiceStore } from "@/shared/serviceStore";

const ServiceDetails: React.FC<{ onClose: () => void }> = ({ onClose }) => {
  const { selectedService, services, setSelectedService, fetchServices } =
    useServiceStore();
  const [searchParameters] = useSearchParams();
  const [commands, setCommands] = useState<
    { name: string; description: string }[]
  >([]);
  const [config, setConfig] = useState<Record<string, object>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [execResult, setExecResult] = useState<string | null>(null);
  const [execLoading, setExecLoading] = useState<string | null>(null);
  const [toggleLoading, setToggleLoading] = useState(false);

  const [logs, setLogs] = useState<string[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [logsError, setLogsError] = useState<string | null>(null);
  const [logFilter, setLogFilter] = useState("");
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [logLevel, setLogLevel] = useState<string>("all");

  console.log("ServiceDetails render:", {
    selectedService,
    servicesCount: services.length,
    searchParams: searchParameters.get("name"),
    loading,
    error,
  });

  useEffect(() => {
    const name = searchParameters.get("name");
    console.log("ServiceDetails useEffect - name from params:", name);
    if (name && name !== selectedService) {
      console.log("Setting selected service:", name);
      setSelectedService(name);
    }
  }, [searchParameters, selectedService, setSelectedService]);

  useEffect(() => {
    if (services.length > 0) {
      return;
    }

    console.log("Fetching services...");
    fetchServices();
  }, [services.length, fetchServices]);

  const info = services.find(s => s.name === selectedService) || null;

  useEffect(() => {
    if (!selectedService) return;
    console.log("Loading service details for:", selectedService);
    setLoading(true);
    setError(null);
    Promise.all([
      axios.get(`/api/ServiceManager/service/${selectedService}/commands`),
      axios.get(`/api/ServiceManager/service/${selectedService}/configuration`),
    ])
      .then(([cmdRes, cfgRes]) => {
        console.log("Service details loaded successfully");
        setCommands(cmdRes.data);
        setConfig(cfgRes.data);
      })
      .catch(error_ => {
        console.error("Error loading service details:", error_);
        setError(error_.message || "Ошибка загрузки данных");
      })
      .finally(() => setLoading(false));
  }, [selectedService]);

  const handleExecute = async (command: string) => {
    if (!selectedService) return;
    setExecLoading(command);
    setExecResult(null);
    try {
      const res = await axios.post(
        `/api/ServiceManager/service/${selectedService}/execute`,
        { command: command }
      );
      setExecResult(
        res.data === true
          ? `Команда '${command}' выполнена успешно`
          : `Ошибка выполнения команды '${command}'`
      );
    } catch {
      setExecResult(`Ошибка выполнения команды '${command}'`);
    } finally {
      setExecLoading(null);
    }
  };

  const handleToggleService = async () => {
    if (!selectedService || !info) return;
    setToggleLoading(true);
    try {
      const action = info.isEnabled ? "stop" : "start";
      await axios.post(
        `/api/ServiceManager/service/${selectedService}/${action}`
      );
      await fetchServices();
      setExecResult(
        `Сервис ${info.isEnabled ? "остановлен" : "запущен"} успешно`
      );
    } catch {
      setExecResult(
        `Ошибка ${info.isEnabled ? "остановки" : "запуска"} сервиса`
      );
    } finally {
      setToggleLoading(false);
    }
  };

  const fetchLogs = useCallback(async () => {
    if (!selectedService) return;
    setLogsLoading(true);
    setLogsError(null);
    try {
      const parameters = new URLSearchParams();
      if (logLevel !== "all") {
        parameters.append("level", logLevel);
      }
      const res = await axios.get(
        `/api/ServiceManager/service/${selectedService}/logs?${parameters}`
      );
      setLogs(Array.isArray(res.data) ? res.data : []);
    } catch (error) {
      console.error("Error loading logs:", error);
      setLogsError("Ошибка загрузки логов");
    } finally {
      setLogsLoading(false);
    }
  }, [selectedService, logLevel]);

  useEffect(() => {
    if (!autoRefresh || !selectedService) return;
    const interval = setInterval(fetchLogs, 5000);
    return () => clearInterval(interval);
  }, [autoRefresh, selectedService, logLevel, fetchLogs]);

  useEffect(() => {
    if (selectedService) {
      fetchLogs();
    }
  }, [selectedService, logLevel, fetchLogs]);

  const filteredLogs = logs.filter(
    log =>
      logFilter === "" || log.toLowerCase().includes(logFilter.toLowerCase())
  );

  if (!selectedService) {
    return (
      <div
        style={{
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <div style={{ textAlign: "center" }}>
          <Spin size="small" />
          <div style={{ marginTop: 12 }}>Загрузка информации о сервисе...</div>
          <div style={{ marginTop: 8, color: "#8c8c8c" }}>
            Параметр name: {searchParameters.get("name") || "не указан"}
          </div>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div
        style={{
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <div style={{ textAlign: "center" }}>
          <Spin size="small" />
          <div style={{ marginTop: 12 }}>Загрузка деталей сервиса...</div>
          <div style={{ marginTop: 8, color: "#8c8c8c" }}>
            Сервис: {selectedService}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div
        style={{
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Alert
          type="error"
          message="Ошибка загрузки"
          description={error}
          style={{ maxWidth: 600 }}
          extra={
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <Button danger ghost onClick={onClose}>
                Закрыть
              </Button>
            </div>
          }
        />
      </div>
    );
  }

  if (!info) {
    return (
      <div
        style={{
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Alert
          type="warning"
          message="Сервис не найден"
          description={
            <>
              Сервис "{selectedService}" не найден в списке доступных сервисов.
              <br />
              Доступные сервисы:{" "}
              <span style={{ color: "#8c8c8c" }}>
                {services.map(s => s.name).join(", ") || "нет"}
              </span>
            </>
          }
          style={{ maxWidth: 600 }}
          extra={
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <Button type="default" onClick={onClose}>
                Закрыть
              </Button>
            </div>
          }
        />
      </div>
    );
  }

  const configColumns = [
    {
      title: "Параметр",
      dataIndex: "key",
      key: "key",
      render: (text: string) => (
        <span style={{ fontWeight: "bold", color: "#8c8c8c" }}>{text}:</span>
      ),
    },
    {
      title: "Значение",
      dataIndex: "value",
      key: "value",
      render: (text: string) => (
        <span style={{ wordBreak: "break-all" }}>{text}</span>
      ),
    },
  ];

  const configData = Object.entries(config).map(([k, v]) => ({
    key: k,
    value: String(v),
  }));

  const commandColumns = [
    {
      title: "Команда",
      dataIndex: "name",
      key: "name",
      render: (text: string) => (
        <code
          style={{
            background: "#f5f5f5",
            padding: "2px 8px",
            borderRadius: 4,
          }}
        >
          {text}
        </code>
      ),
    },
    { title: "Описание", dataIndex: "description", key: "description" },
    {
      title: "Действие",
      key: "action",
      render: (_: unknown, record: { name: string }) => (
        <Button
          size="small"
          type="primary"
          onClick={() => handleExecute(record.name)}
          disabled={execLoading === record.name}
        >
          {execLoading === record.name ? <Spin size="small" /> : "▶ Выполнить"}
        </Button>
      ),
    },
  ];

  const logLevelOptions = [
    { value: "all", label: "Все уровни" },
    { value: "debug", label: "Debug" },
    { value: "info", label: "Info" },
    { value: "warning", label: "Warning" },
    { value: "error", label: "Error" },
  ];

  return (
    <div style={{ height: "100%", padding: 16 }}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 16,
        }}
      >
        <div>
          <h2 style={{ marginBottom: 4 }}>{info.displayName || info.name}</h2>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <Badge
              color={info.isEnabled ? "green" : "default"}
              style={{
                padding: "4px 12px",
                fontSize: 14,
              }}
            >
              {info.status}
            </Badge>
            <span>{info.isEnabled ? "Активен" : "Неактивен"}</span>
          </div>
        </div>
        <Button type="default" size="large" onClick={onClose}>
          ✕ Закрыть
        </Button>
      </div>

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col md={16}>
          <Card title="Информация о сервисе" style={{ height: "100%" }}>
            <p style={{ color: "#8c8c8c", marginBottom: 16 }}>
              {info.description}
            </p>

            <Row gutter={[16, 16]}>
              <Col md={12}>
                <div style={{ marginBottom: 12 }}>
                  <strong>Время старта:</strong>
                  <div style={{ color: "#8c8c8c" }}>
                    {info.startTime
                      ? new Date(info.startTime).toLocaleString()
                      : "-"}
                  </div>
                </div>
              </Col>
              <Col md={12}>
                <div style={{ marginBottom: 12 }}>
                  <strong>Последняя активность:</strong>
                  <div style={{ color: "#8c8c8c" }}>
                    {info.lastActivity
                      ? new Date(info.lastActivity).toLocaleString()
                      : "-"}
                  </div>
                </div>
              </Col>
            </Row>

            <div style={{ marginTop: 16 }}>
              <Button
                type={info.isEnabled ? "primary" : "primary"}
                size="large"
                onClick={handleToggleService}
                disabled={toggleLoading}
                style={{
                  marginRight: 12,
                  ...(info.isEnabled
                    ? { background: "#faad14", borderColor: "#faad14" }
                    : { background: "#52c41a", borderColor: "#52c41a" }),
                }}
              >
                {toggleLoading ? (
                  <>
                    <Spin size="small" style={{ marginRight: 8 }} />
                    Обработка...
                  </>
                ) : info.isEnabled ? (
                  "⏹ Остановить сервис"
                ) : (
                  "▶ Запустить сервис"
                )}
              </Button>
            </div>
          </Card>
        </Col>

        <Col md={8}>
          <Card title="Конфигурация" style={{ height: "100%" }}>
            <div style={{ maxHeight: 400, overflowY: "auto" }}>
              {configData.length > 0 ? (
                <Table
                  columns={configColumns}
                  dataSource={configData}
                  rowKey="key"
                  size="small"
                  pagination={false}
                  bordered={false}
                />
              ) : (
                <div
                  style={{
                    textAlign: "center",
                    color: "#8c8c8c",
                    padding: "16px 0",
                  }}
                >
                  <div>📄</div>
                  <div>Нет данных конфигурации</div>
                </div>
              )}
            </div>
          </Card>
        </Col>
      </Row>

      <Card title="Доступные команды" style={{ marginBottom: 16 }}>
        <div style={{ maxHeight: 300, overflowY: "auto" }}>
          {Array.isArray(commands) && commands.length > 0 ? (
            <Table
              columns={commandColumns}
              dataSource={commands}
              rowKey="name"
              pagination={false}
            />
          ) : (
            <div
              style={{
                textAlign: "center",
                color: "#8c8c8c",
                padding: "16px 0",
              }}
            >
              <div>⚙️</div>
              <div>Нет доступных команд</div>
            </div>
          )}
        </div>
      </Card>

      <Card
        title={
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <span>📋 Логи сервиса</span>
            <div
              style={{
                display: "flex",
                gap: 8,
                alignItems: "center",
              }}
            >
              <Select
                size="small"
                value={logLevel}
                onChange={value => setLogLevel(value)}
                options={logLevelOptions}
                style={{ width: 120 }}
              />
              <Checkbox
                checked={autoRefresh}
                onChange={e => setAutoRefresh(e.target.checked)}
              >
                Автообновление
              </Checkbox>
              <Button
                size="small"
                type="primary"
                ghost
                onClick={fetchLogs}
                disabled={logsLoading}
              >
                {logsLoading ? <Spin size="small" /> : "🔄 Обновить"}
              </Button>
            </div>
          </div>
        }
      >
        <div style={{ marginBottom: 12 }}>
          <Input
            placeholder="🔍 Фильтр логов..."
            value={logFilter}
            onChange={e => setLogFilter(e.target.value)}
            size="small"
          />
        </div>

        <div
          style={{
            maxHeight: 400,
            overflowY: "auto",
            backgroundColor: "#f8f9fa",
            border: "1px solid #dee2e6",
            borderRadius: 6,
            padding: 16,
            fontFamily: "monospace",
            fontSize: 14,
          }}
        >
          {logsError ? (
            <div style={{ color: "#ff4d4f" }}>❌ {logsError}</div>
          ) : logsLoading ? (
            <div style={{ textAlign: "center" }}>
              <Spin size="small" />
              <span style={{ marginLeft: 8 }}>Загрузка логов...</span>
            </div>
          ) : filteredLogs.length > 0 ? (
            <div>
              {filteredLogs.map((log, index) => (
                <div
                  key={index}
                  style={{
                    marginBottom: 4,
                    borderBottom:
                      index < filteredLogs.length - 1
                        ? "1px solid #e9ecef"
                        : "none",
                    paddingBottom: index < filteredLogs.length - 1 ? 8 : 0,
                  }}
                >
                  {log}
                </div>
              ))}
              <div style={{ color: "#8c8c8c", marginTop: 8 }}>
                Показано {filteredLogs.length} из {logs.length} записей
                {logFilter && ` (отфильтровано по "${logFilter}")`}
              </div>
            </div>
          ) : (
            <div
              style={{
                textAlign: "center",
                color: "#8c8c8c",
                padding: "16px 0",
              }}
            >
              <div>📝</div>
              <div>
                {logFilter
                  ? "Нет логов, соответствующих фильтру"
                  : "Нет доступных логов"}
              </div>
            </div>
          )}
        </div>
      </Card>

      {execResult && (
        <Alert
          type="info"
          message={execResult}
          closable
          onClose={() => setExecResult(null)}
          style={{ marginTop: 12 }}
        />
      )}
    </div>
  );
};

export default ServiceDetails;
