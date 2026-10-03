import { memo } from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateStateEnum } from "@/shared/api";

import { usePlayerStore } from "../../stores/usePlayerStore";
import {
  formatDuration,
  getAuthorsString,
  getRequestedByString,
} from "../../utils";
import styles from "./CurrentTrack.module.scss";

/**
 * Компонент отображения текущего трека
 * Показывает информацию о треке и исполнителе
 */
function CurrentTrackComponent() {
  const { currentQueueItem, isPlaying } = usePlayerStore(
    useShallow(state => ({
      currentQueueItem: state.playerState?.currentQueueItem,
      isPlaying: state.playerState?.state === PlayerStateStateEnum.Playing,
    }))
  );

  const track = currentQueueItem?.track;

  if (!track) {
    return (
      <div className={styles.container}>
        <h5 className={styles.title}>Сейчас играет</h5>
        <p className={styles.noTrack}>Нет активного трека</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h5 className={styles.title}>Сейчас играет</h5>
      <div className={`${styles.trackCard} ${isPlaying ? styles.playing : ""}`}>
        {track.artworkUrl && (
          <div className={styles.artwork}>
            <img src={track.artworkUrl} alt={track.trackName} />
            {isPlaying && (
              <div className={styles.playingOverlay}>
                <div className={styles.equalizer}>
                  <span className={styles.bar}></span>
                  <span className={styles.bar}></span>
                  <span className={styles.bar}></span>
                </div>
              </div>
            )}
          </div>
        )}
        <div className={styles.trackInfo}>
          <h6 className={styles.trackName}>{track.trackName}</h6>
          <p className={styles.trackAuthor}>
            {getAuthorsString(track.authors)}
          </p>
          {currentQueueItem.requestedByTwitchUser?.displayName && (
            <p className={styles.requestedBy}>
              Запросил:{" "}
              {getRequestedByString(
                currentQueueItem.requestedByTwitchUser.displayName
              )}
            </p>
          )}
          <div className={styles.duration}>
            {formatDuration(track.duration || "PT0S")}
          </div>
        </div>
      </div>
    </div>
  );
}

export const CurrentTrack = memo(CurrentTrackComponent);
