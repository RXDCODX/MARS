import styles from "./Catisa.module.scss";
import { getImagePath } from "./imageAssets";

export function Catisa() {
  return (
    <div className={styles.catisaContainer}>
      <img
        src={getImagePath("catisa")}
        alt="Catisa"
        className={styles.catisaImage}
      />
    </div>
  );
}
