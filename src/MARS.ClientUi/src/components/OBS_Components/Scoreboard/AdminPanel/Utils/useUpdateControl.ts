import { useCallback, useRef } from "react";

export interface UpdateControl {
  isUpdating: boolean;
  lastUpdateTime: number;
  pendingUpdates: Set<string>;
  startUpdate: (updateId?: string) => boolean;
  finishUpdate: (updateId?: string) => void;
  shouldIgnoreUpdate: (timestamp: number) => boolean;
  generateUpdateId: () => string;
}

export const useUpdateControl = (): UpdateControl => {
  const isUpdatingReference = useRef<boolean>(false);
  const lastUpdateTimeReference = useRef<number>(0);
  const pendingUpdatesReference = useRef<Set<string>>(new Set());

  const generateUpdateId = useCallback(
    () => `update_${Date.now()}_${Math.random().toString(36).slice(2, 11)}`,
    []
  );

  const startUpdate = useCallback((updateId?: string): boolean => {
    // Проверяем, не отправляем ли мы уже обновление
    if (isUpdatingReference.current) {
      console.log("Update already in progress, skipping");
      return false;
    }

    // Устанавливаем флаг обновления
    isUpdatingReference.current = true;
    lastUpdateTimeReference.current = Date.now();

    if (updateId) {
      pendingUpdatesReference.current.add(updateId);
    }

    console.log(`Starting update${updateId ? `: ${updateId}` : ""}`);
    return true;
  }, []);

  const finishUpdate = useCallback((updateId?: string) => {
    // Сбрасываем флаг через задержку для предотвращения рекурсии
    setTimeout(() => {
      isUpdatingReference.current = false;
      if (updateId) {
        pendingUpdatesReference.current.delete(updateId);
      }
      console.log(`Finished update${updateId ? `: ${updateId}` : ""}`);
    }, 300);
  }, []);

  const shouldIgnoreUpdate = useCallback((timestamp: number): boolean => {
    // Игнорируем обновления, если мы сами отправляем данные на сервер
    if (isUpdatingReference.current) {
      console.log("Ignoring server update while local update is in progress");
      return true;
    }

    // Игнорируем обновления, которые пришли слишком быстро после нашего обновления
    if (timestamp - lastUpdateTimeReference.current < 500) {
      console.log("Ignoring server update - too soon after local update");
      return true;
    }

    return false;
  }, []);

  return {
    isUpdating: isUpdatingReference.current,
    lastUpdateTime: lastUpdateTimeReference.current,
    pendingUpdates: pendingUpdatesReference.current,
    startUpdate,
    finishUpdate,
    shouldIgnoreUpdate,
    generateUpdateId,
  };
};
