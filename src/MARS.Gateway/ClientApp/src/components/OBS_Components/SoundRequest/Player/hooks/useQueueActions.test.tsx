import { vi } from "vitest";

// Мокаем SoundRequest API и конфиг до загрузки любых модулей
vi.mock("@/shared/api", () => ({
  SoundRequest: function () {
    return {
      soundRequestQueueReorderCreate: async (_: any) => ({
        data: { success: true },
      }),
    };
  },
}));

vi.mock("@/shared/api/api-config", () => ({
  defaultApiConfig: { baseURL: "" },
}));

import { fireEvent, render, waitFor } from "@testing-library/react";
import React from "react";
import { describe, expect, it } from "vitest";

import { usePlayerStore } from "../stores/usePlayerStore";
import { useQueueActions } from "./useQueueActions";

function TestComponent() {
  const { handleMoveUp, handleMoveDown } = useQueueActions();

  React.useEffect(() => {
    usePlayerStore.getState().setQueue([
      { id: "a", track: { id: "t1", title: "t1" } },
      { id: "b", track: { id: "t2", title: "t2" } },
      { id: "c", track: { id: "t3", title: "t3" } },
    ] as any);
  }, []);

  return (
    <div>
      <button data-testid="up-b" onClick={() => handleMoveUp("b")}>
        up-b
      </button>
      <button data-testid="down-b" onClick={() => handleMoveDown("b")}>
        down-b
      </button>
    </div>
  );
}

describe("useQueueActions reorder", () => {
  it("moves item up and down optimistically", async () => {
    const r = render(<TestComponent />);

    // initial order
    expect(usePlayerStore.getState().queue.map(q => q.id)).toEqual([
      "a",
      "b",
      "c",
    ]);

    fireEvent.click(r.getByTestId("up-b"));

    await waitFor(() =>
      expect(usePlayerStore.getState().queue.map(q => q.id)).toEqual([
        "b",
        "a",
        "c",
      ])
    );

    fireEvent.click(r.getByTestId("down-b"));

    await waitFor(() =>
      expect(usePlayerStore.getState().queue.map(q => q.id)).toEqual([
        "a",
        "b",
        "c",
      ])
    );
  });
});
