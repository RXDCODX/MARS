import { useEffect, useState } from "react";

import { useSoundRequestCommands } from "../../hooks/useSoundRequestCommands";
import styles from "./CommandCarousel.module.scss";

interface CommandInfo {
  command: string;
  description: string;
}

const DISPLAY_DURATION_MS = 7000;
const ANIMATION_DURATION_MS = 600;
const INTERVAL_MS = DISPLAY_DURATION_MS + ANIMATION_DURATION_MS;

type AnimationPhase = "entering" | "displaying" | "exiting";

interface CommandCarouselProperties {
  compact?: boolean;
}

export function CommandCarousel({ compact }: CommandCarouselProperties) {
  const apiCommands = useSoundRequestCommands();
  const commands: CommandInfo[] =
    apiCommands.length > 0
      ? apiCommands
      : [
          { command: "!sr", description: "Добавить трек в очередь" },
          {
            command: "!srwrong",
            description: "Отменить последний заказанный трек",
          },
          { command: "!song", description: "Информация о текущем треке" },
          { command: "!queue", description: "Показать вашу позицию в очереди" },
        ];

  const [currentIndex, setCurrentIndex] = useState(0);
  const [phase, setPhase] = useState<AnimationPhase>("entering");

  useEffect(() => {
    setPhase("entering");

    const enterTimer = setTimeout(() => {
      setPhase("displaying");
    }, ANIMATION_DURATION_MS);

    const exitTimer = setTimeout(() => {
      setPhase("exiting");
    }, DISPLAY_DURATION_MS + ANIMATION_DURATION_MS);

    const nextTimer = setTimeout(() => {
      setCurrentIndex(previous => (previous + 1) % commands.length);
    }, INTERVAL_MS);

    return () => {
      clearTimeout(enterTimer);
      clearTimeout(exitTimer);
      clearTimeout(nextTimer);
    };
  }, [currentIndex, commands.length]);

  const currentCommand = commands[currentIndex];

  const animationClass =
    phase === "entering"
      ? styles.entering
      : phase === "exiting"
        ? styles.exiting
        : styles.displaying;

  const sizeClass = compact ? styles.compact : styles.full;

  return (
    <div className={`${styles.carousel} ${sizeClass}`}>
      <div className={`${styles.slide} ${animationClass}`}>
        <span className={styles.commandText}>{currentCommand.command}</span>
        <span className={styles.descriptionText}>
          {currentCommand.description}
        </span>
      </div>
    </div>
  );
}
