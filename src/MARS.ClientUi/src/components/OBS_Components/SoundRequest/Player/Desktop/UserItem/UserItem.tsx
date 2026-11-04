import { memo, useMemo } from "react";

import { TwitchUser } from "@/shared/api";

import styles from "../SoundRequestPlayerDesktop.module.scss";

interface UserItemProperties {
  user?: TwitchUser;
  lastTimePlays?: string;
  trackId?: string;
  isCurrent?: boolean;
  onMouseEnter?: () => void;
  onMouseLeave?: () => void;
}

function UserItemComponent({
  user,
  lastTimePlays: requestedAt,
  trackId,
  isCurrent = false,
  onMouseEnter,
  onMouseLeave,
}: UserItemProperties) {
  const displayName = user?.displayName ?? user?.userLogin ?? "Неизвестно";

  const formattedDate = useMemo(
    () => (requestedAt ? new Date(requestedAt).toLocaleString() : ""),
    [requestedAt]
  );

  return (
    <div
      className={`${styles.userRow} ${isCurrent ? `${styles.sticky} ${styles.current}` : ""}`}
      data-track-id={trackId}
      data-testid={`user-item-${user?.twitchId ?? "unknown"}`}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      key={`${user?.twitchId}+${trackId}`}
    >
      <div className={styles.avatar}>
        {user?.profileImageUrl ? (
          <img src={user.profileImageUrl} alt="avatar" />
        ) : (
          <div className={styles.avatarPlaceholder} />
        )}
      </div>
      <div className={styles.userBody}>
        <div
          className={styles.userName}
          style={{ color: user?.chatColor ?? "" }}
        >
          {displayName}
        </div>
        <div className={styles.userMeta}>{formattedDate}</div>
      </div>
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации
export const UserItem = memo(UserItemComponent);
