import { useCallback, useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";

import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
import { parseTwitchEventAlertPayload, type TwitchEventAlertPayload } from "@/shared/realtime/twitchEventAlertPayload";

import styles from "./TwitchEventAlerts.module.scss";

const DISPLAY_DURATION_MS = 6500;
const MAX_QUEUED_ALERTS = 5;

export default function TwitchEventAlerts() {
  const [alerts, setAlerts] = useState<TwitchEventAlertPayload[]>([]);
  const seenEventIds = useRef(new Set<string>());

  const handleEvent = useCallback((payload: unknown) => {
    const alert = parseTwitchEventAlertPayload(payload);

    if (!alert || seenEventIds.current.has(alert.eventId)) {
      return;
    }

    // RabbitMQ is at-least-once. Keep recently displayed IDs so a redelivery
    // cannot show the same visual alert twice while the overlay is running.
    seenEventIds.current.add(alert.eventId);
    if (seenEventIds.current.size > 500) {
      seenEventIds.current.clear();
      seenEventIds.current.add(alert.eventId);
    }

    setAlerts(current => [...current, alert].slice(-MAX_QUEUED_ALERTS));
  }, []);

  useOverlayEvent("TwitchEventAlert", handleEvent);

  const activeEventId = alerts[0]?.eventId;

  useEffect(() => {
    if (!activeEventId) {
      return;
    }

    const timeout = globalThis.setTimeout(() => {
      setAlerts(current => current.filter(alert => alert.eventId !== activeEventId));
    }, DISPLAY_DURATION_MS);

    return () => globalThis.clearTimeout(timeout);
  }, [activeEventId]);

  return (
    <div
      className={styles.root}
      data-testid="obs-twitch-event-alerts"
      aria-live="polite"
      aria-relevant="additions"
    >
      <AnimatePresence initial={false}>
        {alerts.map((alert, index) => (
          <motion.article
            key={alert.eventId}
            className={styles.alert}
            data-event-type={alert.eventType}
            initial={{ opacity: 0, y: -24, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -18, scale: 0.97 }}
            transition={{ duration: 0.28, ease: "easeOut" }}
            style={{ zIndex: MAX_QUEUED_ALERTS - index }}
          >
            <div className={styles.accent} />
            <div className={styles.content}>
              <div className={styles.eyebrow}>TWITCH EVENTSUB</div>
              <div className={styles.title}>{alert.title}</div>
              <div className={styles.message}>{alert.message}</div>
              {alert.viewers > 0 && (
                <div className={styles.viewers}>{alert.viewers.toLocaleString("ru-RU")} зрителей</div>
              )}
            </div>
            <div className={styles.brand}>TWITCH</div>
          </motion.article>
        ))}
      </AnimatePresence>
    </div>
  );
}
