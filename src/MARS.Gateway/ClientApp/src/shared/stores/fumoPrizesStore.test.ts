import { beforeEach, describe, expect, it } from "vitest";

import useFumoPrizesStore from "./fumoPrizesStore";

const createPrize = (id: string, text = "") => ({
  id,
  image: `https://example.com/${id}.png`,
  text,
});

describe("fumoPrizesStore", () => {
  beforeEach(() => {
    useFumoPrizesStore.setState({ prizes: [] });
  });

  it("shuffle preserves all items", () => {
    const prizes = [
      createPrize("1"),
      createPrize("2"),
      createPrize("3"),
      createPrize("4"),
      createPrize("5"),
    ];
    useFumoPrizesStore.setState({ prizes });

    useFumoPrizesStore.getState().shuffle();

    const shuffled = useFumoPrizesStore.getState().prizes;
    expect(shuffled).toHaveLength(5);
    const ids = shuffled.map(p => p.id).sort();
    expect(ids).toEqual(["1", "2", "3", "4", "5"]);
  });

  it("shuffle can reorder prizes", () => {
    const prizes = Array.from({ length: 20 }, (_, index) =>
      createPrize(String(index))
    );
    useFumoPrizesStore.setState({ prizes });

    // Shuffle multiple times — at least one should produce a different order
    let isFoundDifferent = false;
    const originalOrder = prizes.map(p => p.id);

    for (let index = 0; index < 10; index++) {
      useFumoPrizesStore.getState().shuffle();
      const currentOrder = useFumoPrizesStore.getState().prizes.map(p => p.id);
      if (currentOrder.some((id, index) => id !== originalOrder[index])) {
        isFoundDifferent = true;
        break;
      }
    }

    expect(isFoundDifferent).toBe(true);
  });

  it("findIndex returns correct position after shuffle", () => {
    const targetId = "target";
    const prizes = [
      createPrize("a"),
      createPrize("b"),
      createPrize(targetId),
      createPrize("d"),
      createPrize("e"),
    ];
    useFumoPrizesStore.setState({ prizes });

    // Shuffle and verify target is still findable
    for (let index = 0; index < 10; index++) {
      useFumoPrizesStore.getState().shuffle();
      const shuffled = useFumoPrizesStore.getState().prizes;
      const foundIndex = shuffled.findIndex(p => p.id === targetId);
      expect(foundIndex).toBeGreaterThanOrEqual(0);
      expect(shuffled[foundIndex].id).toBe(targetId);
    }
  });

  it("addPrizes deduplicates", () => {
    const prizes = [createPrize("1"), createPrize("2")];
    useFumoPrizesStore.setState({ prizes });

    useFumoPrizesStore
      .getState()
      .addPrizes([createPrize("2"), createPrize("3")]);

    const result = useFumoPrizesStore.getState().prizes;
    expect(result).toHaveLength(3);
    expect(result.map(p => p.id)).toEqual(["1", "2", "3"]);
  });

  it("clear empties the store", () => {
    useFumoPrizesStore.setState({
      prizes: [createPrize("1"), createPrize("2")],
    });

    useFumoPrizesStore.getState().clear();

    expect(useFumoPrizesStore.getState().prizes).toHaveLength(0);
  });

  it("addPrizes skips empty additions", () => {
    const prizes = [createPrize("1")];
    useFumoPrizesStore.setState({ prizes });

    useFumoPrizesStore.getState().addPrizes([]);

    expect(useFumoPrizesStore.getState().prizes).toHaveLength(1);
  });
});
