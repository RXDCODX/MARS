import { AnimatePresence } from "framer-motion";
import { useCallback, useEffect, useRef, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import AutoMessageAlert from "./AutoMessageAlert";

interface AutoMessageData {
  id: string;
  message: string;
  timestamp: number;
}

interface AutoMessageBillboardProperties {
  messages?: string[];
}

export default function AutoMessageBillboard({
  messages: externalMessages,
}: AutoMessageBillboardProperties) {
  const [messageQueue, setMessageQueue] = useState<AutoMessageData[]>([]);
  const [currentMessage, setCurrentMessage] = useState<AutoMessageData | null>(
    null
  );
  const [isAnounced, setAnounced] = useState(false);
  const timeoutReference = useRef<NodeJS.Timeout | null>(null);
  const isProcessingReference = useRef(false);

  // Инициализация с внешними сообщениями (для Storybook)
  useEffect(() => {
    if (!(externalMessages && externalMessages.length > 0)) {
      return;
    }

    const initialQueue = externalMessages.map(message => ({
      id: uuidv4(),
      message: message,
      timestamp: Date.now(),
    }));
    setMessageQueue(initialQueue);
  }, [externalMessages]);

  // Обработка очереди сообщений
  const processNextMessage = useCallback(() => {
    if (isProcessingReference.current) {
      return;
    }

    setMessageQueue(previousQueue => {
      if (previousQueue.length === 0) {
        return previousQueue;
      }

      isProcessingReference.current = true;
      const nextMessage = previousQueue[0];

      // Устанавливаем текущее сообщение
      setCurrentMessage(nextMessage);

      // Через 8 секунд переходим к следующему сообщению
      timeoutReference.current = setTimeout(() => {
        setCurrentMessage(null);
        isProcessingReference.current = false;

        // Задержка 2 секунды перед следующим сообщением
        setTimeout(() => {
          processNextMessage();
        }, 2000);
      }, 8000);

      // Возвращаем очередь без первого элемента
      return previousQueue.slice(1);
    });
  }, []);

  // Запускаем обработку очереди при изменении
  useEffect(() => {
    if (
      messageQueue.length > 0 &&
      !currentMessage &&
      !isProcessingReference.current
    ) {
      processNextMessage();
    }
  }, [messageQueue.length, currentMessage, processNextMessage]);

  const handleAutoMessage = useCallback(
    (message: string) => {
      // Если переданы внешние сообщения, игнорируем SignalR
      if (externalMessages) {
        return;
      }

      const newMessage: AutoMessageData = {
        id: uuidv4(),
        message,
        timestamp: Date.now(),
      };

      // Ограничиваем очередь максимум 10 сообщениями
      setMessageQueue(previous => {
        const updatedQueue = [...previous, newMessage];
        // Если очередь больше 10 сообщений, удаляем самые старые
        if (updatedQueue.length > 10) {
          return updatedQueue.slice(-10);
        }
        return updatedQueue;
      });
    },
    [externalMessages]
  );

  const removeMessage = useCallback(() => {
    setCurrentMessage(null);
    isProcessingReference.current = false;

    if (timeoutReference.current) {
      clearTimeout(timeoutReference.current);
      timeoutReference.current = null;
    }

    // Переходим к следующему сообщению
    setTimeout(() => {
      processNextMessage();
    }, 2000);
  }, [processNextMessage]);

  // Подписка на SignalR события
  SignalRContext.useSignalREffect("AutoMessage", handleAutoMessage, [
    handleAutoMessage,
  ]);

  // Очистка таймаутов при размонтировании
  useEffect(
    () => () => {
      if (timeoutReference.current) {
        clearTimeout(timeoutReference.current);
      }
    },
    []
  );

  return (
    <>
      {!isAnounced && (
        <Announce title={"AutoMessage"} callback={() => setAnounced(true)} />
      )}
      <div
        style={{
          position: "fixed",
          top: 0,
          left: 0,
          width: "100vw",
          height: "100vh",
          pointerEvents: "none",
          zIndex: 1000,
          overflow: "hidden",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
        data-testid="obs-auto-message"
      >
        <AnimatePresence mode="wait">
          {currentMessage && (
            <AutoMessageAlert
              key={currentMessage.id}
              message={currentMessage.message}
              onComplete={removeMessage}
            />
          )}
        </AnimatePresence>
      </div>
    </>
  );
}
