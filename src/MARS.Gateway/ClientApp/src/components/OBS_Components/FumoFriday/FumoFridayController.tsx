import { useCallback, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import {
  TelegramusHubSignalRContext as SignalRContext,
  TwitchUser,
} from "@/shared/api";

import { Cirno } from "./Cirno";
import { Reimu } from "./Reimu";
import styles from "./Styles.module.scss";

export interface Message {
  id: string;
  message: string;
  color?: string;
}

export function FumoFridayController() {
  const [, setMessages] = useState<Message[]>([]);
  const [currentMessage, setCurrentMessage] = useState<Message | undefined>(
    undefined
  );
  const [switcher, setSwitcher] = useState(false);

  SignalRContext.useSignalREffect(
    "fumofriday",
    (twitchUser: TwitchUser) => {
      const id = uuidv4();
      const newMessage: Message = {
        id: id,
        message: twitchUser.displayName ?? "",
        color: twitchUser.chatColor ?? undefined,
      };
      handleAddEvent(newMessage);
    },
    []
  );

  const handleAddEvent = useCallback(
    (message: Message) => {
      setMessages(previousMessages => {
        if (!currentMessage) {
          setCurrentMessage(message);
          return previousMessages;
        }
        return [...previousMessages, message];
      });
    },
    [currentMessage]
  );

  const changeSwitcher = useCallback(() => {
    setSwitcher(previousSwitcher => !previousSwitcher);
  }, []);

  const handleRemoveEvent = useCallback(
    (message: Message) => {
      setMessages(previousMessages => {
        const newMessages = previousMessages.filter(
          message_ => message_.id !== message.id
        );
        setCurrentMessage(newMessages[0]);
        return newMessages;
      });
      changeSwitcher();
    },
    [changeSwitcher]
  );

  // Экспортируем функцию play для внешнего использования
  const play = useCallback(() => {
    const testMessage: Message = {
      id: uuidv4(),
      message: "Test User",
      color: "#ff6b6b",
    };
    handleAddEvent(testMessage);
  }, [handleAddEvent]);

  // Делаем функцию play доступной глобально для тестирования
  (globalThis as unknown as { testFumoFriday: typeof play }).testFumoFriday =
    play;

  return (
    <>
      {globalThis?.location?.hostname === "localhost" &&
        window?.parent?.location?.pathname?.includes("iframe.html") && (
          <div className={styles.testControls}>
            <button
              onClick={play}
              className={styles.testButton}
              disabled={!!currentMessage}
              data-testid="button-test-fumo"
            >
              Test FumoFriday Alert
            </button>
          </div>
        )}

      {currentMessage && switcher && (
        <Reimu
          key={currentMessage.id}
          callback={() => handleRemoveEvent(currentMessage)}
          displayName={currentMessage}
        />
      )}
      {currentMessage && !switcher && (
        <Cirno
          key={currentMessage.id}
          callback={() => handleRemoveEvent(currentMessage)}
          displayName={currentMessage}
        />
      )}
    </>
  );
}
