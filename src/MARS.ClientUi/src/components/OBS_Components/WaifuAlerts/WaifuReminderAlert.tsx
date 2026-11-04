import { useEffect, useRef } from "react";
import { Textfit } from "react-textfit";

import { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";
import animate from "@/shared/styles/animate.module.scss";

import { getMarriageDisplayText, getMergeMarriageText } from "./helper";
import styles from "./WaifuAlerts.module.scss";

interface Properties {
  message: WaifuAlertProps;
  onRemove: () => void;
}

export default function WaifuReminderAlert({ message, onRemove }: Properties) {
  const containerReference = useRef<HTMLDivElement>(null);
  const timeoutReference = useRef<NodeJS.Timeout | null>(null);

  useEffect(
    () => () => {
      if (!timeoutReference.current) {
        return;
      }

      clearTimeout(timeoutReference.current);
      timeoutReference.current = null;
    },
    []
  );

  useEffect(() => {
    const timeout = setTimeout(() => {
      onRemove();
    }, 10_000);

    return () => clearTimeout(timeout);
  }, [onRemove]);

  return (
    <div
      id={message.waifu.shikiId}
      ref={containerReference}
      className={styles.baza + " " + animate.bounceIn + " " + animate.animated}
      data-testid="waifu-reminder-container"
    >
      <div className={styles["alert-box"]} data-testid="waifu-reminder-image">
        <img
          src={message.waifu.imageUrl}
          style={{ height: "747px", width: "480px" }}
          onLoad={() => {
            if (timeoutReference.current) {
              clearTimeout(timeoutReference.current);
              timeoutReference.current = null;
            }

            setTimeout(() => {
              containerReference.current!.addEventListener(
                "animationend",
                () => {
                  onRemove();
                }
              );

              containerReference.current!.className =
                styles.baza + " " + animate.bounceOut + " " + animate.animated;
            }, 7000);
          }}
          onError={() => {
            if (timeoutReference.current) {
              clearTimeout(timeoutReference.current);
              timeoutReference.current = null;
            }

            onRemove();
          }}
        />
      </div>
      <div className={styles["alert-box"]} data-testid="waifu-reminder-text">
        <div
          data-testid="waifu-reminder-user-row"
          style={{
            display: "flex",
            alignItems: "center",
            gap: "15px",
          }}
        >
          {message.waifuHusband?.twitchUser?.profileImageUrl && (
            <img
              src={message.waifuHusband.twitchUser.profileImageUrl}
              alt={message.waifuHusband!.twitchUser?.displayName}
              data-testid="waifu-reminder-avatar"
              style={{
                width: "80px",
                height: "80px",
                borderRadius: "50%",
                border: `4px solid ${message.waifuHusband.twitchUser.chatColor || "white"}`,
                boxShadow: `0 0 20px ${message.waifuHusband.twitchUser.chatColor || "white"}`,
              }}
            />
          )}
          <span
            className="text-shadow block-text"
            style={{
              color: message.waifuHusband?.twitchUser?.chatColor || "white",
            }}
          >
            <Textfit min={1} max={1500} forceSingleModeWidth>
              {message.waifuHusband!.twitchUser?.displayName?.toUpperCase()}
            </Textfit>
          </span>
        </div>
        <span
          className="text-shadow block-text"
          style={{ color: "cornflowerblue" }}
        >
          <Textfit min={1} max={1500} forceSingleModeWidth>
            {getMarriageDisplayText(message)}
          </Textfit>
        </span>
        {getMergeMarriageText(message) && (
          <span className="text-shadow block-text" style={{ color: "gold" }}>
            <Textfit min={1} max={1500} forceSingleModeWidth>
              {getMergeMarriageText(message)}
            </Textfit>
          </span>
        )}
      </div>
    </div>
  );
}
