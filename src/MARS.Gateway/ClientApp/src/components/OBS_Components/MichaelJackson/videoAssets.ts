// Импорт видео файлов для использования в компонентах
import michael1Video from "./videos/Michael_1.webm";
import michael2Video from "./videos/Michael_2.webm";

export const videoAssets = {
  michael_1: michael1Video,
  michael_2: michael2Video,
};

// Функция для получения пути к видео с fallback
export const getVideoPath = (videoName: "michael_1" | "michael_2"): string => {
  if (videoName in videoAssets) {
    return videoAssets[videoName as keyof typeof videoAssets];
  }

  // Fallback на статические пути для случаев, когда импорт не работает
  return `./videos/${videoName}.webm`;
};
