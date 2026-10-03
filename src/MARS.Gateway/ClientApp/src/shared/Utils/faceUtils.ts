// Утилиты для работы с лицами из папки ассетов

export interface FaceAsset {
  name: string;
  url: string;
  type: "image" | "video";
  extension: string;
}

// Список всех доступных лиц из папки ассетов
export const FACE_ASSETS: FaceAsset[] = [
  {
    name: "1233233",
    url: "/src/assets/faces/1233233.gif.mp4",
    type: "video",
    extension: ".mp4",
  },
  {
    name: "3d-saul-saul-goodman",
    url: "/src/assets/faces/3d-saul-saul-goodman.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "animation",
    url: "/src/assets/faces/animation.gif.mp4",
    type: "video",
    extension: ".mp4",
  },
  {
    name: "blank-stare-really",
    url: "/src/assets/faces/blank-stare-really.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "blue-archive-arisu",
    url: "/src/assets/faces/blue-archive-blue-archive-arisu.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "clash-royale",
    url: "/src/assets/faces/clash-royale.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "cristiano-ronaldo",
    url: "/src/assets/faces/cristiano-ronaldo-soccer.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "dante-dmc",
    url: "/src/assets/faces/dante-dante-devil-may-cry.mp4",
    type: "video",
    extension: ".mp4",
  },
  {
    name: "minions",
    url: "/src/assets/faces/despicable-me-minions.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "devil-may-cry",
    url: "/src/assets/faces/devil-may-cry-dmc.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "dono-wall",
    url: "/src/assets/faces/dono-wall.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "ferass18",
    url: "/src/assets/faces/ferass18.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "funny-dogs",
    url: "/src/assets/faces/funny-dogs-cute.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "giga-chad",
    url: "/src/assets/faces/giga-chad.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "homelander-milk",
    url: "/src/assets/faces/homelander-milk.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "homelander",
    url: "/src/assets/faces/homelander-the-boys.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "marin-kitagawa",
    url: "/src/assets/faces/marin-kitagawa.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "mika-misono",
    url: "/src/assets/faces/mika-misono-mika.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "plink-cat",
    url: "/src/assets/faces/plink-cat-plink.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "sus",
    url: "/src/assets/faces/sus-suspicious.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "tachibana-hikari",
    url: "/src/assets/faces/tachibana-hikari-blue-archive.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "tendou-aris",
    url: "/src/assets/faces/tendou-aris-blue-archive.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "vargillllll",
    url: "/src/assets/faces/vargillllll-vargil.gif",
    type: "image",
    extension: ".gif",
  },
  {
    name: "video-2025",
    url: "/src/assets/faces/video_2025-02-02_17-11-44.mp4",
    type: "video",
    extension: ".mp4",
  },
];

/**
 * Получает случайное лицо из доступных ассетов
 */
export function getRandomFace(): FaceAsset {
  const randomIndex = Math.floor(Math.random() * FACE_ASSETS.length);
  return FACE_ASSETS[randomIndex];
}

/**
 * Получает случайное лицо определенного типа
 */
export function getRandomFaceByType(type: "image" | "video"): FaceAsset {
  const filteredFaces = FACE_ASSETS.filter(face => face.type === type);
  const randomIndex = Math.floor(Math.random() * filteredFaces.length);
  return filteredFaces[randomIndex];
}

/**
 * Получает лицо по имени
 */
export function getFaceByName(name: string): FaceAsset | undefined {
  return FACE_ASSETS.find(face => face.name === name);
}

/**
 * Проверяет, является ли файл видео
 */
export function isVideoFile(url: string): boolean {
  return url.includes(".mp4") || url.includes(".webm") || url.includes(".avi");
}

/**
 * Получает URL для отображения лица
 */
export function getFaceDisplayUrl(face: FaceAsset): string {
  return face.url;
}
