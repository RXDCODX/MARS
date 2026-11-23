import { useCallback } from "react";

import { MediaDto, MediaType } from "@/shared/api";

import { Audio, Image, Video, Voice } from "./Primitive";
import TelegramSticker from "./Primitive/TelegramSticker";

export interface AlertDestination {
  message: MediaDto;
  type: MediaType;
  callback: () => void;
}

export default function HighPriorityAlert(alert: AlertDestination) {
  const message = alert.message;
  const callback = useCallback(() => alert.callback(), [alert]);

  switch (alert.type) {
    case MediaType.Image:
    case MediaType.Gif: {
      return (
        <Image
          key={message.mediaInfo.id}
          mediaInfo={message}
          callBack={callback}
        />
      );
    }
    case MediaType.Video: {
      return (
        <Video
          key={message.mediaInfo.id}
          MediaInfo={message}
          callback={callback}
          isHighPrior
        />
      );
    }
    case MediaType.Audio: {
      return (
        <Audio
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
          isHighPrior
        />
      );
    }
    case MediaType.Voice: {
      return (
        <Voice
          key={message.mediaInfo.id}
          mediaInfo={message}
          callback={callback}
          isHighPrior
        />
      );
    }
    case MediaType.TelegramSticker: {
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
