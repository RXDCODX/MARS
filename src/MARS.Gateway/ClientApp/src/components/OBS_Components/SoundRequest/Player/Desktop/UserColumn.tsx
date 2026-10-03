import {
  type JSX,
  memo,
  type RefObject,
  type UIEventHandler,
  useMemo,
} from "react";
import { useShallow } from "zustand/react/shallow";

import { useQueueActions } from "../hooks";
import { TrackListViewMode, usePlayerStore } from "../stores/usePlayerStore";
import styles from "./SoundRequestPlayerDesktop.module.scss";
import { UserItem } from "./UserItem";

interface UserColumnProperties {
  scrollListRef?: RefObject<HTMLDivElement>;
  onScroll?: UIEventHandler<HTMLDivElement>;
}

function UserColumnComponent({
  scrollListRef,
  onScroll,
}: UserColumnProperties) {
  // Получаем данные напрямую из стора - ТОЛЬКО нужные поля
  const {
    currentTrack,
    currentQueueItem,
    currentQueueItemId,
    queue,
    history,
    viewMode,
  } = usePlayerStore(
    useShallow(state => ({
      currentTrack: state.playerState?.currentQueueItem?.track || null,
      currentQueueItem: state.playerState?.currentQueueItem,
      currentQueueItemId: state.playerState?.currentQueueItem?.id,
      queue: state.queue,
      history: state.history,
      viewMode: state.viewMode,
    }))
  );

  // Получаем обработчики из хука
  const { handleItemHover } = useQueueActions();

  // Используем переименованные переменные
  const current = currentTrack;

  // Очередь без текущего трека
  const queueWithoutCurrent = useMemo(
    () => queue.filter(x => x.id !== currentQueueItemId),
    [queue, currentQueueItemId]
  );
  const renderUsersList = useMemo(() => {
    const userItems: JSX.Element[] = [];

    switch (viewMode) {
      case TrackListViewMode.Default: {
        // Обычный режим: текущий пользователь -> пользователи очереди (до 8)
        if (current) {
          userItems.push(
            <UserItem
              key={`current-user-${
                currentQueueItem?.requestedByTwitchId + "_" + current.id
              }`}
              user={currentQueueItem?.requestedByTwitchUser ?? undefined}
              lastTimePlays={current.lastTimePlays}
              trackId={current.id}
              isCurrent
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }
        queueWithoutCurrent.forEach(q => {
          if (q.track) {
            userItems.push(
              <UserItem
                key={`queue-user-${q.id}`}
                user={q.requestedByTwitchUser ?? undefined}
                lastTimePlays={q.requestedAt}
                trackId={q.track.id}
                onMouseEnter={() => handleItemHover(q.track?.id, true)}
                onMouseLeave={() => handleItemHover(q.track?.id, false)}
              />
            );
          }
        });
        break;
      }

      case TrackListViewMode.WithHistory: {
        // С историей: история -> текущий трек -> очередь (ограниченная)
        // Ограничиваем общее количество треков, чтобы не выталкивать тулбар
        const MAX_QUEUE_IN_HISTORY_MODE = 4;
        // С историей: заглушки для истории -> текущий -> очередь
        [...history]
          .reverse()
          .slice(0, MAX_QUEUE_IN_HISTORY_MODE)
          .forEach((item, index) => {
            userItems.push(
              <div
                key={`history-user-${item.id}-${index}`}
                className={styles.userRow}
                style={{ opacity: 0, pointerEvents: "none" }}
              >
                <div className={styles.avatar}>
                  <div className={styles.avatarPlaceholder} />
                </div>
                <div className={styles.userBody}>
                  <div className={styles.userName}>-</div>
                  <div className={styles.userMeta}>-</div>
                </div>
              </div>
            );
          });
        if (current) {
          userItems.push(
            <UserItem
              key={`current-user-${
                currentQueueItem?.requestedByTwitchId + "_" + current.id
              }`}
              user={currentQueueItem?.requestedByTwitchUser ?? undefined}
              lastTimePlays={current.lastTimePlays}
              trackId={current.id}
              isCurrent
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }
        queueWithoutCurrent.slice(0, MAX_QUEUE_IN_HISTORY_MODE).forEach(q => {
          if (q.track) {
            userItems.push(
              <UserItem
                key={`queue-user-${q.id}`}
                user={q.requestedByTwitchUser ?? undefined}
                lastTimePlays={q.requestedAt}
                trackId={q.track.id}
                onMouseEnter={() => handleItemHover(q.track?.id, true)}
                onMouseLeave={() => handleItemHover(q.track?.id, false)}
              />
            );
          }
        });
        break;
      }

      case TrackListViewMode.Reversed: {
        // Обратный режим: пустые UserItem для истории -> текущий пользователь
        // Не разворачиваем, так как column-reverse сделает за нас
        history.forEach((item, index) => {
          userItems.push(
            <div
              key={`history-user-${item.id}-${index}`}
              className={styles.userRow}
              style={{ opacity: 0, pointerEvents: "none" }}
            >
              <div className={styles.avatar}>
                <div className={styles.avatarPlaceholder} />
              </div>
              <div className={styles.userBody}>
                <div className={styles.userName}>-</div>
                <div className={styles.userMeta}>-</div>
              </div>
            </div>
          );
        });
        if (current) {
          userItems.push(
            <UserItem
              key={`current-user-${
                currentQueueItem?.requestedByTwitchId + "_" + current.id
              }`}
              user={currentQueueItem?.requestedByTwitchUser ?? undefined}
              lastTimePlays={current.lastTimePlays}
              trackId={current.id}
              isCurrent
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }
        break;
      }
    }

    return userItems;
  }, [
    viewMode,
    current,
    currentQueueItem,
    queueWithoutCurrent,
    history,
    handleItemHover,
  ]);

  return (
    <div
      className={`${styles.rightCol} ${viewMode === TrackListViewMode.Reversed ? styles.reversedMode : ""}`}
    >
      <div
        ref={scrollListRef}
        className={styles.scrollList}
        onScroll={onScroll}
      >
        {renderUsersList}
      </div>
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации
export const UserColumn = memo(UserColumnComponent);
