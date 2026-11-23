import { Badge, Card, Spin, Table } from "antd";
import { FileText, KeyRound } from "lucide-react";

import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "../EnvironmentVariablesPage.module.scss";
import {
  selectFilteredVariables,
  useEnvironmentVariablesStore,
} from "../store/useEnvironmentVariablesStore";
import { formatDateTime } from "../utils/formatDateTime";

const EnvironmentVariablesTable: React.FC = () => {
  const colors = useSiteColors();
  const filteredVariables = useEnvironmentVariablesStore(
    selectFilteredVariables
  );
  const isLoading = useEnvironmentVariablesStore(state => state.isLoading);
  const searchQuery = useEnvironmentVariablesStore(state => state.searchQuery);
  const selectedKey = useEnvironmentVariablesStore(state => state.selectedKey);
  const selectVariable = useEnvironmentVariablesStore(
    state => state.selectVariable
  );

  const columns = [
    {
      title: "Ключ",
      dataIndex: "key",
      key: "key",
      render: (key: string) => (
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <KeyRound size={16} />
          <span>{key}</span>
        </div>
      ),
    },
    {
      title: "Описание",
      dataIndex: "description",
      key: "description",
      render: (description: string) => description || "—",
    },
    {
      title: "Значение",
      dataIndex: "value",
      key: "value",
      render: (value: string) => (
        <span className={styles.valuePreview}>{value || "—"}</span>
      ),
    },
    {
      title: "Обновлено",
      dataIndex: "updatedAt",
      key: "updatedAt",
      render: (updatedAt: string) => (
        <span className={styles.timestamp}>{formatDateTime(updatedAt)}</span>
      ),
    },
    {
      title: "Создано",
      dataIndex: "createdAt",
      key: "createdAt",
      render: (createdAt: string) => (
        <span className={styles.timestamp}>{formatDateTime(createdAt)}</span>
      ),
    },
  ];

  return (
    <Card
      className={styles.tableCard}
      style={{
        backgroundColor: colors.background.card,
        borderColor: colors.border.primary,
      }}
    >
      <div
        className={styles.tableHeader}
        style={{
          backgroundColor: colors.background.secondary,
          borderColor: colors.border.primary,
        }}
      >
        <h2>Список переменных</h2>
        <Badge
          count={filteredVariables.length}
          style={{ backgroundColor: "#1677ff" }}
        />
      </div>
      <div className={styles.tableWrapper}>
        {isLoading ? (
          <div
            style={{
              display: "flex",
              justifyContent: "center",
              padding: "2rem 0",
            }}
          >
            <Spin tip="Загрузка переменных..." />
          </div>
        ) : filteredVariables.length === 0 ? (
          <div className={styles.emptyState}>
            <FileText size={48} />
            <h4>Переменные не найдены</h4>
            <p>
              {searchQuery
                ? `По запросу "${searchQuery}" ничего не найдено. Попробуйте изменить критерии поиска.`
                : "Создайте первую переменную окружения, чтобы она появилась в списке."}
            </p>
          </div>
        ) : (
          <Table
            dataSource={filteredVariables.map(v => ({ ...v, id: v.key }))}
            columns={columns}
            rowKey="key"
            pagination={false}
            onRow={record => ({
              onClick: () => selectVariable(record.key),
              style: {
                cursor: "pointer",
                backgroundColor:
                  selectedKey === record.key
                    ? "var(--chakra-colors-blue-50, #e6f4ff)"
                    : undefined,
              },
            })}
            className={styles.table}
          />
        )}
      </div>
    </Card>
  );
};

export default EnvironmentVariablesTable;
