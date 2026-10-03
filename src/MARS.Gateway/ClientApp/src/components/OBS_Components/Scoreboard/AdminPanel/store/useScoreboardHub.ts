import { useEffect } from "react";

import type { HubConnection } from "@/shared/realtime/hubConnection";
import { scoreboardConnection } from "@/shared/realtime/hubConnections";

import { useScoreboardStore } from "./scoreboardStore";

/** Событие, которым табло отдаёт своё состояние. */
const RECEIVE_STATE = "ReceiveState";

/**
 * Подключение табло к своему хабу.
 *
 * Соединение открывает компонент, а не тело стора. Раньше `start` стоял прямо в
 * `create(...)`, то есть выполнялся на импорте модуля, и это давало два отказа:
 *
 * - любой тест, задевающий стор, запускал настоящее согласование SignalR и
 *   падал по сетевому таймауту вместо проверки своего;
 * - страница, открытая без табло, всё равно поднимала сокет к хабу табло.
 *
 * Оверлей при этом не табло: ему хаб табло не нужен, а единственный его
 * потребитель — `Scoreboard.tsx`, который и вызывает этот хук.
 */
export function useScoreboardHub(
  connection: HubConnection = scoreboardConnection
): void {
  useEffect(() => {
    let disposed = false;

    // Стор шлёт команды тем же соединением, которым подписан: раньше он брал
    // глобальный синглтон напрямую, из-за чего проверить очередь команд можно
    // было только с настоящим SignalR.
    useScoreboardStore.getState()._setConnection(connection);

    void connection
      .start({
        [RECEIVE_STATE]: (payload: never) => {
          useScoreboardStore
            .getState()
            .handleReceiveState(
              payload as Parameters<
                ReturnType<
                  typeof useScoreboardStore.getState
                >["handleReceiveState"]
              >[0]
            );
        },
      })
      .then(() => {
        // Команды, накопленные пока канала не было, уходят сразу после
        // подключения, иначе правка панели до старта соединения потерялась бы.
        if (!disposed) {
          void useScoreboardStore.getState()._flushPendingServerCommands();
        }
      })
      .catch((error: unknown) => {
        if (!disposed) {
          console.error("Не удалось подключиться к хабу табло:", error);
        }
      });

    return () => {
      disposed = true;
    };
  }, [connection]);
}
