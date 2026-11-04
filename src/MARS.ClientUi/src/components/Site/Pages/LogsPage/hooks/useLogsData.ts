import { useCallback, useEffect, useState } from "react";

import {
  LogResponse,
  Logs,
  LogsListParamsLogLevelEnum,
  LogsStatistics,
} from "@/shared/api";
import { messageOf } from "@/shared/types/OperationResult";
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
    statisticsError: "",
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
      // Причина кладётся в состояние, а не в консоль. Эндпоинта статистики в
      // проекте нет, и раньше запрос возвращал `index.html` с кодом 200, тихо
      // подставляя нули. Теперь транспорт называет причину, и `console.error`
      // превращал ожидаемое условие в шум на каждом визите страницы — E2E
      // проверяет отсутствие ошибок в консоли и справедливо это ловил.
      updateState({
        statisticsError: messageOf(error, "Статистика логов недоступна"),
        isLoadingStats: false,
      });
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

  // Живая лента логов недоступна: хаба Logger на сервере нет и не будет.
  //
  // Раньше здесь стояло подключение к LoggerHub с бесконечным повтором при
  // ошибке. Хаба нет: путь не объявлен в Gateway, запрос уходил в catch-all
  // клиента, nginx отвечал отказом, а хук повторял попытку каждые 30 секунд —
  // на каждом экране приложения. В консоли это давало ошибку при каждой
  // загрузке страницы.
  //
  // Логи сервисов хранит Loki, и смотрят их в Grafana; MARS.Admin отдаёт для
  // этого выдуманные строки, поэтому доставлять их в браузер как настоящие
  // нельзя. Здесь честное сообщение вместо тихой неудачи.
  useEffect(() => {
    if (!isRealtime) {
      return;
    }

    showToast({
      success: false,
      message:
        "Живая лента логов недоступна: логи сервисов пишутся в Loki и смотрятся в Grafana.",
    });
  }, [isRealtime, showToast]);

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
