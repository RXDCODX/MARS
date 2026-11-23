import { Textfit } from "react-textfit";

import { ROULETTE_SIZE_PRESETS, RouletteSizeWithFill } from "./rouletteSizes";
import styles from "./WaifuRoulettePrizeItem.module.scss";

interface WaifuRoulettePrizeItemProperties {
  image: string;
  text?: string;
  size?: RouletteSizeWithFill;
  width?: number;
  height?: number;
}

export default function WaifuRoulettePrizeItem({
  image,
  text,
  size = "l",
  width,
  height,
}: WaifuRoulettePrizeItemProperties) {
  if (image) {
    const normalizedText = text?.trim();
    const preset = size === "fill" ? undefined : ROULETTE_SIZE_PRESETS[size];
    const itemWidth =
      width ?? preset?.width ?? ROULETTE_SIZE_PRESETS["l"].width;
    const itemHeight =
      height ?? preset?.height ?? ROULETTE_SIZE_PRESETS["l"].height;

    return (
      <figure
        className={styles.figure}
        style={{
          width: `${itemWidth}px`,
          height: `${itemHeight}px`,
          flexShrink: 0,
        }}
        data-testid="waifu-prize-item"
      >
        <div className={styles.inner}>
          <img
            src={image}
            alt={normalizedText || ""}
            className={styles.image}
            loading="lazy"
          />
        </div>
        {normalizedText ? (
          <figcaption className={styles.caption}>
            <Textfit mode="single" max={48} min={8}>
              {normalizedText}
            </Textfit>
          </figcaption>
        ) : null}
      </figure>
    );
  }

  return null;
}
