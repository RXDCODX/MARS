import {
  ApiMediaInfo,
  MediaFileInfoTypeEnum,
  MediaMetaInfoPriorityEnum,
} from "@/shared/api";

export const mediaInfoFileTypes: MediaFileInfoTypeEnum[] = [
  MediaFileInfoTypeEnum.Image,
  MediaFileInfoTypeEnum.Audio,
  MediaFileInfoTypeEnum.Video,
  MediaFileInfoTypeEnum.Gif,
];

export const mediaInfoPriorities: MediaMetaInfoPriorityEnum[] = [
  MediaMetaInfoPriorityEnum.Low,
  MediaMetaInfoPriorityEnum.Normal,
  MediaMetaInfoPriorityEnum.High,
];

export function getMediaFileTypeByExtension(
  extension: string
): MediaFileInfoTypeEnum {
  const normalizedExtension = extension.toLowerCase();

  switch (normalizedExtension) {
    case ".jpg":
    case ".jpeg":
    case ".png":
    case ".webp": {
      return MediaFileInfoTypeEnum.Image;
    }
    case ".gif": {
      return MediaFileInfoTypeEnum.Gif;
    }
    case ".mp3":
    case ".wav":
    case ".ogg":
    case ".oga": {
      return MediaFileInfoTypeEnum.Audio;
    }
    case ".mp4":
    case ".webm":
    case ".mov":
    case ".avi": {
      return MediaFileInfoTypeEnum.Video;
    }
    case ".tgs": {
      return MediaFileInfoTypeEnum.TelegramSticker;
    }
    default: {
      return MediaFileInfoTypeEnum.None;
    }
  }
}

export function applySelectedFileToMediaInfo(
  source: ApiMediaInfo,
  file: File
): ApiMediaInfo {
  const extension = file.name.includes(".")
    ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase()
    : "";
  const mediaFileType = getMediaFileTypeByExtension(extension);

  let result = updateMediaInfoValue(
    updateMediaInfoValue(
      updateMediaInfoValue(source, "fileInfo.fileName", file.name),
      "fileInfo.extension",
      extension
    ),
    "fileInfo.isLocalFile",
    true
  );

  result = updateMediaInfoValue(result, "fileInfo.type", mediaFileType);

  return result;
}

export function buildMediaInfoFormData(
  alert: ApiMediaInfo,
  file?: File | null
): FormData {
  const formData = new FormData();
  formData.append("alertJson", JSON.stringify(alert));

  if (file) {
    formData.append("file", file);
  }

  return formData;
}

export function createDefaultMediaInfo(
  overrides?: Partial<ApiMediaInfo>
): ApiMediaInfo {
  return {
    id: crypto.randomUUID(),
    textInfo: {
      text: "",
      textColor: "#ffffff",
      triggerWord: "",
      keyWordsColor: "#f7c948",
      keyWordSybmolDelimiter: "#",
    },
    fileInfo: {
      fileName: "",
      filePath: "",
      extension: "",
      isLocalFile: true,
      isFileNotConvertable: false,
      type: MediaFileInfoTypeEnum.Image,
    },
    positionInfo: {
      xCoordinate: 0,
      yCoordinate: 0,
      width: 960,
      height: 540,
      rotation: 0,
      randomCoordinates: false,
      isRotated: false,
      isResizeRequires: false,
      isProportion: true,
      isHorizontalCenter: true,
      isVerticallCenter: true,
      isUseOriginalWidthAndHeight: false,
    },
    metaInfo: {
      displayName: "",
      duration: 15,
      isEnabled: true,
      isFreezeRequired: false,
      isLooped: false,
      priority: MediaMetaInfoPriorityEnum.Normal,
      twitchGuid: undefined,
      twitchPointsCost: 0,
      vip: false,
      volume: 100,
    },
    stylesInfo: {
      isBorder: false,
      isShowLetterbox: false,
    },
    ...overrides,
  };
}

export function updateMediaInfoValue(
  source: ApiMediaInfo,
  path: string,
  value: unknown
): ApiMediaInfo {
  const keys = path.split(".");
  const next = { ...source } as Record<string, unknown>;
  let current: Record<string, unknown> = next;

  for (let index = 0; index < keys.length - 1; index += 1) {
    const key = keys[index];
    current[key] = { ...(current[key] as Record<string, unknown>) };
    current = current[key] as Record<string, unknown>;
  }

  // Ключ уже проверен выше на непустоту, поэтому последний элемент задан по
  // построению. parts.at(-1) даёт string | undefined: компилятор не знает,
  // что массив уже отфильтрован на пустые части.
  const lastKey = keys[keys.length - 1]!;

  current[lastKey] = value;

  // If file info changed and displayName is empty, auto-fill it from filename/path
  try {
    const isFileInfoKey = keys[0] === "fileInfo";
    const metaInfo = (next as unknown as ApiMediaInfo).metaInfo;

    if (isFileInfoKey && metaInfo) {
      const currentDisplay = metaInfo.displayName;
      if (!currentDisplay || currentDisplay.trim() === "") {
        // prefer fileName then filePath
        const fileName = (next as unknown as ApiMediaInfo).fileInfo
          .fileName as string;
        let base = "";

        if (fileName && fileName.trim() !== "") {
          base = fileName;
        } else {
          const fp = (next as unknown as ApiMediaInfo).fileInfo
            .filePath as string;
          if (fp && fp.trim() !== "") {
            const parts = fp.split(/[/\\]/).filter(Boolean);
            base = parts.length > 0 ? parts[parts.length - 1]! : fp;
          }
        }

        if (base) {
          // strip extension
          const dotIndex = base.lastIndexOf(".");
          const nameWithoutExtension =
            dotIndex > 0 ? base.slice(0, dotIndex) : base;
          (next as unknown as ApiMediaInfo).metaInfo.displayName =
            nameWithoutExtension;
        }
      }
    }
  } catch {
    // swallow any errors in helper
  }

  return next as unknown as ApiMediaInfo;
}

export function formatMediaDuration(seconds: number): string {
  if (seconds < 60) {
    return `${seconds}с`;
  }

  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;

  return `${minutes}м ${rest.toString().padStart(2, "0")}с`;
}

export function formatMediaRewardId(rewardId?: string): string {
  if (!rewardId) {
    return "Не привязана";
  }

  return `${rewardId.slice(0, 8)}…${rewardId.slice(-6)}`;
}

/**
 * Итог записи медиа: получился идентификатор или пришёл отказ.
 *
 * Разбор вынесен сюда, потому что одинаковых мест было два — создание и
 * правка, — и оба читали `result.data`, которого на проводе нет. Сервер
 * отвечает `{ success, result, errorMessage }`, из-за чего запись проходила, а
 * страница показывала «Не удалось сохранить»: ошибка ложная, данные на сервере
 * уже лежали.
 *
 * Успех без `result` успехом не считается: увести пользователя на страницу
 * правки несуществующей записи хуже, чем сказать «не удалось».
 */
export type MediaWriteResult =
  | { ok: true; id: string }
  | { ok: false; message: string };

/** Ответ сервиса после разбора конверта транспортом. */
export const readCreateResult = (body: unknown): MediaWriteResult => {
  const message = "Не удалось сохранить медиа";

  if (typeof body !== "object" || body === null) {
    return { ok: false, message };
  }

  const envelope = body as {
    success?: boolean;
    result?: unknown;
    data?: unknown;
    errorMessage?: string | null;
  };

  if (envelope.success !== true) {
    return { ok: false, message: envelope.errorMessage ?? message };
  }

  const saved = (envelope.result ?? envelope.data) as { id?: unknown } | null;

  if (typeof saved?.id !== "string" || saved.id.length === 0) {
    return { ok: false, message: "Сервер вернул запись без идентификатора" };
  }

  return { ok: true, id: saved.id };
};
