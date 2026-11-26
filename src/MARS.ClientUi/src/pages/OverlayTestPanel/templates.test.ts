import { describe, expect, it } from "vitest";

import { OVERLAY_EVENT_NAMES } from "@/shared/api/realtime/overlayEvents";
import { OVERLAY_TEMPLATES, TEMPLATE_BY_EVENT } from "./templates";

describe("OverlayTestPanel templates", () => {
  it("covers all overlay events from manifest", () => {
    const names = [...OVERLAY_EVENT_NAMES].sort();
    const templateNames = OVERLAY_TEMPLATES.map(t => t.event).sort();
    expect(templateNames).toEqual(names);
  });

  it("has unique event names", () => {
    const names = OVERLAY_TEMPLATES.map(t => t.event);
    expect(new Set(names).size).toBe(names.length);
  });

  it("buildPayload returns JSON-serializable value or null", () => {
    for (const t of OVERLAY_TEMPLATES) {
      const payload = t.buildPayload();
      if (payload === null) continue;
      expect(() => JSON.stringify(payload)).not.toThrow();
      const roundTrip = JSON.parse(JSON.stringify(payload));
      expect(roundTrip).toBeTruthy();
    }
  });

  it("every template has at least one overlay route or is a known no-overlay event", () => {
    for (const t of OVERLAY_TEMPLATES) {
      expect(t.title.length).toBeGreaterThan(0);
      // all 25 hub events map to overlays or are known
      expect(Array.isArray(t.overlayRoutes)).toBe(true);
    }
  });

  it("parameterless events return null payload", () => {
    for (const name of ["Credits", "MichaelJackson", "PhonkEdit"] as const) {
      const t = TEMPLATE_BY_EVENT.get(name);
      expect(t).toBeTruthy();
      expect(t!.buildPayload()).toBeNull();
    }
  });
});
