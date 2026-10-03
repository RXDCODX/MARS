import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import path from "path";
import { defineConfig } from "vite";
import viteCompression from "vite-plugin-compression";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    tailwindcss(),
    react(),
    // Compression для production
    viteCompression({
      algorithm: "gzip",
      ext: ".gz",
      threshold: 1024, // Минимальный размер файла для сжатия (1KB)
    }),
  ],
  assetsInclude: ["**/*.webm", "**/*.mp4"],
  css: {
    preprocessorOptions: {
      scss: {
        quietDeps: true,
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
      "@/components": path.resolve(import.meta.dirname, "./src/components"),
      "@/Site": path.resolve(import.meta.dirname, "./src/Site"),
      "@/shared": path.resolve(import.meta.dirname, "./src/shared"),
      "@/contexts": path.resolve(import.meta.dirname, "./src/contexts"),
      "@/routes": path.resolve(import.meta.dirname, "./src/routes"),
      "@/app": path.resolve(import.meta.dirname, "./src/app"),
      "@/assets": path.resolve(import.meta.dirname, "./src/assets"),
      "@/styles": path.resolve(import.meta.dirname, "./src/styles"),
      "@/utils": path.resolve(import.meta.dirname, "./src/shared/Utils"),
      "@/api": path.resolve(import.meta.dirname, "./src/shared/api"),
      "@/types": path.resolve(import.meta.dirname, "./src/shared/types"),
    },
  },
  build: {
    sourcemap: false,
    // Включаем code splitting для CSS - каждый lazy компонент получит свой CSS файл
    cssCodeSplit: true,
    rolldownOptions: {
      onwarn(warning, warn) {
        const isLottieEvalWarning =
          warning.code === "EVAL" &&
          warning.id?.includes("@lottiefiles/lottie-player/dist/tgs-player.js");

        if (!isLottieEvalWarning) {
          warn(warning);
        }
      },
      output: {
        // Настройка имен для JS чанков
        chunkFileNames: "assets/js/[name]-[hash].js",
        // Настройка имен для entry файлов
        entryFileNames: "assets/js/[name]-[hash].js",
        // Настройка имен для статических ассетов по типам
        assetFileNames: assetInfo => {
          // Используем names (массив) вместо устаревшего name
          const name = assetInfo.names?.[0] || "";

          // CSS файлы
          if (name.endsWith(".css")) {
            return "assets/css/[name]-[hash].css";
          }

          // Изображения
          if (/\.(png|jpe?g|gif|svg|webp|avif|ico)$/i.test(name)) {
            return "assets/images/[name]-[hash][extname]";
          }

          // Видео
          if (/\.(mp4|webm|ogg|mov|avi)$/i.test(name)) {
            return "assets/videos/[name]-[hash][extname]";
          }

          // Аудио
          if (/\.(mp3|wav|ogg|flac|aac|m4a)$/i.test(name)) {
            return "assets/audio/[name]-[hash][extname]";
          }

          // Шрифты
          if (/\.(woff2?|eot|ttf|otf)$/i.test(name)) {
            return "assets/fonts/[name]-[hash][extname]";
          }

          // Все остальные файлы
          return "assets/other/[name]-[hash][extname]";
        },
      },
    },
  },
  server: {
    port: 44478,
  },
  optimizeDeps: {
    include: [
      // Twitch библиотеки - для решения проблемы с cacheSymbol в dev режиме
      "@twurple/api",
      "@twurple/auth",
      "@twurple/chat",
      "@mkody/twitch-emoticons",
      // HTML парсер - для предотвращения конфликтов инициализации
      "html-react-parser",
      // UMD-пакет: корректная CJS→ESM конвертация в dev-режиме
      "react-roulette-pro",
    ],
  },
});
