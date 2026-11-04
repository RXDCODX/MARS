import type { CSSProperties, ReactNode } from "react";
import { useEffect, useMemo, useRef, useState } from "react";

import styles from "./CustomMarquee.module.scss";

interface CustomMarqueeProperties {
  children: ReactNode;
  speed?: number; // пиксели в секунду (по умолчанию 60)
  gap?: number; // зазор между циклами в пиксельх (по умолчанию 240)
  trailingGapCount?: number;
  pauseOnHover?: boolean;
}

/**
 * Собственный компонент бегущей строки
 * Использует CSS animation для плавного воспроизведения контента с контролируемым зазором
 */
export function CustomMarquee({
  children,
  speed = 60,
  gap = 240,
  trailingGapCount = 0,
  pauseOnHover = false,
}: CustomMarqueeProperties) {
  const contentReference = useRef<HTMLDivElement>(null);
  const [contentWidth, setContentWidth] = useState(0);
  const [isHovered, setIsHovered] = useState(false);
  const trailingGapElements = useMemo(
    () =>
      Array.from({ length: trailingGapCount }).map((_, index) => (
        <span key={index} className={styles.trailingGap} aria-hidden="true">
          &nbsp;&nbsp;
        </span>
      )),
    [trailingGapCount]
  );

  useEffect(() => {
    if (!contentReference.current) return;

    const updateWidth = () => {
      const width = contentReference.current?.scrollWidth || 0;
      setContentWidth(width);
    };

    // Обновить ширину при монтировании и при изменении размера окна
    updateWidth();
    window.addEventListener("resize", updateWidth);

    // Небольшая задержка для учета отложенной загрузки изображений
    const timer = setTimeout(updateWidth, 300);

    return () => {
      window.removeEventListener("resize", updateWidth);
      clearTimeout(timer);
    };
  }, [children]);

  if (contentWidth === 0) {
    return (
      <div
        className={styles.customMarqueeContainer}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className={styles.customMarqueeContent} ref={contentReference}>
          {children}
          {trailingGapElements}
        </div>
      </div>
    );
  }

  // Вычислить длительность анимации
  const totalDistance = contentWidth + gap;
  const duration = totalDistance / speed;

  return (
    <div
      className={styles.customMarqueeContainer}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={
        {
          "--marquee-duration": `${duration}s`,
          "--marquee-distance": `-${totalDistance}px`,
          "--marquee-paused": isHovered && pauseOnHover ? "paused" : "running",
        } as CSSProperties
      }
    >
      <div className={styles.customMarqueeContent} ref={contentReference}>
        {children}
        {trailingGapElements}
      </div>
      <div className={styles.customMarqueeContent} aria-hidden="true">
        {children}
        {trailingGapElements}
      </div>
    </div>
  );
}
