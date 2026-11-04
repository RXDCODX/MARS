import { useMemo } from "react";

import type { QueuedMikuMondayAlert, RouletteGroup } from "../types";
import { divideTracksIntoGroups } from "../utils/rouletteGroups";

export function useRouletteGroups(
  currentAlert: QueuedMikuMondayAlert | undefined
): RouletteGroup[] {
  return useMemo(() => {
    if (!currentAlert) {
      console.log("[useRouletteGroups] Нет текущего алерта");
      return [];
    }

    console.log("[useRouletteGroups] Создаем группы для алерта", {
      alertId: currentAlert.id,
      selectedTrackId: currentAlert.selectedTrack.id,
      availableTracksCount: currentAlert.availableTracks.length,
    });

    const groups = divideTracksIntoGroups(
      currentAlert.availableTracks,
      currentAlert.selectedTrack
    );

    console.log("[useRouletteGroups] Группы созданы", {
      groupsCount: groups.length,
      winnerGroupIndex: groups.findIndex(g => g.hasWinner),
    });

    return groups;
  }, [currentAlert]);
}
