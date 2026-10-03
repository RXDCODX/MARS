import "react-roulette-pro/dist/index.css";

import {
  CSSProperties,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import { PrizeType, TwitchUser } from "@/shared/api";
import animate from "@/shared/styles/animate.module.scss";
import { getRandomColor } from "@/shared/Utils";
import { RoulettePro } from "@/shared/Utils/reactRoulettePro";

import styles2 from "../OBSCommon.module.scss";
import {
  ROULETTE_SIZE_PRESETS,
  RouletteSizePreset,
  RouletteSizeWithFill,
} from "./components/rouletteSizes";
import WaifuRoulettePrizeItem from "./components/WaifuRoulettePrizeItem";
import styles from "./WaifuAlerts.module.scss";

const LABEL_HEIGHT = 200;
const FILL_HEIGHT_RATIO = 0.85;

function getFillPreset(): RouletteSizePreset {
  const availableHeight = window.innerHeight - LABEL_HEIGHT;
  const referencePreset = ROULETTE_SIZE_PRESETS["xxxxl"];
  const ratio = referencePreset.width / referencePreset.height;
  const height = Math.max(
    Math.round(availableHeight * FILL_HEIGHT_RATIO),
    referencePreset.height
  );
  const width = Math.round(height * ratio);
  return { width, height };
}

interface Properties {
  rouletteIndex: number;
  prizes: PrizeType[];
  callback: () => void;
  shuffle: () => void;
  twitchUser: TwitchUser;
  size?: RouletteSizeWithFill;
}

export default function WaifuRoulette({
  rouletteIndex,
  prizes,
  callback,
  twitchUser,
  shuffle,
  size = "l",
}: Properties) {
  if (prizes.length === 0) {
    throw new Error("Prizes is empty");
  }

  const [rouletteStart, setRouletteStart] = useState(false);
  const rouletteDiv = useRef<HTMLDivElement>(null);
  const heightDiv = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);
  const [trueColor] = useState<string>(
    twitchUser.chatColor || getRandomColor()
  );
  const [baseStyle, setBaseStyle] = useState<CSSProperties>({
    width: "100%",
    height: "100%",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyItems: "center",
    alignSelf: "center",
    animationDuration: "2.2s",
  });

  const preset = useMemo(
    () => (size === "fill" ? getFillPreset() : ROULETTE_SIZE_PRESETS[size]),
    [size]
  );

  const designPlugin = useCallback(
    () => ({
      topChildren: null,
      bottomChildren: null,
      prizeItemWidth: preset.width,
      prizeItemHeight: preset.height,
      classes: {},
    }),
    [preset.width, preset.height]
  );

  useEffect(() => {
    shuffle();
  }, [shuffle]);

  return (
    <div
      ref={rouletteDiv}
      className={" " + animate.animated + " " + animate.fadeIn}
      onAnimationEnd={() => {
        setRouletteStart(true);
        setVisible(true);
      }}
      style={baseStyle}
      data-testid="waifu-roulette"
    >
      <div
        ref={heightDiv}
        className={styles["smooth-box"]}
        style={{
          width: "100%",
          margin: "0 auto",
          ...(size === "fill" ? { flex: 1, minHeight: 0 } : { height: "100%" }),
          alignSelf: "center",
          position: "relative",
        }}
      >
        {visible && (
          <div
            style={{
              position: "absolute",
              top: "50%",
              left: "50%",
              width: "4px",
              height: `100%`,
              background: "orange",
              zIndex: 10,
              transform: "translate(-50%, -50%)",
              visibility: visible ? "visible" : "hidden",
            }}
          ></div>
        )}
        <RoulettePro
          key={rouletteIndex}
          start={rouletteStart}
          prizes={prizes}
          prizeIndex={rouletteIndex}
          spinningTime={20}
          type="horizontal"
          options={{ withoutAnimation: true, stopInCenter: true }}
          designPlugin={designPlugin}
          prizeItemRenderFunction={prize => (
            <WaifuRoulettePrizeItem
              image={prize.image}
              text={prize.text}
              size={size}
              width={preset.width}
              height={preset.height}
            />
          )}
          onPrizeDefined={() => {
            setVisible(false);
            const div = rouletteDiv.current!;
            div.addEventListener("animationend", () => {
              callback();
            });
            setBaseStyle(previous => ({
              ...previous,
              animationDuration: "1.5s",
            }));
            div.className = " " + animate.animated + " " + animate.fadeOut;
          }}
        />
      </div>
      <div
        className={
          styles["roulette-name-text"] + " " + styles2.textStrokeShadow
        }
        style={size === "fill" ? { flexShrink: 0 } : undefined}
      >
        <span>рулетка для</span>
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            gap: "20px",
            marginTop: "10px",
            padding: "20px",
          }}
        >
          {twitchUser.profileImageUrl && (
            <img
              src={twitchUser.profileImageUrl}
              alt={twitchUser.displayName}
              style={{
                width: "80px",
                height: "80px",
                borderRadius: "50%",
                border: `4px solid ${trueColor}`,
                boxShadow: `0 0 20px ${trueColor}`,
              }}
            />
          )}
          <span style={{ color: trueColor, fontSize: "1.2em" }}>
            {twitchUser.displayName}
          </span>
        </div>
      </div>
    </div>
  );
}
