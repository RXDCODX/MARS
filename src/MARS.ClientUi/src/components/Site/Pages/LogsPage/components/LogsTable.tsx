import { Button, Modal, Spin, Table } from "antd";
import { Download, Eye, List, Table as TableIcon } from "lucide-react";
import { useState } from "react";

import styles from "../LogsPage.module.scss";
import { Log, LogsTableProps as LogsTableProperties } from "../LogsPage.types";

const LogsTable: React.FC<LogsTableProperties> = ({
  logs,
  isLoading,
  currentPage,
  pageSize,
  totalPages,
  totalCount,
  onPageChange,
  onPageSizeChange,
}) => {
  const [selectedLog, setSelectedLog] = useState<Log | null>(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [viewMode, setViewMode] = useState<"table" | "text">("table");

  const formatTimestamp = (timestamp: string) =>
    new Date(timestamp).toLocaleString("ru-RU", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      timeZone: "UTC",
    });

  const formatLogForText = (log: Log) => {
    const timestamp = formatTimestamp(log.whenLogged);
    return `[${log.logLevel}](${timestamp}): ${log.message}`;
  };

  const formatAllLogsAsText = () =>
    logs.map(log => formatLogForText(log)).join("\n");

  const getLogLevelBadge = (level: string) => {
    const levelClass = level.toLowerCase();
    return <span className={`${styles.logLevel} ${levelClass}`}>{level}</span>;
  };

  const handleViewDetails = (log: Log) => {
    setSelectedLog(log);
    setShowDetailsModal(true);
  };

  const handleCloseModal = () => {
    setShowDetailsModal(false);
    setSelectedLog(null);
  };

  const handleExportLog = (log: Log) => {
    const logData = {
      timestamp: log.whenLogged,
      level: log.logLevel,
      message: log.message,
      stackTrace: log.stackTrace,
    };

    const blob = new Blob([JSON.stringify(logData, null, 2)], {
      type: "application/json",
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `log-${log.id}.json`;
    document.body.append(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  };

  if (isLoading) {
    return (
      <div className={styles.logsTableCard}>
        <div className={styles.loadingSpinner}>
          <Spin tip="Загрузка логов..." />
        </div>
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className={styles.logsTableCard}>
        <div className={styles.emptyState}>
          <div className={styles.icon}>📋</div>
          <h4>Логи не найдены</h4>
          <p>Попробуйте изменить параметры поиска или фильтры</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className={styles.logsTableCard}>
        <div className={styles.tableHeader}>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <h3>Логи приложения ({totalCount.toLocaleString()})</h3>
            <div style={{ display: "flex", gap: 8 }}>
              <Button
                type={viewMode === "table" ? "primary" : "default"}
                size="small"
                onClick={() => setViewMode("table")}
                style={{ display: "flex", alignItems: "center", gap: 4 }}
              >
                <TableIcon size={16} />
                Таблица
              </Button>
              <Button
                type={viewMode === "text" ? "primary" : "default"}
                size="small"
                onClick={() => setViewMode("text")}
                style={{ display: "flex", alignItems: "center", gap: 4 }}
              >
                <List size={16} />
                Текст
              </Button>
            </div>
          </div>
        </div>

        {viewMode === "table" ? (
          <div style={{ overflowX: "auto" }}>
            <Table
              className={styles.logsTable}
              dataSource={logs}
              rowKey="id"
              pagination={false}
              columns={[
                {
                  title: "Время",
                  dataIndex: "whenLogged",
                  key: "whenLogged",
                  render: (value: string) => (
                    <span className={styles.timestamp}>
                      {formatTimestamp(value)}
                    </span>
                  ),
                },
                {
                  title: "Уровень",
                  dataIndex: "logLevel",
                  key: "logLevel",
                  render: (value: string) => getLogLevelBadge(value),
                },
                {
                  title: "Сообщение",
                  dataIndex: "message",
                  key: "message",
                  render: (_: string, record: Log) => (
                    <div className={styles.message}>
                      <div style={{ maxWidth: "400px" }}>
                        {record.message}
                        {record.stackTrace && (
                          <div style={{ marginTop: 8 }}>
                            <small style={{ color: "#999" }}>
                              Есть стек-трейс
                            </small>
                          </div>
                        )}
                      </div>
                    </div>
                  ),
                },
                {
                  title: "Действия",
                  key: "actions",
                  render: (_: unknown, record: Log) => (
                    <div style={{ display: "flex", gap: 8 }}>
                      <Button
                        size="small"
                        onClick={() => handleViewDetails(record)}
                        title="Просмотреть детали"
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 4,
                        }}
                      >
                        <Eye size={14} />
                        Детали
                      </Button>

                      <Button
                        size="small"
                        onClick={() => handleExportLog(record)}
                        title="Экспортировать лог"
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 4,
                          color: "#52c41a",
                          borderColor: "#52c41a",
                        }}
                      >
                        <Download size={14} />
                        Экспорт
                      </Button>
                    </div>
                  ),
                },
              ]}
            />
          </div>
        ) : (
          <div style={{ padding: 12 }}>
            <textarea
              className="form-control"
              style={{
                height: "400px",
                fontFamily: "monospace",
                fontSize: "0.9rem",
                backgroundColor: "var(--site-bg-primary)",
                color: "var(--site-text-primary)",
                border: "1px solid var(--site-border-primary)",
              }}
              value={formatAllLogsAsText()}
              readOnly
              placeholder="Логи будут отображены здесь..."
            />
            <div
              style={{
                marginTop: 8,
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
              }}
            >
              <small style={{ color: "#999" }}>
                Показано {logs.length} из {totalCount.toLocaleString()} записей
              </small>
              <Button
                size="small"
                onClick={() => {
                  const textContent = formatAllLogsAsText();
                  const blob = new Blob([textContent], { type: "text/plain" });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement("a");
                  a.href = url;
                  a.download = `logs-${new Date().toISOString().slice(0, 10)}.txt`;
                  document.body.append(a);
                  a.click();
                  a.remove();
                  URL.revokeObjectURL(url);
                }}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 4,
                  color: "#52c41a",
                  borderColor: "#52c41a",
                }}
              >
                <Download size={14} />
                Экспорт в TXT
              </Button>
            </div>
          </div>
        )}

        <div className={styles.paginationContainer}>
          <div className={styles.paginationInfo}>
            Показано {(currentPage - 1) * pageSize + 1} -{" "}
            {Math.min(currentPage * pageSize, totalCount)} из{" "}
            {totalCount.toLocaleString()} записей
          </div>

          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <span>Страница:</span>
            <select
              value={currentPage}
              onChange={e => onPageChange(Number(e.target.value))}
              className="form-select form-select-sm"
              style={{ width: "auto" }}
            >
              {Array.from({ length: totalPages }, (_, index) => index + 1).map(
                page => (
                  <option key={page} value={page}>
                    {page}
                  </option>
                )
              )}
            </select>

            <span>из {totalPages}</span>
          </div>

          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <span>Записей на странице:</span>
            <select
              value={pageSize}
              onChange={e => onPageSizeChange(Number(e.target.value))}
              className="form-select form-select-sm"
              style={{ width: "auto" }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
        </div>
      </div>

      <Modal
        open={showDetailsModal}
        onCancel={handleCloseModal}
        width={800}
        centered
        title="Детали лога"
        footer={
          <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
            <Button onClick={handleCloseModal}>Закрыть</Button>
            {selectedLog && (
              <Button
                type="primary"
                onClick={() => {
                  handleExportLog(selectedLog);
                  handleCloseModal();
                }}
                style={{ display: "flex", alignItems: "center", gap: 8 }}
              >
                <Download size={16} />
                Экспортировать
              </Button>
            )}
          </div>
        }
      >
        {selectedLog && (
          <div>
            <div style={{ marginBottom: 16, display: "flex" }}>
              <div style={{ width: "25%", fontWeight: "bold" }}>
                <strong>ID:</strong>
              </div>
              <div style={{ width: "75%" }}>
                <code>{selectedLog.id}</code>
              </div>
            </div>

            <div style={{ marginBottom: 16, display: "flex" }}>
              <div style={{ width: "25%", fontWeight: "bold" }}>
                <strong>Время:</strong>
              </div>
              <div style={{ width: "75%" }}>
                {formatTimestamp(selectedLog.whenLogged)}
              </div>
            </div>

            <div style={{ marginBottom: 16, display: "flex" }}>
              <div style={{ width: "25%", fontWeight: "bold" }}>
                <strong>Уровень:</strong>
              </div>
              <div style={{ width: "75%" }}>
                {getLogLevelBadge(selectedLog.logLevel)}
              </div>
            </div>

            <div style={{ marginBottom: 16, display: "flex" }}>
              <div style={{ width: "25%", fontWeight: "bold" }}>
                <strong>Сообщение:</strong>
              </div>
              <div style={{ width: "75%" }}>
                <div
                  style={{
                    padding: 12,
                    backgroundColor: "var(--site-bg-secondary)",
                    borderRadius: 4,
                  }}
                >
                  {selectedLog.message}
                </div>
              </div>
            </div>

            {selectedLog.stackTrace && (
              <div style={{ marginBottom: 16, display: "flex" }}>
                <div style={{ width: "25%", fontWeight: "bold" }}>
                  <strong>Стек-трейс:</strong>
                </div>
                <div style={{ width: "75%" }}>
                  <pre className={styles.stackTrace}>
                    {selectedLog.stackTrace}
                  </pre>
                </div>
              </div>
            )}
          </div>
        )}
      </Modal>
    </>
  );
};

export default LogsTable;
