import { forwardRef, JSX, ReactNode } from "react";
import type PrizeType from "react-roulette-pro/dist/types/PrizeType";

import { RoulettePro } from "@/shared/Utils/reactRoulettePro";

import styles from "../MikuMonday.module.scss";
import type { RouletteGroup } from "../types";

interface RouletteGroupListProperties {
  groups: RouletteGroup[];
  rouletteStart: boolean;
  rouletteOpacities: number[];
  renderPrizeItem: (prize: PrizeType) => JSX.Element;
  onPrizeDefined: () => void;
  pointer: ReactNode;
  onOthersFaded?: () => void;
  onWinnerFaded?: () => void;
}

const RouletteGroupList = forwardRef<
  HTMLDivElement,
  RouletteGroupListProperties
>(
  (
    {
      groups,
      rouletteStart,
      rouletteOpacities,
      renderPrizeItem,
      onPrizeDefined,
      pointer,
      onOthersFaded,
      onWinnerFaded,
    },
    reference
  ) => {
    const handleTransitionEnd = (index: number) => {
      const group = groups[index];
      const opacity = rouletteOpacities[index];

      if (opacity === 0) {
        if (group?.hasWinner) {
          console.log(
            `[RouletteGroupList] 🎯 Выигрышная рулетка затухла (группа ${index})`,
            {
              winningPrizeId: group.prizes[group.prizeIndex]?.id,
              winningPrizeText: group.prizes[group.prizeIndex]?.text,
            }
          );
          onWinnerFaded?.();
        } else {
          // Проверяем, все ли проигравшие затухли
          const allOthersFaded = groups.every(
            (g, index_) => g.hasWinner || rouletteOpacities[index_] === 0
          );
          if (allOthersFaded) {
            console.log(
              `[RouletteGroupList] ⚪ Все остальные рулетки затухли (группа ${index})`
            );
            onOthersFaded?.();
          }
        }
      }
    };

    return (
      <div ref={reference} className={styles["roulette-container"]}>
        {pointer}
        {groups.map((group, index) => (
          <div
            key={index}
            className={`${styles["single-roulette"]} ${
              group.isReversed ? styles["single-roulette-reversed"] : ""
            }`}
            style={{
              opacity: rouletteOpacities[index] ?? 1,
              transition: "opacity 2s ease-out",
              flex: 1,
              minHeight: 0,
              pointerEvents: "none",
              maskImage:
                "linear-gradient(to right, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 1) 25%, rgba(0, 0, 0, 1) 75%, rgba(0, 0, 0, 0) 100%)",
              WebkitMaskImage:
                "linear-gradient(to right, rgba(0, 0, 0, 0) 0%, rgba(0, 0, 0, 1) 25%, rgba(0, 0, 0, 1) 75%, rgba(0, 0, 0, 0) 100%)",
            }}
            onTransitionEnd={() => handleTransitionEnd(index)}
          >
            <RoulettePro
              start={rouletteStart}
              prizes={group.prizes}
              prizeIndex={group.prizeIndex}
              prizeItemRenderFunction={renderPrizeItem}
              spinningTime={group.hasWinner ? 23 : 20}
              type="horizontal"
              options={{ withoutAnimation: true, stopInCenter: true }}
              defaultDesignOptions={{
                prizesWithText: true,
                hideCenterDelimiter: true,
              }}
              onPrizeDefined={() => {
                const group = groups[index];
                console.log(
                  `[RouletteGroupList] ✨ Рулетка ${index} выпала на приз:`,
                  {
                    prizeIndex: group.prizeIndex,
                    prizeId: group.prizes[group.prizeIndex]?.id,
                    prizeText: group.prizes[group.prizeIndex]?.text,
                    isWinner: group.hasWinner,
                  }
                );
                onPrizeDefined?.();
              }}
            />
          </div>
        ))}
      </div>
    );
  }
);

RouletteGroupList.displayName = "RouletteGroupList";

export default RouletteGroupList;
