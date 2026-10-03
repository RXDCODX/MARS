import { useCallback, useEffect, useReducer, useState } from "react";

import { TelegramusHubSignalRContext } from "@/shared/api";
import Announce from "@/shared/Utils/Announce/Announce";

import styles from "./MichaelJackson.module.scss";
import { getVideoPath } from "./videoAssets";

// Типы для очереди воспроизведения (по аналогии с WaifuAlerts)
interface VideoItem {
  id: string;
  src: string;
}

interface VideoState {
  videos: VideoItem[];
  queue: VideoItem[];
  currentVideo: VideoItem | null;
  isPlaying: boolean;
  isActive: boolean;
  currentIndex: number;
}

type VideoAction =
  | { type: "ADD_TO_QUEUE" }
  | { type: "REMOVE_FROM_QUEUE" }
  | { type: "VIDEO_ENDED" };

// Начальное состояние
const initialState: VideoState = {
  videos: [
    { id: "michael_1", src: getVideoPath("michael_1") },
    { id: "michael_2", src: getVideoPath("michael_2") },
  ],
  queue: [],
  currentVideo: null,
  isPlaying: false,
  isActive: false,
  currentIndex: 0,
};

// Reducer для управления очередью воспроизведения (логика из WaifuAlerts)
const videoReducer = (state: VideoState, action: VideoAction): VideoState => {
  switch (action.type) {
    case "ADD_TO_QUEUE": {
      // Получаем следующее видео по индексу
      const nextVideo = state.videos[state.currentIndex];
      const newIndex = (state.currentIndex + 1) % state.videos.length;

      // Если ничего не играет, сразу начинаем воспроизведение
      if (!state.isPlaying) {
        return {
          ...state,
          currentVideo: nextVideo,
          isPlaying: true,
          isActive: true,
          currentIndex: newIndex,
        };
      }

      // Если уже что-то играет, добавляем в очередь
      return {
        ...state,
        queue: [...state.queue, nextVideo],
        currentIndex: newIndex,
      };
    }

    case "REMOVE_FROM_QUEUE": {
      if (state.queue.length > 0) {
        const nextVideo = state.queue[0];
        return {
          ...state,
          currentVideo: nextVideo,
          isPlaying: true,
          isActive: true,
          queue: state.queue.slice(1),
        };
      }

      return {
        ...state,
        isPlaying: false,
        isActive: false,
        currentVideo: null,
      };
    }

    case "VIDEO_ENDED": {
      return {
        ...state,
        isPlaying: false,
        isActive: false,
        currentVideo: null,
      };
    }

    default: {
      return state;
    }
  }
};

const MichaelJackson = () => {
  const [state, dispatch] = useReducer(videoReducer, initialState);
  const [announced, setAnnounced] = useState<boolean>(false);

  // Обработчик SignalR события для добавления в очередь
  TelegramusHubSignalRContext.useSignalREffect(
    "MichaelJackson",
    () => {
      dispatch({ type: "ADD_TO_QUEUE" });
    },
    []
  );

  // Обработчик завершения видео
  const handleVideoEnded = useCallback(() => {
    dispatch({ type: "VIDEO_ENDED" });
  }, []);

  // Автоматический переход к следующему видео в очереди после завершения текущего
  useEffect(() => {
    if (!state.isPlaying && state.queue.length > 0) {
      dispatch({ type: "REMOVE_FROM_QUEUE" });
    }
  }, [state.isPlaying, state.queue.length]);

  return (
    <>
      {!announced && (
        <Announce callback={() => setAnnounced(true)} title="MichaelJackson" />
      )}
      <div
        className={`${styles.root} ${state.isActive ? styles.active : ""}`}
        data-testid="obs-michael-jackson"
      >
        {state.currentVideo && (
          <video
            src={state.currentVideo.src}
            className={styles.video}
            autoPlay
            controls={false}
            playsInline
            key={state.currentVideo.id}
            onEnded={handleVideoEnded}
          />
        )}
      </div>
    </>
  );
};

export default MichaelJackson;
