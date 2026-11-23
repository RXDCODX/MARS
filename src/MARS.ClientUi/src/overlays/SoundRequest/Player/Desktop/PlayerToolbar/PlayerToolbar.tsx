import { Spin } from "antd";
import {
  CSSProperties,
  memo,
  type ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useShallow } from "zustand/react/shallow";

import { PlayerStateStateEnum, SpotifyAuth } from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { RootState } from "@/shared/api/http-clients/RootState";
import type { SpotifyAuthStatusResult } from "@/shared/api/types/data-contracts";
import type { OperationResult } from "@/shared/types/OperationResult";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { usePlayerStore } from "../../stores/usePlayerStore";
import { parseDurationToSeconds } from "../../utils";
import styles from "../SoundRequestPlayerDesktop.module.scss";
import { useTrackProgress } from "../useTrackProgress";
import { VolumeSlider } from "../VolumeSlider";
import {
  AddTrackButton,
  MuteButton,
  PlayPauseButton,
  PreviousButton,
  SkipButton,
  StopButton,
  VideoStateButton,
} from "./Buttons";
import { ViewModeToggle } from "./ViewModeToggle";

const SOUND_REQUEST_PROVIDER_KEY = "SoundRequestProvider";
const PROVIDER_YOUTUBE = "YouTube";
const PROVIDER_SPOTIFY = "Spotify";
const PROVIDER_ICONS = {
  [PROVIDER_YOUTUBE]: "/icons/providers/youtube.svg",
  [PROVIDER_SPOTIFY]: "/icons/providers/spotify.svg",
} as const;

type ProviderName = typeof PROVIDER_YOUTUBE | typeof PROVIDER_SPOTIFY;

const parseProvider = (value?: string): ProviderName => {
  if (value?.trim().toLowerCase() === PROVIDER_SPOTIFY.toLowerCase()) {
    return PROVIDER_SPOTIFY;
  }

  return PROVIDER_YOUTUBE;
};

function PlayerToolbarComponent() {
  const { showToast } = useToastModal();
  const spotifyAuthApi = useMemo(() => new SpotifyAuth(), []);

  const { state, trackId, trackDuration, currentTrackProgress } =
    usePlayerStore(
      useShallow(storeState => ({
        state: storeState.playerState?.state,
        trackId: storeState.playerState?.currentQueueItem?.track?.id,
        trackDuration:
          storeState.playerState?.currentQueueItem?.track?.duration || "PT0S",
        currentTrackProgress: storeState.playerState?.currentTrackProgress,
      }))
    );

  const [provider, setProvider] = useState<ProviderName>(PROVIDER_YOUTUBE);
  const [isLoadingProvider, setIsLoadingProvider] = useState(true);
  const [isSavingProvider, setIsSavingProvider] = useState(false);

  const loadProvider = useCallback(async () => {
    setIsLoadingProvider(true);

    try {
      const client = new RootState(defaultApiConfig);
      const response = await client.rootStateDetail(SOUND_REQUEST_PROVIDER_KEY);
      const operation = response.data;

      if (operation.success) {
        setProvider(parseProvider(operation.data?.value));
      } else {
        setProvider(PROVIDER_YOUTUBE);
      }
    } catch {
      setProvider(PROVIDER_YOUTUBE);
    } finally {
      setIsLoadingProvider(false);
    }
  }, []);

  useEffect(() => {
    void loadProvider();
  }, [loadProvider]);

  const toggleProvider = useCallback(async () => {
    const nextProvider =
      provider === PROVIDER_YOUTUBE ? PROVIDER_SPOTIFY : PROVIDER_YOUTUBE;

    setIsSavingProvider(true);
    try {
      if (nextProvider === PROVIDER_SPOTIFY) {
        const statusResponse = await spotifyAuthApi.spotifyAuthStatusList();
        const statusOperation =
          statusResponse.data as OperationResult<SpotifyAuthStatusResult>;

        const isSpotifyLinked =
          statusOperation.success && Boolean(statusOperation.data?.isLinked);
        if (!isSpotifyLinked) {
          showToast({
            success: false,
            message:
              "Spotify аккаунт не подключен. Подключите его в админ-панели: /spotify",
          });
          return;
        }
      }

      const client = new RootState(defaultApiConfig);
      const response = await client.rootStateCreate({
        name: SOUND_REQUEST_PROVIDER_KEY,
        value: nextProvider,
        description: "Активный провайдер SoundRequest (YouTube/Spotify)",
        typeDescription: "enum: SoundRequestProvider",
      });
      const operation = response.data;
      if (operation.success) {
        setProvider(nextProvider);
        showToast({
          success: true,
          message: `Провайдер переключен: ${nextProvider}`,
        });
      } else {
        showToast({
          success: false,
          message: operation.message ?? "Не удалось переключить провайдер",
        });
      }
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Ошибка при переключении провайдера";
      showToast({ success: false, message });
    } finally {
      setIsSavingProvider(false);
    }
  }, [provider, showToast, spotifyAuthApi]);

  const isPlaying = state === PlayerStateStateEnum.Playing;
  const durationSec = parseDurationToSeconds(trackDuration);
  const progress = useTrackProgress({
    durationSec,
    isPlaying,
    trackId,
    initialProgress: currentTrackProgress,
  });

  const progressStyle = {
    "--track-progress": `${Math.round(progress * 100)}%`,
  } as CSSProperties;

  const isProviderButtonDisabled = isLoadingProvider || isSavingProvider;
  const providerIconUrl = PROVIDER_ICONS[provider];
  const providerColorClass =
    provider === PROVIDER_SPOTIFY
      ? styles.spotifyProvider
      : styles.youtubeProvider;
  const providerButtonClassName = [
    styles.tbBtn,
    styles.providerToggleButton,
    providerColorClass,
  ].join(" ");

  let providerButtonContent: ReactNode;
  providerButtonContent = isProviderButtonDisabled ? (
    <span className={styles.providerButtonContent}>
      <Spin size="small" />
      <span>{provider}</span>
    </span>
  ) : (
    <span className={styles.providerButtonContent}>
      <img
        src={providerIconUrl}
        alt={`${provider} icon`}
        className={styles.providerIcon}
        loading="lazy"
      />
      <span>{provider}</span>
    </span>
  );

  return (
    <div
      className={styles.toolbar}
      style={progressStyle}
      data-testid="player-toolbar"
    >
      <div className={styles.toolbarInner}>
        <div className={styles.leftSection}>
          <ViewModeToggle />
          <AddTrackButton />
          <button
            type="button"
            className={providerButtonClassName}
            onClick={() => void toggleProvider()}
            disabled={isProviderButtonDisabled}
            title={`Провайдер SoundRequest: ${provider}`}
            data-testid="button-provider-toggle"
          >
            {providerButtonContent}
          </button>
        </div>

        <div className={styles.controlButtons}>
          <PreviousButton />
          <PlayPauseButton />
          <StopButton />
          <SkipButton />
          <MuteButton />
          <VideoStateButton />
        </div>

        <div className={styles.volumeWrap}>
          <VolumeSlider />
        </div>
      </div>
    </div>
  );
}

export const PlayerToolbar = memo(PlayerToolbarComponent);
