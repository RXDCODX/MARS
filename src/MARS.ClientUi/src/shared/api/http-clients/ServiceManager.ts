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

export class ServiceManager<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerStatusList
   * @request GET:/api/ServiceManager/status
   * @response `200` `OperationResult<StringServiceStatusDictionary>` OK
   */
  serviceManagerStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<StringServiceStatusDictionary>, any>({
      path: `/api/ServiceManager/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceDetail
   * @request GET:/api/ServiceManager/service/{serviceName}
   * @response `200` `OperationResult<ServiceInfo>` OK
   */
  serviceManagerServiceDetail = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ServiceInfo>, any>({
      path: `/api/ServiceManager/service/${serviceName}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceStartCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/start
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceStartCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/start`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceStopCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/stop
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceStopCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/stop`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceRestartCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/restart
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceRestartCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/restart`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceLogsList
   * @request GET:/api/ServiceManager/service/{serviceName}/logs
   * @response `200` `OperationResult<ServiceLog[]>` OK
   */
  serviceManagerServiceLogsList = (
    serviceName: string,
    query?: {
      /**
       * @format int32
       * @default 100
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ServiceLog[]>, any>({
      path: `/api/ServiceManager/service/${serviceName}/logs`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceActiveCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/active
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceActiveCreate = (
    serviceName: string,
    data: boolean,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/active`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServicesList
   * @request GET:/api/ServiceManager/services
   * @response `200` `OperationResult<ServiceInfo[]>` OK
   */
  serviceManagerServicesList = (params: RequestParams = {}) =>
    this.request<OperationResult<ServiceInfo[]>, any>({
      path: `/api/ServiceManager/services`,
      method: "GET",
      format: "json",
      ...params,
    });
}
