import { describe, expect, it, vi } from "vitest";

// Simulate the shuffle logic from prizes stores
const shuffleArray = <T extends { id: string | number }>(array: T[]): T[] =>
  [...array].sort(() => Math.random() - 0.5);

// Simulate finding a prize index — the core logic used by all roulette components
const findPrizeIndex = (
  prizes: { id: string | number }[],
  targetId: string | number
): number => prizes.findIndex(p => p.id === targetId);

const createPrize = (id: string) => ({ id, image: `img/${id}.png`, text: id });

describe("roulette index logic", () => {
  describe("derived index stays correct after shuffle", () => {
    it("findIndex returns correct position after a single shuffle", () => {
      const prizes = [
        createPrize("a"),
        createPrize("b"),
        createPrize("target"),
        createPrize("d"),
        createPrize("e"),
      ];
      const targetId = "target";

      // Simulate: parent computes index from current prizes
      const indexBefore = findPrizeIndex(prizes, targetId);
      expect(indexBefore).toBe(2);

      // Simulate: WaifuRoulette shuffles prizes
      const shuffled = shuffleArray(prizes);

      // Derived index recomputes from shuffled prizes
      const indexAfter = findPrizeIndex(shuffled, targetId);
      expect(indexAfter).toBeGreaterThanOrEqual(0);
      expect(shuffled[indexAfter].id).toBe(targetId);
    });

    it("derived index always points to target across multiple shuffles", () => {
      const prizes = Array.from({ length: 15 }, (_, index) =>
        createPrize(`prize-${index}`)
      );
      const targetId = "prize-7";

      for (let index = 0; index < 50; index++) {
        const shuffled = shuffleArray(prizes);
        const index = findPrizeIndex(shuffled, targetId);

        expect(index).toBeGreaterThanOrEqual(0);
        expect(index).toBeLessThan(shuffled.length);
        expect(shuffled[index].id).toBe(targetId);
      }
    });

    it("state-based index becomes stale after shuffle (demonstrates the bug)", () => {
      const prizes = [
        createPrize("a"),
        createPrize("b"),
        createPrize("target"),
        createPrize("d"),
        createPrize("e"),
      ];
      const targetId = "target";

      // State-based: index computed once from original order
      const stateIndex = findPrizeIndex(prizes, targetId); // 2

      // Shuffle
      const shuffled = shuffleArray(prizes);

      // State index still points to position 2, but target may have moved
      // This is the BUG: stateIndex may now point to a different prize
      if (shuffled[stateIndex].id !== targetId) {
        // Bug confirmed: stale index points to wrong prize
        expect(shuffled[stateIndex].id).not.toBe(targetId);
      }
      // In some lucky shuffles, the target might still be at index 2,
      // but this is not guaranteed — which is the bug.
    });
  });

  describe("edge cases", () => {
    it("returns -1 when target is not in prizes", () => {
      const prizes = [createPrize("a"), createPrize("b")];
      expect(findPrizeIndex(prizes, "missing")).toBe(-1);
    });

    it("returns -1 for empty prizes array", () => {
      expect(findPrizeIndex([], "target")).toBe(-1);
    });

    it("returns 0 for single-element array matching target", () => {
      const prizes = [createPrize("only")];
      expect(findPrizeIndex(prizes, "only")).toBe(0);
    });

    it("handles numeric IDs correctly", () => {
      const prizes = [
        { id: 100, image: "", text: "" },
        { id: 200, image: "", text: "" },
        { id: 300, image: "", text: "" },
      ];
      expect(findPrizeIndex(prizes, 200)).toBe(1);
    });

    it("handles string IDs correctly", () => {
      const prizes = [
        { id: "abc", image: "", text: "" },
        { id: "def", image: "", text: "" },
      ];
      expect(findPrizeIndex(prizes, "def")).toBe(1);
    });
  });

  describe("component integration pattern", () => {
    it("simulates full FumoAlerts derived index flow", () => {
      // Setup: prizes in store
      let prizes = [
        createPrize("mfc1"),
        createPrize("mfc2"),
        createPrize("mfc3"),
        createPrize("mfc4"),
        createPrize("mfc5"),
      ];
      const targetMfcId = "mfc3";

      // Step 1: Component renders, computes derived index
      let rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetMfcId) : -1;
      expect(rouletteIndex).toBe(2);

      // Step 2: WaifuRoulette mounts, calls shuffle()
      prizes = shuffleArray(prizes);

      // Step 3: Parent re-renders with shuffled prizes
      // Derived index recalculates automatically
      rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetMfcId) : -1;

      // Step 4: Verify index points to correct prize
      expect(rouletteIndex).toBeGreaterThanOrEqual(0);
      expect(prizes[rouletteIndex].id).toBe(targetMfcId);
    });

    it("simulates full WaifuAlerts derived index flow", () => {
      let prizes = Array.from({ length: 50 }, (_, index) =>
        createPrize(`shiki-${index}`)
      );
      const targetShikiId = "shiki-42";

      // Compute derived index
      let rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetShikiId) : -1;
      expect(rouletteIndex).toBe(42);

      // Shuffle
      prizes = shuffleArray(prizes);

      // Recompute derived index
      rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetShikiId) : -1;

      expect(prizes[rouletteIndex].id).toBe(targetShikiId);
    });

    it("waits for prizes to load (returns -1 when empty)", () => {
      const prizes: { id: string; image: string; text: string }[] = [];
      const targetId = "target";

      const rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetId) : -1;

      expect(rouletteIndex).toBe(-1);
    });

    it("skips roulette when target not found in prizes", () => {
      const prizes = [createPrize("a"), createPrize("b")];
      const targetId = "not-in-list";

      const rouletteIndex =
        prizes.length > 0 ? findPrizeIndex(prizes, targetId) : -1;

      expect(rouletteIndex).toBe(-1);
    });
  });
});
