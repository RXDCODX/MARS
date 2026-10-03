/* eslint-disable simple-import-sort/imports */
import InjectStyles from "@/shared/components/InjectStyles";
import useFumoPrizesStore from "@/shared/stores/fumoPrizesStore";
import useTelegramusHubStore from "@/shared/stores/telegramusHubStore";
import animate from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import Announce from "@/shared/Utils/Announce/Announce";
import { useCallback, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";
import { useShallow } from "zustand/react/shallow";

import common from "../OBSCommon.module.scss";
import styles from "../WaifuAlerts/WaifuAlerts.module.scss";
import WaifuRoulette from "../WaifuAlerts/WaifuRoulette";
import { getFumoText } from "./helper";

export default function FumoAlerts() {
  const currentFumoMessage = useTelegramusHubStore(
    useShallow(state => state.currentFumoMessage)
  );
  const dequeueFumoCurrent = useTelegramusHubStore(
    state => state.dequeueFumoCurrent
  );
  const startHub = useTelegramusHubStore(state => state.start);
  const [announced, setAnnounced] = useState(false);
  const divHard = useRef<HTMLDivElement>(null);
  const [isRouletted, setIsRouletted] = useState(false);
  const sendMessage = useTwitchStore(state => state.sendMsgToPyrokxnezxz);
  const imageLoadTimeoutReference = useRef<NodeJS.Timeout | null>(null);

  const prizes = useFumoPrizesStore(useShallow(state => state.prizes));
  const shufflePrizes = useFumoPrizesStore(state => state.shuffle);

  const rouletteIndex =
    currentFumoMessage && prizes.length > 0
      ? prizes.findIndex(prize => prize.id === currentFumoMessage.fumo.mfcId)
      : -1;

  useEffect(() => {
    startHub();
  }, [startHub]);

  const handleRemoveEvent = useCallback(() => {
    dequeueFumoCurrent();
  }, [dequeueFumoCurrent]);

  useEffect(() => {
    if (!currentFumoMessage) {
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
  }, [currentFumoMessage, prizes, rouletteIndex]);

  useEffect(() => {
    if (!(currentFumoMessage && isRouletted)) {
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
  }, [currentFumoMessage, isRouletted, handleRemoveEvent]);

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
        id="fumo-alerts-styles"
      />
      {!announced && (
        <Announce title={"FumoRoll"} callback={() => setAnnounced(true)} />
      )}
      {currentFumoMessage &&
        !isRouletted &&
        rouletteIndex !== -1 &&
        prizes.length > 0 && (
          <WaifuRoulette
            key={currentFumoMessage.fumo.mfcId}
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
            twitchUser={currentFumoMessage.twitchUser}
            size="fill"
          />
        )}
      {currentFumoMessage &&
        !isRouletted &&
        rouletteIndex === -1 &&
        prizes.length === 0 && (
          <div className={styles["roulette-name-text"]}>
            <span>Загрузка рулетки Fumo...</span>
          </div>
        )}
      {currentFumoMessage && isRouletted && (
        <div
          id={String(currentFumoMessage.fumo.mfcId)}
          key={currentFumoMessage.fumo.mfcId}
          ref={divHard}
          className={
            styles.baza + " " + animate.bounceIn + " " + animate.animated
          }
        >
          <div className={styles["alert-box"]}>
            <img
              src={currentFumoMessage.fumo.thumbnailUrl}
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
                  `@${currentFumoMessage.twitchUser.displayName}, ${getFumoText(currentFumoMessage)}!!${currentFumoMessage.collectedCount != undefined && currentFumoMessage.totalCount != undefined && currentFumoMessage.totalCount > 0 ? ` Твоя коллекция фум насчитывает: ${currentFumoMessage.collectedCount}/${currentFumoMessage.totalCount} уникальных плюшевых фум!` : ""}`
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
              data-testid="fumo-alert-user-row"
              style={{
                display: "flex",
                alignItems: "center",
                gap: "15px",
              }}
            >
              {currentFumoMessage.twitchUser.profileImageUrl && (
                <img
                  src={currentFumoMessage.twitchUser.profileImageUrl}
                  alt={currentFumoMessage.twitchUser.displayName}
                  data-testid="fumo-alert-avatar"
                  style={{
                    width: "80px",
                    height: "80px",
                    borderRadius: "50%",
                    border: `4px solid ${currentFumoMessage.twitchUser.chatColor || "white"}`,
                    boxShadow: `0 0 20px ${currentFumoMessage.twitchUser.chatColor || "white"}`,
                  }}
                />
              )}
              <span
                className="text-shadow block-text"
                style={{
                  color: currentFumoMessage.twitchUser.chatColor || "white",
                }}
              >
                <Textfit min={1} max={1500} forceSingleModeWidth>
                  {currentFumoMessage.twitchUser.displayName.toUpperCase()}
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
                {getFumoText(currentFumoMessage)}
              </Textfit>
            </span>
            {currentFumoMessage.collectedCount != undefined &&
              currentFumoMessage.totalCount != undefined &&
              currentFumoMessage.totalCount > 0 && (
                <span
                  className="text-shadow block-text"
                  style={{
                    color: "lightgreen",
                    fontSize: "0.7em",
                  }}
                >
                  <Textfit min={1} max={1500} forceSingleModeWidth>
                    Коллекция: {currentFumoMessage.collectedCount}/
                    {currentFumoMessage.totalCount}
                  </Textfit>
                </span>
              )}
          </div>
        </div>
      )}
    </div>
  );
}
