import {
  AnimatePresence,
  cubicBezier,
  motion,
  Variant,
  Variants,
} from "framer-motion";
import { useEffect, useRef, useState } from "react";

import styles from "./CurrentTrack.module.scss";

interface Properties {
  children: React.ReactNode;
  AnimationStart: boolean;
  swapTrack: () => void;
}

type AnimationStage =
  | "idle"
  | "compressIn"
  | "compressOut"
  | "nowPlaying"
  | "nowPlayingExit"
  | "compressInFinal"
  | "compressOutFinal"
  | "showChildren";

export default function AnimationControl({
  children,
  AnimationStart,
  swapTrack,
}: Properties) {
  const [animationStage, setAnimationStage] = useState<AnimationStage>("idle");
  const [nowPlayingCount, setNowPlayingCount] = useState(0);

  // Запускаем анимацию ровно один раз на цикл AnimationStart=true
  const hasTriggeredReference = useRef(false);
  const hasSwappedReference = useRef(false);
  useEffect(() => {
    if (AnimationStart && !hasTriggeredReference.current) {
      hasTriggeredReference.current = true;
      setAnimationStage("compressIn");
      setNowPlayingCount(0);
      hasSwappedReference.current = false;
    }
    if (!AnimationStart) {
      hasTriggeredReference.current = false;
      hasSwappedReference.current = false;
    }
  }, [AnimationStart]);

  const handleAnimationComplete = (stage: AnimationStage) => {
    switch (stage) {
      case "compressIn": {
        setAnimationStage("compressOut");
        break;
      }
      case "nowPlaying": {
        // цикл NOW PLAYING управляется самим блоком через AnimatePresence
        break;
      }
      case "compressOut": {
        setAnimationStage("nowPlaying");
        break;
      }
      case "compressInFinal": {
        setAnimationStage("compressOutFinal");
        if (!hasSwappedReference.current) {
          hasSwappedReference.current = true;
          swapTrack();
        }
        break;
      }
      case "compressOutFinal": {
        setAnimationStage("showChildren");
        break;
      }
      case "nowPlayingExit": {
        setAnimationStage("compressInFinal");
        break;
      }
      case "showChildren": {
        setTimeout(() => setAnimationStage("idle"), 500);
        break;
      }
    }
  };

  // Варианты анимации для красных полос: каждая полоса двигается к центру
  const topBarVariants: { [key: string]: Variant } = {
    // Стартуем чуть выше контейнера, чтобы при входе полоса заезжала и закрывала контент
    idle: { top: "-3%", y: "0%" },
    compress: { top: "50%", y: "-50%" },
  };

  const bottomBarVariants: { [key: string]: Variant } = {
    // Стартуем ниже контейнера, чтобы при входе полоса поднималась и закрывала контент
    idle: { top: "100%", y: "0%" },
    compress: { top: "50%", y: "-50%" },
  };

  // Варианты для NOW PLAYING текста
  const nowPlayingVariants: { [key: string]: Variant } = {
    hidden: {
      y: -1000, // Начальная позиция выше экрана
      opacity: 0,
    },
    visible: {
      y: 0, // Конечная позиция по центру
      opacity: 1,
      transition: {
        type: "spring",
        damping: 20,
        stiffness: 300,
      },
    },
    exit: {
      y: 200, // Уезжает вниз
      opacity: 0,
      transition: {
        duration: 0.5,
        ease: "easeIn",
      },
    },
  };

  // Маска области видимости текста NOW PLAYING — совпадает с "окном" между полосами
  const maskVariants: { [key: string]: Variant } = {
    idle: { top: 0, bottom: 0 },
    compress: { top: "50%", bottom: "50%" },
  };

  // Варианты для children
  const bezierEase = cubicBezier(0.25, 0.46, 0.45, 0.94);

  const childrenVariants: Variants = {
    hidden: {
      scale: 0,
      opacity: 0,
      transition: { duration: 0.6, ease: bezierEase },
    },
    visible: {
      scale: 1,
      opacity: 1,
      transition: { duration: 0.6, ease: bezierEase },
    },
  };

  return (
    <div className={styles.stageContainer}>
      {/* Верхняя красная полоса */}
      <motion.div
        className={styles.slide}
        style={{
          position: "absolute",
          left: "2vw",
          right: "2vw",
          zIndex: 10,
        }}
        data-position="top"
        variants={topBarVariants}
        initial="idle"
        animate={
          animationStage === "compressIn" ||
          animationStage === "compressInFinal"
            ? "compress"
            : "idle"
        }
        transition={{ duration: 0.6, ease: bezierEase }}
        onAnimationComplete={() => {
          if (animationStage === "compressIn")
            handleAnimationComplete("compressIn");
          if (animationStage === "compressOut")
            handleAnimationComplete("compressOut");
          if (animationStage === "compressInFinal")
            handleAnimationComplete("compressInFinal");
          if (animationStage === "compressOutFinal")
            handleAnimationComplete("compressOutFinal");
        }}
      />

      {/* Нижняя красная полоса */}
      <motion.div
        className={styles.slide}
        style={{
          position: "absolute",
          left: "2vw",
          right: "2vw",
          zIndex: 10,
        }}
        data-position="bottom"
        variants={bottomBarVariants}
        initial="idle"
        animate={
          animationStage === "compressIn" ||
          animationStage === "compressInFinal"
            ? "compress"
            : "idle"
        }
        transition={{ duration: 0.6, ease: bezierEase }}
      />

      {/* NOW PLAYING текст с маской по окну между красными полосами */}
      <motion.div
        style={{
          position: "absolute",
          left: "2vw",
          right: "2vw",
          zIndex: 6,
          overflow: "hidden",
          pointerEvents: "none",
        }}
        variants={maskVariants}
        initial="idle"
        animate={
          animationStage === "compressIn" ||
          animationStage === "compressInFinal"
            ? "compress"
            : "idle"
        }
        transition={{ duration: 0.6, ease: bezierEase }}
      >
        <AnimatePresence
          mode="wait"
          onExitComplete={() => {
            if (animationStage === "nowPlayingExit") {
              handleAnimationComplete("nowPlayingExit");
            }
          }}
        >
          {animationStage === "nowPlaying" && (
            <motion.div
              key={`now-playing-${nowPlayingCount}`}
              style={{
                position: "absolute",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                pointerEvents: "none",
              }}
              variants={nowPlayingVariants}
              initial="hidden"
              animate="visible"
              exit="exit"
              onAnimationComplete={definition => {
                if (definition === "visible") {
                  if (nowPlayingCount < 2) {
                    setTimeout(
                      () => setNowPlayingCount(previous => previous + 1),
                      200
                    );
                  } else {
                    // Запускаем выход NOW PLAYING, после onExitComplete начнётся второй цикл полос
                    setAnimationStage("nowPlayingExit");
                  }
                }
              }}
            >
              <motion.span
                style={{
                  fontSize: "clamp(2rem, 8vw, 12rem)",
                  fontWeight: "bold",
                  color: "#fff",
                  fontFamily: "Arial, sans-serif",
                  letterSpacing: "0.2em",
                  textAlign: "center",
                  padding: "0 20px",
                }}
              >
                NOW PLAYING
              </motion.span>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>

      {/* Контент между полосами */}
      <motion.div
        style={{
          position: "relative",
          zIndex: 1,
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
        variants={childrenVariants}
        animate={
          animationStage === "showChildren" ||
          animationStage === "idle" ||
          animationStage === "compressOutFinal"
            ? "visible"
            : "hidden"
        }
        onAnimationComplete={definition => {
          if (definition === "visible" && animationStage === "showChildren") {
            handleAnimationComplete("showChildren");
          }
        }}
      >
        {children}
      </motion.div>
    </div>
  );
}
