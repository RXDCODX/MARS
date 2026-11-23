import { memo, useMemo } from "react";

import styles from "../SoundRequestPlayerDesktop.module.scss";

interface UserItemProperties {
  requestedByTwitchId?: string;
  lastTimePlays?: string;
  trackId?: string;
  isCurrent?: boolean;
  onMouseEnter?: () => void;
  onMouseLeave?: () => void;
}

function UserItemComponent({
  requestedByTwitchId,
  lastTimePlays: requestedAt,
  trackId,
  isCurrent = false,
  onMouseEnter,
  onMouseLeave,
}: UserItemProperties) {
  const displayName = requestedByTwitchId ?? "Неизвестно";

  const formattedDate = useMemo(
    () => (requestedAt ? new Date(requestedAt).toLocaleString() : ""),
    [requestedAt]
  );

  return (
    <div
      className={`${styles.userRow} ${isCurrent ? `${styles.sticky} ${styles.current}` : ""}`}
      data-track-id={trackId}
      data-testid={`user-item-${requestedByTwitchId ?? "unknown"}`}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      key={`${requestedByTwitchId ?? "unknown"}+${trackId ?? "track"}`}
    >
      <div className={styles.avatar}>
        <div className={styles.avatarPlaceholder} />
      </div>
      <div className={styles.userBody}>
        <div className={styles.userName}>{displayName}</div>
        <div className={styles.userMeta}>{formattedDate}</div>
      </div>
    </div>
  );
}

// Экспортируем мемоизированную версию для оптимизации
export const UserItem = memo(UserItemComponent);
