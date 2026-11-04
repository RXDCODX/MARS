import { useState } from "react";

import AutoMessageBillboard from "./AutoMessageBillboard";
import styles from "./AutoMessageBillboard.module.scss";

export default function AutoMessageBillboardTest() {
  const [testMode, setTestMode] = useState<"storybook" | "signalr">(
    "storybook"
  );
  const [messages, setMessages] = useState<string[]>([
    "Привет всем! Как дела?",
    "Не забудьте подписаться на канал!",
    "Спасибо за поддержку!",
    "Сегодня отличный день для стрима!",
    "Донатеры - вы лучшие!",
  ]);

  const addMessage = () => {
    const newMessage = `Тестовое сообщение ${Date.now()}`;
    setMessages(previous => [...previous, newMessage]);
  };

  const clearMessages = () => {
    setMessages([]);
  };

  const simulateSignalR = () => {
    // Симуляция получения сообщений через SignalR
    const interval = setInterval(() => {
      const newMessage = `SignalR сообщение ${Date.now()}`;
      setMessages(previous => [...previous, newMessage]);
    }, 5000);

    // Останавливаем через 30 секунд
    setTimeout(() => {
      clearInterval(interval);
    }, 30_000);
  };

  return (
    <div className={styles.testContainer} data-testid="obs-auto-message-test">
      <h1 className={styles.testTitle}>Тест AutoMessageBillboard</h1>

      <div className={styles.testControls}>
        <button
          className={`${styles.testButton} ${testMode === "storybook" ? styles.active : ""}`}
          onClick={() => setTestMode("storybook")}
        >
          Storybook Mode
        </button>
        <button
          className={`${styles.testButton} ${testMode === "signalr" ? styles.active : ""}`}
          onClick={() => setTestMode("signalr")}
        >
          SignalR Mode
        </button>
      </div>

      <div className={styles.testActions}>
        <button
          className={`${styles.testButton} ${styles.addButton}`}
          onClick={addMessage}
        >
          Добавить сообщение
        </button>
        <button
          className={`${styles.testButton} ${styles.clearButton}`}
          onClick={clearMessages}
        >
          Очистить очередь
        </button>
        <button
          className={`${styles.testButton} ${styles.signalrButton}`}
          onClick={simulateSignalR}
        >
          Симулировать SignalR
        </button>
      </div>

      <div className={styles.testInfo}>
        <h3 className={styles.testSubtitle}>Текущий режим: {testMode}</h3>
        <h3 className={styles.testSubtitle}>
          Сообщений в очереди: {messages.length}
        </h3>
        <div className={styles.messageList}>
          {messages.map((message, index) => (
            <div key={index} className={styles.messageItem}>
              {index + 1}. {message}
            </div>
          ))}
        </div>
      </div>

      <AutoMessageBillboard
        messages={testMode === "storybook" ? messages : undefined}
      />
    </div>
  );
}
