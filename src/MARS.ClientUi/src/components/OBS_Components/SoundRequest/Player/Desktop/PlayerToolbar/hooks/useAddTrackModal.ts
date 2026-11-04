import { useCallback, useMemo } from "react";

import { SoundRequest } from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";

/**
 * Хук для работы с добавлением трека
 * Содержит только бизнес-логику добавления
 */
export function useAddTrackModal() {
  const { showToast } = useToastModal();
  const soundRequestApi = useMemo(() => new SoundRequest(defaultApiConfig), []);

  const addTrack = useCallback(
    async (query: string) => {
      if (!query.trim()) {
        return false;
      }

      try {
        const response = await soundRequestApi.soundRequestAddTrackCreate({
          query: query.trim(),
        });

        showToast(response.data);
        // Очередь обновится автоматически через SignalR
        return true;
      } catch (error) {
        console.error("Ошибка при добавлении трека:", error);
        showToast({
          success: false,
          message: "Произошла ошибка при добавлении трека",
        });
        return false;
      }
    },
    [soundRequestApi, showToast]
  );

  return {
    addTrack,
  };
}
