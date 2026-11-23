import {
  ArrowDownUp,
  ChevronsDown,
  ChevronsUp,
  Clock,
  List,
} from "lucide-react";
import { memo, useMemo } from "react";
import { useShallow } from "zustand/react/shallow";

import { TrackListViewMode, usePlayerStore } from "../../stores/usePlayerStore";
import styles from "./ViewModeToggle.module.scss";

function ViewModeToggleComponent() {
  // Получаем viewMode напрямую из стора
  const viewMode = usePlayerStore(useShallow(state => state.viewMode));

  // Обработчик переключения режима отображения
  const handleToggle = () => {
    usePlayerStore.getState().cycleViewMode();
  };
  // Мемоизируем иконку
  const icon = useMemo(() => {
    switch (viewMode) {
      case TrackListViewMode.Default: {
        return <List size={24} />;
      }
      case TrackListViewMode.WithHistory: {
        return <Clock size={24} />;
      }
      case TrackListViewMode.Reversed: {
        return <ArrowDownUp size={24} />;
      }
      default: {
        return <List size={24} />;
      }
    }
  }, [viewMode]);

  // Мемоизируем лейбл
  const label = useMemo(() => {
    switch (viewMode) {
      case TrackListViewMode.Default: {
        return "Обычный";
      }
      case TrackListViewMode.WithHistory: {
        return "Общий";
      }
      case TrackListViewMode.Reversed: {
        return "История↓";
      }
      default: {
        return "Режим";
      }
    }
  }, [viewMode]);

  // Мемоизируем title
  const title = useMemo(() => {
    switch (viewMode) {
      case TrackListViewMode.Default: {
        return "Обычный режим: текущий трек + очередь";
      }
      case TrackListViewMode.WithHistory: {
        return "Общий режим: история → текущий трек → очередь";
      }
      case TrackListViewMode.Reversed: {
        return "Обратный режим: история + текущий трек снизу";
      }
      default: {
        return "Переключить режим отображения";
      }
    }
  }, [viewMode]);

  return (
    <button
      className={`${styles.viewModeToggle} ${styles[viewMode]}`}
      onClick={handleToggle}
      title={title}
      type="button"
      data-testid="button-view-mode-toggle"
    >
      <div className={styles.iconWrapper}>
        {icon}
        {viewMode === TrackListViewMode.Default && (
          <ChevronsDown className={styles.indicator} size={10} />
        )}
        {viewMode === TrackListViewMode.WithHistory && (
          <div className={styles.historyDots}>
            <span />
            <span />
            <span />
          </div>
        )}
        {viewMode === TrackListViewMode.Reversed && (
          <ChevronsUp className={styles.indicator} size={10} />
        )}
      </div>
      <span className={styles.label}>{label}</span>
      <div className={styles.modeIndicators}>
        <span
          className={`${styles.dot} ${viewMode === TrackListViewMode.Default ? styles.active : ""}`}
        />
        <span
          className={`${styles.dot} ${viewMode === TrackListViewMode.WithHistory ? styles.active : ""}`}
        />
        <span
          className={`${styles.dot} ${viewMode === TrackListViewMode.Reversed ? styles.active : ""}`}
        />
      </div>
    </button>
  );
}

// Экспортируем мемоизированную версию
export const ViewModeToggle = memo(ViewModeToggleComponent);
