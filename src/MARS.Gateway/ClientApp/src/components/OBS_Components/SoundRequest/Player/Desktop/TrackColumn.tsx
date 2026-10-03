import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import {
  type JSX,
  memo,
  type RefObject,
  type UIEventHandler,
  useMemo,
  useState,
} from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateStateEnum } from "@/shared/api";

import { useQueueActions } from "../hooks";
import { TrackListViewMode, usePlayerStore } from "../stores/usePlayerStore";
import styles from "./SoundRequestPlayerDesktop.module.scss";
import { TrackItem } from "./TrackItem";

interface TrackColumnProperties {
  scrollListRef?: RefObject<HTMLDivElement>;
  onScroll?: UIEventHandler<HTMLDivElement>;
}

function TrackColumnComponent({
  scrollListRef,
  onScroll,
}: TrackColumnProperties) {
  // Получаем данные напрямую из стора - ТОЛЬКО нужные поля
  const {
    currentTrack,
    currentQueueItemId,
    isPlaying,
    queue,
    history,
    viewMode,
  } = usePlayerStore(
    useShallow(state => ({
      currentTrack: state.playerState?.currentQueueItem?.track || null,
      currentQueueItemId: state.playerState?.currentQueueItem?.id,
      isPlaying: state.playerState?.state === PlayerStateStateEnum.Playing,
      queue: state.queue,
      history: state.history,
      viewMode: state.viewMode,
    }))
  );

  // Получаем обработчики из хука
  const {
    handleItemHover,
    handleDeleteFromQueue,
    handlePlayNow,
    playingNowId,
    handleMoveUp,
    handleMoveDown,
    handleReorder,
  } = useQueueActions();

  // Используем переименованные переменные
  const current = currentTrack;

  // Очередь без текущего трека
  const queueWithoutCurrent = useMemo(
    () => queue.filter(x => x.id !== currentQueueItemId),
    [queue, currentQueueItemId]
  );

  const [activeId, setActiveId] = useState<string | null>(null);
  const sensors = useSensors(useSensor(PointerSensor));

  const renderTracksList = useMemo(() => {
    const trackItems: JSX.Element[] = [];

    switch (viewMode) {
      case TrackListViewMode.Default: {
        // Обычный режим: текущий трек -> вся очередь
        if (current) {
          trackItems.push(
            <TrackItem
              key={`current-${current.id}`}
              track={current}
              isCurrent
              isPlaying={isPlaying}
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }

        // Сортируемые элементы очереди
        queueWithoutCurrent.forEach(q => {
          if (q.track) {
            trackItems.push(
              <TrackItem
                key={`queue-${q.id}`}
                track={q.track}
                queueItemId={q.id}
                onMouseEnter={() => handleItemHover(q.track?.id, true)}
                onMouseLeave={() => handleItemHover(q.track?.id, false)}
                onDelete={handleDeleteFromQueue}
                onPlayNow={handlePlayNow}
                isPlayNowPending={playingNowId === q.id}
                onMoveUp={handleMoveUp}
                onMoveDown={handleMoveDown}
              />
            );
          }
        });

        break;
      }

      case TrackListViewMode.WithHistory: {
        const MAX_QUEUE_IN_HISTORY_MODE = 4;
        if (current) {
          trackItems.push(
            <TrackItem
              key={`current-${current.id}`}
              track={current}
              isCurrent
              isPlaying={isPlaying}
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }
        queueWithoutCurrent.slice(0, MAX_QUEUE_IN_HISTORY_MODE).forEach(q => {
          if (q.track) {
            trackItems.push(
              <TrackItem
                key={`queue-${q.id}`}
                track={q.track}
                queueItemId={q.id}
                onMouseEnter={() => handleItemHover(q.track?.id, true)}
                onMouseLeave={() => handleItemHover(q.track?.id, false)}
                onPlayNow={handlePlayNow}
                isPlayNowPending={playingNowId === q.id}
                onDelete={handleDeleteFromQueue}
                onMoveUp={handleMoveUp}
                onMoveDown={handleMoveDown}
              />
            );
          }
        });
        break;
      }

      case TrackListViewMode.Reversed: {
        history.toReversed().forEach((item, index) => {
          if (item.track) {
            trackItems.push(
              <TrackItem
                key={`history-${item.id}-${index}`}
                track={item.track}
                queueItemId={item.id}
                isHistory
                onMouseEnter={() => handleItemHover(item.track?.id, true)}
                onMouseLeave={() => handleItemHover(item.track?.id, false)}
                onPlayNow={handlePlayNow}
                isPlayNowPending={playingNowId === item.id}
              />
            );
          }
        });
        if (current) {
          trackItems.push(
            <TrackItem
              key={`current-${current.id}`}
              track={current}
              isCurrent
              isPlaying={isPlaying}
              onMouseEnter={() => handleItemHover(current.id, true)}
              onMouseLeave={() => handleItemHover(current.id, false)}
            />
          );
        }
        break;
      }
    }

    return trackItems;
  }, [
    viewMode,
    current,
    queueWithoutCurrent,
    history,
    isPlaying,
    handleItemHover,
    handleDeleteFromQueue,
    handlePlayNow,
    playingNowId,
    handleMoveUp,
    handleMoveDown,
  ]);

  const ids = queueWithoutCurrent.map(q => q.id);

  const handleDragStart = (event: DragStartEvent) => {
    const id = event.active.id as string | undefined;
    setActiveId(id ?? null);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const active = event.active?.id as string | undefined;
    const over = event.over?.id as string | undefined;
    if (active && over && active !== over) {
      const from = ids.indexOf(active);
      let to = ids.indexOf(over);
      if (from !== -1 && to !== -1) {
        // call reorder (handles optimistic update)
        if (to == 0) {
          to += 1; // фикс для корректного перемещения на первое место (там может быть текущий трек, который не участвует в сортировке)
        }

        handleReorder(active, to);
      }
    }
    setActiveId(null);
  };

  return (
    <div
      className={`${styles.leftCol} ${viewMode === TrackListViewMode.Reversed ? styles.reversedMode : ""} ${viewMode === TrackListViewMode.WithHistory ? styles.withHistory : ""}`}
    >
      <div
        ref={scrollListRef}
        className={styles.scrollList}
        onScroll={onScroll}
      >
        <DndContext
          sensors={sensors}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <SortableContext items={ids} strategy={verticalListSortingStrategy}>
            {renderTracksList}
          </SortableContext>
          <DragOverlay>
            {activeId
              ? // Minimal overlay: reuse item appearance for active id
                (() => {
                  const activeItem = queueWithoutCurrent.find(
                    q => q.id === activeId
                  );
                  if (!activeItem) return null;
                  return (
                    <div className={`${styles.item} ${styles.dragOverlay}`}>
                      <div className={styles.thumb}>
                        {activeItem.track?.artworkUrl ? (
                          <img src={activeItem.track.artworkUrl} alt="art" />
                        ) : (
                          <div className={styles.thumbPlaceholder} />
                        )}
                      </div>
                      <div className={styles.itemBody}>
                        <div className={styles.itemTitle}>
                          <span className={styles.trackName}>
                            {activeItem.track?.trackName}
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })()
              : null}
          </DragOverlay>
        </DndContext>
      </div>
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации
export const TrackColumn = memo(TrackColumnComponent);
