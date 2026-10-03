import { useCallback, useMemo, useState } from "react";

import { SoundRequest } from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "../Desktop/SoundRequestPlayerDesktop.module.scss";
import { usePlayerStore } from "../stores/usePlayerStore";

/**
 * Хук для работы с очередью треков
 * Содержит логику удаления треков и обработки hover эффектов
 */
export const useQueueActions = () => {
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [playingNowId, setPlayingNowId] = useState<string | null>(null);
  const { showToast } = useToastModal();
  const soundRequestApi = useMemo(() => new SoundRequest(defaultApiConfig), []);

  // Обработчик hover для синхронизации между колонками
  const handleItemHover = useCallback(
    (trackId: string | undefined, isEnter: boolean) => {
      if (!trackId) return;

      const items = document.querySelectorAll(
        `[data-track-id="${CSS.escape(trackId)}"]`
      );
      items.forEach(item => {
        item.classList.toggle(styles.pairHovered, isEnter);
      });
    },
    []
  );

  // Обработчик удаления трека из очереди с оптимистичным обновлением
  const handleDeleteFromQueue = useCallback(
    async (queueItemId: string) => {
      if (deletingId) return; // Предотвращаем множественные удаления

      setDeletingId(queueItemId);

      // Сохраняем текущую очередь для отката
      const previousQueue = [...usePlayerStore.getState().queue];

      // Оптимистичное обновление - удаляем сразу из UI
      usePlayerStore.getState().removeFromQueue(queueItemId);

      try {
        const response =
          await soundRequestApi.soundRequestQueueDelete(queueItemId);

        if (!response.data.success) {
          // Откатываем изменения при ошибке
          usePlayerStore.getState().rollbackQueue(previousQueue);
          showToast({
            success: false,
            message: response.data.message || "Не удалось удалить трек",
          });
        }
      } catch (error) {
        // Откатываем изменения при ошибке
        usePlayerStore.getState().rollbackQueue(previousQueue);
        console.error("Ошибка при удалении трека:", error);
        showToast({
          success: false,
          message: "Произошла ошибка при удалении трека",
        });
      } finally {
        setDeletingId(null);
      }
    },
    [deletingId, soundRequestApi, showToast]
  );

  const handlePlayNow = useCallback(
    async (queueItemId: string) => {
      if (playingNowId) return;

      setPlayingNowId(queueItemId);

      try {
        const response =
          await soundRequestApi.soundRequestPlayNowCreate(queueItemId);

        if (response.data.success) {
          showToast({
            success: true,
            message: response.data.message || "Трек запущен",
          });
        } else {
          showToast({
            success: false,
            message: response.data.message || "Не удалось запустить трек",
          });
        }
      } catch (error) {
        console.error("Ошибка при немедленном воспроизведении трека:", error);
        showToast({
          success: false,
          message: "Произошла ошибка при запуске трека",
        });
      } finally {
        setPlayingNowId(null);
      }
    },
    [playingNowId, soundRequestApi, showToast]
  );

  // Обработчики перемещения элементов очереди (вверх/вниз)
  const handleMoveTo = useCallback(
    async (queueItemId: string, newPosition: number) => {
      const previousQueue = [...usePlayerStore.getState().queue];

      // Оптимистичное обновление локальной очереди
      const currentQueue = previousQueue;
      const index = currentQueue.findIndex(index_ => index_.id === queueItemId);
      if (index === -1) return;

      const item = currentQueue[index];
      const updated = [...currentQueue];
      // Удаляем элемент
      updated.splice(index, 1);
      // Вставляем на новую позицию (ограничиваем границы)
      const pos = Math.max(0, Math.min(newPosition, updated.length));
      updated.splice(pos, 0, item);
      usePlayerStore.getState().setQueue(updated);

      try {
        const response = await soundRequestApi.soundRequestQueueReorderCreate({
          queueItemId,
          newPosition: pos,
        } as any);

        if (!response.data.success) {
          usePlayerStore.getState().rollbackQueue(previousQueue);
          showToast({
            success: false,
            message: response.data.message || "Не удалось переместить трек",
          });
        }
      } catch (error) {
        console.error("Ошибка при перемещении трека:", error);
        usePlayerStore.getState().rollbackQueue(previousQueue);
        showToast({
          success: false,
          message: "Произошла ошибка при перемещении трека",
        });
      }
    },
    [soundRequestApi, showToast]
  );

  const handleMoveUp = useCallback(
    (queueItemId: string) => {
      const queue = usePlayerStore.getState().queue;
      const index = queue.findIndex(index_ => index_.id === queueItemId);
      if (index <= 0) return;
      handleMoveTo(queueItemId, index - 1);
    },
    [handleMoveTo]
  );

  const handleMoveDown = useCallback(
    (queueItemId: string) => {
      const queue = usePlayerStore.getState().queue;
      const index = queue.findIndex(index_ => index_.id === queueItemId);
      if (index === -1 || index >= queue.length - 1) return;
      handleMoveTo(queueItemId, index + 1);
    },
    [handleMoveTo]
  );

  return {
    handleItemHover,
    handleDeleteFromQueue,
    handlePlayNow,
    handleMoveUp,
    handleMoveDown,
    // Полная переупорядочивающая фунция (queueItemId, newIndex)
    handleReorder: handleMoveTo,
    deletingId,
    playingNowId,
  };
};
