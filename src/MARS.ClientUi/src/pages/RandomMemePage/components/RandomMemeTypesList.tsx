import { Alert, Button, Card, Flex, Spin } from "antd";
import { Edit, Eye, Folder, Plus, RefreshCw, Trash2 } from "lucide-react";

import { RandomMemeTypesListProps as RandomMemeTypesListProperties } from "../RandomMemePage.types";

const RandomMemeTypesList: React.FC<RandomMemeTypesListProperties> = ({
  memeTypes,
  isLoading,
  error,
  onRefresh,
  onViewDetails,
  onEdit,
  onDelete,
  onCreate,
}) => {
  if (isLoading && memeTypes.length === 0) {
    return (
      <div style={{ textAlign: "center", padding: "40px 0" }}>
        <Spin size="large" />
        <h4 style={{ marginTop: 12 }}>Загрузка типов мемов...</h4>
      </div>
    );
  }

  return (
    <div style={{ padding: "16px 0" }}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 12 }}>
        <h1 style={{ marginBottom: 0 }}>Типы мемов</h1>
        <Flex gap={8}>
          <Button
            onClick={onRefresh}
            disabled={isLoading}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <RefreshCw size={16} />
            Обновить
          </Button>
          <Button
            type="primary"
            onClick={onCreate}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Plus size={16} />
            Добавить тип
          </Button>
        </Flex>
      </Flex>

      {error && (
        <Alert
          type="error"
          showIcon
          message="Ошибка загрузки"
          description={error}
          style={{ marginBottom: 12 }}
        />
      )}

      {memeTypes.length === 0 ? (
        <Card style={{ textAlign: "center" }}>
          <Folder size={64} style={{ color: "#8c8c8c", marginBottom: 12 }} />
          <h5 style={{ marginBottom: 4 }}>Типы мемов не найдены</h5>
          <div style={{ color: "#8c8c8c" }}>Создайте первый тип мема</div>
        </Card>
      ) : (
        <Flex wrap="wrap" gap={12}>
          {memeTypes.map(type => (
            <div
              key={type.id}
              style={{
                flex: "0 0 calc(25% - 9px)",
                minWidth: 220,
              }}
            >
              <Card
                hoverable
                style={{ height: "100%", cursor: "pointer" }}
                onClick={() => onViewDetails(type)}
              >
                <Flex align="center" style={{ marginBottom: 12 }}>
                  <div style={{ marginRight: 12 }}>
                    <Folder size={32} style={{ color: "#1677ff" }} />
                  </div>
                  <div style={{ flex: 1 }}>
                    <h6 style={{ marginBottom: 2, fontWeight: 600 }}>
                      {type.name}
                    </h6>
                    <span style={{ color: "#8c8c8c", fontSize: 12 }}>
                      ID: {type.id}
                    </span>
                  </div>
                </Flex>

                <div style={{ marginBottom: 12 }}>
                  <span
                    style={{ color: "#8c8c8c", fontSize: 12, display: "block" }}
                  >
                    Папка
                  </span>
                  <code
                    style={{
                      display: "block",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                      backgroundColor: "var(--bs-light)",
                      padding: "0.25rem 0.5rem",
                      borderRadius: "0.25rem",
                      fontSize: "0.8rem",
                    }}
                    title={type.folderPath}
                  >
                    {type.folderPath}
                  </code>
                </div>

                <Flex gap={4}>
                  <Button
                    size="small"
                    onClick={e => {
                      e.stopPropagation();
                      onViewDetails(type);
                    }}
                    title="Просмотр"
                  >
                    <Eye size={14} />
                  </Button>
                  <Button
                    size="small"
                    onClick={e => {
                      e.stopPropagation();
                      onEdit(type);
                    }}
                    title="Редактировать"
                  >
                    <Edit size={14} />
                  </Button>
                  <Button
                    danger
                    size="small"
                    onClick={e => {
                      e.stopPropagation();
                      onDelete(type);
                    }}
                    title="Удалить"
                  >
                    <Trash2 size={14} />
                  </Button>
                </Flex>
              </Card>
            </div>
          ))}
        </Flex>
      )}
    </div>
  );
};

export default RandomMemeTypesList;
