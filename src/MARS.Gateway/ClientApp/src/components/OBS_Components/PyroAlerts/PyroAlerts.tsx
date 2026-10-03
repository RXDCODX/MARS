import { useCallback, useEffect, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import {
  MediaDto,
  MediaMetaInfoPriorityEnum,
  TelegramusHubSignalRContext as SignalRContext,
} from "@/shared/api";
import { useInjectStyles } from "@/shared/hooks";
import Announce from "@/shared/Utils/Announce/Announce";

import Alert from "./Alert";
import HighPriorityAlert from "./HighPriorityAlert";

export default function PyroAlerts() {
  const [messages, setMessages] = useState<MediaDto[]>([]);
  const [highPriorityQueue, setHighPriorityQueue] = useState<MediaDto[]>([]);
  const [currentHighPriority, setCurrentHighPriority] =
    useState<MediaDto | null>(null);
  const [announced, setAnnounced] = useState(false);

  const handleAlert = useCallback((message: MediaDto) => {
    message.mediaInfo.id = uuidv4();
    const parsedMessage: MediaDto = {
      ...message,
      mediaInfo: {
        ...message.mediaInfo,
        fileInfo: {
          ...message.mediaInfo.fileInfo,
          filePath: message.mediaInfo.fileInfo.isLocalFile
            ? import.meta.env.VITE_BASE_PATH +
              message.mediaInfo.fileInfo.filePath
            : message.mediaInfo.fileInfo.filePath,
        },
      },
    };

    switch (message.mediaInfo.metaInfo.priority) {
      case MediaMetaInfoPriorityEnum.High: {
        setHighPriorityQueue(previous => [...previous, parsedMessage]); // Добавляем в очередь высокоприоритетных
        setMessages([]);
        break;
      }
      case MediaMetaInfoPriorityEnum.Low:
      case MediaMetaInfoPriorityEnum.Normal: {
        setMessages(previous => [...previous, parsedMessage]);
        break;
      }
    }
  }, []);

  const remove = useCallback((message: MediaDto) => {
    setMessages(previous =>
      previous.filter(m => m.mediaInfo.id !== message.mediaInfo.id)
    );
  }, []);

  const removeHighPrior = useCallback(
    (message: MediaDto) => {
      setHighPriorityQueue(previous => {
        previous = previous.filter(
          m => m.mediaInfo.id !== message.mediaInfo.id
        );
        const newPriority = previous.some(Boolean) ? previous[0] : null;
        setCurrentHighPriority(newPriority);
        return previous;
      });
    },
    [setHighPriorityQueue]
  );

  // Эффект для обработки очереди высокоприоритетных алертов
  useEffect(() => {
    if (highPriorityQueue.length === 0 || currentHighPriority) {
      return;
    }

    // Берем первый алерт из очереди
    const nextAlert = highPriorityQueue[0];
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setCurrentHighPriority(nextAlert);

    // Удаляем его из очереди через 2 секунды (время показа)
    const timer = setTimeout(() => {
      setHighPriorityQueue(previous => previous.slice(1));
      setCurrentHighPriority(null);
    }, 2000);

    return () => clearTimeout(timer);
  }, [highPriorityQueue, currentHighPriority]);

  useInjectStyles(`
    body {
    overflow: hidden;
  }
    `);

  // Подписки на SignalR события
  SignalRContext.useSignalREffect("alert", handleAlert, [handleAlert]);
  SignalRContext.useSignalREffect(
    "alerts",
    (messages: MediaDto[]) => messages.forEach(handleAlert),
    [handleAlert]
  );

  return (
    <>
      {!announced && (
        <Announce title={"PyroAlerts"} callback={() => setAnnounced(true)} />
      )}

      {/* Рендерим текущий высокоприоритетный алерт */}
      {currentHighPriority && (
        <HighPriorityAlert
          key={currentHighPriority.mediaInfo.id}
          message={currentHighPriority}
          type={currentHighPriority.mediaInfo.fileInfo.type}
          callback={() => removeHighPrior(currentHighPriority)}
        />
      )}

      {/* Рендерим обычные алерты, если нет высокоприоритетных */}
      {!currentHighPriority &&
        messages.map(messageProperties => (
          <Alert
            key={messageProperties.mediaInfo.id}
            message={messageProperties}
            remove={remove}
          />
        ))}
    </>
  );
}
