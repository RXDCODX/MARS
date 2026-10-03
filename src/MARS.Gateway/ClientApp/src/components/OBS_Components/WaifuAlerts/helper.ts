import { Husband, Waifu } from "@/shared/api";
import { getRandomInt } from "@/shared/Utils";

export function shuffleArray<T>(array: T[]): T[] {
  const shuffledArray = [...array];
  for (let index = shuffledArray.length - 1; index > 0; index--) {
    const index_ = Math.floor(Math.random() * (index + 1));
    [shuffledArray[index], shuffledArray[index_]] = [
      shuffledArray[index_],
      shuffledArray[index],
    ];
  }
  return shuffledArray;
}

export function getText(message: WaifuAlertProps) {
  let text: string;
  if (message.waifu.isAdded) {
    text = "ты добавил";
  } else if (message.waifu.isMerged) {
    text = "поженился с";
  } else {
    text = "тебе выпал(-а)";
  }
  return text + " " + message.waifu.name;
}

export function getTitle(message: WaifuAlertProps) {
  return message.waifu.anime
    ? `из аниме ${message.waifu.anime}`
    : `из манги ${message.waifu.manga}`;
}

export function getMergeCongratulationText(message: WaifuAlertProps): string {
  const husbandName =
    message.waifuHusband?.twitchUser?.displayName ?? message.displayName;
  const title = message.waifu.anime
    ? `аниме ${message.waifu.anime}`
    : `манги ${message.waifu.manga}`;
  return `${husbandName} поженился с ${message.waifu.name} из ${title}`;
}

export function getMarriageDisplayText(message: WaifuAlertProps): string {
  const title = message.waifu.anime
    ? `аниме ${message.waifu.anime}`
    : `манги ${message.waifu.manga}`;
  return `уже в браке с ${message.waifu.name} из ${title}`;
}

const startWords: string[] = [
  "Кстати",
  "К слову",
  "Между прочим",
  "Кстати сказать",
  "В самом деле",
  "Во всяком случае",
  "Фактически",
];

export function getMergeMarriageText(message: WaifuAlertProps): string {
  if (!message.waifuHusband?.whenPrivated) {
    return "";
  }

  const privatedDate = new Date(message.waifuHusband.whenPrivated);
  const now = new Date();
  const timeDiff = now.getTime() - privatedDate.getTime();

  const span = {
    days: Math.floor(timeDiff / (1000 * 60 * 60 * 24)),
    hours: Math.floor((timeDiff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)),
    minutes: Math.floor((timeDiff % (1000 * 60 * 60)) / (1000 * 60)),
    seconds: Math.floor((timeDiff % (1000 * 60)) / 1000),
  };

  return getMarriageDurationText(span, message.waifu);
}

function getMarriageDurationText(span: TimeSpan, waifu: Waifu): string {
  const totalDays = span.days;

  const years = Math.floor(totalDays / 365);
  let remainingDays = totalDays % 365;

  const months = Math.floor(remainingDays / 30);
  remainingDays %= 30;

  const weeks = Math.floor(remainingDays / 7);
  remainingDays %= 7;

  const parts = [
    years > 0
      ? `${years} ${getCorrectForm(years, "год", "года", "лет")}`
      : null,
    months > 0
      ? `${months} ${getCorrectForm(months, "месяц", "месяца", "месяцев")}`
      : null,
    weeks > 0
      ? `${weeks} ${getCorrectForm(weeks, "неделя", "недели", "недель")}`
      : null,
    remainingDays > 0
      ? `${remainingDays} ${getCorrectForm(remainingDays, "день", "дня", "дней")}`
      : null,
    span.hours > 0
      ? `${span.hours} ${getCorrectForm(span.hours, "час", "часа", "часов")}`
      : null,
    span.minutes > 0
      ? `${span.minutes} ${getCorrectForm(span.minutes, "минута", "минуты", "минут")}`
      : null,
    span.seconds > 0
      ? `${span.seconds} ${getCorrectForm(span.seconds, "секунда", "секунды", "секунд")}`
      : null,
  ].filter(Boolean);

  const durationText = parts.length > 0 ? parts.join(", ") : "только что";

  return `уже ${durationText}`;
}

export function getHusbandText(message: WaifuAlertProps) {
  if (
    !message.waifuHusband ||
    !message.waifu.isPrivated ||
    !message.waifuHusband.whenPrivated ||
    message.waifuHusband.waifuBrideId !== message.waifu.shikiId
  ) {
    return "";
  }

  // Если вайфу приватная, считаем время с момента приватизации
  const privatedDate = new Date(message.waifuHusband.whenPrivated);
  const now = new Date();
  const timeDiff = now.getTime() - privatedDate.getTime();

  const span = {
    days: Math.floor(timeDiff / (1000 * 60 * 60 * 24)),
    hours: Math.floor((timeDiff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)),
    minutes: Math.floor((timeDiff % (1000 * 60 * 60)) / (1000 * 60)),
    seconds: Math.floor((timeDiff % (1000 * 60)) / 1000),
  };

  const timeText = getTimeSpanText(span, message.waifu);
  const randomIndex = getRandomInt(0, startWords.length - 1);
  const startWord = startWords[randomIndex];

  return ` ${startWord}, ${timeText}`;
}

function getTimeSpanText(span: TimeSpan, waifu: Waifu): string {
  const totalDays = span.days;

  // Calculate years, months, weeks and remaining days
  const years = Math.floor(totalDays / 365);
  let remainingDays = totalDays % 365;

  const months = Math.floor(remainingDays / 30);
  remainingDays %= 30;

  const weeks = Math.floor(remainingDays / 7);
  remainingDays %= 7;

  // Form strings only for non-zero values
  const parts = [
    years > 0
      ? `${years} ${getCorrectForm(years, "год", "года", "лет")}`
      : null,
    months > 0
      ? `${months} ${getCorrectForm(months, "месяц", "месяца", "месяцев")}`
      : null,
    weeks > 0
      ? `${weeks} ${getCorrectForm(weeks, "неделя", "недели", "недель")}`
      : null,
    remainingDays > 0
      ? `${remainingDays} ${getCorrectForm(remainingDays, "день", "дня", "дней")}`
      : null,
    span.hours > 0
      ? `${span.hours} ${getCorrectForm(span.hours, "час", "часа", "часов")}`
      : null,
    span.minutes > 0
      ? `${span.minutes} ${getCorrectForm(span.minutes, "минута", "минуты", "минут")}`
      : null,
    span.seconds > 0
      ? `${span.seconds} ${getCorrectForm(span.seconds, "секунда", "секунды", "секунд")}`
      : null,
  ].filter(Boolean);

  const title = waifu.anime?.trim()
    ? " из аниме " + waifu.anime
    : " из манги " + waifu.manga;

  return `ты в браке с ${waifu.name}${title} уже ${parts.join(", ")}!`;
}

function getCorrectForm(
  number: number,
  form1: string,
  form2: string,
  form5: string
): string {
  number = Math.abs(number) % 100;
  const remainder = number % 10;

  if (number > 10 && number < 20) return form5;
  if (remainder > 1 && remainder < 5) return form2;
  if (remainder === 1) return form1;
  return form5;
}

export interface WaifuAlertProps {
  waifu: Waifu;
  displayName: string;
  waifuHusband?: Husband;
  isReminder?: boolean;
}

interface TimeSpan {
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
}
