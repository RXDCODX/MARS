import "./animation.scss";

import { CSSProperties, memo, useEffect } from "react";

import { TunaMusicData } from "@/shared/api";

import AnimationControl from "./AnimationControl";
import { CurrentTrackElement } from "./CurrentTrackElement";
import { ProgressBar } from "./ProgressBar";

interface Properties {
  oldTrack: TunaMusicData;
  track: TunaMusicData;
  swap: boolean;
  swapChange: () => void;
  shouldAnimate?: boolean;
}

const baseStyles: CSSProperties = {
  position: "fixed",
  inset: 0,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  height: "70vh",
  top: "50%",
  transform: "translateY(-50%)",
};

function CurrentTrackComponent({
  oldTrack,
  track,
  swap,
  swapChange,
  shouldAnimate = true,
}: Properties) {
  useEffect(() => {
    if (shouldAnimate) {
      console.log(oldTrack, track, swap, shouldAnimate);
    }
  }, [oldTrack, shouldAnimate, swap, track]);

  return (
    <>
      <div
        id="current-track-container"
        style={{
          ...baseStyles,
        }}
        data-testid="obs-current-track"
      >
        <AnimationControl AnimationStart={shouldAnimate} swapTrack={swapChange}>
          {shouldAnimate && !swap && <CurrentTrackElement track={oldTrack} />}
          {(shouldAnimate && swap) || !shouldAnimate ? (
            <CurrentTrackElement track={track} />
          ) : null}
        </AnimationControl>
      </div>
      <ProgressBar track={track} />
    </>
  );
}

// Экспортируем мемоизированную версию для оптимизации
const CurrentTrack = memo(CurrentTrackComponent);
export default CurrentTrack;
