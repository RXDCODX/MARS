import {
  ChatMessage,
  ChatMessageNoisyEnum,
  ChatMessageUserTypeEnum,
} from "@/shared/api";

/**
 * Создает тестовое сообщение чата с базовыми параметрами
 */
export function createTestMessage(
  id: string,
  message: string,
  displayName: string,
  hexColor: string,
  isVip: boolean = false,
  isModerator: boolean = false,
  isBroadcaster: boolean = false
): ChatMessage {
  return {
    id,
    message,
    displayName,
    hexColor,
    isBroadcaster,
    badgeInfo: [],
    badges: [],
    bits: 0,
    bitsInDollars: 0,
    botUsername: "",
    channel: "",
    chatReply: undefined,
    cheerBadge: undefined,
    customRewardId: undefined,
    emoteReplacedMessage: undefined,
    emoteSet: { emotes: [] },
    isFirstMessage: false,
    isHighlighted: false,
    isMe: false,
    isSkippingSubMode: false,
    noisy: ChatMessageNoisyEnum.False,
    rawIrcMessage: "",
    roomId: "",
    subscribedMonthCount: 0,
    tmiSent: new Date(0).toISOString(),
    undocumentedTags: {},
    userDetail: {
      hasTurbo: false,
      isModerator,
      isPartner: false,
      isStaff: false,
      isSubscriber: false,
      isVip,
    },
    userId: "",
    userType: ChatMessageUserTypeEnum.Viewer,
    username: "",
  };
}

/**
 * Создает набор тестовых сообщений для демонстрации
 */
export function createTestMessageSet(
  prefix: string,
  startIndex: number
): ChatMessage[] {
  return [
    createTestMessage(
      `${prefix}-${startIndex}`,
      "Привет всем! 👋",
      "User1",
      "#ff0000"
    ),
    createTestMessage(
      `${prefix}-${startIndex + 1}`,
      "Как дела?",
      "User2",
      "#00ff00",
      true
    ),
    createTestMessage(
      `${prefix}-${startIndex + 2}`,
      "Отличный стрим!",
      "Moderator",
      "#0000ff",
      false,
      true
    ),
    createTestMessage(
      `${prefix}-${startIndex + 3}`,
      "Спасибо за стрим! 🎮",
      "Broadcaster",
      "#ffff00",
      false,
      false,
      true
    ),
  ];
}

/**
 * Создает быстрый набор тестовых сообщений
 */
export function createFastTestMessageSet(
  prefix: string,
  startIndex: number
): ChatMessage[] {
  return [
    createTestMessage(
      `${prefix}-${startIndex}`,
      "Быстрое сообщение! ⚡",
      "FastUser",
      "#ff6600"
    ),
    createTestMessage(
      `${prefix}-${startIndex + 1}`,
      "Еще одно! 🚀",
      "SpeedUser",
      "#ff0066",
      true
    ),
    createTestMessage(
      `${prefix}-${startIndex + 2}`,
      "Третье сообщение! 🎯",
      "ThirdUser",
      "#00ffff",
      false,
      true
    ),
  ];
}

/**
 * Создает демо набор тестовых сообщений
 */
export function createDemoTestMessageSet(
  prefix: string,
  startIndex: number
): ChatMessage[] {
  return [
    createTestMessage(
      `${prefix}-${startIndex}`,
      "Привет всем! 👋",
      "User1",
      "#ff0000"
    ),
    createTestMessage(
      `${prefix}-${startIndex + 1}`,
      "Как дела?",
      "User2",
      "#00ff00",
      true
    ),
    createTestMessage(
      `${prefix}-${startIndex + 2}`,
      "Отличный стрим!",
      "Moderator",
      "#0000ff",
      false,
      true
    ),
  ];
}
