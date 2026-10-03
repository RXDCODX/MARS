import track1 from "./files/audios/1.wav";
import track2 from "./files/audios/2.wav";
import track3 from "./files/audios/3.wav";
import track4 from "./files/audios/4.wav";
import track5 from "./files/audios/5.wav";
import track6 from "./files/audios/6.wav";
import track7 from "./files/audios/7.wav";
import track8 from "./files/audios/8.wav";

// Добавляйте сюда импортируемые аудио-файлы по мере необходимости,
// пример: import gao from "./voices/gao.ogg";
export const audioAssets: Record<string, string> = {
  // примерное заполнение можно добавить при необходимости
  track1,
  track2,
  track3,
  track4,
  track5,
  track6,
  track7,
  track8,
};

// Возвращает путь к аудио (ogg) — сначала проверяет импортированные ресурсы,
// затем fallback на папку `voices`.
export const getAudioPath = (audioName: string): string => {
  if (audioName in audioAssets) {
    return audioAssets[audioName];
  }

  return `./voices/${audioName}.ogg`;
};

export const getRandomAudio = (): string => {
  const audioFiles = Object.values(audioAssets);
  const randomIndex = Math.floor(Math.random() * audioFiles.length);
  return audioFiles[randomIndex];
};
