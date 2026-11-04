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

export class BooruAutoPost<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsList
   * @request GET:/api/BooruAutoPost/configs
   * @response `200` `OperationResult<BooruAutoPostConfigDto[]>` OK
   */
  booruAutoPostConfigsList = (params: RequestParams = {}) =>
    this.request<OperationResult<BooruAutoPostConfigDto[]>, any>({
      path: `/api/BooruAutoPost/configs`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsCreate
   * @request POST:/api/BooruAutoPost/configs
   * @response `200` `OperationResult<BooruAutoPostConfigDto>` OK
   */
  booruAutoPostConfigsCreate = (
    data: BooruAutoPostCreateRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BooruAutoPostConfigDto>, any>({
      path: `/api/BooruAutoPost/configs`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsUpdate
   * @request PUT:/api/BooruAutoPost/configs/{id}
   * @response `200` `OperationResult<BooruAutoPostConfigDto>` OK
   */
  booruAutoPostConfigsUpdate = (
    id: string,
    data: BooruAutoPostUpdateRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BooruAutoPostConfigDto>, any>({
      path: `/api/BooruAutoPost/configs/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsDelete
   * @request DELETE:/api/BooruAutoPost/configs/{id}
   * @response `200` `OperationResult` OK
   */
  booruAutoPostConfigsDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/BooruAutoPost/configs/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsEnabledUpdate
   * @request PUT:/api/BooruAutoPost/configs/{id}/enabled
   * @response `200` `OperationResult<BooruAutoPostConfigDto>` OK
   */
  booruAutoPostConfigsEnabledUpdate = (
    id: string,
    data: BooruSetEnabledRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BooruAutoPostConfigDto>, any>({
      path: `/api/BooruAutoPost/configs/${id}/enabled`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostConfigsTriggerCreate
   * @request POST:/api/BooruAutoPost/configs/{id}/trigger
   * @response `200` `OperationResult` OK
   */
  booruAutoPostConfigsTriggerCreate = (
    id: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/BooruAutoPost/configs/${id}/trigger`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostDiscordChannelsList
   * @request GET:/api/BooruAutoPost/discord-channels
   * @response `200` `OperationResult<DiscordChannelOptionDto[]>` OK
   */
  booruAutoPostDiscordChannelsList = (params: RequestParams = {}) =>
    this.request<OperationResult<DiscordChannelOptionDto[]>, any>({
      path: `/api/BooruAutoPost/discord-channels`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags BooruAutoPost
   * @name BooruAutoPostTelegramChannelsList
   * @request GET:/api/BooruAutoPost/telegram-channels
   * @response `200` `OperationResult<TelegramChannelOptionDto[]>` OK
   */
  booruAutoPostTelegramChannelsList = (params: RequestParams = {}) =>
    this.request<OperationResult<TelegramChannelOptionDto[]>, any>({
      path: `/api/BooruAutoPost/telegram-channels`,
      method: "GET",
      format: "json",
      ...params,
    });
}
