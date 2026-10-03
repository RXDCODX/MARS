import { PrizeType } from "react-roulette-pro";
import { create } from "zustand";
import { devtools } from "zustand/middleware";

interface Actions {
  addPrizes: (prizes: PrizeType[]) => void;
  shuffle: () => void;
  clear: () => void;
  preloadImages: (prizes: PrizeType[]) => void;
}

interface State {
  prizes: PrizeType[];
}

const initialState: State = {
  prizes: [],
};

/**
 * Store для управления призами рулетки вайфу
 */
export const useWaifuPrizesStore = create<State & Actions>()(
  devtools(
    (set, get) => ({
      ...initialState,

      /**
       * Добавляет новые призы, исключая дубликаты
       */
      addPrizes: (newPrizes: PrizeType[]) => {
        const currentPrizes = get().prizes;

        // Фильтруем только те призы, которых еще нет в текущем массиве
        const prizesToAdd = newPrizes.filter(newPrize =>
          currentPrizes.every(existing => existing.id !== newPrize.id)
        );

        // Если нет новых призов для добавления, не обновляем state
        if (prizesToAdd.length === 0) {
          return;
        }

        // Предзагрузка изображений для новых призов
        prizesToAdd.forEach(prize => {
          if (!prize.image) {
            return;
          }

          const img = new Image();
          img.src = prize.image;
        });

        set({ prizes: [...currentPrizes, ...prizesToAdd] });
      },

      /**
       * Перемешивает призы в случайном порядке
       */
      shuffle: () => {
        const currentPrizes = get().prizes;
        const shuffled = [...currentPrizes].sort(() => Math.random() - 0.5);
        set({ prizes: shuffled });
      },

      /**
       * Очищает все призы
       */
      clear: () => {
        set({ prizes: [] });
      },

      /**
       * Предзагружает изображения призов
       */
      preloadImages: (prizes: PrizeType[]) => {
        for (const prize of prizes) {
          if (!prize.image) {
            continue;
          }

          const img = new Image();
          img.src = prize.image;
        }
      },
    }),
    { name: "WaifuPrizesStore" }
  )
);

export default useWaifuPrizesStore;
