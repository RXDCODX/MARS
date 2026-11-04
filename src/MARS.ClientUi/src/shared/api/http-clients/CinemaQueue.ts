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

export class CinemaQueue<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueList
   * @request GET:/api/CinemaQueue
   * @response `200` `OperationResult<CinemaMediaItemDto[]>` OK
   */
  cinemaQueueList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto[]>, any>({
      path: `/api/CinemaQueue`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueCreate
   * @request POST:/api/CinemaQueue
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueCreate = (
    data: CreateMediaItemRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueDetail
   * @request GET:/api/CinemaQueue/{id}
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueDetail = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueUpdate
   * @request PUT:/api/CinemaQueue/{id}
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueUpdate = (
    id: string,
    data: UpdateMediaItemRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueDelete
   * @request DELETE:/api/CinemaQueue/{id}
   * @response `200` `OperationResult` OK
   */
  cinemaQueueDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueNextList
   * @request GET:/api/CinemaQueue/next
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueNextList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/next`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatusDetail
   * @request GET:/api/CinemaQueue/status/{status}
   * @response `200` `OperationResult<CinemaMediaItemDto[]>` OK
   */
  cinemaQueueStatusDetail = (
    status: CinemaQueueStatusDetailParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CinemaMediaItemDto[]>, any>({
      path: `/api/CinemaQueue/status/${status}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueMarkAsNextCreate
   * @request POST:/api/CinemaQueue/{id}/mark-as-next
   * @response `200` `OperationResult` OK
   */
  cinemaQueueMarkAsNextCreate = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/mark-as-next`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatusPartialUpdate
   * @request PATCH:/api/CinemaQueue/{id}/status
   * @response `200` `OperationResult` OK
   */
  cinemaQueueStatusPartialUpdate = (
    id: string,
    data: "Pending" | "InProgress" | "Completed" | "Cancelled" | "Postponed",
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/status`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueuePriorityPartialUpdate
   * @request PATCH:/api/CinemaQueue/{id}/priority
   * @response `200` `OperationResult` OK
   */
  cinemaQueuePriorityPartialUpdate = (
    id: string,
    data: number,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/priority`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatisticsList
   * @request GET:/api/CinemaQueue/statistics
   * @response `200` `OperationResult<CinemaQueueStatistics>` OK
   */
  cinemaQueueStatisticsList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaQueueStatistics>, any>({
      path: `/api/CinemaQueue/statistics`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueMetadataList
   * @request GET:/api/CinemaQueue/metadata
   * @response `200` `OperationResult<MediaMetadata>` OK
   */
  cinemaQueueMetadataList = (
    query?: {
      url: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaMetadata>, any>({
      path: `/api/CinemaQueue/metadata`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
}
