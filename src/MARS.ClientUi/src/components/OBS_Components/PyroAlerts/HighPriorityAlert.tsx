import { useCallback } from "react";

import { MediaDto, MediaFileInfoTypeEnum } from "@/shared/api";

import { Audio, Image, Video, Voice } from "./Primitive";
import TelegramSticker from "./Primitive/TelegramSticker";

export interface AlertDestination {
  message: MediaDto;
  type: MediaFileInfoTypeEnum;
  callback: () => void;
}

export default function HighPriorityAlert(alert: AlertDestination) {
  const message = alert.message;
  const callback = useCallback(() => alert.callback(), [alert]);

  switch (alert.type) {
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
          isHighPrior
        />
      );
    }
    case MediaFileInfoTypeEnum.Audio: {
      return (
        <Audio
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
          isHighPrior
        />
      );
    }
    case MediaFileInfoTypeEnum.Voice: {
      return (
        <Voice
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
          isHighPrior
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
