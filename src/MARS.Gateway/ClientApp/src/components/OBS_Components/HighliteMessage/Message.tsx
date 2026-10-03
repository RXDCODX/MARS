import { useCallback, useReducer, useRef } from "react";
import { Textfit } from "react-textfit";
import { useShallow } from "zustand/react/shallow";

import {
  ChatMessage,
  TelegramusHubSignalRContext as SignalRContext,
} from "@/shared/api";
import InjectStyles from "@/shared/components/InjectStyles";
import animate from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import {
  type FaceAsset,
  getNotWhiteColor,
  getRandomFace,
  isWhiteColor,
  replaceBadges,
} from "@/shared/Utils";

import commonStyles from "../OBSCommon.module.scss";
import styles from "./Message.module.scss";

enum StateStatus {
  add,
  remove,
}

const MESSAGE_LIFETIME_MS = import.meta.env.DEV ? 12_000 : 7000;

export interface HighliteMessageProps {
  message: ChatMessage;
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
          message => message.message.id !== action.messageProps.message.id
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
  const badges = useTwitchStore(useShallow(state => state.badges));
  const divHard = useRef<HTMLDivElement>(null);

  SignalRContext.useSignalREffect(
    "Highlite",
    (message: ChatMessage, color: string) => {
      // Генерируем случайное лицо для каждого сообщения
      const faceImage = getRandomFace();
      dispatch({
        type: StateStatus.add,
        messageProps: { message, color, faceImage },
      });
    },
    []
  );

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
          key={currentMessage.message.id}
          id={currentMessage.message.id}
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
              <div className={styles.icons}>
                <Textfit
                  min={1}
                  max={1500}
                  style={{
                    fontWeight: "bold",
                    color: `${currentMessage.color}`,
                  }}
                  mode="single"
                  forceSingleModeWidth
                  className={`${styles.name} ${commonStyles.textStrokeShadow}`}
                >
                  {currentMessage.message.displayName}:
                </Textfit>
                <div>{replaceBadges(badges, currentMessage.message)}</div>
              </div>
              <Textfit
                min={1}
                max={1500}
                mode="multi"
                className={`${styles.emotes} ${commonStyles.textStrokeShadow}`}
              >
                {currentMessage.message.message}
              </Textfit>
            </div>
            <div
              key={currentMessage.message.id}
              className={styles.expireTimer}
              style={{ animationDuration: `${MESSAGE_LIFETIME_MS}ms` }}
            />
          </div>
        </div>
      )}
    </>
  );
}
