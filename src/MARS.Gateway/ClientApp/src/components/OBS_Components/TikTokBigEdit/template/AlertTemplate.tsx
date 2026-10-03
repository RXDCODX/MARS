import { useRef, useState } from "react";

import { BigTextBlockForAudio } from "@/shared/Utils/BigTexts/BigTextBlockForAudio";

import { getRandomImage } from "../../PhonkLayout/mediaAssets";
import styles from "./AlertTemplate.module.scss";

export interface FirstPartProps {
  audioSrc: string;
  durationMs: number;
}

interface SecondPartProperties {
  message: string;
}

export interface Props {
  firstPart: FirstPartProps;
  secondPart: SecondPartProperties;
  callback: () => void;
}

export default function AlertTemplate({
  firstPart,
  secondPart,
  callback,
}: Props) {
  const { audioSrc, durationMs } = firstPart;
  const { message } = secondPart;
  const imageSource = getRandomImage();

  const [isSecondPartVisible, setIsSecondPartVisible] = useState(false);

  const audioReference = useRef<HTMLAudioElement | null>(null);

  // Используем событие loadedmetadata для чтения длительности аудио
  // и выставления видимости второй части асинхронно (в обработчике),
  // чтобы избежать синхронного вызова setState внутри эффекта.

  return (
    <>
      {/* Невидимый audio элемент в дереве для управления через реф */}
      <audio
        ref={audioReference}
        autoPlay
        onLoadedMetadata={event => {
          const durationSeconds = event.currentTarget.duration;
          if (durationSeconds * 1000 > durationMs && !isSecondPartVisible) {
            setIsSecondPartVisible(true);
          }
        }}
        onPlay={event => {
          event.currentTarget.volume = 0.27;
        }}
        onEnded={callback}
        style={{ display: "none" }}
        src={audioSrc}
      />
      {isSecondPartVisible && (
        <div className={styles.container}>
          <BigTextBlockForAudio content={message} />
          <div>
            <img className={styles.image} src={imageSource} alt="Alert Image" />
          </div>
        </div>
      )}
    </>
  );
}
