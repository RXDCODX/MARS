import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api";

let isBackgroundAudioMuted = false;

export async function requestMuteOtherAudio() {
  if (isBackgroundAudioMuted) {
    return;
  }

  try {
    await SignalRContext.invoke("MuteAll", []);
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

  try {
    await SignalRContext.invoke("UnmuteSessions");
  } finally {
    isBackgroundAudioMuted = false;
  }
}
