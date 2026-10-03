// AnimatedGradientBackground.tsx
import { motion } from "framer-motion";
import { useEffect } from "react";

interface AnimatedGradientBackgroundProperties {
  children: React.ReactNode;
  colors?: string[];
  duration?: number;
  gradientAngle?: number;
}

export default function AnimatedGradientBackground({
  children,
  colors = ["#ee7752", "#e73c7e", "#23a6d5", "#23d5ab"],
  duration = 15,
  gradientAngle = -45,
}: AnimatedGradientBackgroundProperties) {
  // Добавляем глобальные стили для анимации градиента
  useEffect(() => {
    const style = document.createElement("style");
    style.textContent = `
      @keyframes gradient {
        0% {
          background-position: 0% 50%;
        }
        50% {
          background-position: 100% 50%;
        }
        100% {
          background-position: 0% 50%;
        }
      }
    `;
    document.head.append(style);

    return () => style.remove();
  }, []);

  const backgroundStyle = {
    background: `linear-gradient(${gradientAngle}deg, ${colors.join(", ")})`,
    backgroundSize: "400% 400%",
    animation: `gradient ${duration}s ease infinite`,
  };

  return (
    <motion.div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        width: "100vw",
        height: "100vh",
        overflow: "hidden",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        boxShadow: "0 0 10px rgba(0, 0, 0, 0.5)",
        borderRadius: "5rem",
        textShadow: "2px 2px 4px rgba(0, 0, 0, 0.5)",
        zIndex: 1000,
        ...backgroundStyle,
      }}
    >
      {children}
    </motion.div>
  );
}
