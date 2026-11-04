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

export class StreamArchive<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveList
   * @request GET:/api/StreamArchive
   * @response `200` `OperationResult<StreamArchiveConfig[]>` OK
   */
  streamArchiveList = (params: RequestParams = {}) =>
    this.request<OperationResult<StreamArchiveConfig[]>, any>({
      path: `/api/StreamArchive`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveCreate
   * @request POST:/api/StreamArchive
   * @response `200` `OperationResult<StreamArchiveConfig>` OK
   */
  streamArchiveCreate = (
    data: StreamArchiveConfig,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<StreamArchiveConfig>, any>({
      path: `/api/StreamArchive`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveDetail
   * @request GET:/api/StreamArchive/{id}
   * @response `200` `OperationResult<StreamArchiveConfig>` OK
   */
  streamArchiveDetail = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult<StreamArchiveConfig>, any>({
      path: `/api/StreamArchive/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveUpdate
   * @request PUT:/api/StreamArchive/{id}
   * @response `200` `OperationResult` OK
   */
  streamArchiveUpdate = (
    id: string,
    data: StreamArchiveConfig,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/StreamArchive/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveDelete
   * @request DELETE:/api/StreamArchive/{id}
   * @response `200` `OperationResult` OK
   */
  streamArchiveDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/StreamArchive/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveValidateFolderCreate
   * @request POST:/api/StreamArchive/validate-folder
   * @response `200` `OperationResult<ValidateFolderResponse>` OK
   */
  streamArchiveValidateFolderCreate = (
    data: ValidateFolderRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ValidateFolderResponse>, any>({
      path: `/api/StreamArchive/validate-folder`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveFilesList
   * @request GET:/api/StreamArchive/{configId}/files
   * @response `200` `OperationResult<Object>` OK
   */
  streamArchiveFilesList = (configId: string, params: RequestParams = {}) =>
    this.request<OperationResult<Object>, any>({
      path: `/api/StreamArchive/${configId}/files`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveStatisticsList
   * @request GET:/api/StreamArchive/statistics
   * @response `200` `OperationResult<Object>` OK
   */
  streamArchiveStatisticsList = (params: RequestParams = {}) =>
    this.request<OperationResult<Object>, any>({
      path: `/api/StreamArchive/statistics`,
      method: "GET",
      format: "json",
      ...params,
    });
}
