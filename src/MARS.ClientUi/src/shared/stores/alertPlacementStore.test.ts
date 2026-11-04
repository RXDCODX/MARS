import { beforeEach, describe, expect, it } from "vitest";

import { AlertBounds, useAlertPlacementStore } from "./alertPlacementStore";

// Mock window dimensions
Object.defineProperties(globalThis, {
  innerWidth: { value: 1920, writable: true },
  innerHeight: { value: 1080, writable: true },
});

describe("alertPlacementStore", () => {
  beforeEach(() => {
    useAlertPlacementStore.setState({ activeAlerts: [] });
  });

  it("registers an alert", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 100,
      y: 100,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);

    const { activeAlerts } = useAlertPlacementStore.getState();
    expect(activeAlerts).toHaveLength(1);
    expect(activeAlerts[0].id).toBe("test-1");
  });

  it("unregisters an alert", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 100,
      y: 100,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);
    useAlertPlacementStore.getState().unregisterAlert("test-1");

    const { activeAlerts } = useAlertPlacementStore.getState();
    expect(activeAlerts).toHaveLength(0);
  });

  it("updates alert bounds", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 100,
      y: 100,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);
    useAlertPlacementStore.getState().updateAlertBounds("test-1", {
      x: 200,
      y: 300,
      width: 500,
      height: 400,
    });

    const updated = useAlertPlacementStore
      .getState()
      .activeAlerts.find(a => a.id === "test-1");
    expect(updated?.x).toBe(200);
    expect(updated?.y).toBe(300);
    expect(updated?.width).toBe(500);
    expect(updated?.height).toBe(400);
  });

  it("updates remaining time", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 100,
      y: 100,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);
    useAlertPlacementStore.getState().updateRemainingTime("test-1", 5);

    const updated = useAlertPlacementStore
      .getState()
      .activeAlerts.find(a => a.id === "test-1");
    expect(updated?.remainingTime).toBe(5);
  });

  it("finds random free space when screen is empty", () => {
    const result = useAlertPlacementStore.getState().findFreeSpace(400, 300);
    expect(result).not.toBeNull();
    expect(result!.x).toBeGreaterThanOrEqual(0);
    expect(result!.y).toBeGreaterThanOrEqual(0);
    expect(result!.x).toBeLessThanOrEqual(1920 - 400);
    expect(result!.y).toBeLessThanOrEqual(1080 - 300);
  });

  it("finds free space avoiding existing alerts", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 0,
      y: 0,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);

    const result = useAlertPlacementStore.getState().findFreeSpace(400, 300);
    expect(result).not.toBeNull();
    // Should not overlap with the existing alert
    const isOverlaps =
      result!.x < alert.x + alert.width + 4 &&
      result!.x + 400 + 4 > alert.x &&
      result!.y < alert.y + alert.height + 4 &&
      result!.y + 300 + 4 > alert.y;
    expect(isOverlaps).toBe(false);
  });

  it("returns null when no free space available", () => {
    // Fill the screen with alerts
    const alerts: AlertBounds[] = [
      {
        id: "a",
        x: 0,
        y: 0,
        width: 960,
        height: 540,
        remainingTime: 10,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
      {
        id: "b",
        x: 960,
        y: 0,
        width: 960,
        height: 540,
        remainingTime: 10,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
      {
        id: "c",
        x: 0,
        y: 540,
        width: 960,
        height: 540,
        remainingTime: 10,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
      {
        id: "d",
        x: 960,
        y: 540,
        width: 960,
        height: 540,
        remainingTime: 10,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
    ];

    for (const alert of alerts) {
      useAlertPlacementStore.getState().registerAlert(alert);
    }

    const result = useAlertPlacementStore.getState().findFreeSpace(400, 300);
    expect(result).toBeNull();
  });

  it("finds closest to death", () => {
    const alerts: AlertBounds[] = [
      {
        id: "a",
        x: 0,
        y: 0,
        width: 100,
        height: 100,
        remainingTime: 8,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
      {
        id: "b",
        x: 200,
        y: 0,
        width: 100,
        height: 100,
        remainingTime: 2,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
      {
        id: "c",
        x: 400,
        y: 0,
        width: 100,
        height: 100,
        remainingTime: 5,
        totalDuration: 10,
        registeredAt: Date.now(),
      },
    ];

    for (const alert of alerts) {
      useAlertPlacementStore.getState().registerAlert(alert);
    }

    const closest = useAlertPlacementStore.getState().findClosestToDeath();
    expect(closest?.id).toBe("b");
  });

  it("findClosestToDeath returns null when no alerts", () => {
    const closest = useAlertPlacementStore.getState().findClosestToDeath();
    expect(closest).toBeNull();
  });

  it("packs alerts tightly near existing alerts", () => {
    const alert: AlertBounds = {
      id: "test-1",
      x: 100,
      y: 100,
      width: 400,
      height: 300,
      remainingTime: 10,
      totalDuration: 10,
      registeredAt: Date.now(),
    };

    useAlertPlacementStore.getState().registerAlert(alert);

    // Run multiple times to account for randomness
    let adjacentCount = 0;
    const runs = 20;
    for (let index = 0; index < runs; index++) {
      const result = useAlertPlacementStore.getState().findFreeSpace(400, 300);
      expect(result).not.toBeNull();

      // Check it doesn't overlap
      const isOverlaps =
        result!.x < alert.x + alert.width + 4 &&
        result!.x + 400 + 4 > alert.x &&
        result!.y < alert.y + alert.height + 4 &&
        result!.y + 300 + 4 > alert.y;
      expect(isOverlaps).toBe(false);

      // Check if adjacent (within 100px of any edge)
      const isNearRight = Math.abs(result!.x - (alert.x + alert.width)) <= 100;
      const isNearBottom =
        Math.abs(result!.y - (alert.y + alert.height)) <= 100;
      const isNearLeft = Math.abs(result!.x + 400 - alert.x) <= 100;
      const isNearTop = Math.abs(result!.y + 300 - alert.y) <= 100;
      if (isNearRight || isNearBottom || isNearLeft || isNearTop) {
        adjacentCount++;
      }
    }

    // At least some results should be adjacent (proves tight packing works)
    expect(adjacentCount).toBeGreaterThanOrEqual(1);
  });
});
