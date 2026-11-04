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

export class Obs<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Obs
   * @name ObsStatusList
   * @request GET:/api/Obs/status
   * @response `200` `void` OK
   */
  obsStatusList = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/status`,
      method: "GET",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsConnectCreate
   * @request POST:/api/Obs/connect
   * @response `200` `void` OK
   */
  obsConnectCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/connect`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsDisconnectCreate
   * @request POST:/api/Obs/disconnect
   * @response `200` `void` OK
   */
  obsDisconnectCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/disconnect`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsScreenshotCreate
   * @request POST:/api/Obs/screenshot
   * @response `200` `void` OK
   */
  obsScreenshotCreate = (
    query?: {
      sourceName: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, any>({
      path: `/api/Obs/screenshot`,
      method: "POST",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsFreezeCreate
   * @request POST:/api/Obs/freeze
   * @response `200` `void` OK
   */
  obsFreezeCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/freeze`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsUnfreezeCreate
   * @request POST:/api/Obs/unfreeze
   * @response `200` `void` OK
   */
  obsUnfreezeCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/unfreeze`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsPauseSceneCreate
   * @request POST:/api/Obs/pause-scene
   * @response `200` `void` OK
   */
  obsPauseSceneCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/pause-scene`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsUnpauseSceneCreate
   * @request POST:/api/Obs/unpause-scene
   * @response `200` `void` OK
   */
  obsUnpauseSceneCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/unpause-scene`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsToggleCreate
   * @request POST:/api/Obs/toggle
   * @response `200` `void` OK
   */
  obsToggleCreate = (
    query?: {
      /** @default "FreezeFrame" */
      mode: ObsToggleCreateParamsModeEnum;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, any>({
      path: `/api/Obs/toggle`,
      method: "POST",
      query: query,
      ...params,
    });
}
