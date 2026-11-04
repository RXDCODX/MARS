import { Alert, Button, Card, Flex, Input, Select, Spin } from "antd";
import {
  Copy,
  Edit,
  Eye,
  Filter,
  Plus,
  RefreshCw,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";

import { RandomMemeOrdersListProps as RandomMemeOrdersListProperties } from "../RandomMemePage.types";

const RandomMemeList: React.FC<RandomMemeOrdersListProperties> = ({
  memeOrders,
  memeTypes,
  isLoading,
  error,
  onRefresh,
  onViewDetails,
  onEdit,
  onDelete,
  onCreate,
  showToast,
}) => {
  const [searchParameters, setSearchParameters] = useSearchParams();

  const getInitialTypeId = (): number | "all" | "no-type" => {
    const typeParameter = searchParameters.get("type");
    if (typeParameter === "no-type") return "no-type";
    if (typeParameter && !Number.isNaN(Number(typeParameter)))
      return Number(typeParameter);
    return "all";
  };

  const getInitialSearchTerm = (): string =>
    searchParameters.get("search") || "";

  const [selectedTypeId, setSelectedTypeId] = useState<
    number | "all" | "no-type"
  >(getInitialTypeId);
  const [searchTerm, setSearchTerm] = useState<string>(getInitialSearchTerm);
  const [searchInput, setSearchInput] = useState<string>(getInitialSearchTerm);

  const updateTypeFilter = (typeId: number | "all" | "no-type") => {
    setSelectedTypeId(typeId);
    const newSearchParameters = new URLSearchParams(searchParameters);

    if (typeId === "all") {
      newSearchParameters.delete("type");
    } else if (typeId === "no-type") {
      newSearchParameters.set("type", "no-type");
    } else {
      newSearchParameters.set("type", typeId.toString());
    }

    setSearchParameters(newSearchParameters, { replace: true });
  };

  const updateSearchFilter = (search: string) => {
    setSearchTerm(search);
    const newSearchParameters = new URLSearchParams(searchParameters);

    if (search.trim()) {
      newSearchParameters.set("search", search.trim());
    } else {
      newSearchParameters.delete("search");
    }

    setSearchParameters(newSearchParameters, { replace: true });
  };

  const resetFilters = () => {
    setSelectedTypeId("all");
    setSearchTerm("");
    setSearchInput("");
    setSearchParameters({}, { replace: true });
  };

  const copyFilteredLink = async () => {
    const currentUrl = location.href;
    try {
      await navigator.clipboard.writeText(currentUrl);
      showToast?.({
        success: true,
        message: "Ссылка с примененными фильтрами скопирована в буфер обмена",
      });
    } catch (error_) {
      console.error("Ошибка копирования ссылки:", error_);
      showToast?.({
        success: false,
        message: "Не удалось скопировать ссылку в буфер обмена",
      });
    }
  };

  const hasActiveFilters = selectedTypeId !== "all" || searchTerm.trim() !== "";

  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchInput !== searchTerm) {
        updateSearchFilter(searchInput);
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    const typeParameter = searchParameters.get("type");
    const searchParameter = searchParameters.get("search");

    if (typeParameter !== null) {
      const newTypeId =
        typeParameter === "no-type"
          ? "no-type"
          : Number.isNaN(Number(typeParameter))
            ? "all"
            : Number(typeParameter);
      if (newTypeId !== selectedTypeId) {
        setSelectedTypeId(newTypeId);
      }
    } else if (selectedTypeId !== "all") {
      setSelectedTypeId("all");
    }

    if (searchParameter !== null && searchParameter !== searchTerm) {
      setSearchTerm(searchParameter);
      setSearchInput(searchParameter);
    } else if (searchParameter === null && searchTerm !== "") {
      setSearchTerm("");
      setSearchInput("");
    }
  }, [searchParameters, selectedTypeId, searchTerm]);

  const filteredOrders = useMemo(() => {
    let filtered = memeOrders;

    if (selectedTypeId !== "all") {
      filtered =
        selectedTypeId === "no-type"
          ? filtered.filter(
              order => !order.memeTypeId || order.memeTypeId === 0
            )
          : filtered.filter(order => order.memeTypeId === selectedTypeId);
    }

    if (searchTerm.trim()) {
      const search = searchTerm.toLowerCase();
      filtered = filtered.filter(
        order =>
          order.filePath.toLowerCase().includes(search) ||
          order.type?.name?.toLowerCase().includes(search) ||
          order.order.toString().includes(search)
      );
    }

    return filtered;
  }, [memeOrders, selectedTypeId, searchTerm]);

  const ordersWithoutType = useMemo(
    () =>
      memeOrders.filter(order => !order.memeTypeId || order.memeTypeId === 0)
        .length,
    [memeOrders]
  );
  const getFileName = (filePath: string) => {
    const parts = filePath.split(/[/\\]/);
    return parts.at(-1) || filePath;
  };

  const typeFilterOptions = [
    { label: "Все типы", value: "all" },
    {
      label: `Без типа (${ordersWithoutType})`,
      value: "no-type",
    },
    ...memeTypes.map(type => {
      const typeCount = memeOrders.filter(
        order => order.memeTypeId === type.id
      ).length;
      return { label: `${type.name} (${typeCount})`, value: type.id };
    }),
  ];

  if (isLoading && memeOrders.length === 0) {
    return (
      <div style={{ textAlign: "center", padding: "40px 0" }}>
        <Spin size="large" />
        <h4 style={{ marginTop: 12 }}>Загрузка очереди...</h4>
      </div>
    );
  }

  return (
    <div style={{ padding: "16px 0" }}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 12 }}>
        <div>
          <h1 style={{ marginBottom: 0 }}>
            Очередь заказов ({filteredOrders.length})
          </h1>
          {hasActiveFilters && (
            <div style={{ marginTop: 4 }}>
              <span style={{ color: "#8c8c8c", fontSize: 12 }}>
                <Filter size={14} style={{ marginRight: 4 }} />
                Фильтры активны
                {selectedTypeId !== "all" && (
                  <span style={{ marginLeft: 8 }}>
                    Тип:{" "}
                    {selectedTypeId === "no-type"
                      ? "Без типа"
                      : memeTypes.find(t => t.id === selectedTypeId)?.name ||
                        selectedTypeId}
                  </span>
                )}
                {searchTerm.trim() && (
                  <span style={{ marginLeft: 8 }}>Поиск: "{searchTerm}"</span>
                )}
              </span>
            </div>
          )}
        </div>
        <Flex gap={8}>
          {hasActiveFilters && (
            <Button
              size="small"
              onClick={copyFilteredLink}
              style={{ display: "flex", alignItems: "center", gap: 6 }}
              title="Копировать ссылку с фильтрами"
            >
              <Copy size={16} />
              Копировать ссылку
            </Button>
          )}
          <Button
            size="small"
            onClick={onRefresh}
            disabled={isLoading}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <RefreshCw size={16} />
            Обновить
          </Button>
          <Button
            type="primary"
            size="small"
            onClick={onCreate}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Plus size={16} />
            Добавить заказ
          </Button>
        </Flex>
      </Flex>

      <Card style={{ marginBottom: 16 }}>
        <Flex gap={12} align="end">
          <div style={{ flex: "0 0 33.333%" }}>
            <div style={{ fontWeight: 600, marginBottom: 4 }}>
              <Filter size={16} style={{ marginRight: 4 }} />
              Фильтр по типу
            </div>
            <Select
              value={selectedTypeId}
              onChange={value => {
                if (value === "all") {
                  updateTypeFilter("all");
                } else if (value === "no-type") {
                  updateTypeFilter("no-type");
                } else {
                  updateTypeFilter(value);
                }
              }}
              options={typeFilterOptions}
              style={{ width: "100%" }}
            />
          </div>
          <div style={{ flex: "0 0 50%" }}>
            <div style={{ fontWeight: 600, marginBottom: 4 }}>Поиск</div>
            <Input
              placeholder="Поиск по файлу, типу или номеру заказа..."
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
            />
          </div>
          <div style={{ flex: "0 0 16.666%" }}>
            <Button
              onClick={resetFilters}
              style={{ display: "flex", alignItems: "center", gap: 4 }}
              disabled={selectedTypeId === "all" && !searchInput.trim()}
            >
              <X size={16} />
              Сбросить
            </Button>
          </div>
        </Flex>
      </Card>

      {error && (
        <Alert
          type="error"
          showIcon
          message="Ошибка загрузки"
          description={error}
          style={{ marginBottom: 12 }}
        />
      )}

      {filteredOrders.length === 0 ? (
        <Card style={{ textAlign: "center" }}>
          <div>
            <h5 style={{ marginBottom: 4 }}>
              {memeOrders.length === 0 ? "Очередь пуста" : "Ничего не найдено"}
            </h5>
            <div style={{ color: "#8c8c8c" }}>
              {memeOrders.length === 0
                ? "Нет элементов в очереди"
                : "Попробуйте изменить фильтры или поисковый запрос"}
            </div>
            {memeOrders.length > 0 && (
              <Button
                size="small"
                onClick={resetFilters}
                style={{ marginTop: 8 }}
              >
                Сбросить фильтры
              </Button>
            )}
          </div>
        </Card>
      ) : (
        <Flex wrap="wrap" gap={12}>
          {[...filteredOrders]
            .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
            .map(order => (
              <div
                key={order.id}
                style={{
                  flex: "0 0 calc(25% - 9px)",
                  minWidth: 220,
                }}
              >
                <Card
                  hoverable
                  style={{ height: "100%" }}
                  onClick={() => onViewDetails(order)}
                >
                  <div style={{ marginBottom: 8 }}>
                    <span
                      style={{
                        color: "#8c8c8c",
                        fontSize: 12,
                        display: "block",
                      }}
                    >
                      Файл
                    </span>
                    <div
                      style={{
                        fontWeight: 600,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                      title={order.filePath}
                    >
                      {getFileName(order.filePath)}
                    </div>
                  </div>
                  <div style={{ marginBottom: 8 }}>
                    <span
                      style={{
                        color: "#8c8c8c",
                        fontSize: 12,
                        display: "block",
                      }}
                    >
                      Тип
                    </span>
                    <div style={{ fontWeight: 500 }}>
                      {order.type?.name ? (
                        <span style={{ color: "#1677ff" }}>
                          {order.type.name}
                        </span>
                      ) : (
                        <span style={{ color: "#8c8c8c", fontStyle: "italic" }}>
                          Без типа
                        </span>
                      )}
                    </div>
                  </div>
                  <div style={{ marginBottom: 12 }}>
                    <span
                      style={{
                        color: "#8c8c8c",
                        fontSize: 12,
                        display: "block",
                      }}
                    >
                      Номер в очереди
                    </span>
                    <div style={{ fontWeight: 500 }}>#{order.order}</div>
                  </div>
                  <Flex gap={4}>
                    <Button
                      size="small"
                      onClick={e => {
                        e.stopPropagation();
                        onViewDetails(order);
                      }}
                      title="Просмотр"
                    >
                      <Eye size={14} />
                    </Button>
                    <Button
                      size="small"
                      onClick={e => {
                        e.stopPropagation();
                        onEdit(order);
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
                        onDelete(order);
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

export default RandomMemeList;
