import { MessageDemo } from "@/components/OBS_Components/HighliteMessage";

import styles from "./HighliteMessageDemoPage.module.scss";

export default function HighliteMessageDemoPage() {
  return (
    <div className={styles["highlite-message-demo-page"]}>
      <h1>HighliteMessage Demo</h1>
      <p>Демонстрация работы компонента HighliteMessage с лицами из ассетов</p>
      <MessageDemo />
    </div>
  );
}
