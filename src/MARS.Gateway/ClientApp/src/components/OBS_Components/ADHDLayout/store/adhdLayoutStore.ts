import { create } from "zustand";

import { AdhdLayoutConfigDto } from "@/shared/api";
import { subscribeToOverlayEvent } from "@/shared/realtime/overlaySubscription";
import { decodeBytesField, readBranch } from "@/shared/realtime/overlayPayload";

export type AdhdComponentKey = keyof AdhdLayoutConfigDto;

export type AdhdBooleanComponentKey = {
  [Key in AdhdComponentKey]: AdhdLayoutConfigDto[Key] extends boolean
    ? Key
    : never;
}[AdhdComponentKey];

const defaultConfig: AdhdLayoutConfigDto = {
  showRainEffect: true,
  showDVDLogos: true,
  showBreakingNews: true,
  showStreamerVideo: true,
  showFitnessVideo: true,
  showGTAVideo: true,
  showHydraulicMobileVideo: true,
  showSlimeVideo: true,
  showMukbangVideo: true,
  showQuiz: true,
  showSurfer: true,
  showLOFIGirl: true,
  showCatisa: true,
  showNotifications: true,
  showTimer: true,
  dvdLogosCount: 12,
};

interface AdhdLayoutState {
  config: AdhdLayoutConfigDto;
}

interface AdhdLayoutActions {
  setConfig: (config: AdhdLayoutConfigDto) => void;
  toggleComponent: (key: AdhdBooleanComponentKey) => void;
  setAllComponents: (isEnabled: boolean) => void;
  setDvdLogosCount: (count: number) => void;
  resetToDefaults: () => void;
  handleReceiveConfig: (config: AdhdLayoutConfigDto) => void;
  handleConfigUpdated: (config: AdhdLayoutConfigDto) => void;
  _sendToServer: (config: AdhdLayoutConfigDto) => Promise<boolean>;
}

export type AdhdLayoutStore = AdhdLayoutState & AdhdLayoutActions;

/**
 * Разбирает событие конфигурации раскладки.
 *
 * Событие едет содержимым ветки — { configJson }, где полезная нагрузка
 * объявлена полем bytes — приходит массивом байт.
 */
const readAdhdConfig = (payload: unknown): AdhdLayoutConfigDto | null => {
  const branch = readBranch(payload);

  if (branch === null) {
    return null;
  }

  const decoded = decodeBytesField(branch.configJson);

  return decoded === undefined ? null : (decoded as AdhdLayoutConfigDto);
};

const initialState: AdhdLayoutState = {
  config: { ...defaultConfig },
};

export const useAdhdLayoutStore = create<AdhdLayoutStore>((set, get) => {
  // Подписка на конфигурацию раскладки ждёт прихода адаптера.
  //
  // Раньше стояли два метода хаба — ReceiveConfig и ConfigUpdated, — которых на
  // сервере нет вовсе: хаб оверлея объявляет только события из oneof, и эти
  // имена среди них не значились. Подписка была на `hubs/telegramus`, путь
  // которого никто не обслуживал, так что соединение падало, а хук повторял
  // попытку и писал в консоль при каждой загрузке страницы.
  //
  // Теперь подписка на AdhdConfig — событие, которое действительно есть: его
  // публикует TelegramusGrpcService.UpdateAdhdConfig, и реле перекладывает в
  // метод хаба. И подписка отложенная: стор импортируется раньше, чем хаб
  // поднимается, и чтение реестра один раз давало null — конфигурация не
  // приезжала никогда.
  subscribeToOverlayEvent("AdhdConfig", payload => {
    const config = readAdhdConfig(payload);

    if (config !== null) {
      get().handleConfigUpdated(config);
    }
  });

  return {
    ...initialState,
    setConfig: config => {
      set({ config });
    },
    toggleComponent: key => {
      const currentConfig = get().config;
      const currentValue = currentConfig[key];
      const updatedConfig = {
        ...currentConfig,
        [key]: !currentValue,
      };

      set({ config: updatedConfig });
      void get()._sendToServer(updatedConfig);
    },
    setAllComponents: isEnabled => {
      const updatedConfig = {
        ...defaultConfig,
        dvdLogosCount: get().config.dvdLogosCount,
      };

      for (const key of Object.keys(
        defaultConfig
      ) as AdhdBooleanComponentKey[]) {
        updatedConfig[key] = isEnabled;
      }

      set({ config: updatedConfig });
      void get()._sendToServer(updatedConfig);
    },
    setDvdLogosCount: count => {
      const currentConfig = get().config;
      const clampedCount = Math.min(20, Math.max(1, Math.round(count)));
      const updatedConfig = {
        ...currentConfig,
        dvdLogosCount: clampedCount,
      };

      set({ config: updatedConfig });
      void get()._sendToServer(updatedConfig);
    },
    resetToDefaults: () => {
      const updatedConfig = { ...defaultConfig };
      set({ config: updatedConfig });
      void get()._sendToServer(updatedConfig);
    },
    handleReceiveConfig: config => {
      set({ config: { ...defaultConfig, ...config } });
    },
    handleConfigUpdated: config => {
      set({ config: { ...defaultConfig, ...config } });
    },
    // Сохранение на сервере недоступно из браузера.
    //
    // Метод хаба UpdateConfig не существует: хаб оверлея объявляет только
    // события из oneof, и вызовов от клиента в нём нет. Раньше здесь стоял
    // invoke, который всегда падал, команда уходила в очередь и повторялась —
    // без единой попытки стать успешной.
    //
    // Возвращается false явно: вызывающий код проверяет результат и показывает
    // ошибку, а не считает раскладку сохранённой.
    _sendToServer: async () => {
      console.warn(
        "Сохранение раскладки на сервере недоступно: хаб не принимает вызовов от клиента."
      );

      return false;
    },
  };
});

export const useAdhdConfig = () => useAdhdLayoutStore(state => state.config);

export const useAdhdConfigActions = () => {
  const toggleComponent = useAdhdLayoutStore(state => state.toggleComponent);
  const setAllComponents = useAdhdLayoutStore(state => state.setAllComponents);
  const setDvdLogosCount = useAdhdLayoutStore(state => state.setDvdLogosCount);
  const resetToDefaults = useAdhdLayoutStore(state => state.resetToDefaults);

  return {
    toggleComponent,
    setAllComponents,
    setDvdLogosCount,
    resetToDefaults,
  };
};

export { defaultConfig };
