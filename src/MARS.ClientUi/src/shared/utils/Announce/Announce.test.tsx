import { act } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, waitFor } from "@testing-library/react";

import Announce from "./Announce";
import { useOverlayHubStore } from "@/shared/stores/overlayHubStore";
import { FakeHubAdapter } from "@/shared/api/realtime/FakeHubAdapter";

vi.mock("react-textfit", () => ({
  Textfit: ({ children }: { children: React.ReactNode }) =>
    children as React.ReactNode,
}));

describe("Announce", () => {
  beforeEach(() => {
    useOverlayHubStore.getState().reset();
  });

  afterEach(() => {
    cleanup();
  });

  it("показывает нейтральный индикатор и не показывает анонс до подключения", () => {
    render(<Announce title="Test announce" />);

    expect(document.body.textContent).toContain("SignalR: подключение…");
    expect(document.body.textContent).not.toContain("Test announce");
  });

  it("показывает зелёный индикатор и анонс после подключения хаба", async () => {
    const adapter = new FakeHubAdapter();

    render(<Announce title="Test announce" />);

    await act(async () => {
      await useOverlayHubStore.getState().start(adapter);
    });

    expect(document.body.textContent).toContain("SignalR подключён");
    expect(document.body.textContent).toContain("Test announce");
    expect(document.body.textContent).not.toContain("SignalR: подключение…");
  });

  it("показывает красный индикатор и не показывает анонс при ошибке", () => {
    useOverlayHubStore.setState({ status: "error", isConnected: false });

    render(<Announce title="Test announce" />);

    expect(document.body.textContent).toContain("SignalR не подключён");
    expect(document.body.textContent).not.toContain("Test announce");
  });

  it("исчезает после фейда и вызывает callback", async () => {
    const callback = vi.fn();
    const adapter = new FakeHubAdapter();

    await act(async () => {
      await useOverlayHubStore.getState().start(adapter);
    });

    render(<Announce title="Test announce" callback={callback} />);

    await waitFor(() => expect(callback).toHaveBeenCalled(), { timeout: 6000 });
  });

  it("возвращает null в Storybook-режиме", () => {
    vi.stubEnv("VITE_STATE", "book");

    render(<Announce title="Test announce" />);

    expect(document.body.textContent).not.toContain("Test announce");
    expect(document.body.textContent).not.toContain("SignalR: подключение…");

    vi.unstubAllEnvs();
  });
});


