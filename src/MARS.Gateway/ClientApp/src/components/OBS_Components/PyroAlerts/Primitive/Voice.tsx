import { useCallback, useEffect, useRef, useState } from "react";
import { Textfit } from "react-textfit";

import { TelegramusHubSignalRContext as SignalRContext } from "@/shared/api";
import { MediaDto } from "@/shared/api";

import styles from "./Media.module.scss";

declare global {
  interface Window {
    webkitAudioContext?: typeof AudioContext;
  }
}

interface Properties {
  mediaInfo: MediaDto;
  callback: () => void;
  isHighPrior?: boolean;
}

export function Voice({ mediaInfo, callback, isHighPrior }: Properties) {
  const { metaInfo, fileInfo, textInfo } = mediaInfo.mediaInfo;
  const bellSource = import.meta.env.VITE_BASE_PATH + "Alerts/bell.wav";
  const voiceSource = fileInfo.filePath;
  const imageSource = import.meta.env.VITE_BASE_PATH + textInfo.text;

  const [isBellPlayed, setIsBellPlayed] = useState(false);
  const bellAudioReference = useRef<HTMLAudioElement>(null);
  const voiceAudioReference = useRef<HTMLAudioElement>(null);
  const bellAudioContextReference = useRef<AudioContext | null>(null);
  const voiceAudioContextReference = useRef<AudioContext | null>(null);
  const bellGainNodeReference = useRef<GainNode | null>(null);
  const voiceGainNodeReference = useRef<GainNode | null>(null);
  const bellSourceNodeReference = useRef<MediaElementAudioSourceNode | null>(
    null
  );
  const voiceSourceNodeReference = useRef<MediaElementAudioSourceNode | null>(
    null
  );

  const unmuteAll = useCallback(() => {
    if (isHighPrior) {
      SignalRContext.invoke("UnmuteSessions");
    }
  }, [isHighPrior]);

  const error = useCallback(() => {
    unmuteAll();
    callback();
    throw new Error("Failed to play audio");
  }, [callback, unmuteAll]);

  const muteAll = useCallback(() => {
    if (isHighPrior) {
      SignalRContext.invoke("MuteAll", []);
    }
  }, [isHighPrior]);

  const isFreezeRequired = mediaInfo.mediaInfo.metaInfo.isFreezeRequired;

  const freeze = useCallback(() => {
    if (isFreezeRequired) {
      SignalRContext.invoke("ObsFreeze");
    }
  }, [isFreezeRequired]);

  const unfreeze = useCallback(() => {
    if (isFreezeRequired) {
      SignalRContext.invoke("ObsUnfreeze");
    }
  }, [isFreezeRequired]);

  const setupBellAudioContext = useCallback(() => {
    if (!bellAudioReference.current) return;

    try {
      // Создаем AudioContext если его еще нет
      if (!bellAudioContextReference.current) {
        const AudioContextClass =
          globalThis.AudioContext || globalThis.webkitAudioContext;
        if (AudioContextClass) {
          bellAudioContextReference.current = new AudioContextClass();
        } else {
          throw new Error("AudioContext is not supported in this browser");
        }
      }

      // Создаем источник из audio элемента
      if (!bellSourceNodeReference.current) {
        bellSourceNodeReference.current =
          bellAudioContextReference.current.createMediaElementSource(
            bellAudioReference.current
          );
      }

      // Создаем GainNode если его еще нет
      if (!bellGainNodeReference.current) {
        bellGainNodeReference.current =
          bellAudioContextReference.current.createGain();
      }

      // Подключаем цепочку: источник -> GainNode -> выход
      bellSourceNodeReference.current.connect(bellGainNodeReference.current);
      bellGainNodeReference.current.connect(
        bellAudioContextReference.current.destination
      );

      // Устанавливаем громкость (поддерживаем значения больше 100%)
      const volume = metaInfo.volume / 100;
      bellGainNodeReference.current.gain.value = volume;
    } catch (error) {
      console.warn("Failed to setup AudioContext for bell:", error);
    }
  }, [metaInfo.volume]);

  const setupVoiceAudioContext = useCallback(() => {
    if (!voiceAudioReference.current) return;

    try {
      // Создаем AudioContext если его еще нет
      if (!voiceAudioContextReference.current) {
        const AudioContextClass =
          globalThis.AudioContext || globalThis.webkitAudioContext;
        if (AudioContextClass) {
          voiceAudioContextReference.current = new AudioContextClass();
        } else {
          throw new Error("AudioContext is not supported in this browser");
        }
      }

      // Создаем источник из audio элемента
      if (!voiceSourceNodeReference.current) {
        voiceSourceNodeReference.current =
          voiceAudioContextReference.current.createMediaElementSource(
            voiceAudioReference.current
          );
      }

      // Создаем GainNode если его еще нет
      if (!voiceGainNodeReference.current) {
        voiceGainNodeReference.current =
          voiceAudioContextReference.current.createGain();
      }

      // Подключаем цепочку: источник -> GainNode -> выход
      voiceSourceNodeReference.current.connect(voiceGainNodeReference.current);
      voiceGainNodeReference.current.connect(
        voiceAudioContextReference.current.destination
      );

      // Устанавливаем громкость (поддерживаем значения больше 100%)
      const volume = metaInfo.volume / 100;
      voiceGainNodeReference.current.gain.value = volume;
    } catch (error) {
      console.warn("Failed to setup AudioContext for voice:", error);
    }
  }, [metaInfo.volume]);

  // Очистка ресурсов при размонтировании
  useEffect(
    () => () => {
      if (bellAudioContextReference.current) {
        bellAudioContextReference.current.close();
      }
      if (voiceAudioContextReference.current) {
        voiceAudioContextReference.current.close();
      }
    },
    []
  );

  return (
    <>
      {!isBellPlayed && (
        <audio
          ref={bellAudioReference}
          autoPlay
          src={bellSource}
          onCanPlay={e => {
            // Настраиваем AudioContext и GainNode
            setupBellAudioContext();

            // Устанавливаем базовую громкость на 1.0, так как реальная громкость контролируется GainNode
            e.currentTarget.volume = 1;
          }}
          onEnded={() => setIsBellPlayed(true)}
          onError={() => {
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to play VOICE path=${voiceSource}`
            );
            error();
          }}
          onErrorCapture={() => {
            SignalRContext.invoke(
              "LogError",
              `RandomMem: failed to play VOICE path=${voiceSource}`
            );
            error();
          }}
          onCanPlayThrough={() => {
            muteAll();
            freeze();
          }}
        />
      )}
      {isBellPlayed && (
        <audio
          ref={voiceAudioReference}
          autoPlay
          src={voiceSource}
          onCanPlay={e => {
            // Настраиваем AudioContext и GainNode
            setupVoiceAudioContext();

            // Устанавливаем базовую громкость на 1.0, так как реальная громкость контролируется GainNode
            e.currentTarget.volume = 1;
          }}
          onEnded={() => {
            unmuteAll();
            unfreeze();
            callback();
          }}
          onError={() => error()}
          onErrorCapture={() => error()}
        />
      )}
      {
        <div className={styles.container} data-testid="pyro-alert-voice">
          <div className={styles.block}>
            <Textfit forceSingleModeWidth max={2000} min={1}>
              всем тихо
            </Textfit>
          </div>
          <div className={styles.block_image}>
            <img src={import.meta.env.VITE_BASE_PATH + "Alerts/mute.png"} />
          </div>
          <div className={styles.block}>
            <Textfit forceSingleModeWidth max={2000} min={1}>
              Сейчас говорит <img className="emote" src={imageSource} />
              {metaInfo.displayName}
            </Textfit>
          </div>
        </div>
      }
    </>
  );
}
