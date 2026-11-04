import { create } from "zustand";

interface AddTrackModalStore {
  isOpen: boolean;
  open: () => void;
  close: () => void;
}

/**
 * Zustand store для управления состоянием модального окна добавления трека
 */
export const useAddTrackModalStore = create<AddTrackModalStore>(set => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
}));
