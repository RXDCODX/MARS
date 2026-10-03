import { useCallback, useEffect, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import { MediaDto, MediaMetaInfoPriorityEnum } from "@/shared/api";
import { useInjectStyles } from "@/shared/hooks";
import { readBranch } from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
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

  // Подписки на события хаба оверлея.
  //
  // Раньше имена писались в нижнем регистре — «alert», «alerts» — и расходились
  // с сервером, а работали только потому, что резолвер SignalR
  // регистронезависим.
  //
  // Событие едет веткой oneof: { alert: { media, uploadStartTime } }. Форма
  // MediaPayload повторяет поля MediaDto, поэтому разбор сводится к выбору
  // ветки, а приведение типов — к cast.
  useOverlayEvent("Alert", payload => {
    const media = readBranch(payload) as unknown as MediaDto | null;

    if (media !== null) {
      handleAlert(media);
    }
  });

  useOverlayEvent("Alerts", payload => {
    // AlertsEvent несёт список media, а не один объект.
    const branch = readBranch(payload);
    const list = branch?.media;

    if (Array.isArray(list)) {
      for (const media of list) {
        handleAlert(media);
      }
    }
  });

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
