import { SoundRequestPlayer } from "../Player/SoundRequestPlayer";
import styles from "./TrackList.module.scss";

/**
 * Компонент для отображения списка треков и плеера управления SoundRequest
 * Автоматически определяет разрешение экрана и рендерит соответствующий интерфейс
 */
export function TrackList() {
  return (
    <div className={styles.root} data-testid="obs-tracklist">
      <SoundRequestPlayer />
    </div>
  );
}
