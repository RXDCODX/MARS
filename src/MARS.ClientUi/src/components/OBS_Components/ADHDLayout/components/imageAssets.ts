// Импорт всех ассетов для использования в компонентах ADHDLayout

// Данные
import breakingTxt from "../content/breaking.txt";
// Изображения
import catisaGif from "../content/Catisa.gif";
import duolingoWebp from "../content/duolingo-normal.webp";
import dvdSvg from "../content/dvd.svg";
import emailWebp from "../content/email.webp";
// Видео
import fitnessWebm from "../content/fitness-small.webm";
import gtaMp4 from "../content/gta.mp4";
import hydraulicWebm from "../content/hydraulic-mobile.webm";
import lofiGirlGif from "../content/lofi-girl-small.gif";
import mukbangWebm from "../content/mukbang-small.webm";
import owlJson from "../content/owl.json";
import slimeWebm from "../content/slime-small.webm";
import streamerWebm from "../content/streamer.webm";
import surferMp4 from "../content/surfer.mp4";

// Типы для данных
export interface OwlQuestion {
  answer: number;
  choices: string[];
  text: string;
}

// Экспорт всех ассетов
export const dataAssets = {
  breaking: breakingTxt,
  owl: owlJson as OwlQuestion[],
};

export const imageAssets = {
  catisa: catisaGif,
  duolingo: duolingoWebp,
  dvd: dvdSvg,
  email: emailWebp,
  lofiGirl: lofiGirlGif,
};

export const videoAssets = {
  fitness: fitnessWebm,
  gta: gtaMp4,
  hydraulic: hydraulicWebm,
  mukbang: mukbangWebm,
  slime: slimeWebm,
  streamer: streamerWebm,
  surfer: surferMp4,
};

// Функции для получения путей к ассетам
export const getDataPath = (
  dataName: keyof typeof dataAssets
): string | OwlQuestion[] => {
  if (dataName in dataAssets) {
    return dataAssets[dataName];
  }
  return `../content/${dataName}.txt`;
};

export const getImagePath = (imageName: keyof typeof imageAssets): string => {
  if (imageName in imageAssets) {
    return imageAssets[imageName];
  }
  return `../content/${imageName}.gif`;
};

export const getVideoPath = (videoName: keyof typeof videoAssets): string => {
  if (videoName in videoAssets) {
    return videoAssets[videoName];
  }
  return `../content/${videoName}.webm`;
};

// Общая функция для получения любого ассета
export const getAssetPath = (
  assetName: string,
  type: "data" | "image" | "video"
): string | OwlQuestion[] => {
  switch (type) {
    case "data": {
      return getDataPath(assetName as keyof typeof dataAssets);
    }
    case "image": {
      return getImagePath(assetName as keyof typeof imageAssets);
    }
    case "video": {
      return getVideoPath(assetName as keyof typeof videoAssets);
    }
    default: {
      return `../content/${assetName}`;
    }
  }
};
