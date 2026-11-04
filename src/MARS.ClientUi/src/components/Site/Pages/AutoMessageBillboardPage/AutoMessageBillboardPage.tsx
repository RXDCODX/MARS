import { useState } from "react";

import AutoMessageBillboardTest from "@/components/OBS_Components/AutoMessageBillboard/AutoMessageBillboardTest";

import styles from "./AutoMessageBillboardPage.module.scss";

export default function AutoMessageBillboardPage() {
  const [testMessage, setTestMessage] = useState("");

  const handleSendTestMessage = () => {
    if (!testMessage.trim()) {
      return;
    }

    // Симулируем SignalR событие
    const event = new CustomEvent("AutoMessage", { detail: testMessage });
    globalThis.dispatchEvent(event);
    setTestMessage("");
  };

  const sendRandomMessages = () => {
    const messages = [
      "Привет всем! Как дела?",
      "Не забудьте подписаться на канал!",
      "Спасибо за поддержку!",
      "Сегодня отличный день для стрима!",
      "Донатеры - вы лучшие!",
      "Нажмите на колокольчик!",
      "Лайк за старания!",
      "Комментарий для алгоритма!",
    ];

    for (const [index, message] of messages.entries()) {
      setTimeout(() => {
        const event = new CustomEvent("AutoMessage", { detail: message });
        globalThis.dispatchEvent(event);
      }, index * 2000); // Каждые 2 секунды
    }
  };

  const sendEmoteMessages = () => {
    const emoteMessages = [
      "Kappa это круто!",
      "PogChamp момент!",
      "FeelsGoodMan",
      "monkaS",
      "LUL",
      "PepeHands",
    ];

    for (const [index, message] of emoteMessages.entries()) {
      setTimeout(() => {
        const event = new CustomEvent("AutoMessage", { detail: message });
        globalThis.dispatchEvent(event);
      }, index * 2000); // Каждые 2 секунды
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.controls}>
        <h1>AutoMessageBillboard Demo</h1>
        <p>Демонстрация компонента для отображения автоматических сообщений</p>

        <div className={styles.inputGroup}>
          <input
            type="text"
            value={testMessage}
            onChange={e => setTestMessage(e.target.value)}
            placeholder="Введите тестовое сообщение..."
            className={styles.input}
          />
          <button onClick={handleSendTestMessage} className={styles.button}>
            Отправить
          </button>
        </div>

        <button onClick={sendRandomMessages} className={styles.button}>
          Отправить серию сообщений
        </button>

        <button onClick={sendEmoteMessages} className={styles.button}>
          Отправить сообщения с эмодзи
        </button>

        <div className={styles.info}>
          <h3>Как это работает:</h3>
          <ul>
            <li>Компонент подписывается на SignalR событие "AutoMessage"</li>
            <li>При получении сообщения появляется билборд справа</li>
            <li>Билборд отображается 3 секунды с анимацией пульсации</li>
            <li>Затем плавно исчезает за пределы экрана</li>
            <li>Поддерживает эмодзи 7TV, BTTV, FFZ и Twitch</li>
          </ul>
        </div>
      </div>

      <AutoMessageBillboardTest />
    </div>
  );
}
