import { getOverlayAdapter } from "@/shared/realtime/overlayHub";

let isBackgroundAudioMuted = false;

/**
 * Просит хаб заглушить прочие источники звука.
 *
 * Не компонент, а обычный модуль, поэтому хук `useHubInvoke` здесь неприменим:
 * хуку нужен вызов из тела компонента. Адаптер берётся из того же реестра
 * напрямую — иначе модуль ходил бы в другое соединение, чем подписки оверлея.
 *
 * Пока хаб не подключён, вызов не делается, и состояние не меняется: иначе
 * флаг «заглушено» встал бы в true при первом же вызове, и последующая
 * попытка включить звук ничего не сделала бы.
 */
export async function requestMuteOtherAudio() {
  if (isBackgroundAudioMuted) {
    return;
  }

  const adapter = getOverlayAdapter();

  if (adapter === null) {
    return;
  }

  try {
    await adapter.invoke("MuteAll");
    isBackgroundAudioMuted = true;
  } catch (error) {
    isBackgroundAudioMuted = false;
    throw error;
  }
}

export async function requestUnmuteOtherAudio() {
  if (!isBackgroundAudioMuted) {
    return;
  }

  const adapter = getOverlayAdapter();

  try {
    if (adapter !== null) {
      await adapter.invoke("UnmuteSessions");
    }
  } finally {
    isBackgroundAudioMuted = false;
  }
}

/** Заглушен ли сейчас прочий звук. Читается компонентом для показа плашки. */
export function isOtherAudioMuted(): boolean {
  return isBackgroundAudioMuted;
}
