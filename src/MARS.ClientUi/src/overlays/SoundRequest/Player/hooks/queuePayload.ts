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
 *
 * Приводится только одно поле: в proto длительность это `durationSeconds`
 * числами, а контракт клиента ждёт `duration` строкой `hh:mm:ss`. Имена остальных
 * полей совпадают, потому что хаб пишет через camelCase — тем же, что и REST.
 * Без приведения у всех треков очереди длительность показывалась как «00:00».
 * Уже приведённый элемент, пришедший из REST-запроса, не меняется.
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

  return queue.map(item => normalizeQueueItem(item)) as QueueItem[];
}

/**
 * Приводит элемент очереди к форме, которую читают компоненты.
 *
 * Приводится только то, что реально отличается: длительность внутри трека. Остальное
 * proto-сообщения уже совпадает с контрактом клиента — включая `queueOrder` и
 * `id`, поэтому лишних преобразований здесь не делается.
 */
function normalizeQueueItem(item: unknown): unknown {
  if (item === null || typeof item !== "object" || Array.isArray(item)) {
    return item;
  }

  const queueItem = item as Record<string, unknown>;
  const track = queueItem.track;

  return {
    ...queueItem,
    track:
      track === null || typeof track !== "object"
        ? track
        : normalizeTrack(track),
  };
}

/**
 * Приводит трек к форме, которую читают компоненты.
 *
 * `duration_seconds` — целые секунды из proto, а компонент ждёт строку
 * `hh:mm:ss`. Отсутствующая длительность остаётся отсутствующей: подставлять
 * «00:00» значило бы показать трек длиной ноль вместо «длительность неизвестна».
 */
function normalizeTrack(track: object): unknown {
  const source = track as Record<string, unknown>;
  // Поле называется durationSeconds, а не duration_seconds: хаб пишет через
  // camelCase, который задаёт AddMarsSignalR.
  const seconds = source.durationSeconds;

  if (source.duration !== undefined || seconds === undefined) {
    return source;
  }

  return {
    ...source,
    duration: formatSeconds(seconds),
  };
}

/**
 * Собирает `hh:mm:ss` из целых секунд.
 *
 * Нечисловое и отрицательное значение даёт нули, а не `NaN`: длительность идёт
 * в делитель полосы прогресса, и `NaN` отрисовался бы как «-1%».
 */
function formatSeconds(value: unknown): string {
  const total = typeof value === "number" ? value : Number(value);

  if (!Number.isFinite(total) || total < 0) {
    return "00:00:00";
  }

  const whole = Math.floor(total);
  const hours = Math.floor(whole / 3600);
  const minutes = Math.floor((whole % 3600) / 60);
  const seconds = whole % 60;

  return [hours, minutes, seconds]
    .map(part => String(part).padStart(2, "0"))
    .join(":");
}
