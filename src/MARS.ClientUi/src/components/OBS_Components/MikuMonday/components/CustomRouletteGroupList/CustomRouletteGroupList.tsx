import { forwardRef, ReactNode } from "react";

import type { RouletteGroup } from "../../types";
import CustomRoulette from "../CustomRoulette/CustomRoulette";
import styles from "./CustomRouletteGroupList.module.scss";

interface CustomRouletteGroupListProperties {
  groups: RouletteGroup[];
  rouletteStart: boolean;
  rouletteOpacities: number[];
  pointer: ReactNode;
  onPrizeDefined: () => void;
  onOthersFaded?: () => void;
  onWinnerFaded?: () => void;
}

const CustomRouletteGroupList = forwardRef<
  HTMLDivElement,
  CustomRouletteGroupListProperties
>(
  (
    {
      groups,
      rouletteStart,
      rouletteOpacities,
      pointer,
      onPrizeDefined,
      onOthersFaded,
      onWinnerFaded,
    },
    reference
  ) => {
    const handleRouletteComplete = (index: number) => {
      const group = groups[index];
      console.log(`[CustomRouletteGroupList] ✨ Рулетка ${index} завершена`, {
        prizeIndex: group.prizeIndex,
        prizeId: group.prizes[group.prizeIndex]?.id,
        prizeText: group.prizes[group.prizeIndex]?.text,
        isWinner: group.hasWinner,
      });
      onPrizeDefined?.();
    };

    const handleTransitionEnd = (index: number) => {
      const group = groups[index];
      const opacity = rouletteOpacities[index];

      if (opacity === 0) {
        if (group?.hasWinner) {
          console.log(
            `[CustomRouletteGroupList] 🎯 Выигрышная рулетка затухла (группа ${index})`,
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
              `[CustomRouletteGroupList] ⚪ Все остальные рулетки затухли (группа ${index})`
            );
            onOthersFaded?.();
          }
        }
      }
    };

    return (
      <div ref={reference} className={styles.container}>
        {pointer}
        {groups.map((group, index) => {
          // Для невыигрышных рулеток добавляем случайную вариативность
          const getSpinningTime = () => {
            if (group.hasWinner) return 23;
            // Разное время для каждой невыигрышной рулетки (18-22 сек)
            return 18 + ((index * 1.7) % 4);
          };

          // Случайное направление для невыигрышных рулеток
          const getDirection = () => {
            if (group.hasWinner) return group.isReversed;
            // Чередуем направления + добавляем вариативность по индексу
            return index % 2 === 0 ? !group.isReversed : group.isReversed;
          };

          return (
            <div
              key={index}
              className={`${styles.rouletteWrapper} ${
                getDirection() ? styles.reversed : ""
              }`}
              style={{
                opacity: rouletteOpacities[index] ?? 1,
                transition: "opacity 2s ease-out",
              }}
              onTransitionEnd={() => handleTransitionEnd(index)}
            >
              <CustomRoulette
                prizes={group.prizes}
                prizeIndex={group.prizeIndex}
                isReversed={getDirection()}
                start={rouletteStart}
                spinningTime={getSpinningTime()}
                onComplete={() => handleRouletteComplete(index)}
              />
            </div>
          );
        })}
      </div>
    );
  }
);

CustomRouletteGroupList.displayName = "CustomRouletteGroupList";

export default CustomRouletteGroupList;
