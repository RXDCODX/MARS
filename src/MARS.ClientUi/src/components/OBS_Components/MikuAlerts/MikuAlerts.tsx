/* eslint-disable simple-import-sort/imports */
import { useCallback, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";
import { useShallow } from "zustand/react/shallow";
import useTelegramusHubStore from "@/shared/stores/telegramusHubStore";
import { useOverlayHubLifecycle } from "@/shared/realtime/useOverlayHubLifecycle";
import useMikuPrizesStore from "@/shared/stores/mikuPrizesStore";
import animate from "@/shared/styles/animate.module.scss";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import Announce from "@/shared/Utils/Announce/Announce";
import InjectStyles from "@/shared/components/InjectStyles";

import common from "../OBSCommon.module.scss";
import { getMikuText } from "./helper";
import styles from "../WaifuAlerts/WaifuAlerts.module.scss";
import WaifuRoulette from "../WaifuAlerts/WaifuRoulette";

export default function MikuAlerts() {
  const currentMikuMessage = useTelegramusHubStore(
    useShallow(state => state.currentMikuMessage)
  );
  const dequeueMikuCurrent = useTelegramusHubStore(
    state => state.dequeueMikuCurrent
  );
  const [announced, setAnnounced] = useState(false);
  const divHard = useRef<HTMLDivElement>(null);
  const [isRouletted, setIsRouletted] = useState(false);
  const sendMessage = useTwitchStore(state => state.sendMsgToPyrokxnezxz);
  const imageLoadTimeoutReference = useRef<NodeJS.Timeout | null>(null);

  const prizes = useMikuPrizesStore(useShallow(state => state.prizes));
  const shufflePrizes = useMikuPrizesStore(state => state.shuffle);

  const rouletteIndex =
    currentMikuMessage && prizes.length > 0
      ? prizes.findIndex(
          prize => prize.id === currentMikuMessage.mikuModule.pageId
        )
      : -1;

  useOverlayHubLifecycle();

  const handleRemoveEvent = useCallback(() => {
    dequeueMikuCurrent();
  }, [dequeueMikuCurrent]);

  useEffect(() => {
    if (!currentMikuMessage) {
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
  }, [currentMikuMessage, prizes, rouletteIndex]);

  useEffect(() => {
    if (!(currentMikuMessage && isRouletted)) {
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
  }, [currentMikuMessage, isRouletted, handleRemoveEvent]);

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
        id="miku-alerts-styles"
      />
      {!announced && (
        <Announce title={"MikuRoll"} callback={() => setAnnounced(true)} />
      )}
      {currentMikuMessage &&
        !isRouletted &&
        rouletteIndex !== -1 &&
        prizes.length > 0 && (
          <WaifuRoulette
            key={currentMikuMessage.mikuModule.pageId}
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
            twitchUser={currentMikuMessage.twitchUser}
            size="fill"
          />
        )}
      {currentMikuMessage &&
        !isRouletted &&
        rouletteIndex === -1 &&
        prizes.length === 0 && (
          <div className={styles["roulette-name-text"]}>
            <span>Загрузка рулетки Miku...</span>
          </div>
        )}
      {currentMikuMessage && isRouletted && (
        <div
          id={String(currentMikuMessage.mikuModule.pageId)}
          key={currentMikuMessage.mikuModule.pageId}
          ref={divHard}
          className={
            styles.baza + " " + animate.bounceIn + " " + animate.animated
          }
        >
          <div className={styles["alert-box"]}>
            <img
              src={currentMikuMessage.mikuModule.thumbnailUrl}
              style={{ height: "630px", width: "480px" }}
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
                  `@${currentMikuMessage.twitchUser.displayName}, ${getMikuText(currentMikuMessage)}!${currentMikuMessage.collectedCount != undefined && currentMikuMessage.totalCount != undefined && currentMikuMessage.totalCount > 0 ? ` Твоя коллекция костюмов насчитывает: ${currentMikuMessage.collectedCount}/${currentMikuMessage.totalCount} уникальных костюмов!` : ""}`
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
              data-testid="miku-alert-user-row"
              style={{
                display: "flex",
                alignItems: "center",
                gap: "15px",
              }}
            >
              {currentMikuMessage.twitchUser.profileImageUrl && (
                <img
                  src={currentMikuMessage.twitchUser.profileImageUrl}
                  alt={currentMikuMessage.twitchUser.displayName}
                  data-testid="miku-alert-avatar"
                  style={{
                    width: "80px",
                    height: "80px",
                    borderRadius: "50%",
                    border: `4px solid ${currentMikuMessage.twitchUser.chatColor || "white"}`,
                    boxShadow: `0 0 20px ${currentMikuMessage.twitchUser.chatColor || "white"}`,
                  }}
                />
              )}
              <span
                className="text-shadow block-text"
                style={{
                  color: currentMikuMessage.twitchUser.chatColor || "white",
                }}
              >
                <Textfit min={1} max={1500} forceSingleModeWidth>
                  {currentMikuMessage.twitchUser.displayName.toUpperCase()}
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
                {getMikuText(currentMikuMessage)}
              </Textfit>
            </span>
            {currentMikuMessage.collectedCount != undefined &&
              currentMikuMessage.totalCount != undefined &&
              currentMikuMessage.totalCount > 0 && (
                <span
                  className="text-shadow block-text"
                  style={{
                    color: "lightgreen",
                    fontSize: "0.7em",
                  }}
                >
                  <Textfit min={1} max={1500} forceSingleModeWidth>
                    Коллекция: {currentMikuMessage.collectedCount}/
                    {currentMikuMessage.totalCount}
                  </Textfit>
                </span>
              )}
          </div>
        </div>
      )}
    </div>
  );
}
