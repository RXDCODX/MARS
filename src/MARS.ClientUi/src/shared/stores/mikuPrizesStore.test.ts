import { beforeEach, describe, expect, it } from "vitest";

import useMikuPrizesStore from "./mikuPrizesStore";

const createPrize = (id: string, text = "") => ({
  id,
  image: `https://example.com/${id}.png`,
  text,
});

describe("mikuPrizesStore", () => {
  beforeEach(() => {
    useMikuPrizesStore.setState({ prizes: [] });
  });

  it("shuffle preserves all items", () => {
    const prizes = [
      createPrize("1"),
      createPrize("2"),
      createPrize("3"),
      createPrize("4"),
      createPrize("5"),
    ];
    useMikuPrizesStore.setState({ prizes });

    useMikuPrizesStore.getState().shuffle();

    const shuffled = useMikuPrizesStore.getState().prizes;
    expect(shuffled).toHaveLength(5);
    const ids = shuffled.map(p => p.id).sort();
    expect(ids).toEqual(["1", "2", "3", "4", "5"]);
  });

  it("shuffle can reorder prizes", () => {
    const prizes = Array.from({ length: 20 }, (_, index) =>
      createPrize(String(index))
    );
    useMikuPrizesStore.setState({ prizes });

    let isFoundDifferent = false;
    const originalOrder = prizes.map(p => p.id);

    for (let index = 0; index < 10; index++) {
      useMikuPrizesStore.getState().shuffle();
      const currentOrder = useMikuPrizesStore.getState().prizes.map(p => p.id);
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
    useMikuPrizesStore.setState({ prizes });

    for (let index = 0; index < 10; index++) {
      useMikuPrizesStore.getState().shuffle();
      const shuffled = useMikuPrizesStore.getState().prizes;
      const foundIndex = shuffled.findIndex(p => p.id === targetId);
      expect(foundIndex).toBeGreaterThanOrEqual(0);
      expect(shuffled[foundIndex].id).toBe(targetId);
    }
  });

  it("addPrizes deduplicates", () => {
    const prizes = [createPrize("1"), createPrize("2")];
    useMikuPrizesStore.setState({ prizes });

    useMikuPrizesStore
      .getState()
      .addPrizes([createPrize("2"), createPrize("3")]);

    const result = useMikuPrizesStore.getState().prizes;
    expect(result).toHaveLength(3);
    expect(result.map(p => p.id)).toEqual(["1", "2", "3"]);
  });

  it("clear empties the store", () => {
    useMikuPrizesStore.setState({
      prizes: [createPrize("1"), createPrize("2")],
    });

    useMikuPrizesStore.getState().clear();

    expect(useMikuPrizesStore.getState().prizes).toHaveLength(0);
  });
});
