/**
 * Разбор ветки `HighliteEvent`.
 *
 * Ветка — это `{ message_json, color, face_url_json }`, и в `message_json` сервер
 * кладёт **обычную строку**: `notifier.Highlite(messageText, color, image)`
 * передаёт текст подсвеченного сообщения, а не объект чата. Экран же делал
 * `decoded as ChatMessage` и читал `.displayName`, `.message` и `.id` — на строке
 * всё это `undefined`, то есть пустая плашка и `id={undefined}` в разметке.
 *
 * Это единственный экран из семейства чатов, который не переведён на
 * `normalizeChatMessage`: там на проводе объект, здесь строка, и приводить надо
 * к форме экрана, а не наоборот.
 *
 * Ника на проводе нет вовсе, поэтому подставлять его неоткуда: раздутый заголовок
 * с пустым именем был бы враньём на экране. Рендерится сам текст.
 */
export interface HighliteMessage {
  /** Идентификатор для ключа и `id`: на проводе его нет, берётся из позиции. */
  id: string;
  /** Текст подсвеченного сообщения. */
  text: string;
  /** Цвет плашки: приходит на проводе, в отличие от остального. */
  color: string;
}

/**
 * Текст подсвеченного сообщения из содержимого ветки.
 *
 * Пустая строка и мусор отбрасываются: плашка без текста — это то же
 * «залипшее» сообщение, от которого этот экран уже страдал.
 */
export const readHighliteText = (decoded: unknown): string | null => {
  if (typeof decoded === "string") {
    return decoded.length > 0 ? decoded : null;
  }

  // Форма объекта тоже принимается: если сервер когда-нибудь начнёт класть
  // сообщение чата вместо строки, экран не превратится в пустую плашку, а
  // покажет текст.
  if (
    decoded !== null &&
    typeof decoded === "object" &&
    !Array.isArray(decoded)
  ) {
    const text = (decoded as { message?: unknown }).message;

    if (typeof text === "string" && text.length > 0) {
      return text;
    }
  }

  return null;
};
