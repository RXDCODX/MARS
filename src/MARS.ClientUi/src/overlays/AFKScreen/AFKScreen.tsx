import "./AFKScreen.scss";

import { useCallback, useEffect, useRef, useState } from "react";

import { ensureYouTubeApiAsync } from "./youtubeApi";

// Константы с плейлистами YouTube
const PLAYLISTS = [
  "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
  "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
  "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
];

const AFKScreen = () => {
  const hostReference = useRef<HTMLDivElement>(null);
  const mountPointReference = useRef<HTMLDivElement | null>(null);
  const playerInstanceReference = useRef<any>(null);
  const [isMuted, setIsMuted] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [_, setCurrentPlaylist] = useState<string>("");

  // Функция для получения случайного плейлиста
  const getRandomPlaylist = useCallback(() => {
    const randomIndex = Math.floor(Math.random() * PLAYLISTS.length);
    return PLAYLISTS[randomIndex];
  }, []);

  useEffect(() => {
    const initPlayer = async () => {
      if (hostReference.current && !playerInstanceReference.current) {
        try {
          // Готовность, а не наличие: библиотека создаёт `window.YT` раньше,
          // чем появляется конструктор `Player`, и старая проверка
          // `if (!globalThis.YT)` считала такой `YT` годным. Дальше
          // `new globalThis.YT.Player(...)` бросал «is not a constructor»,
          // исключение ловила граница ошибок, и зритель видел заглушку.
          const readiness = await ensureYouTubeApiAsync();

          if (readiness !== "ready") {
            setHasError(true);
            return;
          }

          const playlistId = getRandomPlaylist();
          setCurrentPlaylist(playlistId);

          // Плееру отдаётся не React-узел, а собственный элемент внутри него.
          // Библиотека забирает переданный элемент: создаёт рядом свой `<iframe>`
          // и заменяет им переданный. React про обмен не знает и на следующем
          // рендере вставлял новый узел, опираясь на уже украденный, — `insertBefore`
          // бросал NotFoundError, ошибка приходила из коммита, и `/afkscreen`
          // показывал заглушку границы ошибок. Сосуд, которым владеет React,
          // остаётся пустым всегда: его детьми React не управляет.
          const mountPoint = document.createElement("div");
          mountPoint.style.width = "100%";
          mountPoint.style.height = "100%";
          hostReference.current.appendChild(mountPoint);
          mountPointReference.current = mountPoint;

          // Создаем плеер
          playerInstanceReference.current = new globalThis.YT.Player(
            mountPoint,
            {
              height: "100%",
              width: "100%",
              playerVars: {
                autoplay: 1,
                controls: 1,
                playsinline: 1,
                mute: isMuted ? 1 : 0,
              },
              events: {
                onReady: (event: any) => {
                  console.log("YouTube player is ready");
                  setHasError(false);

                  // Загружаем плейлист с случайным индексом и позицией
                  const randomIndex = Math.floor(Math.random() * 200);
                  const randomStartSeconds = Math.floor(Math.random() * 60);

                  event.target.loadPlaylist({
                    listType: "playlist",
                    list: playlistId,
                    index: randomIndex,
                    startSeconds: randomStartSeconds,
                  });

                  // Включаем перемешивание плейлиста для большей случайности
                  event.target.setShuffle(true);
                },
                onStateChange: (event: any) => {
                  console.log("Player state changed:", event.data);
                  // Если видео закончилось, переходим к следующему
                  if (event.data === globalThis.YT.PlayerState.ENDED) {
                    event.target.nextVideo();
                  }
                },
                onError: (event: any) => {
                  console.error("YouTube player error:", event.data);
                  setHasError(true);
                  // Пытаемся перезапустить с другим плейлистом
                  setTimeout(() => {
                    if (!playerInstanceReference.current) {
                      return;
                    }

                    const newPlaylistId = getRandomPlaylist();
                    setCurrentPlaylist(newPlaylistId);
                    const randomIndex = Math.floor(Math.random() * 200);
                    const randomStartSeconds = Math.floor(Math.random() * 60);

                    playerInstanceReference.current.loadPlaylist({
                      listType: "playlist",
                      list: newPlaylistId,
                      index: randomIndex,
                      startSeconds: randomStartSeconds,
                    });

                    // Включаем перемешивание для нового плейлиста
                    setTimeout(() => {
                      if (playerInstanceReference.current) {
                        playerInstanceReference.current.setShuffle(true);
                      }
                    }, 1000);
                  }, 2000);
                },
              },
            }
          );
        } catch (error) {
          console.error("Error initializing player:", error);
          setHasError(true);
        }
      }
    };

    initPlayer();

    return () => {
      // Порядок обязателен: сперва плеер, потом его место. Наоборот — `destroy`
      // обращается к узлу, которого уже нет, и экран падает при размонтировании.
      if (playerInstanceReference.current) {
        playerInstanceReference.current.destroy();
        playerInstanceReference.current = null;
      }

      if (mountPointReference.current) {
        mountPointReference.current.remove();
        mountPointReference.current = null;
      }
    };
  }, [getRandomPlaylist, isMuted]);

  const handleUserInteraction = useCallback(() => {
    if (!(isMuted && playerInstanceReference.current)) {
      return;
    }

    setIsMuted(false);
    playerInstanceReference.current.unMute();
  }, [isMuted]);

  const handleRetry = useCallback(() => {
    setHasError(false);
    if (playerInstanceReference.current) {
      const newPlaylistId = getRandomPlaylist();
      setCurrentPlaylist(newPlaylistId);
      const randomIndex = Math.floor(Math.random() * 200);
      const randomStartSeconds = Math.floor(Math.random() * 60);

      playerInstanceReference.current.loadPlaylist({
        listType: "playlist",
        list: newPlaylistId,
        index: randomIndex,
        startSeconds: randomStartSeconds,
      });

      // Включаем перемешивание для нового плейлиста
      setTimeout(() => {
        if (playerInstanceReference.current) {
          playerInstanceReference.current.setShuffle(true);
        }
      }, 1000);
    }
  }, [getRandomPlaylist]);

  return (
    <div
      className="afk-screen-container"
      onClick={handleUserInteraction}
      data-testid="obs-afk-screen"
    >
      {hasError && (
        <div className="error-message" data-testid="status-error">
          <p>Ошибка загрузки видео</p>
          <button
            onClick={handleRetry}
            className="retry-button"
            data-testid="button-retry"
          >
            Попробовать снова
          </button>
        </div>
      )}
      <div ref={hostReference} className="youtube-player" />
    </div>
  );
};

export default AFKScreen;
