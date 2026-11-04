import { useCallback, useRef, useState } from "react";
import { useHubInvoke } from "@/shared/realtime/useHubInvoke";

import { BeamUser, normalizeBeamUsers } from "@/shared/realtime/beamUsers";
import { decodeJsonListBranch } from "@/shared/realtime/overlayPayload";
import { useOverlayEvent } from "@/shared/realtime/useOverlayEvent";
import Announce from "@/shared/Utils/Announce/Announce";

import styles from "./MikuMikuBeam.module.scss";
import videoSource from "./video/miku_miku_beam.webm";

interface VideoState {
  isActive: boolean;
  users: BeamUser[];
}

const TICKER_START_TIME = 9.28; // 9 секунд 280 миллисекунд
const TICKER_END_TIME = 18.19; // 18 секунд 190 миллисекунд

const MikuMikuBeamComponent = () => {
  const [videoState, setVideoState] = useState<VideoState>({
    isActive: false,
    users: [],
  });
  const [showTickers, setShowTickers] = useState(false);
  const invoke = useHubInvoke();
  const [announced, setAnnounced] = useState(false);
  const videoReference = useRef<HTMLVideoElement>(null);

  const preloadImages = useCallback((users: BeamUser[]) => {
    // Запускаем предзагрузку в фоне без блокировки
    const imagePromises = users
      .filter(user => user.profileImageUrl)
      .map(user =>
        Promise.race([
          // Загрузка изображения
          new Promise<void>(resolve => {
            const img = new Image();
            img.addEventListener("load", () => resolve());
            img.onerror = () => resolve();
            img.src = user.profileImageUrl as string;
          }),
          // Таймаут 2 секунды на каждое изображение
          new Promise<void>(resolve => setTimeout(resolve, 2000)),
        ])
      );

    // Загружаем в фоне, не блокируя запуск видео
    Promise.all(imagePromises)
      .then(() => {
        console.log("[MikuMikuBeam] Аватарки предзагружены");
      })
      .catch(() => {
        console.log("[MikuMikuBeam] Ошибка предзагрузки аватарок");
      });
  }, []);

  const handleMikuBeamActivation = useCallback(
    (users: BeamUser[]) => {
      // Предзагружаем аватарки
      preloadImages(users);

      // Вызываем MuteAll с пустым массивом
      try {
        invoke("MuteAll");
        console.log("[MikuMikuBeam] MuteAll вызван");
      } catch (error) {
        console.error("[MikuMikuBeam] Ошибка вызова MuteAll:", error);
      }

      // Активируем видео
      setVideoState({
        isActive: true,
        users: users,
      });
    },
    [preloadImages]
  );

  // Обработчик прогресса воспроизведения видео
  const handleTimeUpdate = useCallback(() => {
    if (!videoReference.current) return;

    const currentTime = videoReference.current.currentTime;

    // Показываем бегущие строки в нужный временной интервал
    if (currentTime >= TICKER_START_TIME && currentTime <= TICKER_END_TIME) {
      if (!showTickers) {
        invoke("MikuMikuDeleteTwitchMessages");
      }
      setShowTickers(true);
    } else {
      setShowTickers(false);
    }
  }, [showTickers]);

  // Обработчик завершения видео
  const handleVideoEnded = useCallback(() => {
    console.log("[MikuMikuBeam] Видео завершено");

    // Деактивируем видео
    setVideoState({
      isActive: false,
      users: [],
    });
    setShowTickers(false);

    // Вызываем UnmuteSessions
    try {
      invoke("UnmuteSessions");
      console.log("[MikuMikuBeam] UnmuteSessions вызван");
    } catch (error) {
      console.error("[MikuMikuBeam] Ошибка вызова UnmuteSessions:", error);
    }
  }, []);

  // Обработчик события MikuMikuBeam с хаба оверлея.
  useOverlayEvent("MikuMikuBeam", payload => {
    // Событие едет содержимым ветки — { usersJson }, где поле объявлено
    // как repeated bytes — то есть списком массивов байт. Раньше обработчик
    // получал готовый массив пользователей: форму задавал резолвер SignalR, и
    // форма proto до клиента не доходила.
    const users = normalizeBeamUsers(
      decodeJsonListBranch(payload, "usersJson")
    );

    if (users.length === 0) {
      return;
    }

    handleMikuBeamActivation(users);
  });

  // Создаем массив пользователей для бегущих строк (дублируем для непрерывности)
  // Дублируем достаточное количество раз, чтобы заполнить экран даже с малым количеством пользователей
  //
  // `Array.from({ length: 12 })` даёт `unknown[]`, и после `fill(...).flat()`
  // тип терялся: каждое обращение к полям пользователя в разметке падало с
  // «'user' is of type 'unknown'» — двенадцать ошибок в одном выражении.
  // Форма с функцией-заполнителем сохраняет тип элемента.
  const tickerUsers = Array.from({ length: 12 }, () => videoState.users).flat();

  return (
    <>
      {!announced && (
        <Announce callback={() => setAnnounced(true)} title="MikuMikuBeam" />
      )}
      <div
        className={`${styles.container} ${videoState.isActive ? styles.active : ""}`}
        data-testid="obs-mikumikubeam"
      >
        {videoState.isActive && (
          <>
            {/* Видео на весь экран */}
            <video
              ref={videoReference}
              src={videoSource}
              className={styles.video}
              autoPlay
              playsInline
              onTimeUpdate={handleTimeUpdate}
              onEnded={handleVideoEnded}
            />

            {/* Бегущие строки */}
            {showTickers && videoState.users.length > 0 && (
              <>
                {/* Верхняя бегущая строка (движется налево) */}
                <div className={`${styles.ticker} ${styles.tickerTop}`}>
                  <div className={styles.tickerContent}>
                    {tickerUsers.map((user, index) => (
                      <div key={`top-${index}`} className={styles.userItem}>
                        <div className={styles.crossIcon}></div>
                        {user.profileImageUrl ? (
                          <img
                            src={user.profileImageUrl}
                            alt={user.displayName}
                            className={styles.avatar}
                          />
                        ) : (
                          <div className={styles.avatarPlaceholder}>
                            {user.initial}
                          </div>
                        )}
                        <span
                          className={styles.username}
                          style={{ color: user.chatColor }}
                        >
                          {user.displayName}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Нижняя бегущая строка (движется направо) */}
                <div className={`${styles.ticker} ${styles.tickerBottom}`}>
                  <div className={styles.tickerContent}>
                    {tickerUsers.map((user, index) => (
                      <div key={`bottom-${index}`} className={styles.userItem}>
                        <div className={styles.crossIcon}></div>
                        {user.profileImageUrl ? (
                          <img
                            src={user.profileImageUrl}
                            alt={user.displayName}
                            className={styles.avatar}
                          />
                        ) : (
                          <div className={styles.avatarPlaceholder}>
                            {user.initial}
                          </div>
                        )}
                        <span
                          className={styles.username}
                          style={{ color: user.chatColor }}
                        >
                          {user.displayName}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              </>
            )}
          </>
        )}
      </div>
    </>
  );
};

export default MikuMikuBeamComponent;
