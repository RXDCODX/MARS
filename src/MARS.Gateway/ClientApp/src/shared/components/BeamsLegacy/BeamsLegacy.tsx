import { CSSProperties, useMemo } from "react";

import styles from "./BeamsLegacy.module.scss";

type BeamsLegacyProperties = {
  beamWidth?: number;
  beamHeight?: number;
  beamNumber?: number;
  lightColor?: string;
  speed?: number;
  noiseIntensity?: number;
  scale?: number;
  rotation?: number;
};

const BeamsLegacy: React.FC<BeamsLegacyProperties> = ({
  beamWidth = 2,
  beamHeight = 15,
  beamNumber = 12,
  lightColor = "#ffffff",
  speed = 2,
  noiseIntensity = 1.75,
  scale = 0.2,
  rotation = 0,
}) => {
  const beams = useMemo(
    () =>
      Array.from(
        { length: Math.max(1, Math.floor(beamNumber)) },
        (_, index) => {
          const progress = index / Math.max(1, beamNumber - 1);
          const left = 10 + progress * 80;
          return {
            id: index,
            style: {
              "--beam-left": `${left}%`,
              "--beam-width": `${Math.max(0.5, beamWidth * (0.7 + progress * 0.8))}%`,
              "--beam-height": `${Math.max(80, beamHeight * 8)}%`,
              "--beam-delay": `${index * 0.2}s`,
              "--beam-duration": `${Math.max(4, 14 / Math.max(0.3, speed))}s`,
              "--beam-pulse": `${Math.max(1.5, 5 / Math.max(0.3, speed))}s`,
              "--beam-rot-start": `${-22 + progress * 44 + rotation}deg`,
              "--beam-rot-mid": `${-14 + progress * 28 + rotation}deg`,
              "--beam-rot-end": `${-22 + progress * 44 + rotation}deg`,
              "--beam-opacity": `${Math.min(0.85, 0.2 + noiseIntensity * 0.08 + progress * 0.25)}`,
              "--beam-blur": `${Math.max(0, 6 * scale + index * 0.18)}px`,
              color: lightColor,
            } as CSSProperties,
          };
        }
      ),
    [
      beamNumber,
      beamWidth,
      beamHeight,
      speed,
      rotation,
      noiseIntensity,
      scale,
      lightColor,
    ]
  );

  return (
    <div className={styles.beamsLegacy} aria-hidden>
      {beams.map(beam => (
        <div
          key={beam.id}
          className={styles.beamsLegacyBeam}
          style={beam.style}
        />
      ))}
      <div className={styles.beamsLegacyNoise} />
    </div>
  );
};

export default BeamsLegacy;
