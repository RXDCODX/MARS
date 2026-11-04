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

/**
 * Обобщенный результат операции
 * @template TData - Тип данных, возвращаемых в поле data
 */
export interface OperationResult<TData = any> {
  /** Флаг успешности операции */
  success: boolean;
  /** Сообщение о результате операции */
  message?: string;
  /** Данные результата операции */
  data?: TData;
}

export interface AlertSettingsEntry {
  /** @format int32 */
  twitchPointsCost: number;
  filePath: string;
  /** @format int32 */
  duration: number;
  randomCoordinates: boolean;
  /** @format int32 */
  xCoordinate: number;
  /** @format int32 */
  yCoordinate: number;
  /** @format int32 */
  type: number;
  textPosition: string;
  text: string;
  textColor: string;
  keyWordsColor: string;
  vip: boolean;
  isBorder: boolean;
  isProportion: boolean;
  isResizeRequires: boolean;
  /** @format int32 */
  width: number;
  /** @format int32 */
  height: number;
  isRotated: boolean;
  /** @format int32 */
  rotation: number;
  isLooped: boolean;
}

export interface ApiMediaInfo {
  /** @format uuid */
  id: string;
  textInfo: MediaTextInfo;
  fileInfo: MediaFileInfo;
  positionInfo: MediaPositionInfo;
  metaInfo: MediaMetaInfo;
  stylesInfo: MediaStylesInfo;
}

export interface AutoMessageDto {
  /** @format uuid */
  id: string;
  message: string;
}

export interface BaseCommand {
  commandName: string;
  description: string;
  isAdminCommand: boolean;
  availablePlatforms: BaseCommandAvailablePlatformsEnum[];
  aliases: string[];
  parameters: CommandParameterInfo[];
  visibility: BaseCommandVisibilityEnum;
  supportsInline: boolean;
  supportsMediaInline: boolean;
  inlinePreviewUrl?: string;
  inlineTitle: string;
  inlineDescription: string;
}

export interface BaseTrackInfo {
  /** @format uuid */
  id: string;
  /** @maxLength 300 */
  trackName: string;
  authors?: string[];
  /** @format date-span */
  duration: string;
  /** @format uri */
  url: string;
  /** @format date-time */
  lastTimePlays: string;
  /** @format uri */
  artworkUrl?: string;
  videoId?: string;
  isDeleted: boolean;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  updatedAt: string;
  title: string;
}

export interface BooruAutoPostConfigDto {
  /** @format uuid */
  id: string;
  source: BooruAutoPostConfigDtoSourceEnum;
  targetPlatform: BooruAutoPostConfigDtoTargetPlatformEnum;
  discordChannelId: string;
  telegramChannelId?: string;
  /** @format int32 */
  targetPostCount: number;
  /** @format int32 */
  specificPostId?: number;
  tags: string;
  cronExpression: string;
  /** @format int32 */
  planningHorizonDays: number;
  /** @format int32 */
  pendingPostsCount: number;
  /** @format date-time */
  nextScheduledAtUtc?: string;
  message: string;
  telegramParseMode: BooruAutoPostConfigDtoTelegramParseModeEnum;
  isEnabled: boolean;
  /** @format date-time */
  lastExecutedAtUtc?: string;
  /** @format date-time */
  createdAtUtc: string;
  /** @format date-time */
  updatedAtUtc: string;
}

export interface BooruAutoPostCreateRequest {
  discordChannelId: string;
  tags: string;
  cronExpression: string;
  message: string;
  source: BooruAutoPostCreateRequestSourceEnum;
  targetPlatform: BooruAutoPostCreateRequestTargetPlatformEnum;
  telegramChannelId: string;
  /** @format int32 */
  planningHorizonDays: number;
  /** @format int32 */
  targetPostCount: number;
  /** @format int32 */
  specificPostId?: number;
  telegramParseMode: BooruAutoPostCreateRequestTelegramParseModeEnum;
}

export interface BooruAutoPostUpdateRequest {
  discordChannelId: string;
  tags: string;
  cronExpression: string;
  message: string;
  /** @format uuid */
  id: string;
  source: BooruAutoPostUpdateRequestSourceEnum;
  targetPlatform: BooruAutoPostUpdateRequestTargetPlatformEnum;
  telegramChannelId: string;
  /** @format int32 */
  planningHorizonDays: number;
  /** @format int32 */
  targetPostCount: number;
  /** @format int32 */
  specificPostId?: number;
  telegramParseMode: BooruAutoPostUpdateRequestTelegramParseModeEnum;
}

export interface BooruSetEnabledRequest {
  isEnabled: boolean;
}

export interface ChannelRewardDefinition {
  title: string;
  /** @format int32 */
  cost: number;
  isEnabled: boolean;
  prompt?: string;
  backgroundColor?: string;
  isUserInputRequired: boolean;
  isMaxPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled: boolean;
  /** @format int32 */
  globalCooldownSeconds?: number;
  shouldRedemptionsSkipRequestQueue: boolean;
}

export interface ChannelRewardRecord {
  /** @format uuid */
  id: string;
  /** @minLength 1 */
  title: string;
  /** @format int32 */
  cost: number;
  isEnabled: boolean;
  prompt?: string;
  backgroundColor?: string;
  isUserInputRequired: boolean;
  isMaxPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled: boolean;
  /** @format int32 */
  globalCooldownSeconds?: number;
  shouldRedemptionsSkipRequestQueue: boolean;
  isDeleted: boolean;
  twitchRewardId?: string;
  /** @format uuid */
  mediaInfoId?: string;
}

export interface CinemaMediaItemDto {
  /** @format uuid */
  id: string;
  title?: string;
  description?: string;
  mediaUrl: string;
  status: CinemaMediaItemDtoStatusEnum;
  /** @format int32 */
  priority: number;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  scheduledFor?: string;
  addedBy?: string;
  twitchUserId?: string;
  twitchUsername?: string;
  notes?: string;
  isNext: boolean;
  /** @format date-time */
  lastModified?: string;
}

export interface CinemaQueueStatistics {
  /** @format int32 */
  totalItems: number;
  /** @format int32 */
  pendingItems: number;
  /** @format int32 */
  inProgressItems: number;
  /** @format int32 */
  completedItems: number;
  /** @format int32 */
  cancelledItems: number;
  /** @format int32 */
  postponedItems: number;
}

export interface CommandParameterInfo {
  name: string;
  description: string;
  type: string;
  required: boolean;
  defaultValue?: string;
}

export interface CreateAutoMessageRequest {
  message: string;
}

export interface CreateCustomRewardsRequest {
  title?: string;
  /** @format int32 */
  cost: number;
  prompt?: string;
  isEnabled: boolean;
  backgroundColor?: string;
  isUserInputRequired: boolean;
  isMaxPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled: boolean;
  /** @format int32 */
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled: boolean;
  /** @format int32 */
  globalCooldownSeconds?: number;
  shouldRedemptionsSkipRequestQueue: boolean;
}

export interface CreateMediaItemRequest {
  title?: string;
  description?: string;
  mediaUrl: string;
  /** @format int32 */
  priority: number;
  /** @format date-time */
  scheduledFor?: string;
  addedBy?: string;
  twitchUserId?: string;
  twitchUsername?: string;
  notes?: string;
}

export interface CreateMemeOrderDto {
  /** @minLength 1 */
  filePath: string;
  /** @format int32 */
  memeTypeId?: number;
}

export interface CreateMemeTypeDto {
  /**
   * @minLength 1
   * @maxLength 50
   */
  name: string;
  /** @minLength 1 */
  folderPath: string;
}

export interface CreateTwitchUserRequest {
  twitchId: string;
  userLogin: string;
  displayName: string;
  profileImageUrl?: string;
  chatColor?: string;
  isModerator: boolean;
  isVip: boolean;
  isInBlockList: boolean;
  aliasNickname?: string;
}

export interface CreateWaifuRequest {
  /**
   * @minLength 1
   * @maxLength 20
   */
  shikiId: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  name: string;
  /** @format int64 */
  age: number;
  anime?: string;
  manga?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  imageUrl: string;
  /** @format uuid */
  audioId?: string;
}

export interface CustomReward {
  broadcasterId?: string;
  broadcasterLogin?: string;
  broadcasterName?: string;
  id?: string;
  title?: string;
  prompt?: string;
  /** @format int32 */
  cost: number;
  image?: Image;
  defaultImage?: DefaultImage;
  backgroundColor?: string;
  isEnabled: boolean;
  isUserInputRequired: boolean;
  maxPerStreamSetting?: MaxPerStreamSetting;
  maxPerUserPerStreamSetting?: MaxPerUserPerStreamSetting;
  globalCooldownSetting?: GlobalCooldownSetting;
  isPaused: boolean;
  isInStock: boolean;
  shouldRedemptionsSkipQueue: boolean;
  /** @format int32 */
  redemptionsRedeemedCurrentStream?: number;
  cooldownExpiresAt?: string;
}

export interface DefaultImage {
  url1x?: string;
  url2x?: string;
  url4x?: string;
}

export interface DiscordChannelOptionDto {
  id: string;
  name: string;
  guildId: string;
  guildName: string;
}

export interface EnvironmentVariable {
  key: string;
  value?: string;
  description?: string;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  updatedAt: string;
}

export interface FollowerInfo {
  /** @minLength 1 */
  userId: string;
  twitchUser?: TwitchUser;
}

export interface GetCustomRewardRedemptionResponse {
  data?: RewardRedemption[];
  pagination?: Pagination;
}

export interface GetCustomRewardsResponse {
  data?: CustomReward[];
}

export interface GlobalCooldownSetting {
  isEnabled: boolean;
  /** @format int32 */
  globalCooldownSeconds: number;
}

export interface HusbandDto {
  twitchId: string;
  displayName?: string;
  profileImageUrl?: string;
  /** @format date-time */
  whenOrdered: string;
  waifuBrideId?: string;
  isPrivated: boolean;
  /** @format int64 */
  orderCount: number;
  waifuRollId?: string;
  /** @format date-time */
  whenPrivated?: string;
  /** @format int32 */
  lastWeddingCongratulatedMonths?: number;
}

export interface Image {
  url1x?: string;
  url2x?: string;
  url4x?: string;
}

export interface Log {
  /** @format uuid */
  id: string;
  /** @format date-time */
  whenLogged: string;
  message: string;
  stackTrace?: string;
  logLevel: LogLogLevelEnum;
}

export interface LogResponse {
  logs: Log[];
  /** @format int32 */
  totalCount: number;
  /** @format int32 */
  page: number;
  /** @format int32 */
  pageSize: number;
  /** @format int32 */
  totalPages: number;
}

export interface LogsStatistics {
  /** @format int32 */
  totalLogs: number;
  /** @format int32 */
  warningLogs: number;
  /** @format int32 */
  errorLogs: number;
  /** @format int32 */
  criticalLogs: number;
  /** @format date-time */
  oldestLogDate?: string;
  /** @format date-time */
  newestLogDate?: string;
}

export interface MaxPerStreamSetting {
  isEnabled: boolean;
  /** @format int32 */
  maxPerStream: number;
}

export interface MaxPerUserPerStreamSetting {
  isEnabled: boolean;
  /** @format int32 */
  maxPerUserPerStream: number;
}

export interface MediaDto {
  mediaInfo: MediaInfo;
  /** @format date-time */
  uploadStartTime: string;
}

export interface MediaFileInfo {
  type: MediaFileInfoTypeEnum;
  filePath: string;
  isLocalFile: boolean;
  fileName: string;
  extension: string;
  isFileNotConvertable: boolean;
}

export interface MediaInfo {
  /** @format uuid */
  id: string;
  textInfo: MediaTextInfo;
  fileInfo: MediaFileInfo;
  positionInfo: MediaPositionInfo;
  metaInfo: MediaMetaInfo;
  stylesInfo: MediaStylesInfo;
}

export interface MediaMetaInfo {
  /** @format int32 */
  twitchPointsCost: number;
  /** @format uuid */
  twitchGuid?: string;
  vip: boolean;
  displayName: string;
  isLooped: boolean;
  isFreezeRequired: boolean;
  /** @format int32 */
  duration: number;
  priority: MediaMetaInfoPriorityEnum;
  /** @format int32 */
  volume: number;
  isEnabled: boolean;
}

export interface MediaMetadata {
  title: string;
  description?: string;
  imageUrl?: string;
  sourceUrl?: string;
}

export interface MediaPositionInfo {
  isProportion: boolean;
  isResizeRequires: boolean;
  /** @format int32 */
  height: number;
  /** @format int32 */
  width: number;
  isRotated: boolean;
  /** @format int32 */
  rotation: number;
  /** @format int32 */
  xCoordinate: number;
  /** @format int32 */
  yCoordinate: number;
  randomCoordinates: boolean;
  isVerticallCenter: boolean;
  isHorizontalCenter: boolean;
  isUseOriginalWidthAndHeight: boolean;
}

export interface MediaStylesInfo {
  isBorder: boolean;
  isShowLetterbox: boolean;
}

export interface MediaTextInfo {
  keyWordsColor?: string;
  triggerWord?: string;
  text?: string;
  textColor?: string;
  keyWordSybmolDelimiter?: string;
}

export interface MemeOrderDto {
  /** @format uuid */
  id: string;
  /** @format int32 */
  order: number;
  /** @minLength 1 */
  filePath: string;
  /** @format int32 */
  memeTypeId?: number;
  type?: MemeTypeDto;
}

export interface MemeTypeDto {
  /** @format int32 */
  id: number;
  /**
   * @minLength 1
   * @maxLength 50
   */
  name: string;
  /** @minLength 1 */
  folderPath: string;
}

export interface Pagination {
  cursor?: string;
}

export interface PlayerState {
  /** @format uuid */
  id: string;
  /** @format uuid */
  stateVersion: string;
  /** @format uuid */
  currentQueueItemId?: string;
  /** @format date-span */
  currentTrackProgress?: string;
  state: PlayerStateStateEnum;
  videoState: PlayerStateVideoStateEnum;
  isMuted: boolean;
  pausedByMute: boolean;
  /** @format float */
  volume: number;
  currentQueueItem?: QueueItem;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  /** @format int32 */
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: any;
}

export interface QueueItem {
  /** @format uuid */
  id: string;
  /** @format uuid */
  trackId: string;
  track?: BaseTrackInfo;
  /** @format int32 */
  queueOrder: number;
  /**
   * @minLength 1
   * @maxLength 50
   */
  requestedByTwitchId: string;
  requestedByTwitchUser?: TwitchUser;
  /** @format date-time */
  requestedAt: string;
}

export interface QueueReorderRequest {
  /** @format uuid */
  queueItemId: string;
  /** @format int32 */
  newPosition: number;
}

export interface RateLimiterInfo {
  /** @format int32 */
  availablePerSecond: number;
  /** @format int32 */
  availablePerMinute: number;
  /** @format date-span */
  timeToResetSecond: string;
  /** @format date-span */
  timeToResetMinute: string;
}

export interface Reward {
  id?: string;
  title?: string;
  prompt?: string;
  /** @format int32 */
  cost: number;
}

export interface RewardRedemption {
  broadcasterId?: string;
  broadcasterLogin?: string;
  broadcasterName?: string;
  id?: string;
  userLogin?: string;
  userId?: string;
  userName?: string;
  userInput?: string;
  status: RewardRedemptionStatusEnum;
  /** @format date-time */
  redeemedAt: string;
  reward?: Reward;
}

export interface RootState {
  name: string;
  value: string;
  description: string;
  typeDescription: string;
}

export interface ServerStatsResponse {
  /** @format double */
  cpuUsagePercent: number;
  /** @format int64 */
  memoryWorkingSetBytes: number;
  /** @format int64 */
  memoryPrivateBytes: number;
  /** @format int64 */
  memoryGcHeapBytes: number;
  /** @format int64 */
  memoryTotalBytes: number;
  /** @format double */
  uptimeSeconds: number;
  /** @format int32 */
  threadCount: number;
  /** @format int32 */
  activeServicesCount: number;
  /** @format int32 */
  totalServicesCount: number;
  osVersion: string;
  runtimeVersion: string;
  machineName: string;
  /** @format int32 */
  processorCount: number;
  isEventSubConnected: boolean;
  isTwitchChatConnected: boolean;
  isAudioControllerConnected: boolean;
  isTtsConnected: boolean;
  isPuntoSwitcherEnabled: boolean;
  isTtsFilterEnabled: boolean;
  isDiscordTtsRelayEnabled: boolean;
  nearestWeddingAnniversaryName?: string;
  /** @format date-time */
  nearestWeddingAnniversaryDate?: string;
  nearestWeddingAnniversaryUser?: string;
  /** @format int32 */
  danbooruAutoPostTotalPosts: number;
  /** @format date-time */
  danbooruAutoPostLastPostedAtUtc?: string;
  /** @format int32 */
  nsfwBooruAutoPostTotalPosts: number;
  /** @format date-time */
  nsfwBooruAutoPostLastPostedAtUtc?: string;
  /** @format int32 */
  telegramDiscordBridgeTotalBindings: number;
  /** @format date-time */
  telegramDiscordBridgeLastProcessedAtUtc?: string;
}

export interface ServiceInfo {
  name: string;
  displayName: string;
  description: string;
  status: ServiceInfoStatusEnum;
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  lastActivity?: string;
  isEnabled: boolean;
}

export interface ServiceLog {
  /** @format date-time */
  timestamp: string;
  level: string;
  message: string;
  exception?: string;
}

export interface SetEnvironmentVariableRequest {
  key: string;
  value?: string;
  description?: string;
}

export interface SpotifyAuthCompleteResult {
  success: boolean;
  message: string;
  displayName?: string;
  product?: string;
}

export interface SpotifyAuthStartRequest {
  clientId: string;
  clientSecret: string;
  redirectUri?: string;
}

export interface SpotifyAuthStartResult {
  success: boolean;
  message: string;
  authUrl: string;
  state: string;
}

export interface SpotifyAuthStatusResult {
  isLinked: boolean;
  hasClientCredentials: boolean;
  displayName?: string;
  userId?: string;
  avatarUrl?: string;
  product?: string;
  deviceId?: string;
  /** @format date-time */
  accessTokenExpiresAtUtc?: string;
  message: string;
}

export interface StoreFactRequest {
  twitchId: string;
  fact: string;
  /** @format int32 */
  importance: number;
}

export interface StreamArchiveConfig {
  /** @format uuid */
  id: string;
  /** @format int64 */
  telegramChannelId: number;
  fileNameFormat: string;
  /** @format date-span */
  checkSpan: string;
  folderPath: string;
  isConvertFile: boolean;
  fileConvertType: StreamArchiveConfigFileConvertTypeEnum;
}

export interface TelegramChannelOptionDto {
  id: string;
  title: string;
}

export interface TelegramDiscordBindingCreateRequest {
  /** @format int64 */
  telegramChannelId: number;
  /**
   * Строкой, а не числом: Discord snowflake не помещается в Number браузера.
   */
  discordChannelId: string;
}

export interface TelegramDiscordBindingDto {
  /** @format uuid */
  id: string;
  /** @format int64 */
  telegramChannelId: number;
  /**
   * Строкой, а не числом: Discord snowflake не помещается в Number браузера
   * (64 бита против 53). Сервер отдаёт его строкой.
   */
  discordChannelId: string;
  isEnabled: boolean;
  lastError?: string;
  /** @format date-time */
  createdAtUtc: string;
  /** @format date-time */
  updatedAtUtc: string;
}

export interface TelegramDiscordBindingSetEnabledRequest {
  isEnabled: boolean;
}

export interface TelegramDiscordChannelStateDto {
  /** @format int64 */
  telegramChannelId: number;
  /** @format int32 */
  lastProcessedMessageId: number;
  /** @format date-time */
  lastUpdatedUtc: string;
}

export interface TwitchUser {
  /**
   * @minLength 1
   * @maxLength 50
   */
  twitchId: string;
  /**
   * @minLength 1
   * @maxLength 100
   */
  userLogin: string;
  /**
   * @minLength 1
   * @maxLength 100
   */
  displayName: string;
  /** @maxLength 500 */
  profileImageUrl?: string;
  /** @maxLength 20 */
  chatColor?: string;
  isModerator: boolean;
  isVip: boolean;
  isBroadcaster: boolean;
  /** @format date-time */
  followedAt?: string;
  /** @format date-time */
  lastUpdated: string;
  /** @format date-time */
  createdAt: string;
  /** @maxLength 100 */
  aliasNickname?: string;
  isInBlockList: boolean;
  isSimpleUser: boolean;
}

export interface TwitchUserDto {
  twitchId: string;
  userLogin: string;
  displayName: string;
  profileImageUrl?: string;
  chatColor?: string;
  isModerator: boolean;
  isVip: boolean;
  isBroadcaster: boolean;
  isInBlockList: boolean;
  aliasNickname?: string;
  /** @format date-time */
  followedAt?: string;
  /** @format date-time */
  lastUpdated: string;
  /** @format date-time */
  createdAt: string;
}

export interface UpdateAutoMessageRequest {
  message?: string;
}

export interface UpdateCustomRewardDto {
  title?: string;
  /** @format int32 */
  cost?: number;
  isEnabled?: boolean;
  prompt?: string;
  backgroundColor?: string;
  isUserInputRequired?: boolean;
  isMaxPerStreamEnabled?: boolean;
  /** @format int32 */
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled?: boolean;
  /** @format int32 */
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled?: boolean;
  /** @format int32 */
  globalCooldownSeconds?: number;
  shouldRedemptionsSkipRequestQueue?: boolean;
}

export interface UpdateCustomRewardRedemptionStatusRequest {
  status: UpdateCustomRewardRedemptionStatusRequestStatusEnum;
}

export interface UpdateCustomRewardRequest {
  title?: string;
  prompt?: string;
  /** @format int32 */
  cost?: number;
  backgroundColor?: string;
  isEnabled?: boolean;
  isUserInputRequired?: boolean;
  isMaxPerStreamEnabled?: boolean;
  /** @format int32 */
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled?: boolean;
  /** @format int32 */
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled?: boolean;
  /** @format int32 */
  globalCooldownSeconds?: number;
  isPaused?: boolean;
  shouldRedemptionsSkipRequestQueue?: boolean;
}

export interface UpdateHusbandRequest {
  waifuBrideId?: string;
  isPrivated?: boolean;
  waifuRollId?: string;
  /** @format date-time */
  whenPrivated?: string;
  /** @format int32 */
  lastWeddingCongratulatedMonths?: number;
}

export interface UpdateMediaItemRequest {
  title?: string;
  description?: string;
  mediaUrl?: string;
  status: UpdateMediaItemRequestStatusEnum;
  /** @format int32 */
  priority?: number;
  /** @format date-time */
  scheduledFor?: string;
  notes?: string;
  isNext?: boolean;
}

export interface UpdateMemeOrderDto {
  /** @minLength 1 */
  filePath: string;
  /** @format int32 */
  memeTypeId?: number;
  /**
   * @format int32
   * @min 1
   * @max 2147483647
   */
  order: number;
}

export interface UpdateMemeTypeDto {
  /**
   * @minLength 1
   * @maxLength 50
   */
  name: string;
  /** @minLength 1 */
  folderPath: string;
}

export interface UpdateTwitchUserRequest {
  userLogin?: string;
  displayName?: string;
  profileImageUrl?: string;
  chatColor?: string;
  isModerator?: boolean;
  isVip?: boolean;
  isInBlockList?: boolean;
  aliasNickname?: string;
}

export interface UpdateValueRequest {
  value: string;
}

export interface UpdateWaifuRequest {
  /** @maxLength 200 */
  name?: string;
  /** @format int64 */
  age?: number;
  anime?: string;
  manga?: string;
  /** @maxLength 200 */
  imageUrl?: string;
  isPrivated?: boolean;
  /** @format uuid */
  audioId?: string;
}

export interface ValidateFolderRequest {
  folderPath: string;
}

export interface ValidateFolderResponse {
  exists: boolean;
  accessible: boolean;
  /** @format int32 */
  videoFilesCount: number;
  sampleFiles: string[];
}

export interface VerificationCodeRequest {
  code: string;
}

export interface WaifuDto {
  shikiId: string;
  name: string;
  /** @format int64 */
  age: number;
  anime?: string;
  manga?: string;
  /** @format date-time */
  whenAdded: string;
  /** @format date-time */
  lastOrder: string;
  /** @format int32 */
  orderCount: number;
  isPrivated: boolean;
  imageUrl: string;
  /** @format uuid */
  audioId?: string;
  audioName?: string;
}

export interface WaifuRollAudioDto {
  /** @format uuid */
  id: string;
  name: string;
  fileExtension: string;
  /** @format date-time */
  createdAt: string;
}

export enum BaseCommandAvailablePlatformsEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum BaseCommandVisibilityEnum {
  None = "None",
  FullList = "FullList",
  ShortList = "ShortList",
  Inline = "Inline",
  All = "All",
}

export enum BooruAutoPostConfigDtoSourceEnum {
  Danbooru = "Danbooru",
  Rule34 = "Rule34",
}

export enum BooruAutoPostConfigDtoTargetPlatformEnum {
  Discord = "Discord",
  Telegram = "Telegram",
}

export enum BooruAutoPostConfigDtoTelegramParseModeEnum {
  Default = "Default",
  Html = "Html",
  Markdown = "Markdown",
}

export enum BooruAutoPostCreateRequestSourceEnum {
  Danbooru = "Danbooru",
  Rule34 = "Rule34",
}

export enum BooruAutoPostCreateRequestTargetPlatformEnum {
  Discord = "Discord",
  Telegram = "Telegram",
}

export enum BooruAutoPostCreateRequestTelegramParseModeEnum {
  Default = "Default",
  Html = "Html",
  Markdown = "Markdown",
}

/**
 * Источник постов для запроса изменения.
 *
 * Значения те же, что у запроса создания, и описываются тем же перечислением.
 * Отдельные перечисления под каждую операцию появились из-за генератора, и
 * из-за них объект, собранный для создания, нельзя было передать в
 * обновление: имена типов различались при совпадении значений.
 */
export type BooruAutoPostUpdateRequestSourceEnum =
  BooruAutoPostCreateRequestSourceEnum;

/**
 * Платформа назначения для запроса изменения. Совпадает с запросом создания,
 * см. BooruAutoPostUpdateRequestSourceEnum.
 */
export type BooruAutoPostUpdateRequestTargetPlatformEnum =
  BooruAutoPostCreateRequestTargetPlatformEnum;

/**
 * Режим разбора Telegram для запроса изменения. Совпадает с запросом
 * создания, см. BooruAutoPostUpdateRequestSourceEnum.
 */
export type BooruAutoPostUpdateRequestTelegramParseModeEnum =
  BooruAutoPostCreateRequestTelegramParseModeEnum;

export enum CinemaMediaItemDtoStatusEnum {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Postponed = "Postponed",
}

export enum LogLogLevelEnum {
  Trace = "Trace",
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error",
  Critical = "Critical",
  None = "None",
}

export enum MediaFileInfoTypeEnum {
  None = "None",
  Image = "Image",
  Audio = "Audio",
  Video = "Video",
  TelegramSticker = "TelegramSticker",
  Voice = "Voice",
  Gif = "Gif",
}

export enum MediaMetaInfoPriorityEnum {
  Low = "Low",
  Normal = "Normal",
  High = "High",
}

export enum PlayerStateStateEnum {
  Stopped = "Stopped",
  Playing = "Playing",
  Paused = "Paused",
  SwitchingTrack = "SwitchingTrack",
  WaitingForTrack = "WaitingForTrack",
}

export enum PlayerStateVideoStateEnum {
  Video = "Video",
  NoVideo = "NoVideo",
  AudioOnly = "AudioOnly",
}

export enum RewardRedemptionStatusEnum {
  UNFULFILLED = "UNFULFILLED",
  FULFILLED = "FULFILLED",
  CANCELED = "CANCELED",
}

export enum ServiceInfoStatusEnum {
  Running = "Running",
  Stopped = "Stopped",
  Starting = "Starting",
  Stopping = "Stopping",
  Error = "Error",
  Unknown = "Unknown",
}

export enum StreamArchiveConfigFileConvertTypeEnum {
  None = "None",
  Mp4 = "Mp4",
  Webm = "Webm",
  Mkv = "Mkv",
  Avi = "Avi",
  Mov = "Mov",
}

export enum UpdateCustomRewardRedemptionStatusRequestStatusEnum {
  UNFULFILLED = "UNFULFILLED",
  FULFILLED = "FULFILLED",
  CANCELED = "CANCELED",
}

export enum UpdateMediaItemRequestStatusEnum {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Postponed = "Postponed",
}

export enum CinemaQueueStatusDetailParamsStatusEnum {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Postponed = "Postponed",
}

export enum CinemaQueueStatusDetailParamsEnum {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Postponed = "Postponed",
}

export enum CommandsUserPlatformDetailParamsPlatformEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsUserPlatformDetailParamsEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsAdminPlatformDetailParamsPlatformEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsAdminPlatformDetailParamsEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsUserPlatformInfoListParamsPlatformEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsUserPlatformInfoListParamsEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsAdminPlatformInfoListParamsPlatformEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandsAdminPlatformInfoListParamsEnum {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum LogsListParamsLogLevelEnum {
  Trace = "Trace",
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error",
  Critical = "Critical",
  None = "None",
}

export enum LogsByLevelDetailParamsLogLevelEnum {
  Trace = "Trace",
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error",
  Critical = "Critical",
  None = "None",
}

export enum LogsByLevelDetailParamsEnum {
  Trace = "Trace",
  Debug = "Debug",
  Information = "Information",
  Warning = "Warning",
  Error = "Error",
  Critical = "Critical",
  None = "None",
}

/** @default "FreezeFrame" */
export enum ObsToggleCreateParamsModeEnum {
  FreezeFrame = "FreezeFrame",
  PauseScene = "PauseScene",
}

export enum TestAlertsAlertByTypeCreateParamsTypeEnum {
  None = "None",
  Image = "Image",
  Audio = "Audio",
  Video = "Video",
  TelegramSticker = "TelegramSticker",
  Voice = "Voice",
  Gif = "Gif",
}

/** @default "Normal" */
export enum TestAlertsAlertByTypeCreateParamsPriorityEnum {
  Low = "Low",
  Normal = "Normal",
  High = "High",
}
// ========================================
// SignalR-специфичные типы
// ========================================

export interface AdhdLayoutConfigDto {
  /** @format int32 */
  dvdLogosCount: number;
  showBreakingNews: boolean;
  showCatisa: boolean;
  showDVDLogos: boolean;
  showFitnessVideo: boolean;
  showGTAVideo: boolean;
  showHydraulicMobileVideo: boolean;
  showLOFIGirl: boolean;
  showMukbangVideo: boolean;
  showNotifications: boolean;
  showQuiz: boolean;
  showRainEffect: boolean;
  showSlimeVideo: boolean;
  showStreamerVideo: boolean;
  showSurfer: boolean;
  showTimer: boolean;
}

export interface AudioQuizRoundDto {
  artworkUrl?: string;
  /** @format int32 */
  roundSeconds: number;
  trackUrl: string;
}

export interface AutoArtImage {
  artist: any;
  /** @format int32 */
  byteSize: number;
  dominantColor?: string;
  extension?: string;
  /** @format int32 */
  favorites: number;
  /** @format int32 */
  height: number;
  /** @format int32 */
  imageID: number;
  isNsfw: boolean;
  likedAt: any;
  previewURL?: string;
  signature?: string;
  source?: string;
  /** @format date-time */
  uploadedAt: string;
  url?: string;
  /** @format int32 */
  width: number;
}

export interface ChatMessage {
  badgeInfo: StringStringKeyValuePair[];
  badges: StringStringKeyValuePair[];
  /** @format int32 */
  bits: number;
  /** @format double */
  bitsInDollars: number;
  botUsername: string;
  channel: string;
  chatReply?: ChatReply;
  cheerBadge?: CheerBadge;
  customRewardId?: string;
  displayName: string;
  emoteReplacedMessage?: string;
  emoteSet: EmoteSet;
  hexColor: string;
  hypeChat?: HypeChat;
  id: string;
  isBroadcaster: boolean;
  isFirstMessage: boolean;
  isHighlighted: boolean;
  isMe: boolean;
  isSkippingSubMode: boolean;
  message: string;
  noisy: ChatMessageNoisyEnum;
  rawIrcMessage: string;
  roomId: string;
  /** @format int32 */
  subscribedMonthCount: number;
  /** @format date-time */
  tmiSent: string;
  undocumentedTags: Record<string, string>;
  userDetail: UserDetail;
  userId: string;
  userType: ChatMessageUserTypeEnum;
  username: string;
}

export interface ChatReply {
  parentDisplayName: string;
  parentMsgBody: string;
  parentMsgId: string;
  parentUserId: string;
  parentUserLogin: string;
  threadParentMsgId: string;
  threadParentUserLogin: string;
}

export interface CheerBadge {
  /** @format int32 */
  cheerAmount: number;
  color: CheerBadgeColorEnum;
}

export interface Emote {
  /** @format int32 */
  endIndex: number;
  id: string;
  imageUrl: string;
  name: string;
  /** @format int32 */
  startIndex: number;
}

export interface EmoteSet {
  emotes: Emote[];
  rawEmoteSetString?: string;
}

export interface Frog {
  /** @maxLength 50 */
  category?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  commonName: string;
  /** @maxLength 100 */
  family?: string;
  /** @maxLength 500 */
  habits?: string;
  /** @format date-time */
  lastOrder: string;
  /** @format int32 */
  orderCount: number;
  /** @format int32 */
  pid: number;
  /** @maxLength 200 */
  russianName?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  scientificName: string;
  /** @maxLength 50 */
  size?: string;
  /** @maxLength 100 */
  status?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  thumbnailUrl: string;
  /** @format date-time */
  whenAdded: string;
}

export interface FrogPrizeType {
  /** @format int32 */
  id: number;
  image: string;
  text: string;
}

export interface Fumo {
  /**
   * @minLength 1
   * @maxLength 100
   */
  character: string;
  /** @maxLength 100 */
  characterTranslit?: string;
  /** @format date-time */
  lastOrder: string;
  /** @format int32 */
  mfcId: number;
  /**
   * @minLength 1
   * @maxLength 300
   */
  name: string;
  /** @format int32 */
  orderCount: number;
  /** @maxLength 100 */
  origin?: string;
  /** @format double */
  rating: number;
  /** @format int32 */
  ratingCount: number;
  /**
   * @minLength 1
   * @maxLength 200
   */
  thumbnailUrl: string;
  /** @format date-time */
  whenAdded: string;
}

export interface FumoPrizeType {
  /** @format int32 */
  id: number;
  image: string;
  text: string;
}

export interface GaoAlertDto {
  /** @format uuid */
  id: string;
  isJustText: boolean;
  justText?: string;
  twitchUser?: User;
}

export interface Husband {
  husbandCoolDown?: HusbandCoolDown;
  husbandGreetings?: HusbandAutoHello;
  isPrivated: boolean;
  /** @format int32 */
  lastWeddingCongratulatedMonths?: number;
  /** @format int64 */
  orderCount: number;
  twitchId: string;
  twitchUser?: TwitchUser;
  waifuBrideId?: string;
  waifuRollId?: string;
  /** @format date-time */
  whenOrdered: string;
  /** @format date-time */
  whenPrivated?: string;
}

export interface HusbandAutoHello {
  /** @format uuid */
  guid: string;
  husband?: Husband;
  husbandId: string;
  /** @format date-time */
  time: string;
}

export interface HusbandCoolDown {
  /** @format uuid */
  guid: string;
  husband?: Husband;
  husbandId: string;
  /** @format date-time */
  time: string;
}

export interface HypeChat {
  /** @format int32 */
  amount: number;
  /** @format double */
  calculatedAmount: number;
  currency: string;
  /** @format int32 */
  exponent: number;
  isSystemMessage: boolean;
  level: HypeChatLevelEnum;
}

export interface LogMessageDto {
  category: string;
  connectionId?: string;
  /** @format int32 */
  eventId?: number;
  exception?: string;
  id: string;
  logLevel: string;
  message: string;
  source?: string;
  stackTrace?: string;
  /** @format date-time */
  timestamp: string;
}

export interface MikuModule {
  /** @maxLength 2000 */
  description?: string;
  /** @maxLength 200 */
  designer?: string;
  /** @maxLength 300 */
  japaneseName?: string;
  /** @format date-time */
  lastOrder: string;
  /** @format int32 */
  orderCount: number;
  /** @format int32 */
  pageId: number;
  /** @maxLength 500 */
  songs?: string;
  /**
   * @minLength 1
   * @maxLength 500
   */
  thumbnailUrl: string;
  /**
   * @minLength 1
   * @maxLength 300
   */
  title: string;
  /** @format date-time */
  whenAdded: string;
}

export interface MikuMondayDto {
  availableTracks: MikuTrackDto[];
  /** @format uuid */
  id: string;
  selectedTrack: MikuTrackDto;
  skipAvailableTracksUpdate: boolean;
  twitchUser: TwitchUser;
}

export interface MikuPrizeType {
  /** @format int32 */
  id: number;
  image: string;
  text: string;
}

export interface MikuTrackDto {
  artist: string;
  /** @format uuid */
  id: string;
  /** @format int32 */
  number: number;
  thumbnailUrl?: string;
  title: string;
  url: string;
}

export interface PrizeType {
  id: string;
  image: string;
  text: string;
}

export interface ScoreboardColorsDto {
  backgroundColor: string;
  borderColor: string;
  fightModeColor: string;
  mainColor: string;
  playerNamesColor: string;
  scoreColor: string;
  tournamentTitleColor: string;
}

export interface ScoreboardDto {
  player1: ScoreboardPlayerDto;
  player2: ScoreboardPlayerDto;
  /** @format int32 */
  animationDuration: number;
  colors: ScoreboardColorsDto;
  isVisible: boolean;
  layout?: ScoreboardLayoutDto;
  meta: ScoreboardMetaDto;
}

export interface ScoreboardLayoutDto {
  /** @format int32 */
  flagSize: number;
  /** @format int32 */
  headerHeight: number;
  /** @format int32 */
  headerLeft: number;
  /** @format int32 */
  headerTop: number;
  /** @format int32 */
  headerWidth: number;
  /** @format int32 */
  padding: number;
  /** @format int32 */
  playerBarHeight: number;
  /** @format int32 */
  playerBarWidth: number;
  /** @format int32 */
  playersLeft: number;
  /** @format int32 */
  playersRight: number;
  /** @format int32 */
  playersTop: number;
  /** @format int32 */
  scoreSize: number;
  showFlags: boolean;
  showHeader: boolean;
  showSponsors: boolean;
  showTags: boolean;
  /** @format int32 */
  spacing: number;
}

export interface ScoreboardMetaDto {
  fightRule: string;
  title: string;
}

export interface ScoreboardPlayerDto {
  final: string;
  flag: string;
  name: string;
  /** @format int32 */
  score: number;
  sponsor: string;
  tag: string;
}

export interface StringStringKeyValuePair {
  key?: string;
  value?: string;
}

export interface TunaMusicDTO {
  data: TunaMusicData;
  hostname?: string;
  timestamp?: string;
}

export interface TunaMusicData {
  album_url: string;
  artists: string[];
  cover: string;
  /** @format int64 */
  duration: number;
  /** @format uuid */
  id: string;
  /** @format int64 */
  progress: number;
  status: string;
  title: string;
}

export interface User {
  broadcasterType?: string;
  /** @format date-time */
  createdAt: string;
  description?: string;
  displayName?: string;
  email?: string;
  id?: string;
  login?: string;
  offlineImageUrl?: string;
  profileImageUrl?: string;
  type?: string;
  /**
   * @deprecated
   * @format int64
   */
  viewCount: number;
}

export interface UserDetail {
  hasTurbo: boolean;
  isModerator: boolean;
  isPartner: boolean;
  isStaff: boolean;
  isSubscriber: boolean;
  isVip: boolean;
}

export interface Waifu {
  /** @format int64 */
  age: number;
  anime?: string;
  audio?: WaifuRollAudio;
  /** @format uuid */
  audioId?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  imageUrl: string;
  isAdded: boolean;
  isMerged: boolean;
  isPrivated: boolean;
  /** @format date-time */
  lastOrder: string;
  manga?: string;
  /**
   * @minLength 1
   * @maxLength 200
   */
  name: string;
  /** @format int32 */
  orderCount: number;
  /**
   * @minLength 1
   * @maxLength 20
   */
  shikiId: string;
  /** @format date-time */
  whenAdded: string;
}

export interface WaifuChatResponse {
  correlationId: string;
  messageId?: string;
  response: string;
  twitchId: string;
}

export interface WaifuRollAudio {
  /** @format byte */
  audioData: Blob;
  /** @format date-time */
  createdAt: string;
  /**
   * @minLength 1
   * @maxLength 20
   */
  fileExtension: string;
  /** @format uuid */
  id: string;
  /**
   * @minLength 1
   * @maxLength 100
   */
  name: string;
}

export type TunaMusicDtoRoot = object;

export enum ChatMessageNoisyEnum {
  NotSet = "NotSet",
  True = "True",
  False = "False",
}

export enum ChatMessageUserTypeEnum {
  Viewer = "Viewer",
  Moderator = "Moderator",
  GlobalModerator = "GlobalModerator",
  Broadcaster = "Broadcaster",
  Admin = "Admin",
  Staff = "Staff",
}

export enum CheerBadgeColorEnum {
  Gray = "Gray",
  Purple = "Purple",
  Green = "Green",
  Blue = "Blue",
  Red = "Red",
}

export enum HypeChatLevelEnum {
  One = "One",
  Two = "Two",
  Three = "Three",
  Four = "Four",
  Five = "Five",
  Six = "Six",
  Seven = "Seven",
  Eight = "Eight",
  Nine = "Nine",
  Ten = "Ten",
}

export enum TelegramusMakeScreenParticlesCreateParamsParticlesEnum {
  Confetty = "Confetty",
  Fireworks = "Fireworks",
}
