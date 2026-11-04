import {
  animate,
  motion,
  useMotionValue,
  useMotionValueEvent,
  useTransform,
} from "framer-motion";
import { Volume2, VolumeX } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import styles from "./ElasticSlider.module.scss";

const MAX_OVERFLOW = 50;

interface ElasticSliderProperties {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
}

function decay(value: number, max: number): number {
  if (max === 0) {
    return 0;
  }

  const entry = value / max;
  const sigmoid = 2 * (1 / (1 + Math.exp(-entry)) - 0.5);

  return sigmoid * max;
}

export function ElasticSlider({
  value,
  onChange,
  min = 0,
  max = 100,
  step = 1,
  disabled = false,
}: ElasticSliderProperties) {
  const [localValue, setLocalValue] = useState(value);
  const sliderReference = useRef<HTMLDivElement>(null);
  const [region, setRegion] = useState<"left" | "middle" | "right">("middle");
  const clientX = useMotionValue(0);
  const overflow = useMotionValue(0);
  const scale = useMotionValue(1);

  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  useMotionValueEvent(clientX, "change", latest => {
    if (!sliderReference.current) {
      return;
    }

    const { left, right } = sliderReference.current.getBoundingClientRect();
    let newValue: number;

    if (latest < left) {
      setRegion("left");
      newValue = left - latest;
    } else if (latest > right) {
      setRegion("right");
      newValue = latest - right;
    } else {
      setRegion("middle");
      newValue = 0;
    }

    overflow.jump(decay(newValue, MAX_OVERFLOW));
  });

  const handlePointerMove = useCallback(
    (e: React.PointerEvent<HTMLDivElement>) => {
      if (disabled) return;

      if (e.buttons > 0 && sliderReference.current) {
        const { left, width } = sliderReference.current.getBoundingClientRect();
        let newValue = min + ((e.clientX - left) / width) * (max - min);

        if (step > 0) {
          newValue = Math.round(newValue / step) * step;
        }

        newValue = Math.min(Math.max(newValue, min), max);
        setLocalValue(newValue);
        onChange(newValue);
        clientX.jump(e.clientX);
      }
    },
    [clientX, disabled, max, min, onChange, step]
  );

  const handlePointerDown = useCallback(
    (e: React.PointerEvent<HTMLDivElement>) => {
      if (disabled) return;

      handlePointerMove(e);
      e.currentTarget.setPointerCapture(e.pointerId);
    },
    [disabled, handlePointerMove]
  );

  const handlePointerUp = useCallback(() => {
    if (disabled) return;

    animate(overflow, 0, { type: "spring", bounce: 0.5 });
  }, [disabled, overflow]);

  const rangePercentage = useMemo(() => {
    const totalRange = max - min;
    if (totalRange === 0) return 0;

    return ((localValue - min) / totalRange) * 100;
  }, [localValue, max, min]);

  return (
    <div className={styles.wrapper}>
      <motion.div
        onHoverStart={() => !disabled && animate(scale, 1.2)}
        onHoverEnd={() => !disabled && animate(scale, 1)}
        onTouchStart={() => !disabled && animate(scale, 1.2)}
        onTouchEnd={() => !disabled && animate(scale, 1)}
        style={{
          scale,
          opacity: useTransform(scale, [1, 1.2], [0.7, 1]),
        }}
        className={styles.sliderContainer}
      >
        <motion.div
          animate={{
            scale: region === "left" ? [1, 1.4, 1] : 1,
            transition: { duration: 0.25 },
          }}
          style={{
            x: useTransform(() =>
              region === "left" ? -overflow.get() / scale.get() : 0
            ),
          }}
          className={styles.icon}
        >
          <VolumeX />
        </motion.div>

        <div
          ref={sliderReference}
          className={styles.sliderRoot}
          onPointerMove={handlePointerMove}
          onPointerDown={handlePointerDown}
          onPointerUp={handlePointerUp}
          style={{ cursor: disabled ? "not-allowed" : "pointer" }}
        >
          <motion.div
            style={{
              scaleX: useTransform(() => {
                if (sliderReference.current) {
                  const { width } =
                    sliderReference.current.getBoundingClientRect();
                  return 1 + overflow.get() / width;
                }
                return 1;
              }),
              scaleY: useTransform(overflow, [0, MAX_OVERFLOW], [1, 0.8]),
              transformOrigin: useTransform(() => {
                if (sliderReference.current) {
                  const { left, width } =
                    sliderReference.current.getBoundingClientRect();
                  return clientX.get() < left + width / 2 ? "right" : "left";
                }
                return "center";
              }),
              height: useTransform(scale, [1, 1.2], [6, 12]),
              marginTop: useTransform(scale, [1, 1.2], [0, -3]),
              marginBottom: useTransform(scale, [1, 1.2], [0, -3]),
            }}
            className={styles.sliderTrackWrapper}
          >
            <div className={styles.sliderTrack}>
              <div
                className={styles.sliderRange}
                style={{ width: `${rangePercentage}%` }}
              />
            </div>
          </motion.div>
        </div>

        <motion.div
          animate={{
            scale: region === "right" ? [1, 1.4, 1] : 1,
            transition: { duration: 0.25 },
          }}
          style={{
            x: useTransform(() =>
              region === "right" ? overflow.get() / scale.get() : 0
            ),
          }}
          className={styles.icon}
        >
          <Volume2 />
        </motion.div>
      </motion.div>
      <p className={styles.valueIndicator}>{Math.round(localValue)}%</p>
    </div>
  );
}
