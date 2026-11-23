import { Menu } from "antd";
import React, { useState } from "react";

import Dashboard from "../Dashboard/Dashboard";
import styles from "./AdminPanel.module.scss";
import { AdminPanelProps as AdminPanelProperties } from "./AdminPanel.types";
import SpotifyAuthManager from "./sections/SpotifyAuthManager";

const AdminPanel: React.FC<AdminPanelProperties> = () => {
  const [activeTab, setActiveTab] = useState("dashboard");

  const renderContent = () => {
    switch (activeTab) {
      case "dashboard": {
        return <Dashboard />;
      }
      case "spotify": {
        return <SpotifyAuthManager />;
      }
      case "servers": {
        return (
          <div className={styles.placeholder}>
            <h2>🖥️ Управление серверами</h2>
            <p>Здесь будет интерфейс для управления серверами</p>
          </div>
        );
      }
      case "logs": {
        return (
          <div className={styles.placeholder}>
            <h2>📝 Системные логи</h2>
            <p>Здесь будет детальный просмотр логов</p>
          </div>
        );
      }
      case "users": {
        return (
          <div className={styles.placeholder}>
            <h2>👥 Управление пользователями</h2>
            <p>Здесь будет интерфейс для управления пользователями</p>
          </div>
        );
      }
      case "performance": {
        return (
          <div className={styles.placeholder}>
            <h2>⚡ Детальная производительность</h2>
            <p>Здесь будут расширенные метрики производительности</p>
          </div>
        );
      }
      case "settings": {
        return (
          <div className={styles.placeholder}>
            <h2>⚙️ Настройки системы</h2>
            <p>Здесь будут настройки админ панели</p>
          </div>
        );
      }
      default: {
        return <Dashboard />;
      }
    }
  };

  const menuItems = [
    { key: "dashboard", label: "Дашборд" },
    { key: "spotify", label: "🎵 Spotify" },
    { key: "servers", label: "Серверы" },
    { key: "logs", label: "Логи" },
    { key: "users", label: "Пользователи" },
    { key: "performance", label: "Производительность" },
    { key: "settings", label: "Настройки" },
  ];

  return (
    <div className={styles.adminPanel}>
      <div
        style={{
          background: "#001529",
          padding: "0 24px",
          marginBottom: 12,
          display: "flex",
          alignItems: "center",
        }}
      >
        <span
          style={{
            color: "#fff",
            fontWeight: "bold",
            fontSize: 16,
            marginRight: 32,
            whiteSpace: "nowrap",
          }}
        >
          Админ панель
        </span>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={[activeTab]}
          onClick={({ key }) => setActiveTab(key)}
          items={menuItems}
          style={{ flex: 1, borderBottom: "none", background: "transparent" }}
        />
      </div>
      <main className={styles.content}>{renderContent()}</main>
    </div>
  );
};

export default AdminPanel;
