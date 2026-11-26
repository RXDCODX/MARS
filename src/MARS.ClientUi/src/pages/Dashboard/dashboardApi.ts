import axios from "axios";

import type {
  DashboardActivity,
  DashboardActivityStats,
  DashboardMetricsHistory,
  DashboardOverview,
} from "./Dashboard.types";

const API = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/+$/, "");

const unwrap = <T>(body: unknown): T => {
  if (typeof body !== "object" || body === null) {
    throw new Error("Некорректный ответ сервера");
  }

  const envelope = body as {
    success?: boolean;
    result?: T;
    data?: T;
    message?: string | null;
    errorMessage?: string | null;
  };

  if (typeof envelope.success === "boolean" && !envelope.success) {
    throw new Error(
      envelope.errorMessage ?? envelope.message ?? "Запрос завершился ошибкой"
    );
  }

  const payload = envelope.data !== undefined ? envelope.data : envelope.result;
  if (payload === undefined) {
    throw new Error("Сервер не вернул данные");
  }

  return payload as T;
};

export const dashboardApi = {
  overview: async (signal?: AbortSignal): Promise<DashboardOverview> => {
    const response = await axios.get(`${API}/api/Dashboard/overview`, {
      signal,
    });
    return unwrap<DashboardOverview>(response.data);
  },

  activities: async (
    parameters: {
      hours?: number;
      service?: string;
      type?: string;
      platform?: string;
      outcome?: string;
      search?: string;
      limit?: number;
    },
    signal?: AbortSignal
  ): Promise<DashboardActivity[]> => {
    const response = await axios.get(`${API}/api/Dashboard/activities`, {
      params: parameters,
      signal,
    });
    return unwrap<DashboardActivity[]>(response.data);
  },

  activityStats: async (
    hours = 24,
    signal?: AbortSignal
  ): Promise<DashboardActivityStats> => {
    const response = await axios.get(`${API}/api/Dashboard/activity-stats`, {
      params: { hours },
      signal,
    });
    return unwrap<DashboardActivityStats>(response.data);
  },

  metricsHistory: async (
    hours = 24,
    signal?: AbortSignal
  ): Promise<DashboardMetricsHistory> => {
    const response = await axios.get(`${API}/api/Dashboard/metrics/history`, {
      params: { hours },
      signal,
    });
    return unwrap<DashboardMetricsHistory>(response.data);
  },
};
