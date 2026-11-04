import { useState } from "react";
import { Textfit } from "react-textfit";

import RainbowText from "@/shared/components/RainbowText/RainbowText";

import commonStyles from "../OBSCommon.module.scss";
import { Message } from "./FumoFridayController";
import styles from "./Styles.module.scss";
import { getVideoPath } from "./videoAssets";

interface Properties {
  displayName: Message;
  callback: () => void;
}

export function Reimu({ callback, displayName }: Properties) {
  const [show, setShow] = useState<boolean>(true);

  return (
    <div className={styles["box-container"]} data-testid="fumo-reimu">
      <div
        className={styles.container}
        style={{ visibility: show ? "visible" : "hidden" }}
      >
        <div>
          <video
            src={getVideoPath("reimu")}
            autoPlay
            controls={false}
            style={{ maxWidth: "100%" }}
            onEnded={() => {
              setShow(false);
              setTimeout(() => {
                callback();
              }, 1500);
            }}
          />
        </div>
        <div className={styles.text}>
          <Textfit mode="single">
            <div>
              Поздравляю{" "}
              <span
                className={commonStyles.textStrokeShadow}
                style={{ color: displayName.color }}
              >
                {displayName.message}
              </span>
              !
            </div>
            <div>
              <span className={commonStyles.textStrokeShadow}>
                <RainbowText
                  colors={[
                    "#FF1744",
                    "#FF6600",
                    "#FFD700",
                    "#00E676",
                    "#00E5FF",
                    "#2979FF",
                    "#D500F9",
                    "#FF00FF",
                  ]}
                >
                  HAPPY FUMO FRIDAY!
                </RainbowText>
              </span>
            </div>
          </Textfit>
        </div>
      </div>
    </div>
  );
}
