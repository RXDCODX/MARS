import { useCallback } from "react";

import { MediaDto, MediaFileInfoTypeEnum } from "@/shared/api";

import { Audio, Image, Video, Voice } from "./Primitive";
import TelegramSticker from "./Primitive/TelegramSticker";

interface Properties {
  message?: MediaDto;
  remove: (message: MediaDto) => void;
}

export default function Alert(messageProperties: Properties) {
  const message = messageProperties.message;

  if (!message) {
    throw new Error("Message is required");
  }

  const { fileInfo } = message.mediaInfo;
  const callback = useCallback(
    () => messageProperties.remove(message),
    [message, messageProperties]
  );

  switch (fileInfo.type) {
    case MediaFileInfoTypeEnum.Image:
    case MediaFileInfoTypeEnum.Gif: {
      return (
        <Image
          key={message.mediaInfo.id}
          mediaInfo={message}
          callBack={callback}
        />
      );
    }
    case MediaFileInfoTypeEnum.Video: {
      return (
        <Video
          key={message.mediaInfo.id}
          MediaInfo={message}
          callback={callback}
        />
      );
    }
    case MediaFileInfoTypeEnum.Audio: {
      return (
        <Audio
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
        />
      );
    }
    case MediaFileInfoTypeEnum.Voice: {
      return (
        <Voice
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
        />
      );
    }
    case MediaFileInfoTypeEnum.TelegramSticker: {
      return (
        <TelegramSticker
          key={message.mediaInfo.id}
          mediaInfo={message}
          callBack={callback}
        />
      );
    }
    default: {
      return null;
    }
  }
}
