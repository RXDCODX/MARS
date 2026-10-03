import axios from "axios";
import { create } from "zustand";

export interface ServiceInfo {
  name: string;
  displayName: string;
  description: string;
  status: string;
  startTime?: string;
  lastActivity?: string;
  isEnabled: boolean;
  configuration: object;
}

export interface ServiceLog {
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
}

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
      const res = await axios.get<ServiceInfo[]>(
        API + "/api/ServiceManager/services"
      );
      set({ services: Array.isArray(res.data) ? res.data : [] });
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
      if (action === "toggle") {
        const service = get().services.find(s => s.name === serviceName);
        if (!service) return;
        await axios.post(
          API + `/api/ServiceManager/service/${serviceName}/active`,
          null,
          {
            params: { isActive: !service.isEnabled },
          }
        );
      } else {
        await axios.post(
          API + `/api/ServiceManager/service/${serviceName}/${action}`
        );
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
      const res = await axios.get<ServiceLog[]>(
        API + `/api/ServiceManager/service/${serviceName}/logs`
      );
      set({ logs: res.data });
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
