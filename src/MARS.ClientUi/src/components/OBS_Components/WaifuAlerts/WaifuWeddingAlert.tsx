import { useRef } from "react";
import SchoolPride from "react-canvas-confetti/dist/presets/pride";
import { Textfit } from "react-textfit";

import { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";

import common from "../OBSCommon.module.scss";
import { getMergeCongratulationText } from "./helper";
import styles from "./WaifuAlerts.module.scss";

interface Properties {
  message: WaifuAlertProps;
  onRemove: () => void;
  onMuteAll: () => void;
  onUnmuteAll: () => void;
  onSendMessage: (text: string) => void;
}

export default function WaifuWeddingAlert({
  message,
  onRemove,
  onMuteAll,
  onUnmuteAll,
  onSendMessage,
}: Properties) {
  const containerReference = useRef<HTMLDivElement>(null);
  const sentReference = useRef(false);

  const error = () => {
    onUnmuteAll();
    onRemove();
    throw new Error("Failed to play audio");
  };

  if (!sentReference.current) {
    sentReference.current = true;
    onSendMessage(
      `@${message.waifuHusband!.twitchUser?.displayName}, ${getMergeCongratulationText(message)}`
    );
  }

  return (
    <>
      <SchoolPride
        width="100%"
        height="100%"
        autorun={{ speed: 30, duration: 20 * 1000 }}
        decorateOptions={(): confetti.Options => ({
          particleCount: 2,
          angle: 60,
          spread: 55,
          origin: { x: 0 },
          colors: ["#000000", "#FF0000", "#FFFFFF"],
        })}
      />
      <SchoolPride
        width="100%"
        height="100%"
        autorun={{ speed: 30, duration: 20 * 1000 }}
        decorateOptions={(): confetti.Options => ({
          particleCount: 2,
          angle: 120,
          spread: 55,
          origin: { x: 1 },
          colors: ["#000000", "#FF0000", "#FFFFFF"],
        })}
      />
      <div
        className={styles["merge-container"]}
        data-testid="waifu-wedding-container"
        ref={containerReference}
      >
        <div className={styles["merge-image"]}>
          <img
            src={message.waifuHusband?.twitchUser?.profileImageUrl}
            onError={e => {
              const target = e.currentTarget;
              target.style.display = "none";
            }}
          />
        </div>
        <div className={styles["merge-text"]}>
          <Textfit
            className={common.textStrokeShadow}
            style={{ color: "white" }}
            mode="multi"
            min={1}
            max={2000}
          >
            Поздравляем{" "}
            <span
              className={common.textStrokeShadow}
              style={{
                color: message.waifuHusband?.twitchUser?.chatColor || "white",
              }}
            >
              {message.waifuHusband!.twitchUser?.displayName}
            </span>{" "}
            и{" "}
            <span
              className={common.textStrokeShadow}
              style={{ color: "cornflowerblue" }}
            >
              {message.waifu.name}{" "}
            </span>
            с свадьбой!
          </Textfit>
        </div>
        <div className={styles["merge-image"]}>
          <img
            src={message.waifu.imageUrl}
            onError={e => {
              const target = e.currentTarget;
              target.style.display = "none";
            }}
          />
        </div>
      </div>
      <audio
        key={message.waifu.shikiId}
        controls={false}
        autoPlay
        onError={() => error()}
        onEnded={() => {
          onUnmuteAll();
          onRemove();
        }}
        onCanPlay={event => {
          try {
            event.currentTarget?.play();
          } catch {
            event.currentTarget.muted = true;
            event.currentTarget?.play();
          }
        }}
        onCanPlayThrough={() => onMuteAll()}
      >
        <source src={import.meta.env.VITE_BASE_PATH + "Alerts/svadba.mp3"} />
      </audio>
    </>
  );
}
