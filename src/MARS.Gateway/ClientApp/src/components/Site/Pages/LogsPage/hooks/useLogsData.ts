import type { HubConnection } from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";

import {
  Log,
  LoggerHubSignalRConnectionBuilder,
  LogLogLevelEnum,
  LogMessageDto,
  LogResponse,
  Logs,
  LogsListParamsLogLevelEnum,
  LogsStatistics,
} from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { LogsFilters, LogsPageState } from "../LogsPage.types";

export const useLogsData = () => {
  const { showToast } = useToastModal();
  const [logsService] = useState(() => new Logs(defaultApiConfig));
  const [isRealtime, setIsRealtime] = useState(true);

  // Состояние страницы
  const [state, setState] = useState<LogsPageState>({
    logs: [],
    statistics: null,
    isLoading: false,
    isLoadingStats: false,
    error: "",
    currentPage: 1,
    pageSize: 25,
    totalPages: 0,
    totalCount: 0,
  });

  // Фильтры для поиска
  const [filters, setFilters] = useState<LogsFilters>({
    logLevel: "",
    fromDate: "",
    toDate: "",
    searchText: "",
    sortBy: "whenlogged",
    sortDescending: true,
  });

  // Обновление состояния
  const updateState = useCallback((updates: Partial<LogsPageState>) => {
    setState(previous => ({ ...previous, ...updates }));
  }, []);

  // Обновление фильтров
  const updateFilters = useCallback((updates: Partial<LogsFilters>) => {
    setFilters(previous => ({ ...previous, ...updates }));
  }, []);

  // Загрузка логов
  const loadLogs = useCallback(async () => {
    try {
      updateState({ isLoading: true, error: "" });

      const query: {
        page: number;
        pageSize: number;
        sortBy: string;
        sortDescending: boolean;
        logLevel?: LogsListParamsLogLevelEnum;
        fromDate?: string;
        toDate?: string;
        searchText?: string;
      } = {
        page: state.currentPage,
        pageSize: state.pageSize,
        sortBy: filters.sortBy,
        sortDescending: filters.sortDescending,
      };

      // Добавляем только те параметры, которые имеют значения
      if (filters.logLevel) {
        query.logLevel = filters.logLevel as LogsListParamsLogLevelEnum;
      }
      if (filters.fromDate) {
        query.fromDate = filters.fromDate;
      }
      if (filters.toDate) {
        query.toDate = filters.toDate;
      }
      if (filters.searchText) {
        query.searchText = filters.searchText;
      }

      console.log("Запрос логов с параметрами:", query);

      const response = await logsService.logsList(
        query as Parameters<typeof logsService.logsList>[0]
      );
      const logResponse: LogResponse = response.data.data ?? {
        logs: [],
        totalCount: 0,
        page: 1,
        pageSize: 10,
        totalPages: 0,
      };

      console.log("Ответ от сервера:", logResponse);

      updateState({
        logs: logResponse.logs || [],
        totalPages: logResponse.totalPages || 0,
        totalCount: logResponse.totalCount || 0,
        isLoading: false,
      });
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : String(error ?? "Неизвестная ошибка");
      updateState({
        error: `Ошибка при загрузке логов: ${errorMessage}`,
        isLoading: false,
      });

      showToast({
        success: false,
        message: "Не удалось загрузить логи приложения",
      });
    }
  }, [
    logsService,
    state.currentPage,
    state.pageSize,
    filters,
    updateState,
    showToast,
  ]);

  // Загрузка статистики
  const loadStatistics = useCallback(async () => {
    try {
      updateState({ isLoadingStats: true });

      const response = await logsService.logsStatisticsList();
      const stats: LogsStatistics = response.data.data ?? {
        totalLogs: 0,
        warningLogs: 0,
        errorLogs: 0,
        criticalLogs: 0,
      };

      updateState({
        statistics: stats,
        isLoadingStats: false,
      });
    } catch (error: unknown) {
      console.error("Ошибка при загрузке статистики:", error);
      updateState({ isLoadingStats: false });
    }
  }, [logsService, updateState]);

  // Обработчик изменения страницы
  const handlePageChange = useCallback(
    (page: number) => {
      updateState({ currentPage: page });
    },
    [updateState]
  );

  // Обработчик изменения размера страницы
  const handlePageSizeChange = useCallback(
    (size: number) => {
      updateState({
        pageSize: size,
        currentPage: 1, // Сбрасываем на первую страницу при изменении размера
      });
    },
    [updateState]
  );

  // Обработчик поиска
  const handleSearch = useCallback(() => {
    updateState({ currentPage: 1 }); // Сбрасываем на первую страницу при поиске
    loadLogs();
  }, [loadLogs, updateState]);

  // Обработчик сброса фильтров
  const handleResetFilters = useCallback(() => {
    setFilters({
      logLevel: "",
      fromDate: "",
      toDate: "",
      searchText: "",
      sortBy: "whenlogged",
      sortDescending: true,
    });
    updateState({ currentPage: 1 });
  }, [updateState]);

  // Обработчик изменения режима
  const handleModeChange = useCallback(
    (newIsRealtime: boolean) => {
      setIsRealtime(newIsRealtime);
      if (!newIsRealtime) {
        // При переходе на режим запросов обновим данные сразу
        loadLogs();
        loadStatistics();
      }
    },
    [loadLogs, loadStatistics]
  );

  // Загрузка данных при изменении фильтров или пагинации
  useEffect(() => {
    if (!isRealtime) {
      (async () => {
        await loadLogs();
      })();
    }
  }, [isRealtime, loadLogs]);

  // Загрузка статистики при монтировании компонента
  useEffect(() => {
    (async () => {
      await loadStatistics();
    })();
  }, [loadStatistics]);

  // Автоматическое обновление каждые 30 секунд в режиме REST
  useEffect(() => {
    if (isRealtime) return;
    const interval = setInterval(() => {
      if (state.isLoading) {
        return;
      }

      loadLogs();
      loadStatistics();
    }, 30_000);
    return () => clearInterval(interval);
  }, [isRealtime, loadLogs, loadStatistics, state.isLoading]);

  // Управление прямым подключением к LoggerHub в режиме real-time
  const connectionReference = useRef<HubConnection | null>(null);

  useEffect(() => {
    // При выключении realtime — останавливаем соединение, если оно есть
    if (!isRealtime) {
      if (connectionReference.current) {
        connectionReference.current.stop().catch(() => {});
        connectionReference.current = null;
      }
      return;
    }

    const connection = LoggerHubSignalRConnectionBuilder.build();
    connectionReference.current = connection;

    const onLog = (logMessage: LogMessageDto) => {
      setState(previous => {
        const newLog: Log = {
          id: String(logMessage.id),
          whenLogged: new Date(logMessage.timestamp).toISOString(),
          message: logMessage.message,
          stackTrace:
            logMessage.stackTrace || logMessage.exception || undefined,
          logLevel: logMessage.logLevel as LogLogLevelEnum,
        };

        const updatedLogs = [newLog, ...previous.logs];
        const sliced = updatedLogs.slice(0, previous.pageSize);

        return {
          ...previous,
          logs: sliced,
          totalCount: previous.totalCount + 1,
        };
      });

      loadStatistics();
    };

    connection.on("Log", onLog);

    let retryDelay = 1000;

    const startWithRetry = async () => {
      try {
        await connection.start();
        retryDelay = 1000;
      } catch (error: unknown) {
        const errorMessage =
          error instanceof Error ? error.message : String(error ?? "");

        showToast({
          success: false,
          message:
            "Не удалось установить соединение для получения логов в реальном времени: " +
            errorMessage,
        });

        setTimeout(() => startWithRetry(), retryDelay);
        retryDelay = Math.min(30_000, retryDelay * 2);
      }
    };

    connection.onclose(() => {
      // Попробуем переподключиться
      startWithRetry();
    });

    startWithRetry();

    return () => {
      connection.off("Log", onLog);
      connection.stop().catch(() => {});
      if (connectionReference.current === connection)
        connectionReference.current = null;
    };
  }, [isRealtime, loadStatistics, showToast]);

  return {
    // Состояние
    state,
    filters,
    isRealtime,

    // Действия
    updateState,
    updateFilters,
    loadLogs,
    loadStatistics,
    handlePageChange,
    handlePageSizeChange,
    handleSearch,
    handleResetFilters,
    handleModeChange,
  };
};
