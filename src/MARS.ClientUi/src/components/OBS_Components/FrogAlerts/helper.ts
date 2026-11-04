import { Frog, TwitchUser } from "@/shared/api";

export interface FrogAlertProps {
  frog: Frog;
  twitchUser: TwitchUser;
}

export function getFrogText(message: FrogAlertProps) {
  return `теперь ты ${message.frog.russianName ?? message.frog.commonName}`;
}
