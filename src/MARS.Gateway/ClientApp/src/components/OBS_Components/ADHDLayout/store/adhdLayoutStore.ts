import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { create } from "zustand";

import {
  AdhdLayoutConfigDto,
  TelegramusHubSignalRConnectionBuilder,
} from "@/shared/api";

type PendingServerCommand = {
  config: AdhdLayoutConfigDto;
};

const pendingServerCommands: PendingServerCommand[] = [];
const MAX_PENDING_SERVER_COMMANDS = 40;

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
  _connection: HubConnection;
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

const queueServerCommand = (config: AdhdLayoutConfigDto) => {
  const nextCommand: PendingServerCommand = { config };
  pendingServerCommands.push(nextCommand);
  if (pendingServerCommands.length > MAX_PENDING_SERVER_COMMANDS) {
    pendingServerCommands.shift();
  }
};

const initialState: AdhdLayoutState = {
  _connection: TelegramusHubSignalRConnectionBuilder.build(),
  config: { ...defaultConfig },
};

export const useAdhdLayoutStore = create<AdhdLayoutStore>((set, get) => {
  const connection = initialState._connection;

  const flushPendingServerCommands = async () => {
    if (connection.state === HubConnectionState.Connected) {
      while (pendingServerCommands.length > 0) {
        const pendingCommand = pendingServerCommands.shift();
        if (pendingCommand) {
          const sendResult = await get()._sendToServer(pendingCommand.config);
          if (!sendResult) {
            break;
          }
        }
      }
    }
  };

  connection.on("ReceiveConfig", config => {
    get().handleReceiveConfig(config);
  });
  connection.on("ConfigUpdated", config => {
    get().handleConfigUpdated(config);
  });

  connection.onreconnected(() => {
    console.log("ADHD SignalR reconnected. Flushing queued updates...");
    void flushPendingServerCommands();
  });

  const startConnection = async () => {
    try {
      await connection.start();
      await flushPendingServerCommands();
    } catch (error) {
      console.error("Error starting ADHD SignalR connection:", error);
    }
  };
  void startConnection();

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
    _sendToServer: async config => {
      let isResult = false;

      if (connection.state === HubConnectionState.Connected) {
        try {
          console.log("Sending UpdateConfig:", config);
          await connection.invoke("UpdateConfig", config);
          isResult = true;
        } catch (error) {
          queueServerCommand(config);
          console.error(
            "Error sending UpdateConfig. Command queued for retry.",
            error
          );
        }
      } else {
        queueServerCommand(config);
        console.log(
          `SignalR connection state is '${connection.state}'. Queued UpdateConfig.`
        );
      }

      return isResult;
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
