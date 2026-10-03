import { useEffect, useRef } from "react";
import { Textfit } from "react-textfit";

import { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";
import { getApiBaseUrl } from "@/shared/api/api-config";
import animate from "@/shared/styles/animate.module.scss";

import common from "../OBSCommon.module.scss";
import { getText, getTitle } from "./helper";
import styles from "./WaifuAlerts.module.scss";

interface Properties {
  message: WaifuAlertProps;
  onRemove: () => void;
  onShuffle: () => void;
  onSendMessage: (text: string) => void;
}

export default function WaifuAddAlert({
  message,
  onRemove,
  onShuffle,
  onSendMessage,
}: Properties) {
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

  return (
    <div
      id={message.waifu.shikiId}
      ref={containerReference}
      className={styles.baza + " " + animate.bounceIn + " " + animate.animated}
    >
      <div className={styles["alert-box"]} data-testid="waifu-alert-box">
        <img
          src={message.waifu.imageUrl}
          style={{ height: "630px", width: "480px" }}
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
                  onShuffle();
                }
              );

              containerReference.current!.className =
                styles.baza + " " + animate.bounceOut + " " + animate.animated;
            }, 7000);
            if (!message.waifu.isAdded) {
              onSendMessage(
                `@${message.displayName}, ${getText(message)} ${getTitle(message)}!`
              );
            }
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
      {message.waifu.audioId && (
        <audio
          key={`waifu-audio-${message.waifu.shikiId}`}
          autoPlay
          controls={false}
          src={`${getApiBaseUrl()}/api/WaifuRoll/audios/${message.waifu.audioId}/stream`}
        />
      )}
      <div className={styles["alert-box"]} data-testid="waifu-alert-text">
        <div
          data-testid="waifu-alert-user-row"
          style={{
            display: "flex",
            alignItems: "center",
            gap: "15px",
          }}
        >
          {message.waifuHusband?.twitchUser?.profileImageUrl && (
            <img
              src={message.waifuHusband.twitchUser.profileImageUrl}
              alt={message.displayName}
              data-testid="waifu-alert-avatar"
              style={{
                width: "80px",
                height: "80px",
                borderRadius: "50%",
                border: `4px solid ${message.waifuHusband.twitchUser.chatColor || "white"}`,
                boxShadow: `0 0 20px ${message.waifuHusband.twitchUser.chatColor || "white"}`,
              }}
            />
          )}
          <span className="text-shadow block-text" style={{ color: "white" }}>
            <Textfit min={1} max={1500} forceSingleModeWidth>
              {message.displayName.toUpperCase()}
            </Textfit>
          </span>
        </div>
        <span
          className="text-shadow block-text"
          style={{ color: "cornflowerblue" }}
        >
          <Textfit min={1} max={1500} forceSingleModeWidth>
            {getText(message)}
          </Textfit>
        </span>
        <span className="text-shadow block-text" style={{ color: "red" }}>
          <Textfit min={1} max={1500} forceSingleModeWidth>
            {getTitle(message)}
          </Textfit>
        </span>
      </div>
    </div>
  );
}
