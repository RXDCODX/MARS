import { MikuTrackDto } from "@/shared/api";

import type { RouletteGroup, RoulettePrize } from "../types";

function prizeImageKey(prize: RoulettePrize): string {
  return prize.image || String(prize.id);
}

function ensureNoAdjacentImages(
  prizes: RoulettePrize[],
  winnerId: string
): { prizes: RoulettePrize[]; winnerIndex: number } {
  const attemptsLimit = 80;

  for (let attempt = 0; attempt < attemptsLimit; attempt++) {
    const pool = [...prizes];
    const arranged: RoulettePrize[] = [];

    while (pool.length > 0) {
      const lastPrize = arranged.at(-1);
      const candidates = pool.filter(
        prize => !lastPrize || prizeImageKey(prize) !== prizeImageKey(lastPrize)
      );

      if (candidates.length === 0) {
        // dead end, restart arrangement
        arranged.length = 0;
        break;
      }

      const picked = candidates[Math.floor(Math.random() * candidates.length)];
      arranged.push(picked);
      pool.splice(pool.indexOf(picked), 1);
    }

    if (
      arranged.length === prizes.length &&
      prizeImageKey(arranged[0]) !== prizeImageKey(arranged.at(-1))
    ) {
      const winnerIndex = arranged.findIndex(prize => prize.id === winnerId);
      return {
        prizes: arranged,
        winnerIndex: Math.max(winnerIndex, 0),
      };
    }
  }

  const fallbackIndex = prizes.findIndex(prize => prize.id === winnerId);
  return {
    prizes,
    winnerIndex: Math.max(fallbackIndex, 0),
  };
}

function fillPrizesToTwenty(
  prizes: RoulettePrize[],
  groupId: number
): RoulettePrize[] {
  if (prizes.length >= 20) {
    return prizes;
  }

  const extendedPrizes = [...prizes];
  if (extendedPrizes.length === 0) {
    return extendedPrizes;
  }

  const nonPlaceholderPrizes = extendedPrizes.filter(
    prize => prize.isPlaceholder !== true
  );

  const sourcePrizes =
    nonPlaceholderPrizes.length > 0 ? nonPlaceholderPrizes : extendedPrizes;

  while (extendedPrizes.length < 20) {
    const randomIndex = Math.floor(Math.random() * sourcePrizes.length);
    const prize = sourcePrizes[randomIndex];
    if (prize) {
      extendedPrizes.push({
        ...prize,
        id: `${prize.id}-duplicate-${groupId}-${extendedPrizes.length}`,
        isPlaceholder: false,
      });
    }
  }

  return extendedPrizes;
}

export function divideTracksIntoGroups(
  availableTracks: MikuTrackDto[],
  selectedTrack: MikuTrackDto
): RouletteGroup[] {
  console.log("[divideTracksIntoGroups] Начало создания групп рулеток", {
    selectedTrackId: selectedTrack.id,
    selectedTrackNumber: selectedTrack.number,
    selectedTrackTitle: selectedTrack.title,
    availableTracksCount: availableTracks.length,
  });

  const tracksCount = availableTracks.length;
  const allTracks = [...availableTracks];

  const selectedTrackIndex = allTracks.findIndex(
    track => track.id === selectedTrack.id
  );
  if (selectedTrackIndex === -1) {
    allTracks.push(selectedTrack);
  } else {
    allTracks[selectedTrackIndex] = selectedTrack;
  }

  const sortedTracks = [...allTracks].sort(
    (first, second) => first.number - second.number
  );

  const selectedTrackId = selectedTrack.id;

  const allPrizes: RoulettePrize[] = sortedTracks.map(track => ({
    id: track.id,
    image: track.thumbnailUrl || "",
    text: `#${track.number}: ${track.artist} - ${track.title}`,
    isPlaceholder: false,
  }));

  let roulettesCount = 1;
  if (tracksCount >= 6) {
    roulettesCount = 3;
  } else if (tracksCount >= 4) {
    roulettesCount = 2;
  }

  if (roulettesCount === 1) {
    const { prizes: spacedPrizes, winnerIndex } = ensureNoAdjacentImages(
      allPrizes,
      selectedTrackId
    );
    return [
      {
        prizes: spacedPrizes,
        prizeIndex: winnerIndex,
        hasWinner: true,
        isReversed: false,
      },
    ];
  }

  const groups: RouletteGroup[] = [];
  const prizesPerGroup = Math.ceil(allPrizes.length / roulettesCount);

  const selectedIndex = allPrizes.findIndex(
    prize => prize.id === selectedTrackId
  );
  const winnerGroupIndex = Math.floor(selectedIndex / prizesPerGroup);

  for (let index = 0; index < roulettesCount; index++) {
    const startIndex = index * prizesPerGroup;
    const endIndex = Math.min(startIndex + prizesPerGroup, allPrizes.length);
    const groupPrizes = allPrizes.slice(startIndex, endIndex);

    const hasWinner = index === winnerGroupIndex;
    const localPrizeIndex = hasWinner
      ? selectedIndex - startIndex
      : groupPrizes.length > 0
        ? groupPrizes.length - 1
        : 0;

    const isReversed = index % 2 !== 0;
    const filledPrizes = fillPrizesToTwenty(groupPrizes, index);
    const { prizes: spacedPrizes, winnerIndex } = ensureNoAdjacentImages(
      filledPrizes,
      String(groupPrizes[localPrizeIndex]?.id ?? "")
    );

    groups.push({
      prizes: spacedPrizes,
      prizeIndex: hasWinner ? winnerIndex : localPrizeIndex,
      hasWinner,
      isReversed,
    });
  }

  // Перемешиваем группы, чтобы выигрышная не всегда была внизу
  if (groups.length > 1) {
    for (let index = groups.length - 1; index > 0; index--) {
      const index_ = Math.floor(Math.random() * (index + 1));
      [groups[index], groups[index_]] = [groups[index_], groups[index]];
    }
  }

  console.log(
    "[divideTracksIntoGroups] Готовые группы:",
    groups.map((g, index) => ({
      groupIndex: index,
      hasWinner: g.hasWinner,
      prizeIndex: g.prizeIndex,
      winningPrizeId: g.prizes[g.prizeIndex]?.id,
      winningPrizeText: g.prizes[g.prizeIndex]?.text,
      totalPrizes: g.prizes.length,
    }))
  );

  return groups;
}
