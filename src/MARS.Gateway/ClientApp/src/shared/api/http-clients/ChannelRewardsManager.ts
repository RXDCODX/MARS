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

export class ChannelRewardsManager<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerList
   * @request GET:/api/ChannelRewardsManager
   * @response `200` `OperationResult<ChannelRewardRecord[]>` OK
   */
  channelRewardsManagerList = (params: RequestParams = {}) =>
    this.request<OperationResult<ChannelRewardRecord[]>, any>({
      path: `/api/ChannelRewardsManager`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerLocalList
   * @request GET:/api/ChannelRewardsManager/local
   * @response `200` `OperationResult<ChannelRewardRecord[]>` OK
   */
  channelRewardsManagerLocalList = (params: RequestParams = {}) =>
    this.request<OperationResult<ChannelRewardRecord[]>, any>({
      path: `/api/ChannelRewardsManager/local`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerLocalCreate
   * @request POST:/api/ChannelRewardsManager/local
   * @response `200` `OperationResult<ChannelRewardRecord>` OK
   */
  channelRewardsManagerLocalCreate = (
    data: ChannelRewardDefinition,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ChannelRewardRecord>, any>({
      path: `/api/ChannelRewardsManager/local`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerLocalDetail
   * @request GET:/api/ChannelRewardsManager/local/{localId}
   * @response `200` `OperationResult<ChannelRewardRecord>` OK
   */
  channelRewardsManagerLocalDetail = (
    localId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ChannelRewardRecord>, any>({
      path: `/api/ChannelRewardsManager/local/${localId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerLocalUpdate
   * @request PUT:/api/ChannelRewardsManager/local/{localId}
   * @response `200` `OperationResult` OK
   */
  channelRewardsManagerLocalUpdate = (
    localId: string,
    data: UpdateCustomRewardDto,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ChannelRewardsManager/local/${localId}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerLocalDelete
   * @request DELETE:/api/ChannelRewardsManager/local/{localId}
   * @response `200` `OperationResult` OK
   */
  channelRewardsManagerLocalDelete = (
    localId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ChannelRewardsManager/local/${localId}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerDetail
   * @request GET:/api/ChannelRewardsManager/{rewardId}
   * @response `200` `OperationResult<ChannelRewardRecord>` OK
   */
  channelRewardsManagerDetail = (
    rewardId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ChannelRewardRecord>, any>({
      path: `/api/ChannelRewardsManager/${rewardId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerSyncCreate
   * @request POST:/api/ChannelRewardsManager/sync
   * @response `200` `OperationResult` OK
   */
  channelRewardsManagerSyncCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/ChannelRewardsManager/sync`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ChannelRewardsManager
   * @name ChannelRewardsManagerSyncServicesCreate
   * @request POST:/api/ChannelRewardsManager/sync-services
   * @response `200` `OperationResult<Int32>` OK
   */
  channelRewardsManagerSyncServicesCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<Int32>, any>({
      path: `/api/ChannelRewardsManager/sync-services`,
      method: "POST",
      format: "json",
      ...params,
    });
}
