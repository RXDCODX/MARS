import { Card } from "antd";
import { memo } from "react";

import { CurrentTrack } from "./CurrentTrack";
import { PlayerControls } from "./PlayerControls";
import { QueueList } from "./QueueList";
import styles from "./SoundRequestPlayerMobile.module.scss";
import { VolumeControls } from "./VolumeControls";

function SoundRequestPlayerMobileComponent() {
  return (
    <div className={styles.root} data-testid="obs-sr-player-mobile">
      <Card className={styles.card}>
        <CurrentTrack />
      </Card>

      <Card className={styles.card}>
        <h5 className={styles.sectionTitle}>Управление</h5>
        <div className={styles.controlsSection}>
          <PlayerControls />
          <VolumeControls />
        </div>
      </Card>

      <Card className={styles.card}>
        <QueueList
          title="Очередь"
          showPlayButton
          showDeleteButton
          collapsible
          defaultExpanded={false}
        />
      </Card>

      <Card className={styles.card}>
        <QueueList
          title="Ближайшие 5 заказов"
          limit={5}
          showPlayButton
          collapsible={false}
        />
      </Card>
    </div>
  );
}

export const SoundRequestPlayerMobile = memo(SoundRequestPlayerMobileComponent);
