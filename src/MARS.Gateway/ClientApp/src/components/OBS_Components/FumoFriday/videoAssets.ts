// Импорт видео файлов для использования в компонентах
import cirnoVideo from "./FumosVideos/cirno.webm";
import reimuVideo from "./FumosVideos/reimu.webm";

export const videoAssets = {
  cirno: cirnoVideo,
  reimu: reimuVideo,
};

// Функция для получения пути к видео с fallback
export const getVideoPath = (videoName: "cirno" | "reimu"): string => {
  if (videoName in videoAssets) {
    return videoAssets[videoName as keyof typeof videoAssets];
  }

  // Fallback на статические пути для случаев, когда импорт не работает
  return `./FumosVideos/${videoName}.webm`;
};
