import { useCallback, useReducer, useRef } from "react";
import { Textfit } from "react-textfit";

import {
  decodeJsonBranch,
  readStringField,
} from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
import { readHighliteText } from "@/shared/realtime/highliteMessage";
import InjectStyles from "@/shared/components/InjectStyles";
import animate from "@/shared/styles/animate.module.scss";
import {
  type FaceAsset,
  getNotWhiteColor,
  getRandomFace,
  isWhiteColor,
} from "@/shared/Utils";

import commonStyles from "../OBSCommon.module.scss";
import styles from "./Message.module.scss";

enum StateStatus {
  add,
  remove,
}

const MESSAGE_LIFETIME_MS = import.meta.env.DEV ? 12_000 : 7000;
/**
 * Порядковый номер плашки.
 *
 * Идентификатора на проводе нет, а он нужен для ключа и id: без него React
 * считал бы плашки одинаковыми, и удаление по id срабатывало бы не на ту.
 */
let nextMessageId = 0;

export interface HighliteMessageProps {
  /** Идентификатор для ключа и id: на проводе его нет, даётся по позиции. */
  id: string;
  /** Текст подсвеченного сообщения. Ника на проводе нет вовсе. */
  text: string;
  color: string;
  faceImage: FaceAsset;
}

interface State {
  messages: HighliteMessageProps[];
  currentMessage?: HighliteMessageProps;
  isMessageShowing: boolean;
}

function reducer(
  state: State,
  action: { type: StateStatus; messageProps: HighliteMessageProps }
): State {
  switch (action.type) {
    case StateStatus.add: {
      if (!state.isMessageShowing) {
        return {
          messages: [...state.messages],
          currentMessage: action.messageProps,
          isMessageShowing: true,
        };
      }

      return { ...state, messages: [...state.messages, action.messageProps] };
    }

    case StateStatus.remove: {
      if (state.messages.length > 0) {
        const newArray = state.messages.filter(
          message => message.id !== action.messageProps.id
        );

        if (newArray.length > 0) {
          const newMessage = newArray[0];

          return {
            messages: newArray,
            currentMessage: newMessage,
            isMessageShowing: true,
          };
        }

        return {
          messages: state.messages,
          currentMessage: undefined,
          isMessageShowing: false,
        };
      }

      return {
        currentMessage: undefined,
        isMessageShowing: false,
        messages: [],
      };
    }
  }
}

export default function Message() {
  const [{ currentMessage }, dispatch] = useReducer(reducer, {
    messages: [],
    isMessageShowing: false,
  });
  const divHard = useRef<HTMLDivElement>(null);

  useOverlayEvent("Highlite", payload => {
    // Событие едет содержимым ветки — { messageJson, color, faceUrlJson }, и
    // в `messageJson` лежит **строка**: сервер передаёт текст подсвеченного
    // сообщения. Раньше обработчик получал готовые (message, color) от резолвера
    // SignalR, затем разбор был доведён до формы ветки — но приводил строку к
    // `ChatMessage`, то есть к форме, которой на проводе нет.
    //
    // Ника на проводе тоже нет, поэтому подставлять его неоткуда, и заголовок с
    // ником убран: пустое имя на экране было бы враньём.
    const decoded = decodeJsonBranch(payload, "messageJson");
    const color = readStringField(payload, "color");
    const text = readHighliteText(decoded);

    if (color === undefined || text === null) {
      return;
    }

    // Лицо берётся локально из бандла, а не из `faceUrlJson`: сервер кладёт туда
    // `AutoArtImage` из своей папки `faces`, а раздавать её нечем. Это исходная
    // форма экрана, а не потеря при переносе.
    const faceImage = getRandomFace();

    dispatch({
      type: StateStatus.add,
      messageProps: {
        // Идентификатора на проводе нет, поэтому он выводится из позиции: он
        // нужен только для ключа и `id`, чтобы React различал плашки.
        id: `highlite-${nextMessageId++}`,
        text,
        color,
        faceImage,
      },
    });
  });

  const handleRemoveEvent = useCallback((message: HighliteMessageProps) => {
    dispatch({ type: StateStatus.remove, messageProps: message });
  }, []);

  const startHideAnimation = useCallback(
    (message: HighliteMessageProps) => {
      setTimeout(() => {
        divHard.current!.addEventListener("animationend", () => {
          handleRemoveEvent(message);
        });
        divHard.current!.className =
          styles.container + " " + animate.fadeOut + " " + animate.animated;
      }, MESSAGE_LIFETIME_MS);
    },
    [handleRemoveEvent]
  );

  return (
    <>
      <InjectStyles
        styles={`
          :root {
            --color: #ff0000 #ff0000 transparent transparent;
            --calculated-height: calc(100vh / 29);
            --span-width: 100ch;
          }
        `}
        id="highlite-message-styles"
      />
      {currentMessage && (
        <div
          key={currentMessage.id}
          id={currentMessage.id}
          className={
            styles.container + " " + animate.fadeIn + " " + animate.animated
          }
          ref={divHard}
          data-testid="highlite-message-content"
        >
          {/* IMAGE */}
          <div className={styles["buble-image"]}>
            {currentMessage.faceImage.type === "image" && (
              <img
                alt={`Face: ${currentMessage.faceImage.name}`}
                src={currentMessage.faceImage.url}
                onLoad={() => {
                  startHideAnimation(currentMessage);
                }}
              />
            )}
            {currentMessage.faceImage.type === "video" && (
              <video
                src={currentMessage.faceImage.url}
                autoPlay
                controls={false}
                loop
                muted
                onLoadedMetadata={() => {
                  startHideAnimation(currentMessage);
                }}
              />
            )}
          </div>
          {/* TEXT */}
          <div
            className={styles.bubble + " " + styles.right}
            style={{
              background: `linear-gradient(135deg, ${isWhiteColor(currentMessage.color) ? getNotWhiteColor() : "white"}, ${currentMessage.color}) border-box`,
            }}
          >
            <div className={styles.talktext}>
              {/* Заголовок с ником и значки удалены: на проводе их нет вовсе —
                  `message_json` содержит только текст подсвеченного сообщения.
                  Отрисовывать «имя: » с пустым именем значило бы показывать на
                  экране то, чего не прислали. */}
              <Textfit
                min={1}
                max={1500}
                mode="multi"
                className={`${styles.emotes} ${commonStyles.textStrokeShadow}`}
              >
                {currentMessage.text}
              </Textfit>
            </div>
            <div
              key={currentMessage.id}
              className={styles.expireTimer}
              style={{ animationDuration: `${MESSAGE_LIFETIME_MS}ms` }}
            />
          </div>
        </div>
      )}
    </>
  );
}
