import axios from "axios";
import { create } from "zustand";

import type { ServiceInfo, ServiceLog } from "@/shared/api";

import {
  readActionResult,
  readLogsList,
  readServicesList,
} from "./serviceResponses";

export type { ServiceInfo, ServiceLog };

interface ServiceStoreState {
  services: ServiceInfo[];
  loading: boolean;
  error: string | null;
  actionLoading: string | null;
  logs: ServiceLog[];
  logsService: string | null;
  logsLoading: boolean;
  statusFilter: string;
  search: string;
  autoRefresh: boolean;
  progress: number;
  showLogs: boolean;
  selectedService: string | null;
  setSelectedService: (service: string | null) => void;
  clearSelectedService: () => void;
  fetchServices: () => Promise<void>;
  handleAction: (
    serviceName: string,
    action: "start" | "stop" | "restart" | "toggle"
  ) => Promise<void>;
  handleShowLogs: (serviceName: string) => Promise<void>;
  handleCloseLogs: () => void;
  setStatusFilter: (v: string) => void;
  setSearch: (v: string) => void;
  setAutoRefresh: (v: boolean) => void;
  setProgress: (v: number) => void;
}

const API = import.meta.env.VITE_API_BASE_URL || "";

export const useServiceStore = create<ServiceStoreState>((set, get) => ({
  services: [],
  loading: false,
  error: null,
  actionLoading: null,
  logs: [],
  logsService: null,
  logsLoading: false,
  statusFilter: "",
  search: "",
  autoRefresh: true,
  progress: 0,
  showLogs: false,
  selectedService: null,
  setSelectedService: service => set({ selectedService: service }),
  clearSelectedService: () => set({ selectedService: null }),

  fetchServices: async () => {
    set({ loading: true, error: null });
    try {
      const res = await axios.get(API + "/api/ServiceManager/services");
      // Ответ — конверт `{ success, message, data }`. Раньше в `services`
      // клался сам конверт, `Array.isArray` давал `false`, и список всегда был
      // пустым при зелёном состоянии: ошибки не было, а экрана не было тоже.
      const read = readServicesList(res.data);

      if (read.ok) {
        set({ services: read.services });
      } else {
        set({ error: read.message, services: [] });
      }
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : "Ошибка загрузки сервисов",
        services: [],
      });
    } finally {
      set({ loading: false });
    }
  },

  handleAction: async (serviceName, action) => {
    set({ actionLoading: serviceName + action, error: null });
    try {
      let body: unknown;

      if (action === "toggle") {
        const service = get().services.find(s => s.name === serviceName);
        if (!service) return;
        // Признак активности уходит в тело, а не в query: контроллер читает его
        // как `[FromBody] bool`, и в `params` он просто не доезжал — переключение
        // выглядело выполненным, а состояние не менялось.
        const response = await axios.post(
          API + `/api/ServiceManager/service/${serviceName}/active`,
          !service.isEnabled,
          { headers: { "Content-Type": "application/json" } }
        );

        body = response.data;
      } else {
        const response = await axios.post(
          API + `/api/ServiceManager/service/${serviceName}/${action}`
        );

        body = response.data;
      }

      // Отказ управления приходит кодом 200, поэтому axios резолвится и на
      // неудаче: без разбора тела интерфейс считал бы действие выполненным.
      const read = readActionResult(body);

      if (!read.ok) {
        set({ error: read.message });

        return;
      }

      await get().fetchServices();
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : "Ошибка управления сервисом",
      });
    } finally {
      set({ actionLoading: null });
    }
  },

  handleShowLogs: async serviceName => {
    set({
      logsService: serviceName,
      showLogs: true,
      logsLoading: true,
      logs: [],
    });
    try {
      const res = await axios.get(
        API + `/api/ServiceManager/service/${serviceName}/logs`
      );
      // В `logs` клался конверт, а не массив, и `logs.filter` в просмотрщике
      // ронял страницу целиком.
      const read = readLogsList(res.data);

      if (read.ok) {
        set({ logs: read.logs });
      } else {
        set({
          logs: [
            {
              timestamp: new Date().toISOString(),
              level: "Error",
              message: read.message,
            },
          ],
        });
      }
    } catch (error) {
      set({
        logs: [
          {
            timestamp: new Date().toISOString(),
            level: "Error",
            message:
              error instanceof Error ? error.message : "Ошибка загрузки логов",
          },
        ],
      });
    } finally {
      set({ logsLoading: false });
    }
  },

  handleCloseLogs: () => set({ showLogs: false, logsService: null, logs: [] }),
  setStatusFilter: v => set({ statusFilter: v }),
  setSearch: v => set({ search: v }),
  setAutoRefresh: v => set({ autoRefresh: v }),
  setProgress: v => set({ progress: v }),
}));
