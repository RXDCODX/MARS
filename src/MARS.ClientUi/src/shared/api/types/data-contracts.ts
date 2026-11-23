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
 * Обобщённый результат операции.
 *
 * Поля data и result повторяют друг друга: транспорт кладёт полезную нагрузку
 * в оба поля, потому что вызовы на проекте читают response.data.data, а
 * сервис отвечает result.
 * @template TData - Тип данных, возвращаемых в поле data
 */
export interface OperationResult<TData = any> {
  /** Флаг успешности операции */
  success: boolean;
  /** Сообщение о результате операции */
  message?: string;
  /** Сообщение об ошибке */
  errorMessage?: string;
  /** Данные результата операции */
  data?: TData;
  /** Данные результата операции в форме сервиса */
  result?: TData;
}

export enum StreamArchiveVideoFormats {
  None = "None",
  Mp4 = "Mp4",
  Webm = "Webm",
  Mkv = "Mkv",
  Avi = "Avi",
  Mov = "Mov",
}

export enum ServiceStatus {
  Running = "Running",
  Stopped = "Stopped",
  Starting = "Starting",
  Stopping = "Stopping",
  Error = "Error",
  Unknown = "Unknown",
}

export enum MediaType {
  None = "None",
  Image = "Image",
  Audio = "Audio",
  Video = "Video",
  TelegramSticker = "TelegramSticker",
  Voice = "Voice",
  Gif = "Gif",
}

export enum MediaAlertPriority {
  Low = "Low",
  Normal = "Normal",
  High = "High",
}

export enum MediaStatus {
  Pending = "Pending",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Postponed = "Postponed",
}

export enum ObsPauseMode {
  FreezeFrame = "FreezeFrame",
  PauseScene = "PauseScene",
}

export enum Platform {
  None = "None",
  Api = "Api",
  Telegram = "Telegram",
  Twitch = "Twitch",
  Discord = "Discord",
  Vk = "Vk",
  All = "All",
}

export enum CommandVisibility {
  None = "None",
  FullList = "FullList",
  ShortList = "ShortList",
  Inline = "Inline",
  All = "All",
}

export enum CommandParameterType {
  String = "String",
  Int = "Int",
  Long = "Long",
  Double = "Double",
  Bool = "Bool",
}

export enum CommandErrorCode {
  None = "None",
  UnknownCommand = "UnknownCommand",
  NotAvailableOnPlatform = "NotAvailableOnPlatform",
  BadArguments = "BadArguments",
  NotAllowed = "NotAllowed",
  NotImplemented = "NotImplemented",
  TargetUnreachable = "TargetUnreachable",
  Failed = "Failed",
  Accepted = "Accepted",
}

export enum CommandAttachmentKind {
  Image = "Image",
  Video = "Video",
  Audio = "Audio",
  File = "File",
}

export enum HttpStatusCode {
  Continue = "Continue",
  SwitchingProtocols = "SwitchingProtocols",
  Processing = "Processing",
  EarlyHints = "EarlyHints",
  OK = "OK",
  Created = "Created",
  Accepted = "Accepted",
  NonAuthoritativeInformation = "NonAuthoritativeInformation",
  NoContent = "NoContent",
  ResetContent = "ResetContent",
  PartialContent = "PartialContent",
  MultiStatus = "MultiStatus",
  AlreadyReported = "AlreadyReported",
  IMUsed = "IMUsed",
  MultipleChoices = "MultipleChoices",
  MovedPermanently = "MovedPermanently",
  Found = "Found",
  SeeOther = "SeeOther",
  NotModified = "NotModified",
  UseProxy = "UseProxy",
  Unused = "Unused",
  TemporaryRedirect = "TemporaryRedirect",
  PermanentRedirect = "PermanentRedirect",
  BadRequest = "BadRequest",
  Unauthorized = "Unauthorized",
  PaymentRequired = "PaymentRequired",
  Forbidden = "Forbidden",
  NotFound = "NotFound",
  MethodNotAllowed = "MethodNotAllowed",
  NotAcceptable = "NotAcceptable",
  ProxyAuthenticationRequired = "ProxyAuthenticationRequired",
  RequestTimeout = "RequestTimeout",
  Conflict = "Conflict",
  Gone = "Gone",
  LengthRequired = "LengthRequired",
  PreconditionFailed = "PreconditionFailed",
  RequestEntityTooLarge = "RequestEntityTooLarge",
  RequestUriTooLong = "RequestUriTooLong",
  UnsupportedMediaType = "UnsupportedMediaType",
  RequestedRangeNotSatisfiable = "RequestedRangeNotSatisfiable",
  ExpectationFailed = "ExpectationFailed",
  MisdirectedRequest = "MisdirectedRequest",
  UnprocessableEntity = "UnprocessableEntity",
  Locked = "Locked",
  FailedDependency = "FailedDependency",
  UpgradeRequired = "UpgradeRequired",
  PreconditionRequired = "PreconditionRequired",
  TooManyRequests = "TooManyRequests",
  RequestHeaderFieldsTooLarge = "RequestHeaderFieldsTooLarge",
  UnavailableForLegalReasons = "UnavailableForLegalReasons",
  InternalServerError = "InternalServerError",
  NotImplemented = "NotImplemented",
  BadGateway = "BadGateway",
  ServiceUnavailable = "ServiceUnavailable",
  GatewayTimeout = "GatewayTimeout",
  HttpVersionNotSupported = "HttpVersionNotSupported",
  VariantAlsoNegotiates = "VariantAlsoNegotiates",
  InsufficientStorage = "InsufficientStorage",
  LoopDetected = "LoopDetected",
  NotExtended = "NotExtended",
  NetworkAuthenticationRequired = "NetworkAuthenticationRequired",
}

export enum CustomRewardRedemptionStatus {
  UNFULFILLED = "UNFULFILLED",
  FULFILLED = "FULFILLED",
  CANCELED = "CANCELED",
}

export interface AutoMessageDto {
  /** @format uuid */
  id: string;
  message: string;
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

export interface CustomReward {
  broadcasterId?: string;
  broadcasterLogin?: string;
  broadcasterName?: string;
  id?: string;
  title?: string;
  prompt?: string;
  /** @format int32 */
  cost: number;
  image: Image;
  defaultImage: DefaultImage;
  backgroundColor?: string;
  isEnabled: boolean;
  isUserInputRequired: boolean;
  maxPerStreamSetting: MaxPerStreamSetting;
  maxPerUserPerStreamSetting: MaxPerUserPerStreamSetting;
  globalCooldownSetting: GlobalCooldownSetting;
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

export interface FollowerInfo {
  /** @minLength 1 */
  userId: string;
  twitchUser: TwitchUser;
}

export interface GetCustomRewardRedemptionResponse {
  data?: RewardRedemption[];
  pagination: Pagination;
}

export interface GetCustomRewardsResponse {
  data?: CustomReward[];
}

export interface GlobalCooldownSetting {
  isEnabled: boolean;
  /** @format int32 */
  globalCooldownSeconds: number;
}

export interface Image {
  url1x?: string;
  url2x?: string;
  url4x?: string;
}

export interface LeaderboardEntry {
  twitchId: string;
  displayName?: string;
  /** @format int32 */
  totalWins: number;
  /** @format int32 */
  russianRouletteWins: number;
  /** @format int32 */
  triviaWins: number;
}

export interface LeaderboardStats {
  /** @format int32 */
  place: number;
  user: LeaderboardEntry;
}

export interface LeaderboardTop {
  entries: LeaderboardEntry[];
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

export interface Pagination {
  cursor?: string;
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
  status: CustomRewardRedemptionStatus;
  /** @format date-time */
  redeemedAt: string;
  reward: Reward;
}

export interface RxdcodxAvatarMissingUserDto {
  userId: string;
  userName?: string;
  profileImageUrl?: string;
  /** @format date-time */
  lastUpdated?: string;
}

export interface RxdcodxAvatarsDebugDto {
  /** @format int32 */
  usersWithoutAvatarsCount: number;
  usersWithoutAvatars: RxdcodxAvatarMissingUserDto[];
  message: string;
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
  osVersion: string;
  runtimeVersion: string;
  machineName: string;
  /** @format int32 */
  processorCount: number;
  isEventSubConnected: boolean;
  isTwitchChatConnected: boolean;
  isPuntoSwitcherEnabled: boolean;
  nearestWeddingAnniversaryName?: string;
  /** @format date-time */
  nearestWeddingAnniversaryDate?: string;
  nearestWeddingAnniversaryUser?: string;
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
  status: CustomRewardRedemptionStatus;
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

export interface AutoHelloRequest {
  displayName: string;
}

export interface CollectionInventory {
  /** @format int32 */
  collected: number;
  /** @format int32 */
  total: number;
  items: CollectionItem[];
}

export interface CollectionItem {
  name: string;
  /** @format int32 */
  count: number;
}

export interface CreateWaifuRequest {
  shikiId: string;
  name: string;
  /** @format int64 */
  age: number;
  anime?: string;
  manga?: string;
  imageUrl?: string;
  /** @format uuid */
  audioId?: string;
}

export interface HusbandDto {
  twitchId: string;
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

export interface ShikimoriTitleRef {
  /** @format int64 */
  id: number;
  name?: string;
  russianName?: string;
  /** @format int32 */
  year?: number;
  url: string;
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

export interface UpdateWaifuRequest {
  name?: string;
  /** @format int64 */
  age?: number;
  anime?: string;
  manga?: string;
  imageUrl?: string;
  isPrivated?: boolean;
  /** @format uuid */
  audioId?: string;
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

export interface ShikimoriCharacterRef {
  /** @format int64 */
  id: number;
  name: string;
  russianName?: string;
  description?: string;
  imageUrl: string;
  imagePath: string;
  animeTitle?: string;
  mangaTitle?: string;
}

export interface ShikimoriRateLimiterInfo {
  /** @format int32 */
  availablePerSecond: number;
  /** @format int32 */
  availablePerMinute: number;
  /** @format double */
  secondsToResetSecondWindow: number;
  /** @format double */
  secondsToResetMinuteWindow: number;
}

export interface DiscordChannelOptionDto {
  id: string;
  name: string;
  guildId: string;
  guildName: string;
}

export interface TelegramAdminMessageRequest {
  service: string;
  message: string;
}

export interface TelegramAdminMessageResult {
  /** @format int32 */
  recipients: number;
}

export interface TelegramChannelOptionDto {
  /** @format int64 */
  id: number;
  title: string;
}

export interface TelegramDiscordBindingCreateRequest {
  /** @format int64 */
  telegramChannelId: number;
  discordChannelId: string;
}

export interface TelegramDiscordBindingDto {
  /** @format uuid */
  id: string;
  /** @format int64 */
  telegramChannelId: number;
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

export interface TelegramLogEntryRequest {
  service: string;
  level: string;
  category: string;
  message: string;
  exception: string;
  /** @format date-time */
  timestamp?: string;
  /** @format int32 */
  suppressedCount: number;
}

export interface TelegramLogEntryResult {
  /** @format int32 */
  recipients: number;
  /** @format int32 */
  suppressed: number;
}

export interface VerificationCodeRequest {
  code: string;
}

export interface WTelegramClientStatus {
  isAuthenticated: boolean;
  /** @format int64 */
  userId?: number;
  username?: string;
  phone?: string;
  errorMessage?: string;
  isAwaitingCode: boolean;
}

export interface BaseCommand {
  commandName: string;
  description: string;
  isAdminCommand: boolean;
  availablePlatforms: Platform[];
  aliases: string[];
  parameters: CommandParameterInfo[];
  visibility: CommandVisibility;
  supportsInline: boolean;
  supportsMediaInline: boolean;
  inlinePreviewUrl?: string;
  inlineTitle: string;
  inlineDescription: string;
}

export interface CommandAttachment {
  kind: CommandAttachmentKind;
  url: string;
  caption?: string;
}

export interface CommandParameterInfo {
  name: string;
  description: string;
  type: CommandParameterType;
  required: boolean;
  defaultValue?: string;
}

export interface CommandResult {
  success: boolean;
  text: string;
  errorCode: CommandErrorCode;
  jobId?: string;
  attachments: CommandAttachment[];
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
  /** @format date-time */
  requestedAt: string;
}

export interface QueueReorderRequest {
  /** @format uuid */
  queueItemId: string;
  /** @format int32 */
  newPosition: number;
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

export interface TtsMuteStateRequest {
  isMuted: boolean;
  /** @format double */
  volume: number;
}

export interface OverlayFireResult {
  /** @format int32 */
  subscribers: number;
}

export interface ScoreboardColorsDto {
  mainColor: string;
  playerNamesColor: string;
  tournamentTitleColor: string;
  fightModeColor: string;
  scoreColor: string;
  backgroundColor: string;
  borderColor: string;
}

export interface ScoreboardDto {
  player1: ScoreboardPlayerDto;
  player2: ScoreboardPlayerDto;
  meta: ScoreboardMetaDto;
  colors: ScoreboardColorsDto;
  isVisible: boolean;
  /** @format int32 */
  animationDuration: number;
  layout?: ScoreboardLayoutDto;
}

export interface ScoreboardLayoutDto {
  /** @format int32 */
  headerTop: number;
  /** @format int32 */
  headerLeft: number;
  /** @format int32 */
  playersTop: number;
  /** @format int32 */
  playersLeft: number;
  /** @format int32 */
  playersRight: number;
  /** @format int32 */
  headerHeight: number;
  /** @format int32 */
  headerWidth: number;
  /** @format int32 */
  playerBarHeight: number;
  /** @format int32 */
  playerBarWidth: number;
  /** @format int32 */
  scoreSize: number;
  /** @format int32 */
  flagSize: number;
  /** @format int32 */
  spacing: number;
  /** @format int32 */
  padding: number;
  showHeader: boolean;
  showFlags: boolean;
  showSponsors: boolean;
  showTags: boolean;
}

export interface ScoreboardMetaDto {
  title: string;
  fightRule: string;
}

export interface ScoreboardPlayerDto {
  name: string;
  sponsor: string;
  /** @format int32 */
  score: number;
  tag: string;
  flag: string;
  final: string;
}

export interface CinemaMediaItemDto {
  /** @format uuid */
  id: string;
  title?: string;
  description?: string;
  mediaUrl: string;
  status: MediaStatus;
  /** @format int32 */
  priority: number;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  scheduledFor?: string;
  twitchUserId?: string;
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

export interface CreateMediaItemRequest {
  title?: string;
  description?: string;
  mediaUrl: string;
  /** @format int32 */
  priority: number;
  /** @format date-time */
  scheduledFor?: string;
  twitchUserId?: string;
  notes?: string;
}

export interface UpdateMediaItemRequest {
  title?: string;
  description?: string;
  mediaUrl?: string;
  status: MediaStatus;
  /** @format int32 */
  priority?: number;
  /** @format date-time */
  scheduledFor?: string;
  notes?: string;
  isNext?: boolean;
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

export interface BulkMoveRequest {
  ids: string[];
  dryRun: boolean;
  targetDirectory: string;
}

export interface BulkOperationResultDto {
  /** @format int32 */
  requested: number;
  /** @format int32 */
  succeeded: number;
  /** @format int32 */
  failed: number;
  errors: string[];
}

export interface BulkRestoreRequest {
  ids: string[];
  dryRun: boolean;
}

export interface BulkStorageRequest {
  ids: string[];
  dryRun: boolean;
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

export interface GitStatusResult {
  isRepositoryReady: boolean;
  pendingPaths: string[];
  currentBranch?: string;
  error?: string;
}

export interface IndexResultDto {
  /** @format int32 */
  added: number;
  /** @format int32 */
  updated: number;
}

export interface MediaFileInfo {
  type: MediaType;
  filePath: string;
  isLocalFile: boolean;
  fileName: string;
  extension: string;
  isFileNotConvertable: boolean;
}

export interface MediaGitSyncRequest {
  ids: string[];
}

export interface MediaGitSyncResultDto {
  /** @format int32 */
  filesSelected: number;
  committed: boolean;
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
  priority: MediaAlertPriority;
  /** @format int32 */
  volume: number;
  isEnabled: boolean;
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

export interface MediaStorageBrowseDirectory {
  name: string;
  relativePath: string;
  hasChildren: boolean;
}

export interface MediaStorageBrowseFile {
  name: string;
  relativePath: string;
  /** @format int64 */
  sizeBytes: number;
  /** @format date-time */
  lastWriteTimeUtc: string;
  /** @format uuid */
  storageEntryId?: string;
  /** @format int32 */
  mediaType?: number;
  isIndexed: boolean;
  /** @format date-time */
  uploadedAt?: string;
  /** @format date-time */
  lastDownloadedAt?: string;
}

export interface MediaStorageBrowseResult {
  name: string;
  relativePath: string;
  parentDirectory?: string;
  directories: MediaStorageBrowseDirectory[];
  files: MediaStorageBrowseFile[];
}

export interface MediaStorageEntryDto {
  /** @format uuid */
  id: string;
  path: string;
  fileName: string;
  extension: string;
  mediaType: MediaType;
  /** @format int64 */
  sizeBytes: number;
  /** @format date-time */
  uploadedAt: string;
  /** @format date-time */
  lastDownloadedAt?: string;
  /** @format date-time */
  deletedAt?: string;
  originalPath?: string;
  isDeleted: boolean;
}

export interface MediaStorageSearchResultDto {
  entries: MediaStorageEntryDto[];
  /** @format int32 */
  total: number;
  /** @format int32 */
  page: number;
  /** @format int32 */
  pageSize: number;
}

export interface MediaStorageTextPreviewDto {
  content: string;
  /** @format int64 */
  sizeBytes: number;
  truncated: boolean;
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
  type: MemeTypeDto;
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

export interface ProblemDetails {
  type?: string;
  title?: string;
  /** @format int32 */
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: any;
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

export interface EnvironmentVariable {
  key: string;
  value?: string;
  description?: string;
  /** @format date-time */
  createdAt: string;
  /** @format date-time */
  updatedAt: string;
}

export interface RootState {
  name: string;
  value: string;
  description: string;
  typeDescription: string;
}

export interface ServiceInfo {
  name: string;
  displayName: string;
  description: string;
  status: ServiceStatus;
  /** @format date-time */
  startTime?: string;
  /** @format date-time */
  lastActivity?: string;
  isEnabled: boolean;
  healthEndpoint?: string;
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
  fileConvertType: StreamArchiveVideoFormats;
}

export interface UpdateValueRequest {
  value: string;
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

export type BaseTrackInfoListOperationResultStatusCodeEnum = HttpStatusCode;
export const BaseTrackInfoListOperationResultStatusCodeEnum = HttpStatusCode;

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

export type PlayerStateOperationResultStatusCodeEnum = HttpStatusCode;
export const PlayerStateOperationResultStatusCodeEnum = HttpStatusCode;

export type QueueItemListOperationResultStatusCodeEnum = HttpStatusCode;
export const QueueItemListOperationResultStatusCodeEnum = HttpStatusCode;

export type SpotifyAuthCompleteResultOperationResultStatusCodeEnum =
  HttpStatusCode;
export const SpotifyAuthCompleteResultOperationResultStatusCodeEnum =
  HttpStatusCode;

export type SpotifyAuthStartResultOperationResultStatusCodeEnum =
  HttpStatusCode;
export const SpotifyAuthStartResultOperationResultStatusCodeEnum =
  HttpStatusCode;

export type SpotifyAuthStatusResultOperationResultStatusCodeEnum =
  HttpStatusCode;
export const SpotifyAuthStatusResultOperationResultStatusCodeEnum =
  HttpStatusCode;

export type OverlayFireResultOperationResultStatusCodeEnum = HttpStatusCode;
export const OverlayFireResultOperationResultStatusCodeEnum = HttpStatusCode;

export type ScoreboardDtoOperationResultStatusCodeEnum = HttpStatusCode;
export const ScoreboardDtoOperationResultStatusCodeEnum = HttpStatusCode;
// ========================================
// SignalR-специфичные типы
// ========================================

export interface AddNewWaifuEvent {
  color?: string;
  displayName?: string;
  waifu?: WaifuInfo;
}

export interface AdhdConfigEvent {
  configJson?: number[];
}

export interface AdhdEvent {
  /** @format int32 */
  seconds: number;
}

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

export interface AlertEvent {
  media?: MediaPayload;
}

export interface AlertsEvent {
  media?: MediaPayload[];
}

export interface AllRefundEvent {
  userJson?: number[];
}

export interface AutoMessageEvent {
  message?: string;
}

export interface DeleteMessageEvent {
  id?: string;
}

export interface FrogRollEvent {
  color?: string;
  frogJson?: number[];
  twitchUserJson?: number[];
}

export interface FumoFridayEvent {
  color?: string;
  displayName?: string;
}

export interface FumoRollEvent {
  /** @format int32 */
  collectedCount: number;
  color?: string;
  fumoJson?: number[];
  /** @format int32 */
  totalCount: number;
  twitchUserJson?: number[];
}

export interface GaoAlertEvent {
  gaoAlertJson?: number[];
}

export interface HighliteEvent {
  color?: string;
  faceUrlJson?: number[];
  messageJson?: number[];
}

export interface HusbandInfo {
  avatarUrl?: string;
  displayName?: string;
  id?: string;
}

export interface MediaFilePayload {
  extension?: string;
  fileName?: string;
  filePath?: string;
  isFileNotConvertable: boolean;
  isLocalFile: boolean;
  type: MediaFilePayloadTypeEnum;
}

export interface MediaInfoPayload {
  fileInfo?: MediaFilePayload;
  id?: string;
  metaInfo?: MediaMetaPayload;
  positionInfo?: MediaPositionPayload;
  stylesInfo?: MediaStylesPayload;
  textInfo?: MediaTextPayload;
}

export interface MediaMetaPayload {
  displayName?: string;
  /** @format int32 */
  duration: number;
  isEnabled: boolean;
  isFreezeRequired: boolean;
  isLooped: boolean;
  priority: MediaMetaPayloadPriorityEnum;
  twitchGuid?: string;
  /** @format int32 */
  twitchPointsCost: number;
  vip: boolean;
  /** @format int32 */
  volume: number;
}

export interface MediaPayload {
  mediaInfo?: MediaInfoPayload;
  uploadStartTime?: string;
}

export interface MediaPositionPayload {
  /** @format int32 */
  height: number;
  isHorizontalCenter: boolean;
  isProportion: boolean;
  isResizeRequires: boolean;
  isRotated: boolean;
  isUseOriginalWidthAndHeight: boolean;
  isVerticallCenter: boolean;
  randomCoordinates: boolean;
  /** @format int32 */
  rotation: number;
  /** @format int32 */
  width: number;
  /** @format int32 */
  xCoordinate: number;
  /** @format int32 */
  yCoordinate: number;
}

export interface MediaStylesPayload {
  isBorder: boolean;
  isShowLetterbox: boolean;
}

export interface MediaTextPayload {
  hasKeywordSymbolDelimiter: boolean;
  /** @format int32 */
  keywordSymbolDelimiter: number;
  keywordsColor?: string;
  text?: string;
  textColor?: string;
  triggerWord?: string;
}

export interface MergeWaifuEvent {
  avatar?: string;
  color?: string;
  husband?: HusbandInfo;
  waifu?: WaifuInfo;
}

export interface MikuMikuBeamEvent {
  usersJson?: number[][];
}

export interface MikuMondayEvent {
  mikuMondayJson?: number[];
}

export interface MikuRollEvent {
  /** @format int32 */
  collectedCount: number;
  color?: string;
  mikuModuleJson?: number[];
  /** @format int32 */
  totalCount: number;
  twitchUserJson?: number[];
}

export interface NewMessageEvent {
  id?: string;
  messageJson?: number[];
}

export interface PlayerStateHubDto {
  currentQueueItem?: QueueItemHubDto;
  currentQueueItemId?: string;
  currentTrackProgress?: string;
  id: string;
  isMuted: boolean;
  pausedByMute: boolean;
  state: PlayerStateHubDtoStateEnum;
  videoState: PlayerStateHubDtoVideoStateEnum;
  /** @format float */
  volume: number;
}

export interface PlayerStateSnapshot {
  currentQueueItem?: QueueItemSnapshot;
  currentQueueItemId?: string;
  /** @format int64 */
  currentTrackProgressSeconds: number;
  hasCurrentTrackProgress: boolean;
  id?: string;
  isMuted: boolean;
  pausedByMute: boolean;
  state: PlayerStateSnapshotStateEnum;
  videoState: PlayerStateSnapshotVideoStateEnum;
  /** @format float */
  volume: number;
}

export interface PrizesEvent {
  prizesJson?: number[];
}

export interface QueueItemHubDto {
  id: string;
  track?: TrackInfoHubDto;
}

export interface QueueItemSnapshot {
  id?: string;
  /** @format int32 */
  queueOrder: number;
  requestedAt?: string;
  requestedByTwitchId?: string;
  track?: TrackInfo;
  trackId?: string;
}

export interface QueueState {
  queue?: QueueItemSnapshot[];
}

export interface ScoreboardColors {
  backgroundColor?: string;
  borderColor?: string;
  fightModeColor?: string;
  mainColor?: string;
  playerNamesColor?: string;
  scoreColor?: string;
  tournamentTitleColor?: string;
}

export interface ScoreboardLayoutSnapshot {
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

export interface ScoreboardMeta {
  fightRule?: string;
  title?: string;
}

export interface ScoreboardPlayer {
  final?: string;
  flag?: string;
  name?: string;
  /** @format int32 */
  score: number;
  sponsor?: string;
  tag?: string;
}

export interface ScoreboardSnapshot {
  player1?: ScoreboardPlayer;
  player2?: ScoreboardPlayer;
  /** @format int32 */
  animationDuration: number;
  colors?: ScoreboardColors;
  isVisible: boolean;
  layout?: ScoreboardLayoutSnapshot;
  meta?: ScoreboardMeta;
}

export interface ScreenEmojisParticlesEvent {
  messageJson?: number[];
}

export interface ScreenParticlesEvent {
  particlesJson?: number[];
}

export interface ShowCurrentWifeEvent {
  avatar?: string;
  color?: string;
  husband?: HusbandInfo;
  waifu?: WaifuInfo;
}

export interface TikTokEditEvent {
  guid?: string;
  text?: string;
}

export interface TrackInfo {
  artworkUrl?: string;
  authors?: string[];
  createdAt?: string;
  /** @format int64 */
  durationSeconds: number;
  id?: string;
  isDeleted: boolean;
  lastTimePlays?: string;
  trackName?: string;
  updatedAt?: string;
  url?: string;
  videoId?: string;
}

export interface TrackInfoHubDto {
  artworkUrl?: string;
  authors?: string[];
  duration?: string;
  id: string;
  trackName: string;
  url?: string;
  videoId?: string;
}

export interface WaifuInfo {
  id?: string;
  imageUrl?: string;
  name?: string;
  source?: string;
}

export interface WaifuRollEvent {
  color?: string;
  displayName?: string;
  waifu?: WaifuInfo;
}

export type MediaFilePayloadTypeEnum = MediaType;
export const MediaFilePayloadTypeEnum = MediaType;

export type MediaMetaPayloadPriorityEnum = MediaAlertPriority;
export const MediaMetaPayloadPriorityEnum = MediaAlertPriority;

export type PlayerStateHubDtoStateEnum = PlayerStateStateEnum;
export const PlayerStateHubDtoStateEnum = PlayerStateStateEnum;

export type PlayerStateHubDtoVideoStateEnum = PlayerStateVideoStateEnum;
export const PlayerStateHubDtoVideoStateEnum = PlayerStateVideoStateEnum;

export type PlayerStateSnapshotStateEnum = PlayerStateStateEnum;
export const PlayerStateSnapshotStateEnum = PlayerStateStateEnum;

export type PlayerStateSnapshotVideoStateEnum = PlayerStateVideoStateEnum;
export const PlayerStateSnapshotVideoStateEnum = PlayerStateVideoStateEnum;
