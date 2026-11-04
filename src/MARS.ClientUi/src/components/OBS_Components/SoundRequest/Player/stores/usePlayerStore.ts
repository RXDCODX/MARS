import { create } from "zustand";

import { PlayerState, QueueItem } from "@/shared/api";

export enum TrackListViewMode {
  Default = "default", // Текущий трек сверху + очередь
  WithHistory = "withHistory", // История -> текущий -> очередь
  Reversed = "reversed", // Очередь сверху -> текущий снизу
}

interface PlayerStore {
  // Состояние
  playerState: PlayerState | null;
  queue: QueueItem[];
  history: QueueItem[];
  viewMode: TrackListViewMode;
  volume: number;
  loading: boolean;

  // Player actions (регистрируются из useSoundRequestPlayer)
  actions: PlayerActions | null;

  // Методы управления состоянием плеера
  setPlayerState: (state: PlayerState | null) => void;

  // Методы управления очередью
  setQueue: (queue: QueueItem[]) => void;
  removeFromQueue: (queueItemId: string) => void;
  rollbackQueue: (queue: QueueItem[]) => void;

  // Методы управления историей
  setHistory: (history: QueueItem[]) => void;
  addToHistory: (item: QueueItem) => void;

  // Методы управления громкостью
  setVolume: (volume: number) => void;

  // Методы управления состоянием загрузки
  setLoading: (loading: boolean) => void;

  // Методы управления режимом отображения
  setViewMode: (mode: TrackListViewMode) => void;
  cycleViewMode: () => void;

  // Регистрация player actions
  registerActions: (actions: PlayerActions) => void;
}

/**
 * Actions для управления плеером
 */
export interface PlayerActions {
  handlePlayPrevious: () => void;
  handleTogglePlayPause: () => void;
  handleStop: () => void;
  handleSkip: () => void;
  handleMute: () => void;
  handleVolumeChange: (volume: number) => void;
  handleToggleVideoState: () => void;
}

export const usePlayerStore = create<PlayerStore>((set, get) => ({
  // Начальное состояние
  playerState: null,
  queue: [],
  history: [],
  viewMode: TrackListViewMode.Default,
  volume: 0,
  loading: false,
  actions: null,

  // Установить состояние плеера
  setPlayerState: playerState => set({ playerState }),

  // Установить очередь
  setQueue: queue => set({ queue }),

  // Удалить трек из очереди (оптимистичное обновление)
  removeFromQueue: queueItemId => {
    const currentQueue = get().queue;
    const updatedQueue = currentQueue.filter(item => item.id !== queueItemId);
    set({ queue: updatedQueue });
  },

  // Откатить очередь (в случае ошибки)
  rollbackQueue: queue => set({ queue }),

  // Установить историю
  setHistory: history => set({ history }),

  // Добавить трек в историю
  addToHistory: item => {
    const currentHistory = get().history;

    // Проверяем, нужно ли вообще обновлять историю
    // Если трек уже первый в истории - не делаем ничего
    if (currentHistory.length > 0 && currentHistory[0].id === item.id) {
      return;
    }

    // Создаем новый массив вручную без slice - max 5 элементов
    const MAX_HISTORY = 5;
    const updatedHistory: QueueItem[] = [item];

    // Добавляем элементы из текущей истории, пропуская дубликаты
    for (
      let index = 0;
      index < currentHistory.length && updatedHistory.length < MAX_HISTORY;
      index++
    ) {
      if (currentHistory[index].id !== item.id) {
        updatedHistory.push(currentHistory[index]);
      }
    }

    set({ history: updatedHistory });
  },

  // Установить громкость
  setVolume: volume => set({ volume }),

  // Установить состояние загрузки
  setLoading: loading => set({ loading }),

  // Установить режим отображения
  setViewMode: mode => set({ viewMode: mode }),

  // Переключить режим отображения по кругу
  cycleViewMode: () => {
    const currentMode = get().viewMode;
    let nextMode: TrackListViewMode;

    switch (currentMode) {
      case TrackListViewMode.Default: {
        nextMode = TrackListViewMode.WithHistory;
        break;
      }
      case TrackListViewMode.WithHistory: {
        nextMode = TrackListViewMode.Reversed;
        break;
      }
      case TrackListViewMode.Reversed: {
        nextMode = TrackListViewMode.Default;
        break;
      }
      default: {
        nextMode = TrackListViewMode.Default;
      }
    }

    set({ viewMode: nextMode });
  },

  // Зарегистрировать player actions из хука
  registerActions: actions => set({ actions }),
}));
