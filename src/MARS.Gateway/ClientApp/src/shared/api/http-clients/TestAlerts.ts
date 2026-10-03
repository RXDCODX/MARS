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

export class TestAlerts<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TestAlerts
   * @name TestAlertsAlertCreate
   * @request POST:/api/TestAlerts/alert
   * @response `200` `OperationResult` OK
   */
  testAlertsAlertCreate = (data: MediaDto, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/TestAlerts/alert`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TestAlerts
   * @name TestAlertsAlertsBatchCreate
   * @request POST:/api/TestAlerts/alerts-batch
   * @response `200` `OperationResult` OK
   */
  testAlertsAlertsBatchCreate = (
    data: MediaDto[],
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/TestAlerts/alerts-batch`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TestAlerts
   * @name TestAlertsAlertByTypeCreate
   * @request POST:/api/TestAlerts/alert-by-type
   * @response `200` `OperationResult<MediaDto>` OK
   */
  testAlertsAlertByTypeCreate = (
    query?: {
      type: TestAlertsAlertByTypeCreateParamsTypeEnum;
      /** @default "Normal" */
      priority: TestAlertsAlertByTypeCreateParamsPriorityEnum;
      /**
       * @format int32
       * @default 5
       */
      duration: number;
      text: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaDto>, any>({
      path: `/api/TestAlerts/alert-by-type`,
      method: "POST",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TestAlerts
   * @name TestAlertsSettingsList
   * @request GET:/api/TestAlerts/settings
   * @response `200` `OperationResult<AlertSettingsEntry[]>` OK
   */
  testAlertsSettingsList = (params: RequestParams = {}) =>
    this.request<OperationResult<AlertSettingsEntry[]>, any>({
      path: `/api/TestAlerts/settings`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TestAlerts
   * @name TestAlertsAvailableFilesList
   * @request GET:/api/TestAlerts/available-files
   * @response `200` `OperationResult<MediaTypeStringArrayDictionary>` OK
   */
  testAlertsAvailableFilesList = (params: RequestParams = {}) =>
    this.request<OperationResult<MediaTypeStringArrayDictionary>, any>({
      path: `/api/TestAlerts/available-files`,
      method: "GET",
      format: "json",
      ...params,
    });
}
