import { useState } from "react";

import { ChatMessage } from "@/shared/api";
import { createTestMessageSet } from "@/shared/Utils/testMessageUtils";

interface MessageManagerProperties {
  children: (
    messages: ChatMessage[],
    removeMessage: (id: string) => void
  ) => React.ReactNode;
}

export function MessageManager({ children }: MessageManagerProperties) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [messageCount, setMessageCount] = useState(0);

  const generateMessages = () => {
    const newMessages = createTestMessageSet("demo", messageCount);

    newMessages.forEach((message, index) => {
      setTimeout(() => {
        setMessages(previous => {
          while (previous.length >= 15) {
            previous.pop();
          }
          return previous.find(m => m.id === message.id)
            ? previous
            : [message, ...previous];
        });
      }, index * 300);
    });

    setMessageCount(previous => previous + newMessages.length);
  };

  const removeMessage = (id: string) => {
    setMessages(previous => previous.filter(m => m.id !== id));
  };

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      <button
        onClick={generateMessages}
        style={{
          position: "absolute",
          top: "20px",
          right: "20px",
          zIndex: 1000,
          padding: "10px 20px",
          backgroundColor: "#4CAF50",
          color: "white",
          border: "none",
          borderRadius: "5px",
          cursor: "pointer",
          fontSize: "16px",
        }}
      >
        Generate Messages
      </button>
      {children(messages, removeMessage)}
    </div>
  );
}
