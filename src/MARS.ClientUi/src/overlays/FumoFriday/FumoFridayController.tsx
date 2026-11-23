import { useCallback, useState } from "react";
import { v4 as uuidv4 } from "uuid";

import { useOverlayEvent } from "@/shared/api/realtime/useOverlayEvent";

import { Cirno } from "./Cirno";
import { Reimu } from "./Reimu";

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

  useOverlayEvent("FumoFriday", payload => {
    // Сервер шлёт `FumoFridayEvent { User }` — на проводе `{ user: {...} }`.
    // Имя и цвет берутся из TwitchUser, как и везде.
    const branch = (payload ?? {}) as Record<string, unknown>;
    const user =
      branch.user !== null && typeof branch.user === "object" && !Array.isArray(branch.user)
        ? (branch.user as Record<string, unknown>)
        : {};

    const newMessage: Message = {
      id: uuidv4(),
      message: (typeof user.displayName === "string" ? user.displayName : "") ?? "",
      color:
        (typeof user.chatColor === "string" ? user.chatColor : "") || undefined,
    };

    handleAddEvent(newMessage);
  });

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

  return (
    <>

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
