import { describe, expect, it } from "vitest";

import { resolveLocalMediaUrl } from "./resolveMediaUrl";

describe("resolveLocalMediaUrl", () => {
  it(" collapses double slash for MediaStorage leading-slash paths", () => {
    expect(resolveLocalMediaUrl("/", "/Alerts/uploaded_mems/a.png")).toBe(
      "/Alerts/uploaded_mems/a.png"
    );
  });

  it(" works when base path has no trailing slash", () => {
    expect(resolveLocalMediaUrl("", "/Alerts/x.png")).toBe("/Alerts/x.png");
  });

  it(" prefixes base path for extensionless paths", () => {
    expect(resolveLocalMediaUrl("/app/", "/Alerts/x.png")).toBe("/app/Alerts/x.png");
  });

  it(" adds a leading slash for paths without one (memory/...)", () => {
    expect(resolveLocalMediaUrl("/", "memory/aya.mp3")).toBe("/memory/aya.mp3");
  });

  it(" leaves external absolute URLs untouched", () => {
    expect(resolveLocalMediaUrl("/", "https://example.com/x.png")).toBe(
      "https://example.com/x.png"
    );
  });

  it(" leaves protocol-relative URLs untouched", () => {
    expect(resolveLocalMediaUrl("/", "//cdn.example.com/x.png")).toBe(
      "//cdn.example.com/x.png"
    );
  });

  it(" treats undefined base path as empty", () => {
    expect(resolveLocalMediaUrl(undefined, "/Alerts/x.png")).toBe("/Alerts/x.png");
  });
});