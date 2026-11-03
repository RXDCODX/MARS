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
 * Извлекает MARS reward extension из исходного Twitch EventSub JSON.
 *
 * Twitch metadata/payload остаются в исходном документе, а внутренние данные
 * MARS живут отдельно в mars.reward. Обычные уведомления возвращаются как null.
 */
export function readRewardActivation(
  wirePayload: unknown
): RewardActivationPayload | null {
  const envelope = asRecord(decodeJsonBranch(wirePayload, "eventJson"));
  const mars = asRecord(envelope?.mars);
  const reward = asRecord(mars?.reward);

  if (reward?.type !== "mars.reward.activated") {
    return null;
  }

  const payload = asRecord(reward.payload);

  if (payload === null) {
    return null;
  }

  const metadata = asRecord(envelope?.metadata);

  return {
    ...payload,
    eventId:
      (typeof reward.eventId === "string" && reward.eventId) ||
      (typeof metadata?.message_id === "string" && metadata.message_id) ||
      undefined,
  } as RewardActivationPayload;
}

export function readRewardEffectData(
  activation: RewardActivationPayload | null
): JsonRecord | null {
  return asRecord(activation?.effectData);
}
