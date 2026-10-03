import { motion, useAnimationControls } from "framer-motion";
import {
  lazy,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import {
  FollowerInfo,
  RxdcodxViewers,
  TelegramusHubSignalRContext,
} from "@/shared/api";
import {
  allReactBitsBackgroundNames,
  reactBitsBackgroundComponentRegistry,
} from "@/shared/components/ReactBitsBackgroundsLegacy/registry";
import { useTwitchStore } from "@/shared/twitchStore/twitchStore";
import Announce from "@/shared/Utils/Announce/Announce";

import styles from "./Credits.module.scss";

// ОПТИМИЗАЦИЯ: Lazy loading для тяжелых анимаций - загружаем только когда нужно
const ElectricBorder = lazy(
  () => import("@/shared/components/ElectricBorderLegacy")
);

// ОПТИМИЗАЦИЯ: Загружаем музыку НЕ eager - только когда нужно
const musicFiles = import.meta.glob("./music/*.{mp3,ogg,wav}", {
  eager: false,
  query: "url",
  import: "default",
}) as Record<string, () => Promise<string>>;

// Получаем URLs музыки
const musicUrls = Object.keys(musicFiles).sort();

// Ключ для сохранения индекса трека между запусками
const MUSIC_INDEX_STORAGE_KEY = "credits-music-index";

// Локально завендоренный каталог всех ts-default background-компонентов react-bits
const backgroundConfigs = allReactBitsBackgroundNames.map(name => {
  const BackgroundComponent = reactBitsBackgroundComponentRegistry[name];

  return {
    name,
    element: (
      <Suspense fallback={<div />}>
        <BackgroundComponent />
      </Suspense>
    ),
  };
});

// Импортируем иконки через import.meta.glob
const iconFiles = import.meta.glob("./icons/*.png", {
  eager: true,
  query: "url",
  import: "default",
}) as Record<string, string>;

// Создаем объект с иконками
const icons = {
  follower: iconFiles["./icons/follower.png"],
  mods: iconFiles["./icons/mods.png"],
  streamer: iconFiles["./icons/streamer.png"],
  vip: iconFiles["./icons/vip.png"],
};

const SectionTitle: React.FC<{
  children: React.ReactNode;
  leftIcon?: string;
  rightIcon?: string;
}> = ({ children, leftIcon, rightIcon }) => (
  <div className={styles.sectionTitle}>
    {leftIcon && <img src={leftIcon} alt="" className={styles.sectionIcon} />}
    <span className={styles.sectionText}>{children}</span>
    {rightIcon && <img src={rightIcon} alt="" className={styles.sectionIcon} />}
  </div>
);

const NameRow: React.FC<{ follower: FollowerInfo }> = ({ follower }) => {
  const textColor = follower.twitchUser?.chatColor || "#FFFFFF";
  const textShadow = follower.twitchUser?.chatColor
    ? "1px 1px 2px rgba(0, 0, 0, 0.8)"
    : "1px 1px 2px rgba(0, 0, 0, 1), -1px -1px 2px rgba(0, 0, 0, 1), 1px -1px 2px rgba(0, 0, 0, 1), -1px 1px 2px rgba(0, 0, 0, 1)";

  return (
    <div className={styles.nameRow}>
      {follower.twitchUser?.profileImageUrl && (
        <img
          src={follower.twitchUser.profileImageUrl}
          alt={follower.twitchUser.displayName}
          className={styles.profileImage}
        />
      )}
      <div
        className={styles.nameText}
        style={{
          color: textColor,
          textShadow: textShadow,
        }}
      >
        {follower.twitchUser?.displayName ?? follower.userId}
      </div>
    </div>
  );
};

const Credits: React.FC = () => {
  const api = useMemo(() => new RxdcodxViewers(), []);
  const controls = useAnimationControls();
  const { getStreamerInfo, getStreamerChatColor } = useTwitchStore();

  const [moderators, setModerators] = useState<FollowerInfo[]>([]);
  const [vips, setVips] = useState<FollowerInfo[]>([]);
  const [followers, setFollowers] = useState<FollowerInfo[]>([]);
  const [contentReady, setContentReady] = useState(false);
  const [isActive, setIsActive] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [announced, setAnnounced] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);
  const [streamerInfo, setStreamerInfo] = useState<{
    displayName: string;
    profileImageUrl: string;
    chatColor: string;
  } | null>(null);

  const [selectedBackground, setSelectedBackground] = useState<{
    name: string;
    element: React.ReactElement;
  } | null>(null);

  const containerReference = useRef<HTMLDivElement | null>(null);
  const animationTimeoutReference = useRef<number | null>(null);
  const contentReadyReference = useRef(false);
  const runIdReference = useRef(0);

  // Аудио и WebAudio для плавного затухания
  const audioReference = useRef<HTMLAudioElement | null>(null);
  const audioContextReference = useRef<AudioContext | null>(null);
  const gainNodeReference = useRef<GainNode | null>(null);
  const sourceNodeReference = useRef<MediaElementAudioSourceNode | null>(null);
  const fadeTimeoutReference = useRef<number | null>(null);
  const selectedTrackUrlReference = useRef<string | null>(null);

  const ensureAudioGraph = useCallback(() => {
    if (!audioReference.current) return;
    if (!audioContextReference.current) {
      type WindowWithWebkitAC = typeof window & {
        webkitAudioContext?: typeof AudioContext;
      };
      const AudioContextCtor =
        (globalThis as WindowWithWebkitAC).webkitAudioContext ?? AudioContext;
      audioContextReference.current = new AudioContextCtor();
    }
    const context = audioContextReference.current;

    if (!gainNodeReference.current) {
      gainNodeReference.current = context.createGain();
      gainNodeReference.current.gain.setValueAtTime(1, context.currentTime);
    }

    if (!sourceNodeReference.current && audioReference.current) {
      try {
        sourceNodeReference.current = context.createMediaElementSource(
          audioReference.current
        );
      } catch {
        // Источник уже создан для данного элемента, игнорируем
      }
    }

    // Переподключаем граф
    try {
      // Отключаем прежние соединения
      sourceNodeReference.current?.disconnect();
    } catch {
      // ignore
    }
    try {
      gainNodeReference.current?.disconnect();
    } catch {
      // ignore
    }

    if (sourceNodeReference.current && gainNodeReference.current) {
      sourceNodeReference.current.connect(gainNodeReference.current);
      gainNodeReference.current.connect(context.destination);
    }
  }, []);

  const stopAndCleanupAudio = useCallback(() => {
    if (fadeTimeoutReference.current) {
      globalThis.clearTimeout(fadeTimeoutReference.current);
      fadeTimeoutReference.current = null;
    }
    if (audioReference.current) {
      try {
        audioReference.current.pause();
        audioReference.current.currentTime = 0;
        audioReference.current.src = "";
      } catch {
        // ignore
      }
    }
    // Возвращаем громкость к 1 на всякий случай
    if (audioContextReference.current && gainNodeReference.current) {
      gainNodeReference.current.gain.cancelScheduledValues(
        audioContextReference.current.currentTime
      );
      gainNodeReference.current.gain.setValueAtTime(
        1,
        audioContextReference.current.currentTime
      );
    }
  }, []);

  const selectNextMusicUrl = useCallback(async (): Promise<string | null> => {
    if (musicUrls.length === 0) return null;
    let storedIndex = Number(localStorage.getItem(MUSIC_INDEX_STORAGE_KEY));
    if (Number.isNaN(storedIndex) || storedIndex < 0) storedIndex = -1;
    const nextIndex = (storedIndex + 1) % musicUrls.length;
    localStorage.setItem(MUSIC_INDEX_STORAGE_KEY, String(nextIndex));

    // ОПТИМИЗАЦИЯ: Загружаем только нужный музыкальный файл
    const musicKey = musicUrls[nextIndex];
    const musicLoader = musicFiles[musicKey];
    return musicLoader ? await musicLoader() : null;
  }, []);

  const selectRandomBackground = useCallback(() => {
    if (backgroundConfigs.length === 0) return;

    // В режиме разработки используем только 4 конкретных фона
    const developmentBackgrounds = new Set([
      "LightPillar",
      "FloatingLines",
      "ColorBends",
      "GridScan",
    ]);
    const availableBackgrounds = import.meta.env.DEV
      ? backgroundConfigs.filter(bg => developmentBackgrounds.has(bg.name))
      : backgroundConfigs;

    if (availableBackgrounds.length === 0) return;

    const randomIndex = Math.floor(Math.random() * availableBackgrounds.length);
    const selected = availableBackgrounds[randomIndex];
    console.log("Выбран фон для титров:", selected.name);
    setSelectedBackground(selected);
  }, []);

  const playSelectedTrack = useCallback(
    async (url: string) => {
      if (!audioReference.current) return;
      ensureAudioGraph();
      audioReference.current.src = url;
      audioReference.current.loop = false;
      audioReference.current.muted = false;
      audioReference.current.preload = "auto";
      try {
        await audioReference.current.play();
      } catch {
        // Автоплей может запретиться — пробуем с muted
        audioReference.current.muted = true;
        // Снимем mute как только можно
        setTimeout(() => {
          if (audioReference.current) audioReference.current.muted = false;
        }, 200);
      }
    },
    [ensureAudioGraph]
  );

  // Ожидание загрузки всех изображений внутри контейнера
  const waitImagesLoaded = useCallback(async () => {
    if (!containerReference.current) return;
    const images = [...containerReference.current.querySelectorAll("img")];
    if (images.length === 0) return;
    await Promise.all(
      images.map(img => {
        const image = img as HTMLImageElement;
        if (image.complete) return Promise.resolve(undefined);
        return new Promise<void>(resolve => {
          image.addEventListener("load", () => resolve());
          image.onerror = () => resolve();
        });
      })
    );
  }, []);

  // Функция для загрузки информации о стримере
  const loadStreamerInfo = useCallback(async () => {
    try {
      // ID стримера rxdcodx (785975641)
      const streamerId = "785975641";
      const [streamerData, chatColor] = await Promise.all([
        getStreamerInfo(streamerId),
        getStreamerChatColor(streamerId),
      ]);

      if (streamerData) {
        setStreamerInfo({
          displayName: streamerData.displayName,
          profileImageUrl: streamerData.profilePictureUrl,
          chatColor: chatColor || "#FFFFFF",
        });
        console.log("Информация о стримере загружена:", {
          displayName: streamerData.displayName,
          chatColor: chatColor || "#FFFFFF",
        });
      }
    } catch (error) {
      console.error("Ошибка загрузки информации о стримере:", error);
    }
  }, [getStreamerInfo, getStreamerChatColor]);

  // Функция для загрузки данных титров
  const loadCreditsData = useCallback(async () => {
    if (isLoading) {
      console.log("Загрузка уже идет, пропускаем...");
      return;
    }

    console.log("Начинаем загрузку данных титров...");
    setIsLoading(true);
    setContentReady(false);

    try {
      // Загружаем данные параллельно
      const [allRes] = await Promise.all([
        api.rxdcodxViewersAllList({ forceUseCash: true }),
        loadStreamerInfo(),
      ]);

      const allData = allRes.data.data ?? [];

      // Фильтруем данные по типам
      const moderatorsData = allData.filter(
        user => user.twitchUser?.isModerator
      );
      const vipsData = allData.filter(user => user.twitchUser?.isVip);
      const followersData = allData.filter(
        user => !user.twitchUser?.isModerator && !user.twitchUser?.isVip
      );

      setModerators(moderatorsData);
      setVips(vipsData);
      setFollowers(followersData);

      console.log("Данные титров загружены:", {
        moderators: moderatorsData.length,
        vips: vipsData.length,
        followers: followersData.length,
      });

      setContentReady(true);
      contentReadyReference.current = true;
      console.log("contentReady установлен в true");
    } catch (error) {
      console.error("Ошибка загрузки данных титров:", error);
      setContentReady(false);
      contentReadyReference.current = false;
    } finally {
      setIsLoading(false);
      console.log("Загрузка завершена, isLoading установлен в false");
    }
  }, [api, isLoading, loadStreamerInfo]);

  // Функция для запуска анимации титров через framer-motion + музыка
  const startCreditsAnimation = useCallback(async () => {
    const currentRunId = runIdReference.current;
    if (!containerReference.current) return;
    if (!contentReadyReference.current) return;
    if (isAnimating) return;

    setIsAnimating(true);

    // Ждем рендер и загрузку изображений
    await new Promise(resolve => requestAnimationFrame(resolve));
    await new Promise(resolve => requestAnimationFrame(resolve));
    await waitImagesLoaded();

    if (currentRunId !== runIdReference.current) {
      setIsAnimating(false);
      return;
    }

    const element = containerReference.current;
    const containerHeight = element.scrollHeight;
    const viewportHeight = window.innerHeight;
    const rect = element.getBoundingClientRect();
    const padding = 40;
    const startY = viewportHeight + padding - rect.top;
    const endY = -containerHeight - padding - rect.top;

    setIsPlaying(true);
    await controls.set({ y: startY });

    if (currentRunId !== runIdReference.current) {
      setIsPlaying(false);
      setIsAnimating(false);
      return;
    }

    // Продолжительность прокрутки титров
    const animationDurationSec = 45;

    // Планируем экспоненциальное затухание за 5 сек до конца анимации
    if (audioContextReference.current && gainNodeReference.current) {
      const context = audioContextReference.current;
      const startDelayMs = Math.max(0, (animationDurationSec - 5) * 1000);
      fadeTimeoutReference.current = globalThis.setTimeout(() => {
        try {
          // Устанавливаем текущее значение как старт и уходим в 0.001 за 5 секунд
          const now = context.currentTime;
          const gain = gainNodeReference.current!.gain;
          gain.cancelScheduledValues(now);
          // Стартуем с max(текущего, маленького положительного)
          const startValue = Math.max(0.01, gain.value || 1);
          gain.setValueAtTime(startValue, now);
          gain.exponentialRampToValueAtTime(0.001, now + 5);
        } catch {
          // ignore
        }
      }, startDelayMs);
    }

    await controls.start({
      y: endY,
      transition: { duration: animationDurationSec, ease: "linear" },
    });

    if (currentRunId !== runIdReference.current) return;

    setIsPlaying(false);
    setIsActive(false);
    setIsAnimating(false);

    // Останавливаем музыку и размьючиваем
    stopAndCleanupAudio();
    TelegramusHubSignalRContext.invoke("UnmuteSessions");
  }, [controls, isAnimating, waitImagesLoaded, stopAndCleanupAudio]);

  // Обработчик SignalR события Credits — показать экран и запустить титры после затемнения
  TelegramusHubSignalRContext.useSignalREffect(
    "Credits",
    async () => {
      try {
        // Новый запуск
        runIdReference.current += 1;
        // Сбрасываем состояние
        setIsActive(true);
        setIsPlaying(false);
        setContentReady(false);
        contentReadyReference.current = false;

        // Выбираем случайный фон
        selectRandomBackground();

        // Выбираем следующий трек (теперь async)
        const nextUrl = await selectNextMusicUrl();
        selectedTrackUrlReference.current = nextUrl;
        // На новый запуск гарантированно останавливаем предыдущий звук
        stopAndCleanupAudio();

        // Сразу запускаем музыку и глушим остальные источники (старт затемнения)
        if (selectedTrackUrlReference.current) {
          try {
            TelegramusHubSignalRContext.invoke("MuteAll", []);
            await playSelectedTrack(selectedTrackUrlReference.current);
          } catch {
            // ignore
          }
        }

        // Загружаем данные титров
        await loadCreditsData();

        // Запустить прокрутку спустя 2 секунды (время затемнения фона)
        animationTimeoutReference.current = globalThis.setTimeout(() => {
          startCreditsAnimation();
        }, 2000);

        return () => {
          if (animationTimeoutReference.current) {
            globalThis.clearTimeout(animationTimeoutReference.current);
            animationTimeoutReference.current = null;
          }
          controls.stop();
          stopAndCleanupAudio();
          TelegramusHubSignalRContext.invoke("UnmuteSessions");
        };
      } catch {
        setIsActive(false);
        setContentReady(false);
        contentReadyReference.current = false;
      }
    },
    [
      loadCreditsData,
      startCreditsAnimation,
      controls,
      selectNextMusicUrl,
      selectRandomBackground,
      stopAndCleanupAudio,
      playSelectedTrack,
    ]
  );

  // Cleanup эффект для очистки таймеров
  useEffect(
    () => () => {
      if (animationTimeoutReference.current) {
        globalThis.clearTimeout(animationTimeoutReference.current);
        animationTimeoutReference.current = null;
      }
      if (fadeTimeoutReference.current) {
        globalThis.clearTimeout(fadeTimeoutReference.current);
        fadeTimeoutReference.current = null;
      }
      stopAndCleanupAudio();
      TelegramusHubSignalRContext.invoke("UnmuteSessions");
    },
    [stopAndCleanupAudio]
  );

  const renderNames = useCallback((list: FollowerInfo[]) => {
    if (!list || list.length === 0)
      return <div className={styles.empty}>—</div>;
    return [...list]
      .sort((a, b) =>
        (
          a.twitchUser?.displayName ||
          a.twitchUser?.userLogin ||
          a.userId
        ).localeCompare(
          b.twitchUser?.displayName || b.twitchUser?.userLogin || b.userId,
          "ru"
        )
      )
      .map(u => <NameRow key={u.userId} follower={u} />);
  }, []);

  return (
    <>
      {!announced && (
        <Announce title={"Credits"} callback={() => setAnnounced(true)} />
      )}
      <audio ref={audioReference} style={{ visibility: "hidden" }} />
      <div
        className={`${styles.root} ${isActive ? styles.active : ""}`}
        data-testid="obs-credits"
      >
        {isActive && (
          <>
            {/* Рендерим выбранный фон */}
            {selectedBackground && (
              <div className={styles.backgroundContainer}>
                {selectedBackground.element}
              </div>
            )}
            <div className={styles.maskTop} />
            <div className={styles.maskBottom} />
            {/* Невидимый аудио-элемент для проигрывания музыки титров */}
            {isLoading && (
              <div className={styles.loading} data-testid="status-loading">
                Загрузка данных титров...
              </div>
            )}
            {contentReady && (
              <motion.div
                ref={containerReference}
                className={styles.scroll}
                animate={controls}
                initial={{ y: 0 }}
                style={{ visibility: isPlaying ? "visible" : "hidden" }}
              >
                <div className={styles.block}>
                  {streamerInfo && (
                    <div
                      className={styles.streamerSection}
                      data-testid="credits-streamer-section"
                    >
                      <Suspense
                        fallback={<div style={{ width: 400, height: 400 }} />}
                      >
                        <ElectricBorder
                          color="rgb(230,172,12)"
                          speed={3}
                          chaos={0.3}
                          style={{
                            width: "400px",
                            height: "400px",
                            borderRadius: 200,
                          }}
                        >
                          <img
                            src={streamerInfo.profileImageUrl}
                            alt={streamerInfo.displayName}
                            className={styles.streamerAvatar}
                            data-testid="img-streamer-avatar"
                          />
                        </ElectricBorder>
                      </Suspense>
                      <SectionTitle
                        leftIcon={icons.streamer}
                        rightIcon={icons.streamer}
                      >
                        <span className={styles.twitchPrefix}>TWITCH.TV/</span>
                        <span
                          className={styles.streamerName}
                          style={{ color: streamerInfo.chatColor }}
                          data-testid="text-streamer-name"
                        >
                          {streamerInfo.displayName.toUpperCase()}
                        </span>
                      </SectionTitle>
                    </div>
                  )}
                  {!streamerInfo && (
                    <SectionTitle
                      leftIcon={icons.streamer}
                      rightIcon={icons.streamer}
                    >
                      TWITCH.TV/RXDCODX
                    </SectionTitle>
                  )}
                </div>

                <div className={styles.block} data-testid="credits-moderators">
                  <SectionTitle leftIcon={icons.mods} rightIcon={icons.mods}>
                    СПАСИБО МОДЕРАТОРАМ
                  </SectionTitle>
                  <div className={styles.list}>{renderNames(moderators)}</div>
                </div>

                <div className={styles.block} data-testid="credits-vips">
                  <SectionTitle leftIcon={icons.vip} rightIcon={icons.vip}>
                    СПАСИБО VIP
                  </SectionTitle>
                  <div className={styles.list}>{renderNames(vips)}</div>
                </div>

                <div className={styles.block} data-testid="credits-followers">
                  <SectionTitle
                    leftIcon={icons.follower}
                    rightIcon={icons.follower}
                  >
                    СПАСИБО ФОЛОВЕРАМ
                  </SectionTitle>
                  <div className={styles.list}>{renderNames(followers)}</div>
                </div>
              </motion.div>
            )}
          </>
        )}
      </div>
    </>
  );
};

export default Credits;
