import { motion } from "framer-motion";
import { useRef, useState } from "react";

interface MarqueeTrackTitleProperties {
  text: string;
  className: string;
}

const GAP_SIZE = 56; // пиксели для промежутка
const ANIMATION_DURATION = 12; // секунды на полный цикл

export default function MarqueeTrackTitle({
  text,
  className,
}: MarqueeTrackTitleProperties) {
  const [key, setKey] = useState(0);
  const containerReference = useRef<HTMLDivElement>(null);

  const handleAnimationComplete = () => {
    setKey(previous => previous + 1);
  };

  return (
    <div
      ref={containerReference}
      className={className}
      style={{
        overflow: "hidden",
        width: "100%",
        position: "relative",
        display: "flex",
        alignItems: "center",
      }}
    >
      <motion.div
        key={key}
        initial={{ x: 0 }}
        animate={{ x: "calc(-100% - 56px)" }}
        transition={{
          duration: ANIMATION_DURATION,
          ease: "linear",
        }}
        onAnimationComplete={handleAnimationComplete}
        style={{
          display: "flex",
          whiteSpace: "nowrap",
          gap: `${GAP_SIZE}px`,
          willChange: "transform",
        }}
      >
        <span style={{ color: "white", flexShrink: 0 }}>{text}</span>
      </motion.div>
    </div>
  );
}
