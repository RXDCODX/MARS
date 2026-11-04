import { Alert, Button, Card, Input, Spin } from "antd";
import React, { useCallback, useEffect, useMemo, useState } from "react";

import { SpotifyAuth } from "@/shared/api";
import {
  OperationResult,
  SpotifyAuthStartResult,
  SpotifyAuthStatusResult,
} from "@/shared/api/types/data-contracts";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./SpotifyAuthManager.module.scss";

const SpotifyAuthManager: React.FC = () => {
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [status, setStatus] = useState<SpotifyAuthStatusResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isCheckingStatus, setIsCheckingStatus] = useState(true);
  const { showToast } = useToastModal();

  const apiClient = useMemo(() => new SpotifyAuth(), []);

  const checkAuthStatus = useCallback(async () => {
    setIsCheckingStatus(true);
    try {
      const response = await apiClient.spotifyAuthStatusList();
      const result = response.data;
      if (result.success && result.data) {
        setStatus(result.data);
        setClientId(result.data.hasClientCredentials ? "████████████" : "");
        setClientSecret(result.data.hasClientCredentials ? "████████████" : "");
      }
    } catch (error) {
      console.error("Ошибка проверки статуса Spotify:", error);
    } finally {
      setIsCheckingStatus(false);
    }
  }, [apiClient]);

  const handleStartAuth = async () => {
    if (
      !clientId ||
      !clientSecret ||
      clientId === "████████████" ||
      clientSecret === "████████████"
    ) {
      showToast({
        success: false,
        message: "Пожалуйста, введите ClientId и ClientSecret",
      });
      return;
    }

    setIsLoading(true);
    try {
      const response = await apiClient.spotifyAuthStartCreate({
        clientId: clientId.trim(),
        clientSecret: clientSecret.trim(),
        redirectUri: `${location.origin}/api/SpotifyAuth/callback`,
      });
      const result = response.data as OperationResult<SpotifyAuthStartResult>;

      if (result.success && result.data?.authUrl) {
        showToast({
          success: true,
          message: "Открываю Spotify для авторизации...",
        });
        window.open(result.data.authUrl, "_blank");

        setTimeout(() => {
          checkAuthStatus();
        }, 2000);
      } else {
        showToast({
          success: false,
          message: result.message || "Ошибка при старте авторизации",
        });
      }
    } catch (error) {
      console.error("Ошибка при старте авторизации:", error);
      showToast({
        success: false,
        message: "Ошибка при старте авторизации",
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleDisconnect = async () => {
    if (!globalThis.confirm("Вы уверены, что хотите отключить Spotify?")) {
      return;
    }

    setIsLoading(true);
    try {
      const response = await apiClient.spotifyAuthDisconnectCreate();
      const result = response.data as OperationResult;
      if (result.success) {
        showToast({
          success: true,
          message: "Spotify аккаунт отключен",
        });
        setStatus(null);
        setClientId("");
        setClientSecret("");
      } else {
        showToast({
          success: false,
          message: result.message || "Ошибка при отключении",
        });
      }
    } catch (error) {
      console.error("Ошибка при отключении:", error);
      showToast({
        success: false,
        message: "Ошибка при отключении",
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    checkAuthStatus();
  }, [checkAuthStatus]);

  if (isCheckingStatus) {
    return (
      <Card className={styles.spotifyManager}>
        <div style={{ textAlign: "center" }}>
          <Spin />
          <span style={{ marginLeft: 8 }}>Проверяю статус Spotify...</span>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.spotifyManager}>
      <div className={styles.header}>
        <h3>🎵 Spotify для SoundRequest</h3>
      </div>
      {status?.isLinked && (
        <Alert
          type="success"
          className={styles.statusAlert}
          message={
            <div className={styles.statusContent}>
              <div className={styles.profileBlock}>
                {status.avatarUrl && (
                  <img
                    src={status.avatarUrl}
                    alt={`Аватар ${status.displayName || status.userId || "Spotify"}`}
                    className={styles.avatar}
                  />
                )}

                <div>
                  <strong>Spotify подключен</strong>
                  <p className={styles.userInfo}>
                    👤 {status.displayName || status.userId || "Без имени"}
                  </p>
                  {status.userId && (
                    <p className={styles.nickInfo}>
                      🏷️ Никнейм: @{status.userId}
                    </p>
                  )}
                  {status.product && (
                    <p className={styles.productInfo}>
                      🎼 Тариф: {status.product}
                    </p>
                  )}
                  {status.deviceId && (
                    <p className={styles.deviceInfo}>
                      🎧 Устройство: {status.deviceId}
                    </p>
                  )}
                </div>
              </div>
              <Button
                danger
                onClick={handleDisconnect}
                disabled={isLoading}
                className={styles.disconnectBtn}
              >
                Отключить
              </Button>
            </div>
          }
        />
      )}

      {!status?.isLinked && (
        <div className={styles.formContainer}>
          <div style={{ marginBottom: 12 }}>
            <label style={{ display: "block", marginBottom: 4 }}>
              Spotify Client ID
            </label>
            <Input
              type="text"
              placeholder="Введите Client ID"
              value={clientId}
              onChange={e => setClientId(e.target.value)}
              disabled={isLoading || status?.hasClientCredentials}
            />
            <span
              style={{
                display: "block",
                marginTop: 4,
                color: "#8c8c8c",
                fontSize: 12,
              }}
            >
              Получите на{" "}
              <a
                href="https://developer.spotify.com/dashboard"
                target="_blank"
                rel="noreferrer"
              >
                Spotify Developer Dashboard
              </a>
            </span>
          </div>

          <div style={{ marginBottom: 12 }}>
            <label style={{ display: "block", marginBottom: 4 }}>
              Spotify Client Secret
            </label>
            <Input.Password
              placeholder="Введите Client Secret"
              value={clientSecret}
              onChange={e => setClientSecret(e.target.value)}
              disabled={isLoading || status?.hasClientCredentials}
            />
            <span
              style={{
                display: "block",
                marginTop: 4,
                color: "#8c8c8c",
                fontSize: 12,
              }}
            >
              Никогда не делитесь этим ключом
            </span>
          </div>

          <div className={styles.buttonGroup}>
            <Button
              type="primary"
              onClick={handleStartAuth}
              disabled={isLoading || !clientId || !clientSecret}
              className={styles.connectBtn}
              style={{
                background: isLoading ? undefined : "#52c41a",
                borderColor: isLoading ? undefined : "#52c41a",
              }}
            >
              {isLoading ? (
                <>
                  <Spin size="small" style={{ marginRight: 8 }} />
                  Подключение...
                </>
              ) : (
                "Подключить Spotify"
              )}
            </Button>
          </div>

          {status?.hasClientCredentials && !status?.isLinked && (
            <Alert
              type="warning"
              message="⚠️ Учетные данные сохранены, но аккаунт еще не подключен. Нажмите кнопку выше для авторизации."
              style={{ marginTop: 12 }}
            />
          )}
        </div>
      )}

      <div className={styles.info}>
        <h5>ℹ️ Информация</h5>
        <ul>
          <li>
            Spotify режим позволяет заказывать треки из Spotify вместо YouTube
          </li>
          <li>
            После подключения приватные ключи хранятся безопасно только на
            сервере
          </li>
          <li>
            Управление соединением работает как Songify: берет активное
            устройство Spotify
          </li>
        </ul>
      </div>
    </Card>
  );
};

export default SpotifyAuthManager;
