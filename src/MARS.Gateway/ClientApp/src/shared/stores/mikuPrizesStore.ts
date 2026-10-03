import { PrizeType } from "react-roulette-pro";
import { create } from "zustand";
import { devtools } from "zustand/middleware";

interface Actions {
  addPrizes: (prizes: PrizeType[]) => void;
  shuffle: () => void;
  clear: () => void;
}

interface State {
  prizes: PrizeType[];
}

const initialState: State = {
  prizes: [],
};

export const useMikuPrizesStore = create<State & Actions>()(
  devtools(
    (set, get) => ({
      ...initialState,

      addPrizes: (newPrizes: PrizeType[]) => {
        const currentPrizes = get().prizes;
        const prizesToAdd = newPrizes.filter(newPrize =>
          currentPrizes.every(existing => existing.id !== newPrize.id)
        );

        if (prizesToAdd.length === 0) {
          return;
        }

        prizesToAdd.forEach(prize => {
          if (!prize.image) {
            return;
          }

          const img = new Image();
          img.src = prize.image;
        });

        set({ prizes: [...currentPrizes, ...prizesToAdd] });
      },

      shuffle: () => {
        const currentPrizes = get().prizes;
        const shuffled = [...currentPrizes].sort(() => Math.random() - 0.5);
        set({ prizes: shuffled });
      },

      clear: () => {
        set({ prizes: [] });
      },
    }),
    { name: "MikuPrizesStore" }
  )
);

export default useMikuPrizesStore;
