/* eslint-disable simple-import-sort/imports */
import { useCallback, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";
import { useShallow } from "zustand/react/shallow";
import useTelegramusHubStore from "@/shared/stores/telegramusHubStore";
import { useOverlayHubLifecycle } from "@/shared/realtime/useOverlayHubLifecycle";
import useFrogPrizesStore from "@/shared/stores/frogPrizesStore";
import animate from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import Announce from "@/shared/Utils/Announce/Announce";
import InjectStyles from "@/shared/components/InjectStyles";

import common from "../OBSCommon.module.scss";
import { getFrogText } from "./helper";
import styles from "../WaifuAlerts/WaifuAlerts.module.scss";
import WaifuRoulette from "../WaifuAlerts/WaifuRoulette";

export default function FrogAlerts() {
  const currentFrogMessage = useTelegramusHubStore(
    useShallow(state => state.currentFrogMessage)
  );
  const dequeueFrogCurrent = useTelegramusHubStore(
    state => state.dequeueFrogCurrent
  );
  const [announced, setAnnounced] = useState(false);
  const divHard = useRef<HTMLDivElement>(null);
  const [isRouletted, setIsRouletted] = useState(false);
  const sendMessage = useTwitchStore(state => state.sendMsgToPyrokxnezxz);
  const imageLoadTimeoutReference = useRef<NodeJS.Timeout | null>(null);

  const prizes = useFrogPrizesStore(useShallow(state => state.prizes));
  const shufflePrizes = useFrogPrizesStore(state => state.shuffle);

  const rouletteIndex =
    currentFrogMessage && prizes.length > 0
      ? prizes.findIndex(prize => prize.id === currentFrogMessage.frog.pid)
      : -1;

  useOverlayHubLifecycle();

  const handleRemoveEvent = useCallback(() => {
    dequeueFrogCurrent();
  }, [dequeueFrogCurrent]);

  useEffect(() => {
    if (!currentFrogMessage) {
      return;
    }

    if (prizes.length > 0 && rouletteIndex === -1) {
      setIsRouletted(true);
      return;
    }

    if (prizes.length === 0) {
      const timeout = setTimeout(() => {
        setIsRouletted(true);
      }, 5000);
      return () => clearTimeout(timeout);
    }
  }, [currentFrogMessage, prizes, rouletteIndex]);

  useEffect(() => {
    if (!(currentFrogMessage && isRouletted)) {
      return;
    }

    imageLoadTimeoutReference.current = setTimeout(() => {
      handleRemoveEvent();
      setIsRouletted(false);
    }, 10_000);

    return () => {
      if (!imageLoadTimeoutReference.current) {
        return;
      }

      clearTimeout(imageLoadTimeoutReference.current);
      imageLoadTimeoutReference.current = null;
    };
  }, [currentFrogMessage, isRouletted, handleRemoveEvent]);

  return (
    <div className={common.textStrokeShadow}>
      <InjectStyles
        styles={`
          html, body {
            height: 100%;
            margin: 0;
          }
          
          div#root {
            min-height: 100%;
            flex-direction: column;
          }
        `}
        id="frog-alerts-styles"
      />
      {!announced && (
        <Announce title={"FrogRoll"} callback={() => setAnnounced(true)} />
      )}
      {currentFrogMessage &&
        !isRouletted &&
        rouletteIndex !== -1 &&
        prizes.length > 0 && (
          <WaifuRoulette
            key={currentFrogMessage.frog.pid}
            shuffle={shufflePrizes}
            callback={() => {
              setIsRouletted(true);
            }}
            rouletteIndex={rouletteIndex}
            prizes={(prizes || []).map(p => ({
              ...p,
              id: String(p.id),
              text: p.text || "",
            }))}
            twitchUser={currentFrogMessage.twitchUser}
            size="fill"
          />
        )}
      {currentFrogMessage &&
        !isRouletted &&
        rouletteIndex === -1 &&
        prizes.length === 0 && (
          <div className={styles["roulette-name-text"]}>
            <span>Загрузка рулетки Frog...</span>
          </div>
        )}
      {currentFrogMessage && isRouletted && (
        <div
          id={String(currentFrogMessage.frog.pid)}
          key={currentFrogMessage.frog.pid}
          ref={divHard}
          className={
            styles.baza + " " + animate.bounceIn + " " + animate.animated
          }
        >
          <div className={styles["alert-box"]}>
            <img
              src={currentFrogMessage.frog.thumbnailUrl}
              style={{ height: "620px", width: "480px" }}
              onLoad={() => {
                if (imageLoadTimeoutReference.current) {
                  clearTimeout(imageLoadTimeoutReference.current);
                  imageLoadTimeoutReference.current = null;
                }

                setTimeout(() => {
                  divHard.current!.addEventListener("animationend", () => {
                    handleRemoveEvent();
                    setIsRouletted(false);
                    shufflePrizes();
                  });

                  divHard.current!.className =
                    styles.baza +
                    " " +
                    animate.bounceOut +
                    " " +
                    animate.animated;
                }, 7000);

                sendMessage(
                  `@${currentFrogMessage.twitchUser.displayName}, ${getFrogText(currentFrogMessage)}!`
                );
              }}
              onError={() => {
                if (imageLoadTimeoutReference.current) {
                  clearTimeout(imageLoadTimeoutReference.current);
                  imageLoadTimeoutReference.current = null;
                }

                handleRemoveEvent();
                setIsRouletted(false);
              }}
            />
          </div>
          <div className={styles["alert-box"]}>
            <div
              data-testid="frog-alert-user-row"
              style={{
                display: "flex",
                alignItems: "center",
                gap: "15px",
              }}
            >
              {currentFrogMessage.twitchUser.profileImageUrl && (
                <img
                  src={currentFrogMessage.twitchUser.profileImageUrl}
                  alt={currentFrogMessage.twitchUser.displayName}
                  data-testid="frog-alert-avatar"
                  style={{
                    width: "80px",
                    height: "80px",
                    borderRadius: "50%",
                    border: `4px solid ${currentFrogMessage.twitchUser.chatColor || "white"}`,
                    boxShadow: `0 0 20px ${currentFrogMessage.twitchUser.chatColor || "white"}`,
                  }}
                />
              )}
              <span
                className="text-shadow block-text"
                style={{
                  color: currentFrogMessage.twitchUser.chatColor || "white",
                }}
              >
                <Textfit min={1} max={1500} forceSingleModeWidth>
                  {currentFrogMessage.twitchUser.displayName.toUpperCase()}
                </Textfit>
              </span>
            </div>
            <span
              className="text-shadow block-text"
              style={{
                color: "cornflowerblue",
              }}
            >
              <Textfit min={1} max={1500} forceSingleModeWidth>
                {getFrogText(currentFrogMessage)}
              </Textfit>
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
