import { useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";

import { ChatMessage } from "@/shared/api";
import animate from "@/shared/styles/animate.module.scss";
import {
  type FaceAsset,
  getNotWhiteColor,
  getRandomFace,
  getRandomFaceByType,
  isWhiteColor,
} from "@/shared/Utils";

import commonStyles from "../OBSCommon.module.scss";
import styles from "./Message.module.scss";

interface DemoMessageProperties {
  message: ChatMessage;
  color: string;
  faceImage: FaceAsset;
  onRemove: () => void;
}

function DemoMessage({
  message,
  color,
  faceImage,
  onRemove,
}: DemoMessageProperties) {
  const divReference = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      if (!divReference.current) {
        return;
      }

      divReference.current.addEventListener("animationend", () => {
        onRemove();
      });
      divReference.current.className =
        styles.container + " " + animate.fadeOut + " " + animate.animated;
    }, 5000);

    return () => clearTimeout(timer);
  }, [onRemove]);

  return (
    <div
      key={message.id}
      id={message.id}
      className={
        styles.container + " " + animate.fadeIn + " " + animate.animated
      }
      ref={divReference}
    >
      {/* IMAGE */}
      <div className={styles["buble-image"]}>
        {faceImage.type === "image" && (
          <img alt={`Face: ${faceImage.name}`} src={faceImage.url} />
        )}
        {faceImage.type === "video" && (
          <video src={faceImage.url} autoPlay controls={false} loop muted />
        )}
      </div>
      {/* TEXT */}
      <div
        className={styles.bubble + " " + styles.right}
        style={{
          background: `linear-gradient(135deg, ${isWhiteColor(color) ? getNotWhiteColor() : "white"}, ${color}) border-box`,
        }}
      >
        <div className={styles.talktext}>
          <div className={styles.icons}>
            <Textfit
              min={1}
              max={1500}
              style={{
                fontWeight: "bold",
                color: `${color}`,
              }}
              mode="single"
              forceSingleModeWidth
              className={`${styles.name} ${commonStyles.textStrokeShadow}`}
            >
              {message.displayName}:
            </Textfit>
          </div>
          <Textfit
            min={1}
            max={1500}
            mode="multi"
            className={`${styles.emotes} ${commonStyles.textStrokeShadow}`}
          >
            {message.message}
          </Textfit>
        </div>
      </div>
    </div>
  );
}

export default function MessageDemo() {
  const [messages, setMessages] = useState<
    Array<{
      message: ChatMessage;
      color: string;
      faceImage: FaceAsset;
    }>
  >([]);

  const addDemoMessage = () => {
    const demoMessages = [
      {
        message: {
          id: `demo-${Date.now()}`,
          displayName: "Viewer123",
          message: "Это демо-сообщение для тестирования! 🎉",
        } as ChatMessage,
        color: "#FF6B6B",
        faceImage: getRandomFace(),
      },
      {
        message: {
          id: `demo-${Date.now() + 1}`,
          displayName: "StreamFan",
          message: "Отличный стрим! Спасибо за контент 😊",
        } as ChatMessage,
        color: "#4ECDC4",
        faceImage: getRandomFaceByType("video"),
      },
      {
        message: {
          id: `demo-${Date.now() + 2}`,
          displayName: "GamerPro",
          message: "Как дела? Все хорошо? 🎮",
        } as ChatMessage,
        color: "#45B7D1",
        faceImage: getRandomFace(),
      },
    ];

    const randomMessage =
      demoMessages[Math.floor(Math.random() * demoMessages.length)];
    setMessages(previous => [...previous, randomMessage]);
  };

  const removeMessage = (id: string) => {
    setMessages(previous =>
      previous.filter(message => message.message.id !== id)
    );
  };

  useEffect(() => {
    // Добавляем первое сообщение при монтировании
    addDemoMessage();

    // Добавляем новые сообщения каждые 3 секунды
    const interval = setInterval(addDemoMessage, 3000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div
      style={{
        position: "relative",
        width: "100vw",
        height: "100vh",
        overflow: "hidden",
      }}
    >
      {messages.map(({ message, color, faceImage }) => (
        <DemoMessage
          key={message.id}
          message={message}
          color={color}
          faceImage={faceImage}
          onRemove={() => removeMessage(message.id || "")}
        />
      ))}

      <button
        onClick={addDemoMessage}
        style={{
          position: "fixed",
          top: "20px",
          left: "20px",
          zIndex: 1000,
          padding: "10px 20px",
          backgroundColor: "#007bff",
          color: "white",
          border: "none",
          borderRadius: "5px",
          cursor: "pointer",
        }}
      >
        Добавить сообщение
      </button>
    </div>
  );
}
