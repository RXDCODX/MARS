import { memo } from "react";
import { Textfit } from "react-textfit";

import { TunaMusicData } from "@/shared/api";

import styles from "./CurrentTrack.module.scss";

interface Properties {
  track: TunaMusicData;
}

const CurrentTrackElementComponent = ({ track }: Properties) => (
  <div className={styles.wrapper} data-testid="current-track-element">
    <div className={styles.cover}>
      {track.cover && (
        <img
          src={track.cover}
          alt="Album cover"
          data-testid="img-album-cover"
        />
      )}
    </div>
    <div className={styles.container}>
      <div className={styles.trackinfo}>
        <div className={styles.info}>
          <Textfit
            mode="multi"
            className={styles.textContainer}
            max={9999}
            min={20}
            style={{
              width: "100%",
              textAlign: "center",
            }}
            data-testid="text-track-artists"
          >
            {track.artists.join(", ")}
          </Textfit>
          <Textfit
            mode="multi"
            className={styles.textContainer}
            max={9999}
            min={30}
            style={{
              width: "100%",
              textAlign: "center",
            }}
            data-testid="text-track-title"
          >
            {track.title}
          </Textfit>
        </div>
      </div>
    </div>
  </div>
);

// Экспортируем мемоизированную версию для оптимизации
export const CurrentTrackElement = memo(CurrentTrackElementComponent);
