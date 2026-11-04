import { useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";

import styles from "./ChoosePath.module.scss";
import CurrentTrackInfo from "./CurrentTrack/CurrentTrackManager";
import { TrackList } from "./TrackList/TrackList";
import { VideoScreen } from "./VideoScreen/VideoScreen";

const ChoosePath: React.FC = () => {
  const location = useLocation();
  const [isInitialized, setIsInitialized] = useState(false);

  useEffect(() => {
    setIsInitialized(true);
  }, []);

  if (!isInitialized) {
    return null;
  }

  const path = location.pathname;

  // Если путь /sr - показываем страницу выбора
  if (path === "/sr") {
    return (
      <div className={styles["choose-path"]} data-testid="obs-choose-path">
        <Link
          to="/sr/player"
          className={styles.column}
          data-testid="link-video-screen"
        >
          <span className={styles.label}>VideoScreen</span>
        </Link>
        <Link
          to="/sr/tracklist"
          className={styles.column}
          data-testid="link-tracklist"
        >
          <span className={styles.label}>TRACKLIST</span>
        </Link>
        <Link
          to="/sr/currenttrack"
          className={styles.column}
          data-testid="link-current-track"
        >
          <span className={styles.label}>CurrentTrack</span>
        </Link>
      </div>
    );
  }

  // Если путь /sr/player - рендерим VideoScreen
  if (path === "/sr/player") {
    return <VideoScreen />;
  }

  // Если путь /sr/tracklist - рендерим TrackList (Player)
  if (path === "/sr/tracklist") {
    return <TrackList />;
  }

  // Если путь /sr/currenttrack - рендерим CurrentTrack
  if (path === "/sr/currenttrack") {
    return <CurrentTrackInfo />;
  }

  // Если путь не соответствует ни одному из известных - показываем страницу выбора
  return (
    <div className={styles["choose-path"]} data-testid="obs-choose-path">
      <Link
        to="/sr/player"
        className={styles.column}
        data-testid="link-video-screen"
      >
        <span className={styles.label}>VideoScreen</span>
      </Link>
      <Link
        to="/sr/tracklist"
        className={styles.column}
        data-testid="link-tracklist"
      >
        <span className={styles.label}>TRACK LIST</span>
      </Link>
      <Link
        to="/sr/currenttrack"
        className={styles.column}
        data-testid="link-current-track"
      >
        <span className={styles.label}>CurrentTrack</span>
      </Link>
    </div>
  );
};

export default ChoosePath;
export { ChoosePath };
