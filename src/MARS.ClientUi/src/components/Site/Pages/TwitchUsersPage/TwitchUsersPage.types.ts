import type { TwitchUserDto as TwitchUserDto } from "@/shared/api/types/data-contracts";

export type TwitchUserFormMode = "create" | "edit";

export interface TwitchUserViewModel {
  twitchId: string;
  userLogin: string;
  displayName: string;
  profileImageUrl: string | null;
  chatColor: string | null;
  isModerator: boolean;
  isVip: boolean;
  isBroadcaster: boolean;
  isInBlockList: boolean;
  aliasNickname: string | null;
  followedAt: string | null;
  lastUpdated: string;
  createdAt: string;
}

export interface TwitchUserFormValues {
  userLogin: string;
  displayName: string;
  profileImageUrl: string;
  chatColor: string;
  isModerator: boolean;
  isVip: boolean;
  isInBlockList: boolean;
  aliasNickname: string;
}

export const createTwitchUserViewModel = (
  dto: TwitchUserDto
): TwitchUserViewModel => ({
  twitchId: dto.twitchId,
  userLogin: dto.userLogin,
  displayName: dto.displayName,
  profileImageUrl: dto.profileImageUrl ?? null,
  chatColor: dto.chatColor ?? null,
  isModerator: dto.isModerator,
  isVip: dto.isVip,
  isBroadcaster: dto.isBroadcaster,
  isInBlockList: dto.isInBlockList,
  aliasNickname: dto.aliasNickname ?? null,
  followedAt: dto.followedAt ?? null,
  lastUpdated: dto.lastUpdated,
  createdAt: dto.createdAt,
});

export const defaultFormValues: TwitchUserFormValues = {
  userLogin: "",
  displayName: "",
  profileImageUrl: "",
  chatColor: "",
  isModerator: false,
  isVip: false,
  isInBlockList: false,
  aliasNickname: "",
};
