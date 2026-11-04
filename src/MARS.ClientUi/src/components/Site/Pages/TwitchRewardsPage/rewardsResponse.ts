import type { CustomReward } from "@/shared/api";

/**
 * Разбор ответа Helix-обёрткой.
 *
 * `TwitchRewardsController.GetRewards` отдаёт `OperationResult<GetCustomRewardsResponse?>`, а
 * `GetCustomRewardsResponse` — это `{ data?: CustomReward[] }`. То есть полезная
 * нагрузка конверта — объект, и список лежит ещё на уровень глубже.
 *
 * Страница брала `result.data.data` и проверяла `Array.isArray`: массива там
 * никогда не было, `setRewards([])` выполнялся, и `/twitch-rewards` показывал
 * «Награды не найдены» без единой ошибки. Остальные четыре действия страницы
 * при этом работали, так что список пустой не выглядел как сломанный сервис.
 *
 * Разбор вынесен отдельно, чтобы уровень вложенности был назван прямо: он и
 * был причиной дефекта.
 */

/** Ответ со списком наград: либо список, либо причина отказа. */
export type RewardsRead =
  | { ok: true; rewards: CustomReward[] }
  | { ok: false; message: string };

export const readRewardsList = (body: unknown): RewardsRead => {
  const fallback = "Не удалось загрузить награды";

  if (typeof body !== "object" || body === null) {
    return { ok: false, message: fallback };
  }

  const envelope = body as {
    success?: boolean;
    result?: unknown;
    data?: unknown;
    errorMessage?: string | null;
    message?: string | null;
  };

  if (envelope.success !== true) {
    return {
      ok: false,
      message: envelope.errorMessage ?? envelope.message ?? fallback,
    };
  }

  // Сначала полезная нагрузка конверта, потом обёртка Helix. Порядок важен:
  // у формы `{ success, result }` поля `data` нет, и наоборот.
  const payload = envelope.result ?? envelope.data;

  if (Array.isArray(payload)) {
    return { ok: true, rewards: payload as CustomReward[] };
  }

  if (typeof payload !== "object" || payload === null) {
    return { ok: false, message: fallback };
  }

  const wrapped = (payload as { data?: unknown }).data;

  if (!Array.isArray(wrapped)) {
    return { ok: false, message: fallback };
  }

  // Пустой список — законный ответ «наград нет», и он отличается от «ответ не
  // тот»: первый рисует пустую таблицу, второй обязан показать ошибку.
  return { ok: true, rewards: wrapped as CustomReward[] };
};
