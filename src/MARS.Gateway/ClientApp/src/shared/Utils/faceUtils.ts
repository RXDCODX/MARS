// Утилиты для работы с лицами из папки ассетов

export interface FaceAsset {
  name: string;
  url: string;
  type: "image" | "video";
  extension: string;
}

/**
 * URL берутся из бандла, а не пишутся строковыми литералами.
 *
 * Литералы вида `/src/assets/faces/giga-chad.gif` работают только в `yarn dev`,
 * где исходники отдаёт сам Vite. В production-сборке Vite про такие строки не
 * знает и файлы в бандл не кладёт, а nginx отвечает на них `try_files`
 * индексом: `<img>` получает HTML с `X-Content-Type-Options: nosniff`, стреляет
 * `error`, а не `load`. На `/highlite` это было не косметикой — событие загрузки
 * лица двигает единственный путь удаления сообщения, то есть сообщение залипало
 * на экране навсегда и копилось в очереди.
 *
 * Ключи здесь — имена файлов, а не URL: при хэшировании имена меняются.
 */
const bundledFaces = import.meta.glob<string>(
  "../../assets/faces/*.{gif,mp4}",
  {
    eager: true,
    import: "default",
    query: "?url",
  }
);

const bundledByFileName = new Map(
  Object.entries(bundledFaces).map(([path, url]) => [
    path.split("/").pop() ?? path,
    url,
  ])
);

/**
 * Имя лица — это подпись, а не часть контракта.
 *
 * Имя файла и подпись совпадают у большинства лиц, но не у всех: `minions`
 * лежит в `despicable-me-minions.gif`, а `vargillllll` — в
 * `vargilllll-vargil.gif`. Подпись показывается в `alt`, поэтому сопоставление
 * приходится держать руками. Никакой разбор события по имени не идёт:
 * `Message.tsx` берёт случайное лицо из списка, а поле `faceUrlJson` с сервера
 * не читает вовсе — это отдельное решение, а не контракт этого файла.
 */
const FACE_NAMES: Readonly<Record<string, string>> = {
  "1233233.gif.mp4": "1233233",
  "3d-saul-saul-goodman.gif": "3d-saul-saul-goodman",
  "animation.gif.mp4": "animation",
  "blank-stare-really.gif": "blank-stare-really",
  "blue-archive-blue-archive-arisu.gif": "blue-archive-arisu",
  "clash-royale.gif": "clash-royale",
  "cristiano-ronaldo-soccer.gif": "cristiano-ronaldo",
  "dante-dante-devil-may-cry.mp4": "dante-dmc",
  "despicable-me-minions.gif": "minions",
  "devil-may-cry-dmc.gif": "devil-may-cry",
  "dono-wall.gif": "dono-wall",
  "ferass18.gif": "ferass18",
  "funny-dogs-cute.gif": "funny-dogs",
  "giga-chad.gif": "giga-chad",
  "homelander-milk.gif": "homelander-milk",
  "homelander-the-boys.gif": "homelander",
  "marin-kitagawa.gif": "marin-kitagawa",
  "mika-misono-mika.gif": "mika-misono",
  "plink-cat-plink.gif": "plink-cat",
  "sus-suspicious.gif": "sus",
  "tachibana-hikari-blue-archive.gif": "tachibana-hikari",
  "tendou-aris-blue-archive.gif": "tendou-aris",
  "vargillllll-vargil.gif": "vargillllll",
  "video_2025-02-02_17-11-44.mp4": "video-2025",
};

const VIDEO_EXTENSIONS = new Set([".mp4", ".webm", ".avi"]);

export const FACE_ASSETS: FaceAsset[] = Object.entries(FACE_NAMES)
  .map(([fileName, name]): FaceAsset => {
    const url = bundledByFileName.get(fileName);

    // Файл есть в папке, но не в списке: это новое лицо. Молча включать его в
    // ротацию нельзя — от неё зависит то, что видит зритель, — а молча ронять
    // тоже: событие выбрало бы его и не показало бы.
    if (url === undefined) {
      throw new Error(
        `Лицо «${name}» (${fileName}) есть в FACE_NAMES, но файла нет в assets/faces.`
      );
    }

    const extension = fileName.slice(fileName.lastIndexOf("."));

    return {
      name,
      url,
      type: VIDEO_EXTENSIONS.has(extension) ? "video" : "image",
      extension,
    };
  })
  .sort((left, right) => left.name.localeCompare(right.name));

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
