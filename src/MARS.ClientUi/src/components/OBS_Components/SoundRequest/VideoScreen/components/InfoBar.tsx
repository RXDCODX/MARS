import { useSoundRequestCommands } from "../hooks/useSoundRequestCommands";
import { useVideoScreenStore } from "../store/useVideoScreenStore";
import { CustomMarquee } from "./CustomMarquee";
import styles from "./InfoBar.module.scss";
import { ProgressPercentLabel } from "./ProgressPercentLabel";

function InfoBarComponent() {
  const commands = useSoundRequestCommands();
  const playerState = useVideoScreenStore(state => state.playerState);
  const currentQueueItem = playerState?.currentQueueItem;

  const displayCommands =
    commands.length > 0
      ? commands
      : [
          { command: "!sr", description: "Добавить трек в очередь" },
          {
            command: "!srwrong",
            description: "Отменить последний заказанный трек",
          },
          {
            command: "!song",
            description: "Информация о текущем треке",
          },
          {
            command: "!queue",
            description: "Показать вашу позицию в очереди",
          },
        ];

  // Empty queue — show static command info (NoVideo mode only)
  if (!currentQueueItem) {
    return (
      <div className={styles.infoBar} data-testid="infobar-empty">
        <div className={styles.silkBackground} />
        <div className={styles.infoBarContent}>
          <div className={styles.commandsMarqueeContainer}>
            <CustomMarquee speed={60} trailingGapCount={5}>
              {displayCommands.map((command, index) => (
                <span key={index} className={styles.commandItem}>
                  <span className={styles.commandName}>{command.command}</span>
                  <span className={styles.commandSep}>—</span>
                  <span className={styles.commandDesc}>
                    {command.description}
                  </span>
                </span>
              ))}
            </CustomMarquee>
          </div>
        </div>
      </div>
    );
  }

  // Has track — track info
  const currentTrack = currentQueueItem.track;
  const requestedByUser = currentQueueItem.requestedByTwitchUser;

  const userName =
    requestedByUser?.displayName ??
    requestedByUser?.userLogin ??
    "Неизвестный пользователь";
  const userAvatar = requestedByUser?.profileImageUrl;
  const userColor = requestedByUser?.chatColor;
  const trackName = currentTrack?.trackName ?? "";
  const artistName = currentTrack?.authors?.join(", ") ?? "Неизвестный автор";
  const trackDuration = currentTrack?.duration;

  return (
    <div className={styles.infoBar} style={{ backgroundColor: userColor }}>
      <div className={styles.silkBackground} />
      <div className={styles.infoBarContent}>
        <div className={styles.mainMarqueeContainer}>
          <CustomMarquee speed={60} trailingGapCount={5}>
            {userAvatar && (
              <img src={userAvatar} alt={userName} className={styles.avatar} />
            )}

            <span className={styles.userNameBlock}>
              <span className={styles.userName}>{userName}</span>
            </span>

            {userAvatar && (
              <img src={userAvatar} alt={userName} className={styles.avatar} />
            )}

            <span className={styles.contentSeparator}>•</span>
            <span className={styles.trackNameBlock}>
              <span className={styles.trackName}>
                {artistName ? `${artistName} - ${trackName}` : trackName}
              </span>
            </span>
          </CustomMarquee>
        </div>

        <ProgressPercentLabel trackDuration={trackDuration} />
      </div>
    </div>
  );
}

export const InfoBar = InfoBarComponent;
