import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { ChevronDown, ChevronUp, Move, Play, X } from "lucide-react";
import { memo, useCallback } from "react";

import { BaseTrackInfo } from "@/shared/api";

import { formatDuration } from "../../utils";
import styles from "../SoundRequestPlayerDesktop.module.scss";

interface TrackItemProperties {
  track: BaseTrackInfo;
  queueItemId?: string;
  isCurrent?: boolean;
  isPlaying?: boolean;
  isHistory?: boolean;
  onMouseEnter?: () => void;
  onMouseLeave?: () => void;
  onPlayNow?: (queueItemId: string) => void;
  onDelete?: (queueItemId: string) => void;
  onMoveUp?: (queueItemId: string) => void;
  onMoveDown?: (queueItemId: string) => void;
  onDropTo?: (targetQueueItemId: string, sourceQueueItemId: string) => void;
  onNativeDragEnter?: (queueItemId: string) => void;
  onNativeDragOver?: (queueItemId: string) => void;
  onNativeDragLeave?: (queueItemId: string) => void;
  showInsertAbove?: boolean;
  isPlayNowPending?: boolean;
}

function TrackItemComponent({
  track,
  queueItemId,
  isCurrent = false,
  isPlaying = false,
  isHistory = false,
  onMouseEnter,
  onMouseLeave,
  onPlayNow,
  onDelete,
  isPlayNowPending = false,
  onMoveUp,
  onMoveDown,
  onNativeDragOver,
  showInsertAbove = false,
}: TrackItemProperties) {
  const showPlayingIndicator = isCurrent && isPlaying;

  const aa = useSortable({ id: queueItemId! });

  // dnd-kit sortable
  const sortable = queueItemId ? aa : null;
  const setNodeReference = sortable ? sortable.setNodeRef : undefined;
  const transform = sortable ? sortable.transform : undefined;
  const transition = sortable ? sortable.transition : undefined;
  const isDragging = sortable ? sortable.isDragging : false;
  const attributes = sortable ? sortable.attributes : undefined;
  const listeners = sortable ? sortable.listeners : undefined;

  const handlePlayNow = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      if (queueItemId && onPlayNow) {
        onPlayNow(queueItemId);
      }
    },
    [queueItemId, onPlayNow]
  );

  const handleDelete = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      if (queueItemId && onDelete) {
        onDelete(queueItemId);
      }
    },
    [queueItemId, onDelete]
  );

  if (queueItemId && onNativeDragOver) onNativeDragOver(queueItemId);
  const handleMoveUp = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      if (queueItemId && onMoveUp) onMoveUp(queueItemId);
    },
    [onMoveUp, queueItemId]
  );

  const handleMoveDown = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      if (queueItemId && onMoveDown) onMoveDown(queueItemId);
    },
    [queueItemId, onMoveDown]
  );

  // we no longer use native drag handlers when using dnd-kit; keep callbacks for fallback

  const style: React.CSSProperties = {};
  if (transform) {
    style.transform = CSS.Transform.toString(transform) || undefined;
  }
  if (transition) {
    style.transition = transition;
  }

  return (
    <div
      ref={setNodeReference}
      className={`${styles.item} ${isCurrent ? `${styles.sticky} ${styles.current}` : ""} ${isHistory ? styles.historyItem : ""} ${isDragging ? "dragging" : ""}`}
      data-track-id={track.id}
      data-testid={`track-item-${track.id}`}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      key={track.id}
      tabIndex={queueItemId ? 0 : -1}
      onKeyDown={e => {
        if (!queueItemId) return;
        if (e.key === "ArrowUp") {
          e.preventDefault();
          if (onMoveUp) onMoveUp(queueItemId);
        } else if (e.key === "ArrowDown") {
          e.preventDefault();
          if (onMoveDown) onMoveDown(queueItemId);
        }
      }}
      style={style}
    >
      {showInsertAbove && <div className={styles.insertIndicator} />}

      <div className={styles.controlsColumn}>
        {/* <button
          className={styles.moveButton}
          onClick={handleMoveUp}
          title="Переместить выше"
          type="button"
        >
          <ChevronUp size={16} />
        </button>
        <button
          className={styles.moveButton}
          onClick={handleMoveDown}
          title="Переместить ниже"
          type="button"
        >
          <ChevronDown size={16} />
        </button> */}
        <div
          className={styles.dragHandle}
          title="Перетащить"
          {...(attributes || {})}
          {...(listeners || {})}
        >
          <Move size={14} />
        </div>
      </div>
      <div
        className={`${styles.thumb} ${showPlayingIndicator ? styles.thumbPlaying : ""}`}
      >
        {track.artworkUrl ? (
          <img src={track.artworkUrl} alt="art" />
        ) : (
          <div className={styles.thumbPlaceholder} />
        )}
        {showPlayingIndicator && (
          <div className={styles.playingOverlay}>
            <div className={styles.playingIndicator}>
              <span className={styles.equalizer}>
                <span className={styles.bar}></span>
                <span className={styles.bar}></span>
                <span className={styles.bar}></span>
              </span>
            </div>
          </div>
        )}
        {!isCurrent && queueItemId && onPlayNow && (
          <button
            className={`${styles.thumbPlayButton}`}
            onClick={handlePlayNow}
            title="Воспроизвести сейчас"
            type="button"
            disabled={isPlayNowPending}
          >
            <Play size={24} />
          </button>
        )}
      </div>
      {!isCurrent && queueItemId && onDelete && (
        <button
          className={`${styles.actionButton} ${styles.deleteButton}`}
          onClick={handleDelete}
          title="Удалить из очереди"
          type="button"
        >
          <X size={18} />
        </button>
      )}
      <div className={styles.itemBody}>
        <div className={styles.itemTitle}>
          <span className={styles.trackName}>{track.trackName}</span>
          {track.authors && track.authors.length > 0 && (
            <span className={styles.itemAuthors}>
              {track.authors.join(", ")}
            </span>
          )}
        </div>
        <div className={styles.itemMeta}>
          <a
            href={track.url}
            target="_blank"
            rel="noreferrer"
            className={styles.itemLink}
            title={track.url}
          >
            {track.url}
          </a>
          <span className={styles.itemDuration}>
            {formatDuration(track.duration || "PT0S")}
          </span>
        </div>
      </div>
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации
export const TrackItem = memo(TrackItemComponent);
