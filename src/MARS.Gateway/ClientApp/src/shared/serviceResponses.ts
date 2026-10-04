/**
 * Разбор ответов `/api/ServiceManager`.
 *
 * Эндпоинты отвечают конвертом `MARS.Admin` — `{ success, message, data }`, то
 * есть второй формой, а не `{ success, result, errorMessage }` у общих
 * контроллеров. Стор звал `axios` напрямую, минуя транспорт, который разворачивает
 * обе формы, и отсюда три поломки: список сервисов всегда был пустым без ошибки,
 * логи клались конвертом и роняли просмотрщик на `logs.filter`, а отказ
 * управления приходил кодом 200 и выглядел выполненным.
 *
 * Разбор вынесен отдельно от стора, чтобы его проверяли без поднятия zustand и
 * без модуля axios. Типы перечислены здесь, а не импортированы из стора: иначе
 * возник бы круг «стор → разбор → стор».
 */

/** Сервис в том виде, в каком его отдаёт `/api/ServiceManager/services`. */
export interface ServiceInfo {
  name: string;
  displayName: string;
  description: string;
  status: string;
  startTime?: string;
  lastActivity?: string;
  isEnabled: boolean;
  configuration: object;
}

/** Строка журнала сервиса. */
export interface ServiceLog {
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
}

/** Конверт `MARS.Admin` в том виде, в каком он приходит. */
type AdminEnvelope = {
  success?: boolean;
  message?: string | null;
  data?: unknown;
};

const asEnvelope = (body: unknown): AdminEnvelope | null =>
  typeof body === "object" && body !== null ? (body as AdminEnvelope) : null;

/** Ответ со списком: либо данные, либо причина отказа. */
export type ServicesRead =
  | { ok: true; services: ServiceInfo[] }
  | { ok: false; message: string };

/**
 * Список сервисов.
 *
 * Отсутствие `data` при `success: true` — отказ, а не пустой список: на экране
 * это выглядит одинаково, а значит разное, и молчаливый «сервисов нет» скрыл бы
 * неверный ответ.
 */
export const readServicesList = (body: unknown): ServicesRead => {
  const message = "Ошибка загрузки сервисов";
  const envelope = asEnvelope(body);

  if (envelope === null || envelope.success !== true) {
    return { ok: false, message: envelope?.message ?? message };
  }

  if (!Array.isArray(envelope.data)) {
    return { ok: false, message };
  }

  return { ok: true, services: envelope.data as ServiceInfo[] };
};

/** Ответ с логами: либо массив, либо причина отказа. */
export type LogsRead =
  | { ok: true; logs: ServiceLog[] }
  | { ok: false; message: string };

/**
 * Логи сервиса.
 *
 * Массив обязателен: конверт в `logs` ронял `logs.filter` в просмотрщике и
 * вместе с ним страницу.
 */
export const readLogsList = (body: unknown): LogsRead => {
  const message = "Ошибка загрузки логов";
  const envelope = asEnvelope(body);

  if (envelope === null || envelope.success !== true) {
    return { ok: false, message: envelope?.message ?? message };
  }

  if (!Array.isArray(envelope.data)) {
    return { ok: false, message };
  }

  return { ok: true, logs: envelope.data as ServiceLog[] };
};

/** Ответ на действие: сервис отвечает 200 даже при отказе, решает только тело. */
export type ActionRead =
  | { ok: true; message: string }
  | { ok: false; message: string };

export const readActionResult = (body: unknown): ActionRead => {
  const fallback = "Ошибка управления сервисом";
  const envelope = asEnvelope(body);

  if (envelope === null) {
    return { ok: false, message: fallback };
  }

  const text = envelope.message ?? fallback;

  return envelope.success === true
    ? { ok: true, message: text }
    : { ok: false, message: text };
};
