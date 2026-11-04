import { beforeEach, describe, expect, it } from "vitest";

import useFrogPrizesStore from "./frogPrizesStore";

const createPrize = (id: string, text = "") => ({
  id,
  image: `https://example.com/${id}.png`,
  text,
});

describe("frogPrizesStore", () => {
  beforeEach(() => {
    useFrogPrizesStore.setState({ prizes: [] });
  });

  it("shuffle preserves all items", () => {
    const prizes = [
      createPrize("1"),
      createPrize("2"),
      createPrize("3"),
      createPrize("4"),
      createPrize("5"),
    ];
    useFrogPrizesStore.setState({ prizes });

    useFrogPrizesStore.getState().shuffle();

    const shuffled = useFrogPrizesStore.getState().prizes;
    expect(shuffled).toHaveLength(5);
    const ids = shuffled.map(p => p.id).sort();
    expect(ids).toEqual(["1", "2", "3", "4", "5"]);
  });

  it("shuffle can reorder prizes", () => {
    const prizes = Array.from({ length: 20 }, (_, index) =>
      createPrize(String(index))
    );
    useFrogPrizesStore.setState({ prizes });

    let isFoundDifferent = false;
    const originalOrder = prizes.map(p => p.id);

    for (let index = 0; index < 10; index++) {
      useFrogPrizesStore.getState().shuffle();
      const currentOrder = useFrogPrizesStore.getState().prizes.map(p => p.id);
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
    useFrogPrizesStore.setState({ prizes });

    for (let index = 0; index < 10; index++) {
      useFrogPrizesStore.getState().shuffle();
      const shuffled = useFrogPrizesStore.getState().prizes;
      const foundIndex = shuffled.findIndex(p => p.id === targetId);
      expect(foundIndex).toBeGreaterThanOrEqual(0);
      expect(shuffled[foundIndex].id).toBe(targetId);
    }
  });

  it("addPrizes deduplicates", () => {
    const prizes = [createPrize("1"), createPrize("2")];
    useFrogPrizesStore.setState({ prizes });

    useFrogPrizesStore
      .getState()
      .addPrizes([createPrize("2"), createPrize("3")]);

    const result = useFrogPrizesStore.getState().prizes;
    expect(result).toHaveLength(3);
    expect(result.map(p => p.id)).toEqual(["1", "2", "3"]);
  });

  it("clear empties the store", () => {
    useFrogPrizesStore.setState({
      prizes: [createPrize("1"), createPrize("2")],
    });

    useFrogPrizesStore.getState().clear();

    expect(useFrogPrizesStore.getState().prizes).toHaveLength(0);
  });
});
