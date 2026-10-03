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

export class Commands<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserList
   * @request GET:/api/Commands/user
   * @response `200` `OperationResult<String>` OK
   */
  commandsUserList = (params: RequestParams = {}) =>
    this.request<OperationResult<String>, any>({
      path: `/api/Commands/user`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminList
   * @request GET:/api/Commands/admin
   * @response `200` `OperationResult<String>` OK
   */
  commandsAdminList = (params: RequestParams = {}) =>
    this.request<OperationResult<String>, any>({
      path: `/api/Commands/admin`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserPlatformDetail
   * @request GET:/api/Commands/user/platform/{platform}
   * @response `200` `OperationResult<String[]>` OK
   */
  commandsUserPlatformDetail = (
    platform: CommandsUserPlatformDetailParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String[]>, any>({
      path: `/api/Commands/user/platform/${platform}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminPlatformDetail
   * @request GET:/api/Commands/admin/platform/{platform}
   * @response `200` `OperationResult<String[]>` OK
   */
  commandsAdminPlatformDetail = (
    platform: CommandsAdminPlatformDetailParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String[]>, any>({
      path: `/api/Commands/admin/platform/${platform}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserPlatformInfoList
   * @request GET:/api/Commands/user/platform/{platform}/info
   * @response `200` `OperationResult<BaseCommand[]>` OK
   */
  commandsUserPlatformInfoList = (
    platform: CommandsUserPlatformInfoListParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BaseCommand[]>, any>({
      path: `/api/Commands/user/platform/${platform}/info`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminPlatformInfoList
   * @request GET:/api/Commands/admin/platform/{platform}/info
   * @response `200` `OperationResult<BaseCommand[]>` OK
   */
  commandsAdminPlatformInfoList = (
    platform: CommandsAdminPlatformInfoListParamsEnum,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BaseCommand[]>, any>({
      path: `/api/Commands/admin/platform/${platform}/info`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsParametersList
   * @request GET:/api/Commands/{commandName}/parameters
   * @response `200` `OperationResult<CommandParameterInfo[]>` OK
   */
  commandsParametersList = (commandName: string, params: RequestParams = {}) =>
    this.request<OperationResult<CommandParameterInfo[]>, any>({
      path: `/api/Commands/${commandName}/parameters`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsExecuteCreate
   * @request POST:/api/Commands/{commandName}/execute
   * @response `200` `OperationResult<String>` OK
   */
  commandsExecuteCreate = (
    commandName: string,
    data: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<String>, any>({
      path: `/api/Commands/${commandName}/execute`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
