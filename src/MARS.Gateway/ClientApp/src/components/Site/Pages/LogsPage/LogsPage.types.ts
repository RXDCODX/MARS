import { Log, LogsStatistics } from "@/shared/api";

// Re-export Log type for use in other components

// Состояние страницы логов
export interface LogsPageState {
  logs: Log[];
  statistics: LogsStatistics | null;
  isLoading: boolean;
  isLoadingStats: boolean;
  error: string;
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
}

// Фильтры для поиска логов
export interface LogsFilters {
  logLevel: string;
  fromDate: string;
  toDate: string;
  searchText: string;
  sortBy: string;
  sortDescending: boolean;
}

// Пропсы для компонента фильтров
export interface LogsFiltersProps {
  filters: LogsFilters;
  onFiltersChange: (filters: Partial<LogsFilters>) => void;
  onSearch: () => void;
  onReset: () => void;
  isLoading: boolean;
}

// Пропсы для компонента статистики
export interface LogsStatisticsProps {
  statistics: LogsStatistics | null;
  isLoading: boolean;
}

// Пропсы для компонента таблицы логов
export interface LogsTableProps {
  logs: Log[];
  isLoading: boolean;
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

// Пропсы для компонента пагинации
export interface LogsPaginationProps {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

// Типы для уровней логирования
export type LogLevel = "Info" | "Warning" | "Error" | "Critical" | "Debug";

// Интерфейс для экспорта логов
export interface LogsExportOptions {
  format: "json" | "csv" | "txt";
  includeStackTrace: boolean;
  dateRange: {
    from: string;
    to: string;
  };
}

// Состояние для модального окна деталей лога
export interface LogDetailsModalState {
  isOpen: boolean;
  selectedLog: Log | null;
}

// Типы для сортировки
export type SortField = "whenlogged" | "logLevel" | "message";
export type SortDirection = "asc" | "desc";

// Интерфейс для настроек отображения
export interface LogsDisplaySettings {
  showTimestamp: boolean;
  showLogLevel: boolean;
  showMessage: boolean;
  showStackTrace: boolean;
  compactMode: boolean;
  autoRefresh: boolean;
  refreshInterval: number;
}

// Типы для действий с логами
export type LogAction = "view" | "export" | "delete" | "markAsRead";

// Интерфейс для параметров запроса логов
export interface LogsQueryParams {
  page: number;
  pageSize: number;
  sortBy: string;
  sortDescending: boolean;
  logLevel?: string;
  fromDate?: string;
  toDate?: string;
  searchText?: string;
}

// Интерфейс для контекста логов
export interface LogsContextType {
  state: LogsPageState;
  filters: LogsFilters;
  displaySettings: LogsDisplaySettings;
  actions: {
    loadLogs: () => Promise<void>;
    loadStatistics: () => Promise<void>;
    updateFilters: (filters: Partial<LogsFilters>) => void;
    updateDisplaySettings: (settings: Partial<LogsDisplaySettings>) => void;
    exportLogs: (options: LogsExportOptions) => Promise<void>;
    clearLogs: () => Promise<void>;
  };
}

export { type Log } from "@/shared/api";
