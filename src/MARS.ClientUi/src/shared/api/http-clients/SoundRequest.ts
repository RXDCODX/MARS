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

export class SoundRequest<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestStateList
   * @request GET:/api/SoundRequest/state
   * @response `200` `OperationResult<PlayerState>` OK
   */
  soundRequestStateList = (params: RequestParams = {}) =>
    this.request<OperationResult<PlayerState>, any>({
      path: `/api/SoundRequest/state`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestQueueList
   * @request GET:/api/SoundRequest/queue
   * @response `200` `OperationResult<QueueItem[]>` OK
   */
  soundRequestQueueList = (params: RequestParams = {}) =>
    this.request<OperationResult<QueueItem[]>, any>({
      path: `/api/SoundRequest/queue`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestHistoryList
   * @request GET:/api/SoundRequest/history
   * @response `200` `OperationResult<BaseTrackInfo[]>` OK
   */
  soundRequestHistoryList = (
    query?: {
      /**
       * @format int32
       * @default 20
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BaseTrackInfo[]>, any>({
      path: `/api/SoundRequest/history`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestHistoryQueueItemsList
   * @request GET:/api/SoundRequest/history/queue-items
   * @response `200` `OperationResult<QueueItem[]>` OK
   */
  soundRequestHistoryQueueItemsList = (
    query?: {
      /**
       * @format int32
       * @default 20
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<QueueItem[]>, any>({
      path: `/api/SoundRequest/history/queue-items`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestCurrentSongList
   * @request GET:/api/SoundRequest/current-song
   * @response `200` `OperationResult<String>` OK
   */
  soundRequestCurrentSongList = (params: RequestParams = {}) =>
    this.request<OperationResult<String>, any>({
      path: `/api/SoundRequest/current-song`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestQueueDelete
   * @request DELETE:/api/SoundRequest/queue/{queueItemId}
   * @response `200` `OperationResult` OK
   */
  soundRequestQueueDelete = (queueItemId: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/SoundRequest/queue/${queueItemId}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestAddTrackCreate
   * @request POST:/api/SoundRequest/add-track
   * @response `200` `OperationResult<String>` OK
   */
  soundRequestAddTrackCreate = (
    query?: {
      query: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String>, any>({
      path: `/api/SoundRequest/add-track`,
      method: "POST",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestPlayNowCreate
   * @request POST:/api/SoundRequest/play-now/{queueItemId}
   * @response `200` `OperationResult<String>` OK
   */
  soundRequestPlayNowCreate = (
    queueItemId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String>, any>({
      path: `/api/SoundRequest/play-now/${queueItemId}`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SoundRequest
   * @name SoundRequestQueueReorderCreate
   * @request POST:/api/SoundRequest/queue/reorder
   * @response `200` `OperationResult<String>` OK
   */
  soundRequestQueueReorderCreate = (
    data: QueueReorderRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String>, any>({
      path: `/api/SoundRequest/queue/reorder`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
