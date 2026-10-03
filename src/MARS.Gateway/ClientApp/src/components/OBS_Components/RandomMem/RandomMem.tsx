import { useCallback, useEffect, useRef, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import { MediaDto, MediaMetaInfoPriorityEnum } from "@/shared/api";
import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import Alert from "../PyroAlerts/Alert";
import HighPriorityAlert from "../PyroAlerts/HighPriorityAlert";

export default function PyroAlerts() {
  const [messages, setMessages] = useState<MediaDto[]>([]);
  const [highPriorityQueue, setHighPriorityQueue] = useState<MediaDto[]>([]);
  const [currentHighPriority, setCurrentHighPriority] =
    useState<MediaDto | null>(null);
  const [announced, setAnnounced] = useState(false);

  const handleAlert = useCallback((message: MediaDto) => {
    message.mediaInfo.id = uuidv4();

    switch (message.mediaInfo.metaInfo.priority) {
      case MediaMetaInfoPriorityEnum.High: {
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

        setHighPriorityQueue(previous => [...previous, parsedMessage]); // Добавляем в очередь высокоприоритетных
        setMessages([]);
        break;
      }
      case MediaMetaInfoPriorityEnum.Low:
      case MediaMetaInfoPriorityEnum.Normal: {
        const coolMessage: MediaDto = {
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

        setMessages(previous => {
          const next = [...previous, coolMessage];
          return next;
        });
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
      setHighPriorityQueue(previous =>
        previous.filter(m => m.mediaInfo.id !== message.mediaInfo.id)
      );

      const newPriority = highPriorityQueue.some(Boolean)
        ? highPriorityQueue[0]
        : null;
      setCurrentHighPriority(newPriority);
    },
    [highPriorityQueue]
  );

  // Эффект для обработки очереди высокоприоритетных алертов
  useEffect(() => {
    if (highPriorityQueue.length === 0 || currentHighPriority) {
      return;
    }

    // Берем первый алерт из очереди
    const nextAlert = highPriorityQueue[0];
    setCurrentHighPriority(nextAlert);

    // Удаляем его из очереди через 2 секунды (время показа)
    const timer = setTimeout(() => {
      setHighPriorityQueue(previous => previous.slice(1));
      setCurrentHighPriority(null);
    }, 2000);

    return () => clearTimeout(timer);
  }, [highPriorityQueue, currentHighPriority]);

  // Подписки на SignalR события
  SignalRContext.useSignalREffect("RandomMem", handleAlert, [handleAlert]);

  const containerReference = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!containerReference.current) return;

    const observer = new MutationObserver(mutations => {
      for (const m of mutations) {
        for (const node of m.addedNodes) {
          if (!(node instanceof HTMLElement)) continue;

          // Ищем первый элемент с id внутри добавленной ноды (media id ставится на element: img/video wrapper)
          const elementWithId =
            node.matches && node.matches("[id]")
              ? node
              : node.querySelector("[id]");
          const id = elementWithId?.id;
          if (!id) continue;

          // Через 1.5s проверяем, появился ли элемент и видим ли он
          setTimeout(() => {
            const element = document.getElementById(id);
            if (!element) {
              SignalRContext.invoke(
                "LogError",
                `RandomMem: element with id=${id} was not found after mutation`
              );
              return;
            }

            const style = globalThis.getComputedStyle(element);
            const isVisible =
              style.visibility !== "hidden" &&
              element.getBoundingClientRect().width > 0 &&
              element.getBoundingClientRect().height > 0;
            if (!isVisible) {
              SignalRContext.invoke(
                "LogError",
                `RandomMem: element id=${id} src = ${messageProps.mediaInfo.fileInfo.filePath} is not visible after mutation`
              );
            }
          }, 1500);
        }
      }
    });

    observer.observe(containerReference.current, {
      childList: true,
      subtree: true,
    });

    return () => observer.disconnect();
  }, []);

  return (
    <div ref={containerReference} data-testid="obs-random-mem">
      {!announced && (
        <Announce title={"RandomMem"} callback={() => setAnnounced(true)} />
      )}

      {/* Render HP alert over everything */}
      {currentHighPriority && (
        <HighPriorityAlert
          key={currentHighPriority.mediaInfo.id}
          message={currentHighPriority}
          type={currentHighPriority.mediaInfo.fileInfo.type}
          callback={() => removeHighPrior(currentHighPriority)}
        />
      )}

      {/* Render normal alerts if no HP playing */}
      {!currentHighPriority &&
        messages.map(messageProperties => (
          <Alert
            key={messageProperties.mediaInfo.id}
            message={messageProperties}
            remove={remove}
          />
        ))}
    </div>
  );
}
