/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import type { AxiosRequestConfig, AxiosResponse } from "axios";
import {
  HttpClient,
  RequestParams,
  ContentType,
  HttpResponse,
} from "./http-client";
import type {
  AlertSettingsEntry,
  ApiMediaInfo,
  AutoMessageDto,
  BaseCommand,
  BaseTrackInfo,
  Boolean,
  BooruAutoPostConfigDto,
  BooruAutoPostCreateRequest,
  BooruAutoPostUpdateRequest,
  BooruSetEnabledRequest,
  ChannelRewardDefinition,
  ChannelRewardRecord,
  CinemaMediaItemDto,
  CinemaQueueStatistics,
  CommandParameterInfo,
  CreateAutoMessageRequest,
  CreateCustomRewardsRequest,
  CreateMediaItemRequest,
  CreateMemeOrderDto,
  CreateMemeTypeDto,
  CreateTwitchUserRequest,
  CreateWaifuRequest,
  CustomReward,
  DefaultImage,
  DiscordChannelOptionDto,
  EnvironmentVariable,
  FollowerInfo,
  GetCustomRewardRedemptionResponse,
  GetCustomRewardsResponse,
  GlobalCooldownSetting,
  HusbandDto,
  Image,
  Log,
  LogResponse,
  LogsStatistics,
  MaxPerStreamSetting,
  MaxPerUserPerStreamSetting,
  MediaDto,
  MediaFileInfo,
  MediaInfo,
  MediaMetaInfo,
  MediaMetadata,
  MediaPositionInfo,
  MediaStylesInfo,
  MediaTextInfo,
  MediaTypeStringArrayDictionary,
  MemeOrderDto,
  MemeTypeDto,
  OperationResult,
  Pagination,
  PlayerState,
  ProblemDetails,
  QueueItem,
  QueueReorderRequest,
  RateLimiterInfo,
  Reward,
  RewardRedemption,
  RootState,
  ServerStatsResponse,
  ServiceInfo,
  ServiceLog,
  SetEnvironmentVariableRequest,
  SpotifyAuthCompleteResult,
  SpotifyAuthStartRequest,
  SpotifyAuthStartResult,
  SpotifyAuthStatusResult,
  StoreFactRequest,
  StreamArchiveConfig,
  StringServiceStatusDictionary,
  TelegramChannelOptionDto,
  TelegramDiscordBindingCreateRequest,
  TelegramDiscordBindingDto,
  TelegramDiscordBindingSetEnabledRequest,
  TelegramDiscordChannelStateDto,
  TwitchUser,
  TwitchUserDto,
  UpdateAutoMessageRequest,
  UpdateCustomRewardDto,
  UpdateCustomRewardRedemptionStatusRequest,
  UpdateCustomRewardRequest,
  UpdateHusbandRequest,
  UpdateMediaItemRequest,
  UpdateMemeOrderDto,
  UpdateMemeTypeDto,
  UpdateTwitchUserRequest,
  UpdateValueRequest,
  UpdateWaifuRequest,
  ValidateFolderRequest,
  ValidateFolderResponse,
  VerificationCodeRequest,
  WaifuDto,
  WaifuRollAudioDto,
  BaseCommandAvailablePlatformsEnum,
  BaseCommandVisibilityEnum,
  BooruAutoPostConfigDtoSourceEnum,
  BooruAutoPostConfigDtoTargetPlatformEnum,
  BooruAutoPostConfigDtoTelegramParseModeEnum,
  BooruAutoPostCreateRequestSourceEnum,
  BooruAutoPostCreateRequestTargetPlatformEnum,
  BooruAutoPostCreateRequestTelegramParseModeEnum,
  BooruAutoPostUpdateRequestSourceEnum,
  BooruAutoPostUpdateRequestTargetPlatformEnum,
  BooruAutoPostUpdateRequestTelegramParseModeEnum,
  CinemaMediaItemDtoStatusEnum,
  LogLogLevelEnum,
  MediaFileInfoTypeEnum,
  MediaMetaInfoPriorityEnum,
  PlayerStateStateEnum,
  PlayerStateVideoStateEnum,
  RewardRedemptionStatusEnum,
  ServiceInfoStatusEnum,
  StreamArchiveConfigFileConvertTypeEnum,
  UpdateCustomRewardRedemptionStatusRequestStatusEnum,
  UpdateMediaItemRequestStatusEnum,
  CinemaQueueStatusDetailParamsEnum,
  CinemaQueueStatusDetailParamsStatusEnum,
  CommandsAdminPlatformDetailParamsEnum,
  CommandsAdminPlatformDetailParamsPlatformEnum,
  CommandsAdminPlatformInfoListParamsEnum,
  CommandsAdminPlatformInfoListParamsPlatformEnum,
  CommandsUserPlatformDetailParamsEnum,
  CommandsUserPlatformDetailParamsPlatformEnum,
  CommandsUserPlatformInfoListParamsEnum,
  CommandsUserPlatformInfoListParamsPlatformEnum,
  LogsByLevelDetailParamsEnum,
  LogsByLevelDetailParamsLogLevelEnum,
  LogsListParamsLogLevelEnum,
  ObsToggleCreateParamsModeEnum,
  TestAlertsAlertByTypeCreateParamsPriorityEnum,
  TestAlertsAlertByTypeCreateParamsTypeEnum,
} from "../types/data-contracts";

export class SpotifyAuth<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthStartCreate
   * @request POST:/api/SpotifyAuth/start
   * @response `200` `OperationResult<SpotifyAuthStartResult>` OK
   */
  spotifyAuthStartCreate = (
    data: SpotifyAuthStartRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<SpotifyAuthStartResult>, any>({
      path: `/api/SpotifyAuth/start`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthCallbackList
   * @request GET:/api/SpotifyAuth/callback
   * @response `200` `OperationResult<SpotifyAuthCompleteResult>` OK
   */
  spotifyAuthCallbackList = (
    query?: {
      code: string;
      state: string;
      error: string;
      error_description: string;
      redirectUri: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<SpotifyAuthCompleteResult>, any>({
      path: `/api/SpotifyAuth/callback`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthStatusList
   * @request GET:/api/SpotifyAuth/status
   * @response `200` `OperationResult<SpotifyAuthStatusResult>` OK
   */
  spotifyAuthStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<SpotifyAuthStatusResult>, any>({
      path: `/api/SpotifyAuth/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthDisconnectCreate
   * @request POST:/api/SpotifyAuth/disconnect
   * @response `200` `OperationResult` OK
   */
  spotifyAuthDisconnectCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/SpotifyAuth/disconnect`,
      method: "POST",
      format: "json",
      ...params,
    });
}
