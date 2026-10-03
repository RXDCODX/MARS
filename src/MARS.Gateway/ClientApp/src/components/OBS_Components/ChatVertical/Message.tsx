import { useEffect, useRef, useState } from "react";
import GradientText from "react-bits/src/ts-default/TextAnimations/GradientText/GradientText";

import catisaVideo from "@/assets/catisa.mp4";
import { ChatMessage } from "@/shared/api";
import ElectricBorder from "@/shared/components/ElectricBorderLegacy";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import { parseContent, replaceBadges, replaceEmotes } from "@/shared/Utils";

import commonStyles from "../OBSCommon.module.scss";
import { ANIMATION_TIMINGS } from "./animationTimings";
import styles from "./Message.module.scss";

interface Properties {
  message: ChatMessage;
  onRemove?: () => void;
}

function getRoleColor(message: ChatMessage) {
  if (message.userDetail?.isVip) return "#e005b9"; // pink
  if (message.isBroadcaster) return "#e10d00"; // red
  if (message.userDetail?.isModerator) return "#00ad03"; // green
  return "transparent";
}

// В DEV окружении сообщения живут 10 минут, в production - 30 секунд
const messageLifetime = import.meta.env.DEV
  ? ANIMATION_TIMINGS.MESSAGE_LIFETIME.DEV
  : ANIMATION_TIMINGS.MESSAGE_LIFETIME.PRODUCTION;

export function Message({ message, onRemove }: Properties) {
  const badges = useTwitchStore(state => state.badges);
  const parser = useTwitchStore(state => state.parser);
  const parserToLink = useTwitchStore(state => state.parseToLink);
  const [parts] = useState(() => parseContent(message.message));
  const [isMultilineMessage, setIsMultilineMessage] = useState(false);
  const roleColor = getRoleColor(message);
  const messageReference = useRef<HTMLDivElement>(null);
  const messageContentReference = useRef<HTMLDivElement>(null);

  // Появление/исчезновение управляется фреймером (в родителе)

  // Проверка на catisa-триггер
  const isCatisa =
    typeof message.message === "string" &&
    (/^!catisa/i.test(message.message.trim()) ||
      /^!катиса/i.test(message.message.trim()));

  useEffect(() => {
    const timer = setTimeout(() => onRemove && onRemove(), messageLifetime);
    return () => clearTimeout(timer);
  }, [onRemove, message.id]);

  useEffect(() => {
    const messageElement = messageContentReference.current;
    if (!messageElement) {
      return;
    }

    const updateMultilineState = () => {
      const computedStyles = globalThis.getComputedStyle(messageElement);
      const parsedLineHeight = Number.parseFloat(computedStyles.lineHeight);
      const parsedFontSize = Number.parseFloat(computedStyles.fontSize);
      const lineHeight = Number.isFinite(parsedLineHeight)
        ? parsedLineHeight
        : (Number.isFinite(parsedFontSize) ? parsedFontSize : 16) * 1.2;
      const messageHeight = messageElement.getBoundingClientRect().height;
      const hasMultipleTextLines =
        !isCatisa && messageHeight > lineHeight * 1.6;
      setIsMultilineMessage(hasMultipleTextLines);
    };

    updateMultilineState();

    const resizeObserver = new ResizeObserver(updateMultilineState);
    resizeObserver.observe(messageElement);

    return () => {
      resizeObserver.disconnect();
    };
  }, [isCatisa, message.message]);

  return (
    <>
      <div className={styles.wrapper}>
        <ElectricBorder
          color={roleColor ?? "transparent"}
          thickness={2}
          chaos={0.6}
          speed={1.2}
        >
          <div style={{ display: "block", padding: "3px 15px" }}>
            <div
              ref={messageReference}
              className={`${styles.container} ${isMultilineMessage ? styles.containerLargeMessage : ""}`}
            >
              <div
                className={`${styles.userInfo} ${isMultilineMessage ? styles.userInfoLargeMessage : ""}`}
                style={{ backgroundColor: roleColor ?? "transparent" }}
              >
                <div className={styles.badges}>
                  {replaceBadges(badges, message)}
                </div>
                <div
                  className={`${styles.nickname} ${commonStyles.textStrokeShadow}`}
                  style={{
                    color: message.hexColor ?? "white",
                  }}
                >
                  {message.displayName}
                </div>
              </div>
              <div
                ref={messageContentReference}
                className={`${styles.message} ${isMultilineMessage ? styles.messageLargeMessage : ""}`}
              >
                {isCatisa ? (
                  <video
                    src={catisaVideo}
                    autoPlay
                    loop
                    muted
                    controls={false}
                    style={{
                      maxWidth: "180px",
                      maxHeight: "120px",
                      margin: "0 4px",
                      borderRadius: 12,
                      background: "black",
                      boxShadow: "0 2px 8px rgba(0,0,0,0.3)",
                    }}
                  />
                ) : (
                  parts?.map(part => {
                    if (message.isBroadcaster) {
                      // Стример — всегда градиент
                      if (part.type === "text") {
                        // Получаем результат с эмодзи
                        const emoteContent =
                          parser && parserToLink
                            ? replaceEmotes({
                                text: part.content,
                                parser,
                                newParser: parserToLink,
                              })
                            : part.content;
                        // Если это строка — старый способ, если массив/ReactNode — новый
                        return (
                          <span key={part.id}>
                            <GradientText
                              colors={[
                                "#ff00cc",
                                "#ff0088",
                                "#ff0044",
                                "#ff0088",
                                "#ff00cc",
                                "#cc00ff",
                                "#8800ff",
                                "#4400ff",
                                "#8800ff",
                                "#cc00ff",
                                "#9900ff",
                                "#6600ff",
                                "#3300ff",
                                "#6600ff",
                                "#9900ff",
                                "#0066ff",
                                "#0088ff",
                                "#00aaff",
                                "#0088ff",
                                "#0066ff",
                                "#00ccff",
                                "#00eeff",
                                "#00ffff",
                                "#00eeff",
                                "#00ccff",
                                "#00ffcc",
                                "#00ffaa",
                                "#00ff88",
                                "#00ffaa",
                                "#00ffcc",
                                "#00ff99",
                                "#00ff77",
                                "#00ff55",
                                "#00ff77",
                                "#00ff99",
                                "#00ff66",
                                "#00ff44",
                                "#00ff22",
                                "#00ff44",
                                "#00ff66",
                                "#ccff00",
                                "#eeff00",
                                "#ffff00",
                                "#eeff00",
                                "#ccff00",
                                "#ffcc00",
                                "#ffee00",
                                "#ffff00",
                                "#ffee00",
                                "#ffcc00",
                                "#ff9900",
                                "#ffbb00",
                                "#ffdd00",
                                "#ffbb00",
                                "#ff9900",
                                "#ff6600",
                                "#ff8800",
                                "#ffaa00",
                                "#ff8800",
                                "#ff6600",
                                "#ff3300",
                                "#ff5500",
                                "#ff7700",
                                "#ff5500",
                                "#ff3300",
                              ]}
                              animationSpeed={15}
                            >
                              {emoteContent}
                            </GradientText>
                          </span>
                        );
                      }
                      if (part.type === "image") {
                        return (
                          <img
                            key={part.id}
                            src={part.content}
                            alt="image"
                            style={{
                              maxWidth: "120px",
                              maxHeight: "120px",
                              margin: "0 4px",
                            }}
                          />
                        );
                      }
                      if (part.type === "video") {
                        return (
                          <video
                            key={part.id}
                            src={part.content}
                            controls={false}
                            autoPlay
                            muted
                            style={{
                              maxWidth: "180px",
                              maxHeight: "120px",
                              margin: "0 4px",
                            }}
                          />
                        );
                      }
                      if (part.type === "link") {
                        return (
                          <span
                            key={part.id}
                            className={`${styles.linkStub} ${commonStyles.textStrokeShadow}`}
                          >
                            ссылка
                          </span>
                        );
                      }
                    } else {
                      // Обычные пользователи (включая VIP и модераторов) — белый текст, но с поддержкой смайлов
                      if (part.type === "text") {
                        return (
                          <span
                            key={part.id}
                            style={{ color: "white" }}
                            className={commonStyles.textStrokeShadow}
                          >
                            {parser && parserToLink
                              ? replaceEmotes({
                                  text: part.content,
                                  parser,
                                  newParser: parserToLink,
                                })
                              : part.content}
                          </span>
                        );
                      }
                      if (
                        part.type === "link" ||
                        part.type === "video" ||
                        part.type === "image"
                      ) {
                        return (
                          <span
                            key={part.id}
                            className={`${styles.linkStub} ${commonStyles.textStrokeShadow}`}
                          >
                            ссылка
                          </span>
                        );
                      }
                    }
                    return null;
                  })
                )}
              </div>
            </div>
          </div>
        </ElectricBorder>
      </div>
    </>
  );
}
