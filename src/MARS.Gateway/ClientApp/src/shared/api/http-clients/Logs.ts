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

export class Logs<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Logs
   * @name LogsList
   * @request GET:/api/Logs
   * @response `200` `OperationResult<LogResponse>` OK
   */
  logsList = (
    query?: {
      /**
       * @format int32
       * @default 1
       */
      page: number;
      /**
       * @format int32
       * @default 50
       */
      pageSize: number;
      /** @default "whenlogged" */
      sortBy: string;
      /** @default true */
      sortDescending: boolean;
      logLevel: LogsListParamsLogLevelEnum;
      /** @format date-time */
      fromDate: string;
      /** @format date-time */
      toDate: string;
      searchText: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<LogResponse>, any>({
      path: `/api/Logs`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Logs
   * @name LogsByLevelDetail
   * @request GET:/api/Logs/by-level/{logLevel}
   * @response `200` `OperationResult<Log[]>` OK
   */
  logsByLevelDetail = (
    logLevel: LogsByLevelDetailParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<Log[]>, any>({
      path: `/api/Logs/by-level/${logLevel}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Logs
   * @name LogsByDateRangeList
   * @request GET:/api/Logs/by-date-range
   * @response `200` `OperationResult<Log[]>` OK
   */
  logsByDateRangeList = (
    query?: {
      /** @format date-time */
      fromDate: string;
      /** @format date-time */
      toDate: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<Log[]>, any>({
      path: `/api/Logs/by-date-range`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Logs
   * @name LogsRecentList
   * @request GET:/api/Logs/recent
   * @response `200` `OperationResult<Log[]>` OK
   */
  logsRecentList = (
    query?: {
      /**
       * @format int32
       * @default 100
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<Log[]>, any>({
      path: `/api/Logs/recent`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Logs
   * @name LogsStatisticsList
   * @request GET:/api/Logs/statistics
   * @response `200` `OperationResult<LogsStatistics>` OK
   */
  logsStatisticsList = (params: RequestParams = {}) =>
    this.request<OperationResult<LogsStatistics>, any>({
      path: `/api/Logs/statistics`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Logs
   * @name LogsTestCreate
   * @request POST:/api/Logs/test
   * @response `200` `OperationResult<Object>` OK
   */
  logsTestCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<Object>, any>({
      path: `/api/Logs/test`,
      method: "POST",
      format: "json",
      ...params,
    });
}
