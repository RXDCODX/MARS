import { createContext, ReactNode, useContext } from "react";

/**
 * Контекст для передачи обработчиков управления плеером
 * Избегает props drilling для обработчиков
 */
interface PlayerActions {
  handlePlayPrevious: () => void;
  handleTogglePlayPause: () => void;
  handleStop: () => void;
  handleSkip: () => void;
  handleMute: () => void;
  handleVolumeChange: (volume: number) => void;
  handleToggleVideoState: () => void;
}

const PlayerActionsContext = createContext<PlayerActions | null>(null);

interface PlayerActionsProviderProperties {
  children: ReactNode;
  actions: PlayerActions;
}

export function PlayerActionsProvider({
  children,
  actions,
}: PlayerActionsProviderProperties) {
  return (
    <PlayerActionsContext.Provider value={actions}>
      {children}
    </PlayerActionsContext.Provider>
  );
}

export function usePlayerActions(): PlayerActions {
  const context = useContext(PlayerActionsContext);
  if (!context) {
    throw new Error(
      "usePlayerActions must be used within PlayerActionsProvider"
    );
  }
  return context;
}
