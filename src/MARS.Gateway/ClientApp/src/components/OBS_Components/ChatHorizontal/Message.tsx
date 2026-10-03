import { useEffect, useRef, useState } from "react";
import { useAnimate } from "react-simple-animate";

import { ChatMessage } from "@/shared/api";
import animateStyles from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import {
  getNotWhiteColor,
  getRandomInt,
  isWhiteColor,
  parseContent,
  replaceEmotes,
} from "@/shared/Utils";
import GradientText from "@/shared/Utils/Animations/GradientText";

import commonStyles from "../OBSCommon.module.scss";
import styles from "./Message.module.scss";

interface Properties {
  message: ChatMessage;
  callback?: () => void;
  slotTop?: number; // добавлен prop для вертикального позиционирования
}

export const Message = ({ message, callback, slotTop }: Properties) => {
  const [handler, setHandler] = useState(true);
  const divReference = useRef<HTMLDivElement>(null);
  const parser = useTwitchStore(state => state.parser);
  const parserToLink = useTwitchStore(state => state.parseToLink);
  const isVip = message.userDetail?.isVip ?? false;
  const isModerator = message.userDetail?.isModerator ?? false;

  const [text] = useState(parseContent(message.message));
  const [fontSize] = useState(getRandomInt(10, 40));
  const [duration] = useState(fontSize * 0.7);
  const [opacity] = useState(getRandomInt(0.4, 0.6));
  const [mainColor] = useState(
    isWhiteColor(message.hexColor ?? "white")
      ? getNotWhiteColor(opacity)
      : (message.hexColor ?? "white")
  );
  const [bg] = useState(
    `linear-gradient(125deg, ${mainColor} , transparent 75%) border-box`
  );
  const [baseStyles, setBaseStyles] = useState<React.CSSProperties>({
    visibility: "hidden",
    fontSize: fontSize + "px", // Случайный размер шрифта
    position: "absolute",
    background: isVip || isModerator || message.isBroadcaster ? bg : undefined,
    margin: fontSize / 10 + "px",
    top: slotTop === undefined ? undefined : slotTop + "px", // если slotTop передан — используем его
  });
  const [divOffset, setDivOffset] = useState(0);
  const { play, style } = useAnimate({
    duration: duration,
    start: {
      ...baseStyles,
      left: window.outerWidth + divOffset + "px",
    },
    end: {
      ...baseStyles,
      left: -divOffset - 100 + "px",
    },
    onComplete: () => {
      setHandler(false);
    },
  });

  useEffect(() => {
    const element = divReference.current!;
    const randPosY = Math.floor(
      Math.random() * (window.innerHeight - element.offsetHeight)
    );

    setBaseStyles({
      ...baseStyles,
      top: slotTop === undefined ? randPosY + "px" : slotTop + "px",
      visibility: "visible",
      left: window.outerWidth + "px",
    });
  }, [message.id, baseStyles, slotTop]);

  useEffect(() => {
    if (divReference.current && divReference.current.offsetWidth) {
      setDivOffset(divReference.current.offsetWidth);

      setBaseStyles(previous => ({
        ...previous,
        left: window.outerWidth + divOffset + "px",
      }));
    }

    if (divOffset !== 0) {
      play(true);
    }
  }, [divReference.current?.offsetWidth, divOffset, play]);

  // Проверяем parser после определения всех переменных
  if (!parser || !parserToLink) {
    // Для Storybook используем упрощенный рендер
    return (
      handler && (
        <div
          ref={divReference}
          key={message.id}
          style={style}
          className={styles.message + " " + animateStyles.animated}
          onAnimationEnd={callback}
        >
          <div
            id={`${message.id}_nickname`}
            className={`${styles.nickname} ${commonStyles.textStrokeShadow}`}
            style={{ color: message.hexColor }}
          >
            {message.displayName}:
          </div>
          <div
            style={{ color: "white", marginLeft: "10px" }}
            className={`${styles.text} ${commonStyles.textStrokeShadow}`}
          >
            {message.message}
          </div>
        </div>
      )
    );
  }

  return (
    handler && (
      <div
        ref={divReference}
        key={message.id}
        style={style}
        className={styles.message + " " + animateStyles.animated}
        onAnimationEnd={callback}
      >
        <div
          id={`${message.id}_nickname`}
          className={styles.nickname}
          style={{ color: message.hexColor }}
        >
          {message.displayName}:
        </div>
        <div
          className={styles.text}
          style={{ color: "white", marginLeft: "10px" }}
        >
          {text?.map((part, index) => {
            if (message.isBroadcaster || isVip || isModerator) {
              return (
                <span key={index} className={commonStyles.textStrokeShadow}>
                  <GradientText
                    text={
                      typeof part.content === "string"
                        ? part.content
                        : (message.message ?? "")
                    }
                    fontWeight={600}
                    speed="very-fast"
                  />
                </span>
              );
            }
            switch (part.type) {
              case "text": {
                return (
                  <span className={commonStyles.textStrokeShadow}>
                    {replaceEmotes({
                      text: message,
                      parser,
                      newParser: parserToLink,
                    })}
                  </span>
                );
              }
              case "image": {
                return (
                  <span key={index} className={commonStyles.textStrokeShadow}>
                    Ссылка
                  </span>
                );
              }
              case "link": {
                return (
                  <span key={index} className={commonStyles.textStrokeShadow}>
                    Ссылка
                  </span>
                );
              }
              case "video": {
                return (
                  <span key={index} className={commonStyles.textStrokeShadow}>
                    Ссылка
                  </span>
                );
              }
            }
          })}
        </div>
      </div>
    )
  );
};
