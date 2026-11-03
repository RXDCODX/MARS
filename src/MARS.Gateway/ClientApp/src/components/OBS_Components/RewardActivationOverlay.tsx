import { useCallback, useEffect, useRef, useState } from "react";

import { decodeJsonBranch } from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";

type JsonRecord = Record<string, unknown>;

const SPECIAL_REWARD_KEYS = new Set([
  "confetti",
  "fireworks",
  "tiktokedit",
  "randomart",
  "mikumikebeam",
  "gaoalert",
  "adhdsuperpower",
  "allrefund",
  "closegame",
]);

type RewardNotice = {
  eventId: string;
  title: string;
  userName: string;
  cost: number;
  userInput?: string;
};

function asRecord(value: unknown): JsonRecord | null {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return null;
  }

  return value as JsonRecord;
}

/**
 * Универсальное отображение обычной Channel Points-награды.
 *
 * Специальные эффекты также приходят в том же TwitchEventSub JSON и могут
 * использовать payload.effectData. Компонент не знает Twitch reward ID и не
 * требует регистрации React-компонента для каждой обычной награды.
 */
export default function RewardActivationOverlay() {
  const [queue, setQueue] = useState<RewardNotice[]>([]);
  const seen = useRef(new Set<string>());

  const handleEvent = useCallback((wirePayload: unknown) => {
    const envelope = asRecord(decodeJsonBranch(wirePayload, "eventJson"));

    if (envelope?.type !== "mars.reward.activated") {
      return;
    }

    const payload = asRecord(envelope.payload);

    if (payload === null) {
      return;
    }

    // Награды со своим экранным поведением обрабатывают профильные компоненты
    // через тот же TwitchEventSub. Общую карточку показываем только там, где
    // у награды нет собственного представления.
    const alertKey =
      typeof payload.alertKey === "string" ? payload.alertKey : undefined;

    if (alertKey && SPECIAL_REWARD_KEYS.has(alertKey)) {
      return;
    }

    const user = asRecord(payload.user);
    const eventId =
      typeof envelope.eventId === "string"
        ? envelope.eventId
        : [
            String(payload.rewardId ?? "reward"),
            String(user?.twitchId ?? user?.userLogin ?? "viewer"),
            String(payload.redeemedAt ?? ""),
          ].join(":");

    if (seen.current.has(eventId)) {
      return;
    }

    seen.current.add(eventId);

    if (seen.current.size > 500) {
      const oldest = seen.current.values().next().value;

      if (oldest !== undefined) {
        seen.current.delete(oldest);
      }
    }

    const title =
      typeof payload.rewardTitle === "string" && payload.rewardTitle.length > 0
        ? payload.rewardTitle
        : "Новая награда";
    const userName =
      (typeof user?.displayName === "string" && user.displayName) ||
      (typeof user?.userLogin === "string" && user.userLogin) ||
      "Зритель";
    const cost = typeof payload.cost === "number" ? payload.cost : 0;
    const userInput =
      typeof payload.userInput === "string" ? payload.userInput : undefined;

    setQueue(previous => [
      ...previous,
      { eventId, title, userName, cost, userInput },
    ].slice(-20));
  }, []);

  useOverlayEvent("TwitchEventSub", handleEvent);

  const current = queue[0];

  useEffect(() => {
    if (current === undefined) {
      return;
    }

    const timer = window.setTimeout(() => {
      setQueue(previous => previous.slice(1));
    }, 6000);

    return () => window.clearTimeout(timer);
  }, [current?.eventId]);

  if (current === undefined) {
    return null;
  }

  return (
    <aside
      role="status"
      data-testid="reward-activation-overlay"
      style={{
        position: "fixed",
        right: 36,
        bottom: 36,
        zIndex: 10000,
        width: 440,
        maxWidth: "calc(100vw - 48px)",
        padding: "22px 26px",
        border: "1px solid rgba(255,255,255,.24)",
        borderRadius: 18,
        background: "linear-gradient(135deg, rgba(26,20,42,.94), rgba(55,32,82,.94))",
        boxShadow: "0 18px 60px rgba(0,0,0,.38)",
        color: "#fff",
        fontFamily: "inherit",
        pointerEvents: "none",
        animation: "mars-reward-enter .24s ease-out",
      }}
    >
      <style>
        {`@keyframes mars-reward-enter {
          from { opacity: 0; transform: translateY(18px) scale(.98); }
          to { opacity: 1; transform: translateY(0) scale(1); }
        }`}
      </style>
      <div
        style={{
          fontSize: 12,
          fontWeight: 800,
          letterSpacing: ".16em",
          opacity: 0.72,
          textTransform: "uppercase",
          marginBottom: 8,
        }}
      >
        Channel Points
      </div>
      <div style={{ fontSize: 25, fontWeight: 800, lineHeight: 1.15 }}>
        {current.title}
      </div>
      <div style={{ marginTop: 10, fontSize: 17, opacity: 0.92 }}>
        {current.userName} активировал награду · {current.cost} баллов
      </div>
      {current.userInput && (
        <div
          style={{
            marginTop: 12,
            padding: "10px 12px",
            borderRadius: 10,
            background: "rgba(255,255,255,.1)",
            fontSize: 15,
            overflowWrap: "anywhere",
          }}
        >
          {current.userInput}
        </div>
      )}
    </aside>
  );
}
