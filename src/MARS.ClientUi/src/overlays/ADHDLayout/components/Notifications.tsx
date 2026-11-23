import { getImagePath } from "./imageAssets";
import styles from "./Notifications.module.scss";

export const Notifications: React.FC = () => (
  <div className={styles.notifications} data-testid="adhd-notifications">
    <div className={styles.notificationsContainer}>
      <div className={`${styles.notification} ${styles.notificationFirst}`}>
        <div className={styles.notificationIconWrapper}>
          <img
            src={getImagePath("email")}
            className={styles.notificationIcon}
            alt="Email notification"
          />
        </div>
        <div className={styles.notificationInfo}>
          <div className={styles.notificationFrom}>Cloud</div>
          <div className={styles.notificationText}>
            Your storage is 100% full
          </div>
        </div>
      </div>

      <div className={`${styles.notification} ${styles.notificationSecond}`}>
        <div className={styles.notificationIconWrapper}>
          <img
            src={getImagePath("email")}
            className={styles.notificationIcon}
            alt="Email notification"
          />
        </div>
        <div className={styles.notificationInfo}>
          <div className={styles.notificationFrom}>Cloud</div>
          <div className={styles.notificationText}>
            Your storage is 100% full
          </div>
        </div>
      </div>

      <div className={`${styles.notification} ${styles.notificationThird}`}>
        <div className={styles.notificationIconWrapper}>
          <img
            src={getImagePath("email")}
            className={styles.notificationIcon}
            alt="Email notification"
          />
        </div>
        <div className={styles.notificationInfo}>
          <div className={styles.notificationFrom}>Cloud</div>
          <div className={styles.notificationText}>
            Your storage is 100% full
          </div>
        </div>
      </div>
    </div>
  </div>
);
