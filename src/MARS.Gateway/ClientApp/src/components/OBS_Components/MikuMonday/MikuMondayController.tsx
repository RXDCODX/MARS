import { useEffect, useState } from "react";

import InjectStyles from "@/shared/components/InjectStyles";
import { createErrorResult } from "@/shared/types/OperationResult";
import Announce from "@/shared/Utils/Announce/Announce";
import { useToastModal } from "@/shared/Utils/ToastModal";

import MikuMonday from "./MikuMonday";
import { requestUnmuteOtherAudio } from "./mikuMondayAudio";
import useMikuMondayStore from "./store/mikuMondayStore";

export default function MikuMondayController() {
  const [isAnnounced, setAnnounced] = useState(false);
  const startHub = useMikuMondayStore(state => state.start);
  const stopHub = useMikuMondayStore(state => state.stop);
  const status = useMikuMondayStore(state => state.status);
  const error = useMikuMondayStore(state => state.error);
  const currentAlert = useMikuMondayStore(state => state.currentAlert);
  const isAlertShowing = useMikuMondayStore(state => state.isAlertShowing);
  const { showToast } = useToastModal();

  useEffect(() => {
    console.log("[MikuMondayController] currentAlert изменился", {
      alertId: currentAlert?.id,
      queueId: currentAlert?.queueId,
      displayName: currentAlert?.twitchUser.displayName,
      trackNumber: currentAlert?.selectedTrack.number,
      isAlertShowing,
    });
  }, [
    currentAlert?.id,
    currentAlert?.queueId,
    currentAlert?.selectedTrack.number,
    currentAlert?.twitchUser.displayName,
    isAlertShowing,
  ]);

  useEffect(() => {
    startHub().catch(error_ => {
      showToast(
        createErrorResult(error_?.message || "Не удалось подключиться к хабу")
      );
    });

    return () => {
      stopHub().catch(() => void 0);
    };
  }, [startHub, stopHub, showToast]);

  useEffect(() => {
    if (status === "error" && error) {
      showToast(createErrorResult(error));
    }
  }, [status, error, showToast]);

  useEffect(() => {
    if (!currentAlert && !isAlertShowing) {
      requestUnmuteOtherAudio().catch(() => void 0);
    }
  }, [currentAlert, isAlertShowing]);

  useEffect(() => {
    const preloadVideo = document.createElement("video");
    preloadVideo.src = "/static/miku.mp4";
    preloadVideo.preload = "auto";
    preloadVideo.muted = true;
    preloadVideo.playsInline = true;
    preloadVideo.load();

    return () => {
      preloadVideo.pause();
      preloadVideo.src = "";
    };
  }, []);

  return (
    <>
      {!isAnnounced && (
        <Announce title={"Miku Monday"} callback={() => setAnnounced(true)} />
      )}
      <div data-testid="obs-miku-monday">
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
          id="miku-monday-styles"
        />

        {isAlertShowing && currentAlert ? (
          <MikuMonday key={currentAlert.id ?? currentAlert.queueId} />
        ) : null}
      </div>
    </>
  );
}
