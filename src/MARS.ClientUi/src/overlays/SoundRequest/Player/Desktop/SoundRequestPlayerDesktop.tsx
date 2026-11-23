import { memo, type UIEvent, useCallback, useRef } from "react";

import { AddTrackModal, PlayerToolbar } from "./PlayerToolbar";
import styles from "./SoundRequestPlayerDesktop.module.scss";
import { TrackColumn } from "./TrackColumn";
import { UserColumn } from "./UserColumn";

/**
 * Desktop версия Sound Request плеера
 * Чистый UI компонент - инициализация и фон управляются на уровне выше
 */
function SoundRequestPlayerDesktopComponent() {
  const trackScrollListReference = useRef<HTMLDivElement>(null);
  const userScrollListReference = useRef<HTMLDivElement>(null);
  const scrollSyncSourceReference = useRef<"track" | "user" | null>(null);

  const handleTrackScroll = useCallback((event: UIEvent<HTMLDivElement>) => {
    if (scrollSyncSourceReference.current === "user") {
      scrollSyncSourceReference.current = null;
    } else {
      const nextScrollTop = event.currentTarget.scrollTop;
      const userScrollList = userScrollListReference.current;

      if (userScrollList && userScrollList.scrollTop !== nextScrollTop) {
        scrollSyncSourceReference.current = "track";
        userScrollList.scrollTop = nextScrollTop;
      }
    }
  }, []);

  const handleUserScroll = useCallback((event: UIEvent<HTMLDivElement>) => {
    if (scrollSyncSourceReference.current === "track") {
      scrollSyncSourceReference.current = null;
    } else {
      const nextScrollTop = event.currentTarget.scrollTop;
      const trackScrollList = trackScrollListReference.current;

      if (trackScrollList && trackScrollList.scrollTop !== nextScrollTop) {
        scrollSyncSourceReference.current = "user";
        trackScrollList.scrollTop = nextScrollTop;
      }
    }
  }, []);

  return (
    <div className={styles.root} data-testid="obs-sr-player-desktop">
      <div className={styles.container1}>
        {/* Верхний блок 9 частей высоты: 7:3 по ширине */}
        <div className={styles.topSplit}>
          <TrackColumn
            scrollListRef={trackScrollListReference}
            onScroll={handleTrackScroll}
          />
          <UserColumn
            scrollListRef={userScrollListReference}
            onScroll={handleUserScroll}
          />
        </div>
      </div>

      {/* Нижний блок 1 часть высоты — тулбар с прогресс-заливкой */}
      <div className={styles.container3}>
        <PlayerToolbar />
      </div>

      {/* Модальное окно добавления трека - управляет своим состоянием */}
      <AddTrackModal />
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации производительности
export const SoundRequestPlayerDesktop = memo(
  SoundRequestPlayerDesktopComponent
);
