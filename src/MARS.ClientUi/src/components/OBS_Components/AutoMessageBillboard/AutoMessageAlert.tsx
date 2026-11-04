import { motion } from "framer-motion";
import { useEffect, useMemo, useRef } from "react";

import useTwitchStore from "@/shared/twitchStore/twitchStore";
import { replaceEmotes } from "@/shared/Utils";

import common from "../OBSCommon.module.scss";

interface Properties {
  message: string;
  onComplete: () => void;
}

// Массив ярких цветов для градиентов
const gradientColors = [
  ["#667eea", "#764ba2"], // Фиолетово-синий
  ["#f093fb", "#f5576c"], // Розово-красный
  ["#4facfe", "#00f2fe"], // Голубой
  ["#43e97b", "#38f9d7"], // Зеленый
  ["#fa709a", "#fee140"], // Розово-желтый
  ["#a8edea", "#fed6e3"], // Мятно-розовый
  ["#ff9a9e", "#fecfef"], // Кораллово-розовый
  ["#ffecd2", "#fcb69f"], // Оранжево-персиковый
  ["#ff9a9e", "#fad0c4"], // Розово-персиковый
  ["#a18cd1", "#fbc2eb"], // Лавандово-розовый
  ["#fad0c4", "#ffd1ff"], // Персиково-розовый
  ["#ffecd2", "#fcb69f"], // Кремово-оранжевый
  ["#ff9a9e", "#fecfef"], // Розово-коралловый
  ["#a8edea", "#fed6e3"], // Мятно-розовый
  ["#d299c2", "#fef9d7"], // Сиренево-кремовый
];

export default function AutoMessageAlert({ message, onComplete }: Properties) {
  const timeoutReference = useRef<NodeJS.Timeout | null>(null);

  // Получаем парсеры для эмодзи
  const parser = useTwitchStore(state => state.parser);
  const parserToLink = useTwitchStore(state => state.parseToLink);

  // Генерируем случайный градиент для каждого сообщения
  const randomGradient = useMemo(() => {
    const randomIndex = Math.floor(Math.random() * gradientColors.length);
    const [color1, color2] = gradientColors[randomIndex];
    return `linear-gradient(135deg, ${color1} 0%, ${color2} 100%)`;
  }, []);

  useEffect(() => {
    // Через 8 секунд вызываем onComplete для исчезновения
    timeoutReference.current = setTimeout(() => {
      onComplete();
    }, 8000);

    return () => {
      if (timeoutReference.current) {
        clearTimeout(timeoutReference.current);
      }
    };
  }, [onComplete]);

  // Обрабатываем сообщение с эмодзи
  const processedMessage =
    parser && parserToLink
      ? replaceEmotes({
          text: message,
          parser,
          newParser: parserToLink,
        })
      : message;

  return (
    <motion.div
      initial={{ x: 400, opacity: 0 }}
      animate={{
        x: 0,
        opacity: 1,
        boxShadow: [
          "0 10px 30px rgba(0, 0, 0, 0.3)",
          "0 15px 40px rgba(255, 255, 255, 0.4)",
          "0 20px 50px rgba(0, 0, 0, 0.2)",
          "0 15px 40px rgba(255, 255, 255, 0.4)",
          "0 10px 30px rgba(0, 0, 0, 0.3)",
        ],
      }}
      exit={{
        x: 400,
        opacity: 0,
        scale: 0.8,
      }}
      transition={{
        type: "spring",
        stiffness: 100,
        damping: 20,
        duration: 0.5,
        boxShadow: {
          duration: 1.5,
          repeat: Infinity,
          ease: "easeInOut",
        },
      }}
      style={{
        position: "absolute",
        top: "50%",
        right: "20px",
        transform: "translateY(-50%)",
        width: "350px",
        background: randomGradient,
        borderRadius: "15px",
        padding: "20px",
        color: "white",
        fontFamily: '"Segoe UI", Tahoma, Geneva, Verdana, sans-serif',
        fontWeight: 600,
        fontSize: "16px",
        lineHeight: 1.4,
        textAlign: "center",
        border: "2px solid rgba(255, 255, 255, 0.2)",
        overflow: "visible",
        whiteSpace: "break-spaces",
        zIndex: 1000,
      }}
    >
      <div
        className={common.textStrokeShadow}
        style={{
          wordWrap: "break-word",
          hyphens: "auto",
          textShadow:
            "2px 2px 4px rgba(0, 0, 0, 0.5), 0 0 10px rgba(0, 0, 0, 0.3), 0 0 20px rgba(0, 0, 0, 0.2)",
          margin: 0,
          padding: 0,
          fontWeight: 700,
        }}
      >
        {processedMessage}
        <style>{`
          .emote {
            display: inline-block !important;
            vertical-align: middle !important;
            height: 1.2em !important;
            width: auto !important;
            margin: 0 2px !important;
            border-radius: 4px !important;
            transition: all 0.3s ease !important;
            filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3)) !important;
            animation: emoteFloat 2s ease-in-out infinite !important;
          }
          
          .emote:hover {
            transform: scale(1.1) rotate(5deg) !important;
            filter: drop-shadow(0 4px 8px rgba(0, 0, 0, 0.4)) brightness(1.1) !important;
          }
          
          @keyframes emoteFloat {
            0%, 100% {
              transform: translateY(0px);
            }
            50% {
              transform: translateY(-2px);
            }
          }
        `}</style>
      </div>
    </motion.div>
  );
}
