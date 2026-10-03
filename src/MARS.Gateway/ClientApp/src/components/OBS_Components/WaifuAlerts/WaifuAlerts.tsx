/* eslint-disable simple-import-sort/imports */
import InjectStyles from "@/shared/components/InjectStyles";
import useTelegramusHubStore from "@/shared/stores/telegramusHubStore";
import useWaifuPrizesStore from "@/shared/stores/waifuPrizesStore";
import useTwitchStore from "@/shared/twitchStore/twitchStore";
import Announce from "@/shared/Utils/Announce/Announce";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useShallow } from "zustand/react/shallow";

import common from "../OBSCommon.module.scss";
import WaifuAddAlert from "./WaifuAddAlert";
import styles from "./WaifuAlerts.module.scss";
import WaifuMarriageAlert from "./WaifuMarriageAlert";
import WaifuRoulette from "./WaifuRoulette";

export default function WaifuAlerts() {
  const currentMessage = useTelegramusHubStore(
    useShallow(state => state.currentMessage)
  );
  const dequeueCurrent = useTelegramusHubStore(state => state.dequeueCurrent);
  const startHub = useTelegramusHubStore(state => state.start);
  const [announced, setAnnounced] = useState(false);
  const [isRouletted, setIsRouletted] = useState(false);
  const sendMessage = useTwitchStore(state => state.sendMsgToPyrokxnezxz);

  const prizes = useWaifuPrizesStore(useShallow(state => state.prizes));
  const shufflePrizes = useWaifuPrizesStore(state => state.shuffle);

  const preparedPrizes = useMemo(
    () =>
      (prizes || []).map(p => ({
        ...p,
        id: String(p.id),
        text: p.text || "",
      })),
    [prizes]
  );

  const rouletteIndex =
    currentMessage && prizes.length > 0
      ? prizes.findIndex(prize => prize.id === currentMessage.waifu.shikiId)
      : -1;

  useEffect(() => {
    startHub();
  }, [startHub]);

  const handleRemoveEvent = useCallback(() => {
    dequeueCurrent();
    setIsRouletted(false);
  }, [dequeueCurrent]);

  useEffect(() => {
    if (!currentMessage) {
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
  }, [currentMessage, prizes, rouletteIndex]);

  useEffect(() => {
    if (
      currentMessage &&
      (currentMessage.waifu.isMerged ||
        currentMessage.waifu.isAdded ||
        currentMessage.isReminder)
    ) {
      queueMicrotask(() => {
        setIsRouletted(true);
      });
    }
  }, [currentMessage]);

  const invokeHub = useTelegramusHubStore(state => state.invoke);
  const muteAll = useCallback(() => {
    invokeHub("MuteAll", []);
  }, [invokeHub]);

  const unmuteAll = useCallback(() => {
    invokeHub("UnmuteSessions");
  }, [invokeHub]);

  return (
    <div className={common.textStrokeShadow} data-testid="obs-waifu-alerts">
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
        id="waifu-alerts-styles"
      />
      {!announced && (
        <Announce title={"WaifuRoll"} callback={() => setAnnounced(true)} />
      )}
      {currentMessage &&
        !isRouletted &&
        !currentMessage.waifu.isMerged &&
        rouletteIndex !== -1 &&
        prizes.length > 0 &&
        currentMessage.waifuHusband?.twitchUser && (
          <WaifuRoulette
            key={"roulette-" + currentMessage.waifu.shikiId}
            shuffle={shufflePrizes}
            callback={() => {
              setIsRouletted(true);
            }}
            rouletteIndex={rouletteIndex}
            prizes={preparedPrizes}
            twitchUser={currentMessage.waifuHusband.twitchUser}
            size="fill"
          />
        )}
      {currentMessage &&
        !isRouletted &&
        !currentMessage.waifu.isMerged &&
        rouletteIndex === -1 &&
        prizes.length === 0 && (
          <div
            key={"loading-" + currentMessage.waifu.shikiId}
            className={styles["roulette-name-text"]}
            data-testid="status-loading-roulette"
          >
            <span>Загрузка рулетки...</span>
          </div>
        )}
      {currentMessage &&
        (isRouletted || currentMessage.waifu.isAdded) &&
        !currentMessage.waifu.isMerged &&
        !currentMessage.isReminder && (
          <WaifuAddAlert
            key={"add-" + currentMessage.waifu.shikiId}
            message={currentMessage}
            onRemove={handleRemoveEvent}
            onShuffle={shufflePrizes}
            onSendMessage={sendMessage}
          />
        )}
      {currentMessage &&
        ((isRouletted && currentMessage.waifu.isMerged) ||
          currentMessage.isReminder) && (
          <WaifuMarriageAlert
            key={"marriage-" + currentMessage.waifu.shikiId}
            message={currentMessage}
            onRemove={handleRemoveEvent}
            onMuteAll={muteAll}
            onUnmuteAll={unmuteAll}
            onSendMessage={sendMessage}
          />
        )}
    </div>
  );
}
