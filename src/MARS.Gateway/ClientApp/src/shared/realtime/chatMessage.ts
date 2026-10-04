import type { ChatMessage } from "@/shared/api/types/data-contracts";

/**
 * Приведение сообщения чата с провода к форме, которой живут экраны.
 *
 * Экраны `/chatv` и `/chath` написаны под `ChatMessage` из Twurple — это их
 * контракт, и переписывать их под `ChatMessageEvent` незачем. Но на проводе
 * едет не `ChatMessage`, а `ChatMessageEvent` из `MARS.Shared`: имена полей
 * другие, и вложенности `userDetail` нет вовсе.
 *
 * Из-за этого без приведения плашка ника оставалась пустой, цвет всегда был
 * белым, а рамки VIP и модератора не появлялись: `displayName` давал
 * `undefined`, `hexColor` — то же, `userDetail` не существовало. Текст при этом
 * показывался, потому что `message` — единственное поле, совпавшее.
 *
 * Приведение делается здесь, на границе разбора, а не в компонентах: знать о
 * форме провода должны ровно два места, а не десять экранов.
 */
export const normalizeChatMessage = (
  decoded: unknown,
  branchId: string | undefined
): ChatMessage | null => {
  if (
    decoded === null ||
    typeof decoded !== "object" ||
    Array.isArray(decoded)
  ) {
    return null;
  }

  const wire = decoded as Record<string, unknown>;

  const message = wire.message;

  // Сообщение без текста показывать нечем, а показывать пустую плашку вредно.
  if (typeof message !== "string") {
    return null;
  }

  const userId = typeof wire.userId === "string" ? wire.userId : "";
  const timestamp = typeof wire.timestamp === "string" ? wire.timestamp : "";

  return {
    ...(wire as unknown as ChatMessage),

    id: branchId ?? userId,
    displayName: typeof wire.userName === "string" ? wire.userName : "",
    hexColor: typeof wire.chatColor === "string" ? wire.chatColor : "white",
    message,
    // `isVip` и `isModerator` в `ChatMessage` живут только внутри `userDetail` —
    // верхнего уровня таких полей в контракте нет. На проводе, наоборот, всё
    // лежит наверху, поэтому разбор обязан собрать `userDetail` сам.
    isBroadcaster: wire.isBroadcaster === true,
    // Состав ровно из `UserDetail` из контракта: экраны читают только `isVip` и
    // `isModerator`, а дописывать поля, которых в типе нет, значило бы
    // описывать несуществующую форму.
    userDetail: {
      hasTurbo: false,
      isModerator: wire.isModerator === true,
      isPartner: false,
      isStaff: false,
      isSubscriber: false,
      isVip: wire.isVip === true,
    },
    customRewardId:
      typeof wire.customRewardId === "string" ? wire.customRewardId : undefined,

    // Остальных полей на проводе нет, а экраны их читают. Значения по
    // умолчанию взяты из `ChatMessage`, чтобы отсутствие выглядело как
    // «обычное сообщение», а не как частично заполненный объект.
    badges: [],
    badgeInfo: [],
    channel: "",
    tmiSent: timestamp,
  } as ChatMessage;
};
