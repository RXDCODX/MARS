/**
 * Участники MikuMikuBeam в форме, которой живёт экран.
 *
 * Экран написан под `TwitchUser` из сгенерированного контракта и читает
 * `displayName`, `profileImageUrl` и `chatColor`. На проводе же едет совсем
 * другое: `MikuMikuBeamHandler` кладёт в список только `TwitchId`, потому что
 * больше у него ничего и нет.
 *
 * Из-за этого в ветке без аватарки вызывался `user.displayName.charAt(0)` на
 * `undefined` — компонент падал в рендере, а `ErrorBoundary` в приложение не
 * смонтирован, то есть белый экран целиком. Данных для полноценной карточки
 * здесь нет и быть не может, поэтому подставляется то, что есть: идентификатор
 * Twitch. Приводить надо на границе разбора, а не в разметке: падать должен
 * разбор, а не рендер.
 */
export interface BeamUser {
  twitchId: string;
  /**
   * Подпись на экране. На проводе её нет, поэтому берётся идентификатор: пустая
   * подпись выглядела бы как «участник без имени».
   */
  displayName: string;
  /** Заглушка аватарки: первая буква подписи, а при пустой — «?». */
  initial: string;
  chatColor: string;
  /**
   * Аватар на проводе не приходит, но ветка отрисовки остаётся: сервер вправе
   * начать его класть, и тогда экран подхватит это без правок. Пока поля нет,
   * `undefined` — и экран рисует заглушку.
   */
  profileImageUrl?: string;
}

const asRecord = (value: unknown): Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {};

/**
 * Участник луча из того, что реально приехало.
 *
 * Поля, которых на проводе нет, не выдумываются: возвращается то, что есть, и
 * `initial`, из которого экран рисует заглушку. Пустой `twitchId` отбрасывается
 * — такой участник не показывался бы вообще, и на его месте зритель увидел бы
 * пустую плашку.
 */
export const normalizeBeamUser = (value: unknown): BeamUser | null => {
  const wire = asRecord(value);

  const twitchId =
    typeof wire.twitchId === "string"
      ? wire.twitchId
      : typeof wire.userId === "string"
        ? wire.userId
        : "";

  if (twitchId.length === 0) {
    return null;
  }

  return {
    twitchId,
    displayName: twitchId,
    initial: twitchId.charAt(0).toUpperCase(),
    chatColor: typeof wire.chatColor === "string" ? wire.chatColor : "#FFFFFF",
    profileImageUrl:
      typeof wire.profileImageUrl === "string"
        ? wire.profileImageUrl
        : undefined,
  };
};

/**
 * Весь список участников: без пустых, без мусора вместо объектов.
 *
 * Фильтрация обязательна и здесь: пустой список на экране означает «никого не
 * было», а список из пустых плашек — «что-то сломалось», и выглядит это
 * одинаково.
 */
export const normalizeBeamUsers = (value: unknown): BeamUser[] => {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map(normalizeBeamUser)
    .filter((user): user is BeamUser => user !== null);
};
