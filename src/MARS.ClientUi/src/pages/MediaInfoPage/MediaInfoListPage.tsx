import "./MediaInfoPage.scss";

import { Alert, Button, Flex, Input, Select, Spin, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { ApiMediaInfo, MediaInfoApi } from "@/shared/api";

import {
  formatMediaDuration,
  formatMediaRewardId,
  mediaInfoFileTypes,
  mediaInfoPriorities,
} from "./mediaInfoPageHelpers";

type MediaInfoSortField =
  | "metaInfo.displayName"
  | "fileInfo.filePath"
  | "fileInfo.type"
  | "metaInfo.duration"
  | "metaInfo.priority"
  | "metaInfo.twitchPointsCost";

type MediaInfoFilters = {
  search: string;
  type: string;
  priority: string;
  localFile: string;
  rewardState: string;
};

const defaultFilters: MediaInfoFilters = {
  search: "",
  type: "",
  priority: "",
  localFile: "",
  rewardState: "",
};

const columnLabels: Record<MediaInfoSortField, string> = {
  "metaInfo.displayName": "Название",
  "fileInfo.filePath": "Путь",
  "fileInfo.type": "Тип",
  "metaInfo.duration": "Длительность",
  "metaInfo.priority": "Приоритет",
  "metaInfo.twitchPointsCost": "Баллы",
};

const typeTagColor: Record<string, string> = {
  Image: "cyan",
  Audio: "purple",
  Video: "blue",
  Gif: "magenta",
  TelegramSticker: "orange",
  None: "default",
};

const priorityTagColor: Record<string, string> = {
  Low: "default",
  Normal: "blue",
  High: "red",
};

export const MediaInfoListPage: React.FC = () => {
  const [items, setItems] = useState<ApiMediaInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<MediaInfoFilters>(defaultFilters);
  const [sortField, setSortField] = useState<MediaInfoSortField>(
    "metaInfo.displayName"
  );
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");

  const mediaInfoApi = useMemo(() => new MediaInfoApi(), []);

  const loadItems = useCallback(async () => {
    setLoading(true);

    try {
      const response = await mediaInfoApi.mediaInfoApiList();
      const payload = response.data;

      if (payload.success) {
        setItems(payload.data ?? []);
        setError(null);
      } else {
        setItems([]);
        setError(payload.message ?? "Не удалось загрузить список медиа");
      }
    } catch (loadError) {
      setError(
        loadError instanceof Error
          ? loadError.message
          : "Не удалось загрузить список медиа"
      );
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [mediaInfoApi]);

  useEffect(() => {
    loadItems();
  }, [loadItems]);

  const filteredItems = useMemo(() => {
    const search = filters.search.trim().toLowerCase();

    return items.filter(item => {
      const rewardId = item.metaInfo.twitchGuid ?? "";
      const searchable = [
        item.metaInfo.displayName,
        item.fileInfo.fileName,
        item.fileInfo.filePath,
        item.textInfo.triggerWord,
        rewardId,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      if (search && !searchable.includes(search)) {
        return false;
      }

      if (filters.type && item.fileInfo.type !== filters.type) {
        return false;
      }

      if (filters.priority && item.metaInfo.priority !== filters.priority) {
        return false;
      }

      if (filters.localFile === "true" && item.fileInfo.isLocalFile !== true) {
        return false;
      }

      if (
        filters.localFile === "false" &&
        item.fileInfo.isLocalFile !== false
      ) {
        return false;
      }

      if (filters.rewardState === "linked" && !rewardId) {
        return false;
      }

      return !(filters.rewardState === "unlinked" && rewardId);
    });
  }, [filters, items]);

  const sortedItems = useMemo(() => {
    const sorted = [...filteredItems].sort((left, right) => {
      const leftValue = getSortValue(left, sortField);
      const rightValue = getSortValue(right, sortField);

      if (leftValue < rightValue) {
        return sortDirection === "asc" ? -1 : 1;
      }

      if (leftValue > rightValue) {
        return sortDirection === "asc" ? 1 : -1;
      }

      return 0;
    });

    return sorted;
  }, [filteredItems, sortDirection, sortField]);

  const handleSort = useCallback((field: MediaInfoSortField) => {
    setSortField(previousField => {
      if (previousField === field) {
        setSortDirection(previousDirection =>
          previousDirection === "asc" ? "desc" : "asc"
        );
        return previousField;
      }

      setSortDirection("asc");
      return field;
    });
  }, []);

  const handleDelete = useCallback(
    async (item: ApiMediaInfo) => {
      const confirmDelete = globalThis.confirm(
        `Удалить медиа «${item.metaInfo.displayName || item.fileInfo.fileName}»?`
      );

      if (!confirmDelete) {
        return;
      }

      try {
        const response = await mediaInfoApi.mediaInfoApiDelete(item.id);
        if (response.data.success) {
          setItems(previous =>
            previous.filter(current => current.id !== item.id)
          );
          setError(null);
        } else {
          setError(response.data.message ?? "Не удалось удалить запись");
        }
      } catch (deleteError) {
        setError(
          deleteError instanceof Error
            ? deleteError.message
            : "Не удалось удалить запись"
        );
      }
    },
    [mediaInfoApi]
  );

  const resetFilters = useCallback(() => {
    setFilters(defaultFilters);
  }, []);

  const activeFiltersCount = useMemo(
    () => Object.values(filters).filter(value => value.length > 0).length,
    [filters]
  );

  const tableColumns: ColumnsType<ApiMediaInfo> = useMemo(
    () => [
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "metaInfo.displayName" ? "active" : ""}`}
            onClick={() => handleSort("metaInfo.displayName")}
          >
            <span>{columnLabels["metaInfo.displayName"]}</span>
            <span className="sort-icon">
              {sortField === "metaInfo.displayName"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["metaInfo", "displayName"],
        key: "displayName",
        render: (_: unknown, record: ApiMediaInfo) => (
          <div className="title-cell" data-testid={`cell-title-${record.id}`}>
            <strong>{record.metaInfo.displayName || "Без названия"}</strong>
            <span>{record.fileInfo.fileName || "Без файла"}</span>
            {record.textInfo.triggerWord ? (
              <span className="muted-line">
                Триггер: {record.textInfo.triggerWord}
              </span>
            ) : null}
          </div>
        ),
      },
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "fileInfo.filePath" ? "active" : ""}`}
            onClick={() => handleSort("fileInfo.filePath")}
          >
            <span>{columnLabels["fileInfo.filePath"]}</span>
            <span className="sort-icon">
              {sortField === "fileInfo.filePath"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["fileInfo", "filePath"],
        key: "filePath",
        render: (_: unknown, record: ApiMediaInfo) => (
          <div
            className="muted-line path-cell"
            data-testid={`cell-path-${record.id}`}
          >
            {record.fileInfo.filePath || "—"}
          </div>
        ),
      },
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "fileInfo.type" ? "active" : ""}`}
            onClick={() => handleSort("fileInfo.type")}
          >
            <span>{columnLabels["fileInfo.type"]}</span>
            <span className="sort-icon">
              {sortField === "fileInfo.type"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["fileInfo", "type"],
        key: "type",
        render: (_: unknown, record: ApiMediaInfo) => (
          <Tag
            color={typeTagColor[record.fileInfo.type] || "default"}
            data-testid={`tag-type-${record.id}`}
          >
            {record.fileInfo.type}
          </Tag>
        ),
      },
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "metaInfo.duration" ? "active" : ""}`}
            onClick={() => handleSort("metaInfo.duration")}
          >
            <span>{columnLabels["metaInfo.duration"]}</span>
            <span className="sort-icon">
              {sortField === "metaInfo.duration"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["metaInfo", "duration"],
        key: "duration",
        render: (duration: number) => formatMediaDuration(duration),
      },
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "metaInfo.priority" ? "active" : ""}`}
            onClick={() => handleSort("metaInfo.priority")}
          >
            <span>{columnLabels["metaInfo.priority"]}</span>
            <span className="sort-icon">
              {sortField === "metaInfo.priority"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["metaInfo", "priority"],
        key: "priority",
        render: (_: unknown, record: ApiMediaInfo) => (
          <Tag
            color={priorityTagColor[record.metaInfo.priority] || "default"}
            data-testid={`tag-priority-${record.id}`}
          >
            {record.metaInfo.priority}
          </Tag>
        ),
      },
      {
        title: (
          <button
            type="button"
            className={`sort-button ${sortField === "metaInfo.twitchPointsCost" ? "active" : ""}`}
            onClick={() => handleSort("metaInfo.twitchPointsCost")}
          >
            <span>{columnLabels["metaInfo.twitchPointsCost"]}</span>
            <span className="sort-icon">
              {sortField === "metaInfo.twitchPointsCost"
                ? sortDirection === "asc"
                  ? "↑"
                  : "↓"
                : "↕"}
            </span>
          </button>
        ),
        dataIndex: ["metaInfo", "twitchPointsCost"],
        key: "points",
      },
      {
        title: "Состояние",
        key: "status",
        render: (_: unknown, record: ApiMediaInfo) => (
          <div
            className="status-stack"
            data-testid={`cell-status-${record.id}`}
          >
            <Tag color={record.fileInfo.isLocalFile ? "cyan" : "default"}>
              {record.fileInfo.isLocalFile ? "Локальный файл" : "Внешний файл"}
            </Tag>
            <Tag color={record.metaInfo.twitchGuid ? "green" : "default"}>
              {record.metaInfo.twitchGuid
                ? formatMediaRewardId(record.metaInfo.twitchGuid)
                : "Награда не привязана"}
            </Tag>
            {record.metaInfo.isFreezeRequired && (
              <Tag color="orange">Freeze</Tag>
            )}
          </div>
        ),
      },
      {
        title: "Действия",
        key: "actions",
        render: (_: unknown, record: ApiMediaInfo) => (
          <Flex gap={8} wrap="wrap" data-testid={`cell-actions-${record.id}`}>
            <Link to={`/media-info/edit/${record.id}`}>
              <Button type="primary" size="small">
                Редактировать
              </Button>
            </Link>
            <Button
              danger
              size="small"
              onClick={() => handleDelete(record)}
              data-testid={`button-delete-${record.id}`}
            >
              Удалить
            </Button>
          </Flex>
        ),
      },
    ],
    [sortField, sortDirection, handleSort, handleDelete]
  );

  return (
    <div
      className="media-info-page media-info-list-page"
      data-testid="media-info-list-page"
    >
      <section className="media-info-hero">
        <div className="hero-copy">
          <p className="eyebrow">Media info manager</p>
          <h1>Медиа-алерты и кастомные награды</h1>
          <p className="hero-description">
            Таблица с поиском, фильтрами и быстрыми действиями для создания,
            редактирования и удаления записей.
          </p>
        </div>

        <div className="hero-actions">
          <Link to="/media-info/create">
            <Button
              type="primary"
              className="hero-button"
              data-testid="button-create"
            >
              Создать запись
            </Button>
          </Link>
          <Button
            className="hero-button"
            onClick={loadItems}
            data-testid="button-refresh"
          >
            Обновить список
          </Button>
        </div>
      </section>

      <section className="media-info-toolbar card-shell">
        <Flex
          gap={14}
          align="flex-end"
          wrap="wrap"
          style={{ marginBottom: 18 }}
        >
          <div style={{ flex: 1, minWidth: 200 }}>
            <Input.Search
              value={filters.search}
              onChange={event =>
                setFilters(previous => ({
                  ...previous,
                  search: event.target.value,
                }))
              }
              placeholder="Название, путь, триггер, reward id"
              allowClear
              data-testid="input-search"
            />
          </div>

          <Button onClick={resetFilters} data-testid="button-reset-filters">
            Сбросить фильтры
          </Button>
        </Flex>

        <Flex gap={14} wrap="wrap" style={{ marginBottom: 16 }}>
          <div style={{ flex: 1, minWidth: 150 }}>
            <Select
              value={filters.type || undefined}
              onChange={value =>
                setFilters(previous => ({
                  ...previous,
                  type: value ?? "",
                }))
              }
              placeholder="Тип"
              allowClear
              style={{ width: "100%" }}
              options={[
                { label: "Все", value: "" },
                ...mediaInfoFileTypes.map(type => ({
                  label: type,
                  value: type,
                })),
              ]}
              data-testid="select-type-filter"
            />
          </div>

          <div style={{ flex: 1, minWidth: 150 }}>
            <Select
              value={filters.priority || undefined}
              onChange={value =>
                setFilters(previous => ({
                  ...previous,
                  priority: value ?? "",
                }))
              }
              placeholder="Приоритет"
              allowClear
              style={{ width: "100%" }}
              options={[
                { label: "Все", value: "" },
                ...mediaInfoPriorities.map(priority => ({
                  label: priority,
                  value: priority,
                })),
              ]}
              data-testid="select-priority-filter"
            />
          </div>

          <div style={{ flex: 1, minWidth: 150 }}>
            <Select
              value={filters.localFile || undefined}
              onChange={value =>
                setFilters(previous => ({
                  ...previous,
                  localFile: value ?? "",
                }))
              }
              placeholder="Файл"
              allowClear
              style={{ width: "100%" }}
              options={[
                { label: "Все", value: "" },
                { label: "Локальные", value: "true" },
                { label: "Внешние", value: "false" },
              ]}
              data-testid="select-local-filter"
            />
          </div>

          <div style={{ flex: 1, minWidth: 150 }}>
            <Select
              value={filters.rewardState || undefined}
              onChange={value =>
                setFilters(previous => ({
                  ...previous,
                  rewardState: value ?? "",
                }))
              }
              placeholder="Награда"
              allowClear
              style={{ width: "100%" }}
              options={[
                { label: "Все", value: "" },
                { label: "Привязана", value: "linked" },
                { label: "Не привязана", value: "unlinked" },
              ]}
              data-testid="select-reward-filter"
            />
          </div>
        </Flex>

        <Flex gap={10} wrap="wrap" className="toolbar-summary">
          <span>Всего: {items.length}</span>
          <span>Показано: {sortedItems.length}</span>
          <span>Активных фильтров: {activeFiltersCount}</span>
        </Flex>
      </section>

      {error && (
        <Alert
          type="error"
          message={`Ошибка: ${error}`}
          closable
          onClose={() => setError(null)}
          style={{ marginBottom: 18, borderRadius: 18 }}
          data-testid="error-alert"
        />
      )}

      <section className="media-info-table-shell card-shell">
        {loading ? (
          <div className="state-block">
            <Spin size="large" data-testid="loading-spinner" />
            <p>Загружаем медиа...</p>
          </div>
        ) : sortedItems.length === 0 ? (
          <div className="state-block empty-state">
            <h2>Ничего не найдено</h2>
            <p>Измени фильтры или создай новую запись.</p>
            <div className="empty-actions">
              <Link to="/media-info/create">
                <Button type="primary" data-testid="button-create-empty">
                  Создать запись
                </Button>
              </Link>
              <Button
                onClick={resetFilters}
                data-testid="button-reset-filters-empty"
              >
                Сбросить фильтры
              </Button>
            </div>
          </div>
        ) : (
          <div className="table-scroll">
            <Table<ApiMediaInfo>
              columns={tableColumns}
              dataSource={sortedItems}
              rowKey="id"
              pagination={false}
              size="middle"
              data-testid="media-info-table"
            />
          </div>
        )}
      </section>
    </div>
  );
};

function getSortValue(
  item: ApiMediaInfo,
  field: MediaInfoSortField
): string | number {
  switch (field) {
    case "fileInfo.filePath": {
      return item.fileInfo.filePath || "";
    }
    case "fileInfo.type": {
      return item.fileInfo.type || "";
    }
    case "metaInfo.duration": {
      return item.metaInfo.duration;
    }
    case "metaInfo.priority": {
      return item.metaInfo.priority || "";
    }
    case "metaInfo.twitchPointsCost": {
      return item.metaInfo.twitchPointsCost ?? 0;
    }
    case "metaInfo.displayName":
    default: {
      return item.metaInfo.displayName || "";
    }
  }
}
