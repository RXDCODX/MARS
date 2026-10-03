import { useRef, useState } from "react";

import { useInjectStyles } from "@/shared/hooks";

import { getRandomAudio, getRandomImage } from "./mediaAssets";
import styles from "./PhonkShitAlerts.module.scss";

export default function PhonkShitAlerts({
  callback,
}: {
  callback: () => void;
}) {
  const [imageSource] = useState<string>(getRandomImage());
  const [audioSource] = useState<string>(getRandomAudio());
  const audioReference = useRef<HTMLAudioElement | null>(null);

  useInjectStyles(`
    body {
      background-color: #00000052 !important;
    }
    
    `);

  return (
    <div className={styles.container} data-testid="phonk-alert">
      {/* Left black bar */}
      <div className={`${styles.sideBar} ${styles.leftBar}`} />

      {/* Right black bar */}
      <div className={`${styles.sideBar} ${styles.rightBar}`} />

      {/* Невидимый audio элемент в дереве для управления через реф */}
      <audio
        ref={audioReference}
        autoPlay
        onPlay={event => {
          event.currentTarget.volume = 0.22;
        }}
        onEnded={callback}
        style={{ display: "none" }}
        src={audioSource}
      />

      {/* Random image positioned 22% from bottom, centered horizontally */}
      {imageSource && <img src={imageSource} alt="" className={styles.image} />}
    </div>
  );
}
