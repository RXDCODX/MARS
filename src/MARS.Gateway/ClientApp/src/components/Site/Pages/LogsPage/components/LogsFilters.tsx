import { Button, Input, Select } from "antd";
import { Filter, RotateCcw, Search } from "lucide-react";

import styles from "../LogsPage.module.scss";
import {
  LogsFilters as LogsFiltersType,
  LogsFiltersProps as LogsFiltersProperties,
} from "../LogsPage.types";

const LogsFilters: React.FC<LogsFiltersProperties> = ({
  filters,
  onFiltersChange,
  onSearch,
  onReset,
  isLoading,
}) => {
  const handleInputChange = (
    field: keyof LogsFiltersType,
    value: string | boolean
  ) => {
    onFiltersChange({ [field]: value });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSearch();
  };

  const handleReset = () => {
    onFiltersChange({
      logLevel: "",
      fromDate: "",
      toDate: "",
      searchText: "",
      sortBy: "whenlogged",
      sortDescending: true,
    });
    onReset();
  };

  const logLevels = [
    { value: "", label: "Все уровни" },
    { value: "Info", label: "Информация" },
    { value: "Warning", label: "Предупреждение" },
    { value: "Error", label: "Ошибка" },
    { value: "Critical", label: "Критическая ошибка" },
    { value: "Debug", label: "Отладка" },
  ];

  const sortOptions = [
    { value: "whenlogged", label: "По времени" },
    { value: "logLevel", label: "По уровню" },
    { value: "message", label: "По сообщению" },
  ];

  return (
    <div className={styles.controlsCard}>
      <form onSubmit={handleSubmit}>
        <div className={styles.controlsGrid}>
          <div className={styles.controlGroup}>
            <label htmlFor="searchText">Поиск по тексту</label>
            <div className={styles.searchBox}>
              <Search size={20} className={styles.searchIcon} />
              <Input
                id="searchText"
                placeholder="Поиск в сообщениях логов..."
                value={filters.searchText}
                onChange={e => handleInputChange("searchText", e.target.value)}
              />
            </div>
          </div>

          <div className={styles.controlGroup}>
            <label htmlFor="logLevel">Уровень логирования</label>
            <Select
              id="logLevel"
              value={filters.logLevel || undefined}
              onChange={value => handleInputChange("logLevel", value)}
              options={logLevels}
              style={{ width: "100%" }}
            />
          </div>

          <div className={styles.controlGroup}>
            <label htmlFor="fromDate">Дата начала</label>
            <Input
              type="datetime-local"
              id="fromDate"
              value={filters.fromDate}
              onChange={e => handleInputChange("fromDate", e.target.value)}
            />
          </div>

          <div className={styles.controlGroup}>
            <label htmlFor="toDate">Дата окончания</label>
            <Input
              type="datetime-local"
              id="toDate"
              value={filters.toDate}
              onChange={e => handleInputChange("toDate", e.target.value)}
            />
          </div>

          <div className={styles.controlGroup}>
            <label htmlFor="sortBy">Сортировка</label>
            <Select
              id="sortBy"
              value={filters.sortBy}
              onChange={value => handleInputChange("sortBy", value)}
              options={sortOptions}
              style={{ width: "100%" }}
            />
          </div>

          <div className={styles.controlGroup}>
            <label htmlFor="sortDescending">Направление</label>
            <Select
              id="sortDescending"
              value={filters.sortDescending ? "desc" : "asc"}
              onChange={value =>
                handleInputChange("sortDescending", value === "desc")
              }
              options={[
                { value: "desc", label: "По убыванию" },
                { value: "asc", label: "По возрастанию" },
              ]}
              style={{ width: "100%" }}
            />
          </div>
        </div>

        <div className={styles.buttonsRow}>
          <div className={styles.buttonsGroup}>
            <Button
              type="submit"
              disabled={isLoading}
              style={{ display: "flex", alignItems: "center", gap: 8 }}
            >
              <Search size={16} />
              {isLoading ? "Поиск..." : "Найти логи"}
            </Button>

            <Button
              type="button"
              onClick={handleReset}
              disabled={isLoading}
              style={{ display: "flex", alignItems: "center", gap: 8 }}
            >
              <RotateCcw size={16} />
              Сбросить
            </Button>

            <Button
              type="button"
              onClick={() => {
                const now = new Date();
                const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000);

                onFiltersChange({
                  fromDate: yesterday.toISOString().slice(0, 16),
                  toDate: now.toISOString().slice(0, 16),
                });
              }}
              disabled={isLoading}
              style={{ display: "flex", alignItems: "center", gap: 8 }}
            >
              <Filter size={16} />
              Последние 24 часа
            </Button>

            <Button
              type="button"
              onClick={() => {
                const now = new Date();

                const dayOfWeek = now.getDay();
                const daysToMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

                const monday = new Date(now);
                monday.setDate(now.getDate() - daysToMonday);
                monday.setHours(0, 0, 0, 0);

                const sunday = new Date(monday);
                sunday.setDate(monday.getDate() + 6);
                sunday.setHours(23, 59, 59, 999);

                onFiltersChange({
                  fromDate: monday.toISOString().slice(0, 16),
                  toDate: sunday.toISOString().slice(0, 16),
                });
              }}
              disabled={isLoading}
              style={{ display: "flex", alignItems: "center", gap: 8 }}
            >
              <Filter size={16} />
              За эту неделю
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
};

export default LogsFilters;
