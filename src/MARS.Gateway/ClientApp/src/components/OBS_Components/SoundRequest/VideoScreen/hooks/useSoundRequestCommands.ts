import { useEffect, useState } from "react";

import {
  Commands,
  CommandsUserPlatformInfoListParamsEnum,
  getApiBaseUrl,
} from "@/shared/api";

interface SoundRequestCommandInfo {
  command: string;
  description: string;
}

const SOUND_REQUEST_COMMAND_NAMES = new Set(["sr", "srwrong", "song", "queue"]);

export function useSoundRequestCommands(): SoundRequestCommandInfo[] {
  const [commands, setCommands] = useState<SoundRequestCommandInfo[]>([]);

  useEffect(() => {
    let isCancelled = false;
    const api = new Commands({ baseURL: getApiBaseUrl() });

    api
      .commandsUserPlatformInfoList(CommandsUserPlatformInfoListParamsEnum.Api)
      .then(response => {
        if (isCancelled) {
          return;
        }

        const allCommands = response.data?.data ?? [];
        const filtered = allCommands
          .filter(command =>
            SOUND_REQUEST_COMMAND_NAMES.has(command.commandName)
          )
          .map(command => ({
            command: `!${command.commandName}`,
            description: command.description,
          }));

        setCommands(filtered);
      })
      .catch(() => {
        if (!isCancelled) {
          setCommands([]);
        }
      });

    return () => {
      isCancelled = true;
    };
  }, []);

  return commands;
}
