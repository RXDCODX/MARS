import type { MikuTrackDto } from "@/shared/api";
import type { AdhdLayoutConfigDto } from "@/shared/api/types/data-contracts";

/**
 * Команды, которые клиент оверлея действительно отправляет в AlertsHub.
 *
 * Имена и аргументы соответствуют методам
 * `MARS.Alerts.Presentation.Overlay.Hubs.AlertsHub`.
 * Строковые и несуществующие методы сюда не попадают: иначе мы бы снова
 * скрыли расхождение между клиентом и сервером за предупреждением в консоли.
 */
export type OverlayHubCommands = {
  MuteAll: {
    args: [excludedProcessNames?: string[]];
    result: void;
  };
  UnmuteSessions: {
    args: [];
    result: void;
  };
  TwitchMsg: {
    args: [message: string];
    result: void;
  };
  MikuMondayTracks: {
    args: [];
    result: MikuTrackDto[];
  };
  DecrementAvailableMikuTrack: {
    args: [];
    result: void;
  };
  UpdateAdhdConfig: {
    args: [config: AdhdLayoutConfigDto];
    result: boolean;
  };
};

export type OverlayHubCommandName = keyof OverlayHubCommands;

export type OverlayHubCommandArgs<Name extends OverlayHubCommandName> =
  OverlayHubCommands[Name]["args"];

export type OverlayHubCommandResult<Name extends OverlayHubCommandName> =
  OverlayHubCommands[Name]["result"];