import { AnimatePresence, motion } from "framer-motion";
import { useCallback, useEffect, useRef, useState } from "react";

import {
  ChatMessage,
  TelegramusHubSignalRContext as SignalRContext,
} from "@/shared/api";
import InjectStyles from "@/shared/components/InjectStyles";
import Announce from "@/shared/Utils/Announce/Announce";

import { MOTION, SCROLL_CONFIG, SCROLL_TIMEOUT } from "./animationTimings";
import styles from "./ChatVertical.module.scss";
import { Message } from "./Message";

type ChatMessageWithPending = ChatMessage & { _pendingRemove?: boolean };

interface ChatVerticalProperties {
  messages?: ChatMessage[];
  onRemoveMessage?: (id: string) => void;
}

export default function ChatVertical({
  messages: externalMessages,
  onRemoveMessage,
}: ChatVerticalProperties) {
  const [internalMessages, setInternalMessages] = useState<ChatMessage[]>([]);
  const [announced, setAnnounced] = useState(false);

  // Используем внешние сообщения или внутренние
  const messages =
    externalMessages === undefined ? internalMessages : externalMessages;
  const handleRemove = useCallback(
    (id: string) => {
      if (onRemoveMessage) {
        onRemoveMessage(id);
      } else {
        setInternalMessages(previous => previous.filter(m => m.id !== id));
      }
    },
    [onRemoveMessage]
  );

  // ScrollToBottom после появления нового сообщения (и анимации)
  const scrollReference = useRef<HTMLDivElement>(null);
  useEffect(() => {
    // Ждём завершения всех анимаций (время берется из animationTimings.ts)
    const timeout = setTimeout(() => {
      if (scrollReference.current) {
        scrollReference.current.scrollIntoView(SCROLL_CONFIG);
      }
    }, SCROLL_TIMEOUT);
    return () => clearTimeout(timeout);
  }, [messages.length]);

  // Убираем постоянный автоскролл — контейнер закреплён у низа через CSS (column-reverse)

  SignalRContext.useSignalREffect(
    "NewMessage",
    (id: string, message: ChatMessage) => {
      if (externalMessages) {
        return null;
      }
      message.id ??= id;
      setInternalMessages(previous => {
        while (previous.length >= 15) {
          previous.pop();
        }
        return previous.find(m => m.id === message.id)
          ? previous
          : [message, ...previous];
      });
    },
    []
  );

  SignalRContext.useSignalREffect(
    "deletemessage",
    (id: string) => {
      // Удаляем сообщение, framer-motion проиграет exit-анимацию
      setInternalMessages(previous => previous.filter(m => m.id !== id));
    },
    []
  );

  return (
    <>
      <InjectStyles
        styles={`
          body {
            overflow: hidden;
          }
        `}
        id="chat-vertical-styles"
      />
      {!announced && (
        <Announce title={"Chat Vertical"} callback={() => setAnnounced(true)} />
      )}
      <div className={styles.chatContainer} data-testid="obs-chat-vertical">
        <AnimatePresence initial={false} mode="popLayout">
          {internalMessages.map((message: ChatMessageWithPending) => (
            <motion.div
              key={message.id}
              layout="position"
              initial={{ opacity: 0, x: `-${MOTION.ENTRY.OFFSCREEN_PERCENT}%` }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: `${MOTION.EXIT.OFFSCREEN_PERCENT}%` }}
              transition={{
                layout: { duration: MOTION.LAYOUT.DURATION_MS / 1000 },
                type: "tween",
                x: {
                  type: "tween",
                  ease: "easeOut",
                  duration: MOTION.ENTRY.DURATION_MS / 1000,
                  delay:
                    message === internalMessages[0]
                      ? MOTION.ENTRY.DELAY_MS / 1000
                      : 0,
                },
                opacity: {
                  duration: MOTION.OPACITY_DURATION_MS / 1000,
                  ease: "easeOut",
                },
              }}
            >
              <Message
                message={message}
                onRemove={() => handleRemove(message.id!)}
              />
            </motion.div>
          ))}
        </AnimatePresence>
      </div>
      <div ref={scrollReference} />
    </>
  );
}
