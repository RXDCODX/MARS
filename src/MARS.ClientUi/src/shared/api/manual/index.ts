/**
 * Типы, которых нет в OpenAPI-документах сервисов.
 *
 * Часть полезных нагрузок хабов объявлена в proto полями `bytes`
 * (`messageJson`, `fumoJson`, `twitchUserJson` и подобными). В документе они
 * выглядят как массивы байт, а на проводе приезжает JSON внутри этих байт.
 * Сгенерировать такой тип нельзя, поэтому он описан здесь руками и не
 * удаляется генератором (`build-api.js` чистит только сгенерированные
 * каталоги).
 *
 * Правки в этом файле должны совпадать с тем, что реально кладёт на провод
 * сервер: см. `tests/MARS.Alerts.Tests/Hubs/OverlayPayloadWireFormatTests.cs`.
 */

import type { ApiMediaInfo, TwitchUser } from "../types/data-contracts";

/**
 * Медиа-информация алерта.
 *
 * REST-контракт хранилища уже описывает ту же структуру как `ApiMediaInfo`,
 * поэтому имя остаётся только алиасом. Отдельный тип нужен, потому что
 * полезная нагрузка хаба описывает вложенные объекты необязательными: proto
 * не выражает обязательность. Сервер же всегда их заполняет, и код отрисовки
 * на это опирается.
 */
export type MediaInfo = ApiMediaInfo;

/**
 * Полезная нагрузка события алерта: `AlertEvent.media` в hub-документе.
 */
export interface MediaDto {
  mediaInfo: MediaInfo;
  /** @format date-time */
  uploadStartTime: string;
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

export interface StringStringKeyValuePair {
  key?: string;
  value?: string;
}

export interface UserDetail {
  hasTurbo: boolean;
  isModerator: boolean;
  isPartner: boolean;
  isStaff: boolean;
  isSubscriber: boolean;
  isVip: boolean;
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

export interface PrizeType {
  id: string;
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

export interface Waifu {
  /** @format int64 */
  age: number;
  anime?: string;
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
