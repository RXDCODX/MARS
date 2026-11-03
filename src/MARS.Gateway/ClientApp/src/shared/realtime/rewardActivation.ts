import { decodeJsonBranch } from "./overlayPayload";

export type JsonRecord = Record<string, unknown>;

export interface RewardActivationPayload extends JsonRecord {
  alertKey?: string | null;
  effectData?: unknown;
  eventId?: string;
}

function asRecord(value: unknown): JsonRecord | null {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return null;
  }

  return value as JsonRecord;
}

/**
 * Извлекает MARS reward extension из единого TwitchEventSub SignalR метода.
 *
 * Обычные Twitch notifications возвращаются как null и продолжают жить в
 * универсальном event pipeline. Специальные награды используют тот же метод,
 * а alertKey определяет только клиентский эффект, не транспорт.
 */
export function readRewardActivation(
  wirePayload: unknown
): RewardActivationPayload | null {
  const envelope = asRecord(decodeJsonBranch(wirePayload, "eventJson"));

  if (envelope?.type !== "mars.reward.activated") {
    return null;
  }

  return asRecord(envelope.payload) as RewardActivationPayload | null;
}

export function readRewardEffectData(
  activation: RewardActivationPayload | null
): JsonRecord | null {
  return asRecord(activation?.effectData);
}
