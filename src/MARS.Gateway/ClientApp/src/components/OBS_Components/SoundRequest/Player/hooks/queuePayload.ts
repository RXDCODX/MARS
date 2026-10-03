/**
 * Форма события очереди звуковых запросов.
 *
 * `QueueItemSnapshot` с сервера повторяет поля DTO, которые уже описаны в
 * `shared/api`, поэтому отдельный тип здесь только для разбора.
 */
import type { QueueItem } from "@/shared/api";

/**
 * Разворачивает конверт `QueueState` в список треков.
 *
 * В proto у `QueueState` единственное поле `repeated QueueItemSnapshot queue`,
 * и реле кладёт в метод хаба само сообщение, то есть объект `{ queue: [...] }`.
 *
 * Раньше обработчик принимал `QueueItem[]` и клал в стор объект: длина у него
 * была `undefined`, и очередь на экране не обновлялась. Возвращается `null`,
 * когда очереди в сообщении нет, — «очередь неизвестна» и «зрителей нет» это
 * разные вещи, и подменять одно другим нельзя.
 */
export function readQueueItems(payload: unknown): QueueItem[] | null {
  if (
    payload === null ||
    typeof payload !== "object" ||
    Array.isArray(payload)
  ) {
    return null;
  }

  const queue = (payload as { queue?: unknown }).queue;

  if (!Array.isArray(queue)) {
    return null;
  }

  return queue as QueueItem[];
}
