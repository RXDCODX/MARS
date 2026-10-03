/**
 * Имена методов оверлейного хаба.
 *
 * Порядок и написание совпадают с `overlay-hub.manifest.json` в
 * `src/MARS.Alerts/Hubs` — тем же списком, который сверяется тестом на C# с
 * интерфейсом `ITelegramusHub` и с ветками `oneof` из `telegramus.proto`.
 *
 * Почему список здесь, а не генерируется из proto: браузер по gRPC не говорит и
 * proto в бандл не тащит. Имя метода приходит строкой в вызове SignalR, и
 * единственное, что удерживает эту строку от расхождения с сервером, —
 * сверка с манифестом. Она выполняется в vitest-тесте ниже.
 */
export const OVERLAY_EVENT_NAMES = [
  "Alert",
  "Alerts",
  "WaifuRoll",
  "AddNewWaifu",
  "ShowCurrentWife",
  "MergeWaifu",
  "UpdateWaifuPrizes",
  "FumoFriday",
  "NewMessage",
  "DeleteMessage",
  "Highlite",
  "PostTwitchInfo",
  "MakeScreenParticles",
  "MakeScreenEmojisParticles",
  "RandomMem",
  "AutoMessage",
  "Adhd",
  "Explosion",
  "LeroyAlert",
  "GaoAlert",
  "Credits",
  "MichaelJackson",
  "MikuMonday",
  "MikuMikuBeam",
  "PhonkEdit",
  "TikTokEdit",
  "AllRefund",
  "AudioQuizStart",
  "AudioQuizStop",
  "FumoRoll",
  "UpdateFumoPrizes",
  "FrogRoll",
  "UpdateFrogPrizes",
  "MikuRoll",
  "UpdateMikuPrizes",
  "AdhdConfig",
] as const;

/** Имя одного из 36 методов хаба. Вне списка — ошибка компиляции. */
export type OverlayEventName = (typeof OVERLAY_EVENT_NAMES)[number];

/**
 * Полезная нагрузка события.
 *
 * Раньше полезная нагрузка приходила как `object` из gRPC и на клиенте была
 * `any`: форму события нельзя было ни проверить при разработке, ни описать в
 * моке. Здесь — `unknown`, потому что реальные типы сообщений живут в
 * protobuf-контракте и в бандл не попадают; их подстановка — задача
 * protoc-gen-es, отдельным шагом. `unknown` честнее `any`: он запрещает
 * обращаться к полям, не зная их формы, вместо того чтобы тихо вернуть
 * `undefined`.
 */
export type OverlayPayload = unknown;

/**
 * Аргументы события. Шесть методов хаба не принимают ничего: это ветки `oneof`
 * с `EmptyEvent`, и клиенту нечего в них передавать.
 */
export type OverlayEventArgs = {
  Alert: [payload: OverlayPayload];
  Alerts: [payload: OverlayPayload];
  WaifuRoll: [payload: OverlayPayload];
  AddNewWaifu: [payload: OverlayPayload];
  ShowCurrentWife: [payload: OverlayPayload];
  MergeWaifu: [payload: OverlayPayload];
  UpdateWaifuPrizes: [payload: OverlayPayload];
  FumoFriday: [payload: OverlayPayload];
  NewMessage: [payload: OverlayPayload];
  DeleteMessage: [payload: OverlayPayload];
  Highlite: [payload: OverlayPayload];
  PostTwitchInfo: [payload: OverlayPayload];
  MakeScreenParticles: [payload: OverlayPayload];
  MakeScreenEmojisParticles: [payload: OverlayPayload];
  RandomMem: [payload: OverlayPayload];
  AutoMessage: [payload: OverlayPayload];
  Adhd: [payload: OverlayPayload];
  Explosion: [];
  LeroyAlert: [];
  GaoAlert: [payload: OverlayPayload];
  Credits: [];
  MichaelJackson: [];
  MikuMonday: [payload: OverlayPayload];
  MikuMikuBeam: [payload: OverlayPayload];
  PhonkEdit: [];
  TikTokEdit: [payload: OverlayPayload];
  AllRefund: [payload: OverlayPayload];
  AudioQuizStart: [payload: OverlayPayload];
  AudioQuizStop: [];
  FumoRoll: [payload: OverlayPayload];
  UpdateFumoPrizes: [payload: OverlayPayload];
  FrogRoll: [payload: OverlayPayload];
  UpdateFrogPrizes: [payload: OverlayPayload];
  MikuRoll: [payload: OverlayPayload];
  UpdateMikuPrizes: [payload: OverlayPayload];
  AdhdConfig: [payload: OverlayPayload];
};

/**
 * Карта обработчиков: на каждое событие — ровно один обработчик.
 *
 * Тип с mapped-конструкцией, поэтому пропущенное событие не собирается: с
 * первом шаге в TypeScript видно, что для добавленного события забыли обработчик.
 * С обычным `Record<string, fn>` этого не было бы.
 */
export type OverlayHandlers = {
  [K in OverlayEventName]: (...args: OverlayEventArgs[K]) => void;
};
