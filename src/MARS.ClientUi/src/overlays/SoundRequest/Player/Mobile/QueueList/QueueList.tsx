import { ChevronDown, ChevronUp } from "lucide-react";
import { memo, useCallback, useState } from "react";
import { useShallow } from "zustand/react/shallow";

import { useQueueActions } from "../../hooks";
import { usePlayerStore } from "../../stores/usePlayerStore";
import { QueueItem } from "../QueueItem";
import styles from "./QueueList.module.scss";

interface QueueListProperties {
  title: string;
  limit?: number;
  showPlayButton?: boolean;
  showDeleteButton?: boolean;
  collapsible?: boolean;
  defaultExpanded?: boolean;
}

/**
 * Список очереди треков с возможностью сворачивания
 */
function QueueListComponent({
  title,
  limit,
  showDeleteButton = false,
  collapsible = false,
  defaultExpanded = true,
}: QueueListProperties) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);
  const { handleDeleteFromQueue, deletingId } = useQueueActions();

  const { queue, loading } = usePlayerStore(
    useShallow(state => ({
      queue: state.queue,
      loading: state.loading,
    }))
  );

  const toggleExpanded = useCallback(() => {
    if (collapsible) {
      setIsExpanded(previous => !previous);
    }
  }, [collapsible]);

  const displayQueue = limit ? queue.slice(0, limit) : queue;

  return (
    <div className={styles.container}>
      <div
        className={`${styles.header} ${collapsible ? styles.clickable : ""}`}
        onClick={toggleExpanded}
      >
        <h5 className={styles.title}>
          {title} ({queue.length})
        </h5>
        {collapsible && (
          <button className={styles.toggleButton} type="button">
            {isExpanded ? <ChevronUp /> : <ChevronDown />}
          </button>
        )}
      </div>

      {isExpanded && (
        <div className={styles.list}>
          {displayQueue.length > 0 ? (
            displayQueue.map((item, index) => (
              <QueueItem
                key={item.id}
                item={item}
                index={index}
                showDeleteButton={showDeleteButton}
                onDelete={handleDeleteFromQueue}
                isDeleting={deletingId === item.id}
                isLoading={loading}
              />
            ))
          ) : (
            <p className={styles.emptyQueue}>Очередь пуста</p>
          )}
        </div>
      )}
    </div>
  );
}

export const QueueList = memo(QueueListComponent);
