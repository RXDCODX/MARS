import type { ServiceInfo, ServiceLog } from "@/shared/api";

export type { ServiceInfo, ServiceLog };

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
 * Типы берутся из сгенерированного контракта, а не объявляются здесь. Раньше были
 * свои копии, и они разошлись с сервером: у локального `ServiceInfo` было
 * обязательное поле `configuration`, которого нет ни в `MARS.Admin.Entities.ServiceInfo`,
 * ни в контракте, и которое никто не читает, а `status` был строкой вместо
 * перечисления. Свои копии пришлось бы поддерживать вручную.
 */

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
