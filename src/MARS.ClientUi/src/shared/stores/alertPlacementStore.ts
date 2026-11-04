import { create } from "zustand";
import { devtools } from "zustand/middleware";

export interface AlertBounds {
  id: string;
  x: number;
  y: number;
  width: number;
  height: number;
  remainingTime: number;
  totalDuration: number;
  registeredAt: number;
}

interface AlertPlacementState {
  activeAlerts: AlertBounds[];
  registerAlert: (bounds: AlertBounds) => void;
  unregisterAlert: (id: string) => void;
  updateAlertBounds: (
    id: string,
    bounds: Pick<AlertBounds, "x" | "y" | "width" | "height">
  ) => void;
  updateRemainingTime: (id: string, remaining: number) => void;
  findFreeSpace: (
    width: number,
    height: number
  ) => { x: number; y: number } | null;
  findClosestToDeath: () => AlertBounds | null;
}

const MARGIN = 4;

function rectsOverlap(
  a: { x: number; y: number; width: number; height: number },
  b: { x: number; y: number; width: number; height: number }
): boolean {
  return !(
    a.x + a.width + MARGIN <= b.x ||
    b.x + b.width + MARGIN <= a.x ||
    a.y + a.height + MARGIN <= b.y ||
    b.y + b.height + MARGIN <= a.y
  );
}

function isWithinViewport(
  x: number,
  y: number,
  width: number,
  height: number
): boolean {
  return (
    x >= 0 &&
    y >= 0 &&
    x + width <= window.innerWidth &&
    y + height <= window.innerHeight
  );
}

function getRandomInt(min: number, max: number): number {
  const buffer = new Uint32Array(1);
  crypto.getRandomValues(buffer);
  return Math.floor((buffer[0] / (0xff_ff_ff_ff + 1)) * (max - min + 1)) + min;
}

export const useAlertPlacementStore = create<AlertPlacementState>()(
  devtools(
    (set, get) => ({
      activeAlerts: [],

      registerAlert: (bounds: AlertBounds) => {
        set(state => ({
          activeAlerts: [
            ...state.activeAlerts.filter(a => a.id !== bounds.id),
            bounds,
          ],
        }));
      },

      unregisterAlert: (id: string) => {
        set(state => ({
          activeAlerts: state.activeAlerts.filter(a => a.id !== id),
        }));
      },

      updateAlertBounds: (
        id: string,
        bounds: Pick<AlertBounds, "x" | "y" | "width" | "height">
      ) => {
        set(state => ({
          activeAlerts: state.activeAlerts.map(a =>
            a.id === id ? { ...a, ...bounds } : a
          ),
        }));
      },

      updateRemainingTime: (id: string, remaining: number) => {
        set(state => ({
          activeAlerts: state.activeAlerts.map(a =>
            a.id === id ? { ...a, remainingTime: remaining } : a
          ),
        }));
      },

      findFreeSpace: (
        width: number,
        height: number
      ): { x: number; y: number } | null => {
        const { activeAlerts } = get();
        const maxX = window.innerWidth - width;
        const maxY = window.innerHeight - height;

        if (maxX < 0 || maxY < 0) {
          return null;
        }

        // Empty screen — random position
        if (activeAlerts.length === 0) {
          return { x: getRandomInt(0, maxX), y: getRandomInt(0, maxY) };
        }

        // Collect candidate positions: edges of existing alerts + random
        const candidates: Array<{ x: number; y: number }> = [];

        // Try positions adjacent to existing alerts (tight packing)
        for (const alert of activeAlerts) {
          // Right of alert
          candidates.push({
            x: alert.x + alert.width + MARGIN,
            y: alert.y,
          });
          // Below alert
          candidates.push({
            x: alert.x,
            y: alert.y + alert.height + MARGIN,
          });
          // Left of alert
          candidates.push({
            x: alert.x - width - MARGIN,
            y: alert.y,
          });
          // Above alert
          candidates.push({
            x: alert.x,
            y: alert.y - height - MARGIN,
          });
          // Diagonal: right-below
          candidates.push({
            x: alert.x + alert.width + MARGIN,
            y: alert.y + alert.height + MARGIN,
          });
          // Diagonal: below-right
          candidates.push({
            x: alert.x,
            y: alert.y + alert.height + MARGIN,
          });
        }

        // Add random candidates
        for (let index = 0; index < 20; index++) {
          candidates.push({
            x: getRandomInt(0, maxX),
            y: getRandomInt(0, maxY),
          });
        }

        // Shuffle candidates for variety
        for (let index = candidates.length - 1; index > 0; index--) {
          const index_ = getRandomInt(0, index);
          [candidates[index], candidates[index_]] = [
            candidates[index_],
            candidates[index],
          ];
        }

        // Find first non-overlapping candidate
        for (const candidate of candidates) {
          if (!isWithinViewport(candidate.x, candidate.y, width, height)) {
            continue;
          }
          const hasOverlap = activeAlerts.some(alert =>
            rectsOverlap(
              { x: candidate.x, y: candidate.y, width, height },
              alert
            )
          );
          if (!hasOverlap) {
            return candidate;
          }
        }

        return null;
      },

      findClosestToDeath: (): AlertBounds | null => {
        const { activeAlerts } = get();
        if (activeAlerts.length === 0) {
          return null;
        }

        return activeAlerts.reduce((closest, current) =>
          current.remainingTime < closest.remainingTime ? current : closest
        );
      },
    }),
    { name: "alert-placement-store" }
  )
);
