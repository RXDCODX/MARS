import { Alert, Button, Card, Space, Spin, Tag, Typography } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import { TwitchAuth } from "@/shared/api/http-clients/TwitchAuth";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./TwitchAuthPage.module.scss";

const { Text, Title } = Typography;

const POLL_INTERVAL_MS = 3000;

interface TwitchAuthStatus {
  isChatConnected: boolean;
  isEventSubConnected: boolean;
  joinedChannels: string;
  lastConnectedAt?: string | null;
  lastDisconnectedAt?: string | null;
  lastDisconnectReason?: string | null;
  reconnectAttempts: number;
  isReconnecting: boolean;
  hasToken: boolean;
  tokenWhenCreated?: string | null;
  tokenExpiresIn?: string | null;
  isTokenExpired: boolean;
}

interface TwitchAuthOperationResult {
  success: boolean;
  message: string;
  errorDetails?: string | null;
}

const TwitchAuthPage: React.FC = () => {
  const { showToast } = useToastModal();
  const api = useMemo(() => new TwitchAuth(), []);

  const [status, setStatus] = useState<TwitchAuthStatus | null>(null);
  const [reconnecting, setReconnecting] = useState(false);
  const [error, setError] = useState("");

  const fetchStatus = useCallback(async () => {
    try {
      const res = await api.twitchAuthStatusList();
      setStatus(res.data?.result as TwitchAuthStatus);
    } catch {
      // silent on poll
    }
  }, [api]);

  useEffect(() => {
    void fetchStatus();
    const interval = setInterval(() => void fetchStatus(), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [fetchStatus]);

  const handleReconnect = useCallback(async () => {
    setReconnecting(true);
    setError("");
    try {
      const res = await api.twitchAuthReconnectCreate();
      const result = res.data as TwitchAuthOperationResult;
      showToast(result.message || "Переподключение запущено", "success");
      await fetchStatus();
    } catch (error_: unknown) {
      const message =
        error_ instanceof Error ? error_.message : "Ошибка переподключения";
      setError(message);
      showToast(message, "error");
    } finally {
      setReconnecting(false);
    }
  }, [api, fetchStatus, showToast]);

  const formatTime = (iso?: string | null): string => {
    if (!iso) return "—";
    try {
      return new Date(iso).toLocaleString("ru-RU");
    } catch {
      return iso;
    }
  };

  return (
    <div data-testid="page-twitch-auth" className={styles.page}>
      <Title level={2}>Twitch Авторизация</Title>

      {error && (
        <Alert
          type="error"
          message={error}
          closable
          onClose={() => setError("")}
          style={{ marginBottom: "1rem" }}
          data-testid="twitch-auth-error-alert"
        />
      )}

      <Card
        title="Статус подключения"
        className={styles.statusCard}
        data-testid="twitch-auth-status-card"
      >
        {status === null ? (
          <Spin data-testid="twitch-auth-loading" />
        ) : (
          <Space direction="vertical" size="middle">
            <div>
              <Text strong>Чат подключён: </Text>
              <Tag
                color={status.isChatConnected ? "green" : "red"}
                data-testid="twitch-chat-connected-tag"
              >
                {status.isChatConnected ? "Да" : "Нет"}
              </Tag>
            </div>
            <div>
              <Text strong>EventSub подключён: </Text>
              <Tag
                color={status.isEventSubConnected ? "green" : "red"}
                data-testid="twitch-eventsub-connected-tag"
              >
                {status.isEventSubConnected ? "Да" : "Нет"}
              </Tag>
            </div>
            {status.joinedChannels && (
              <div data-testid="twitch-joined-channels">
                <Text strong>Каналы: </Text>
                <Text>{status.joinedChannels}</Text>
              </div>
            )}
            <div data-testid="twitch-last-connected">
              <Text strong>Последнее подключение: </Text>
              <Text>{formatTime(status.lastConnectedAt)}</Text>
            </div>
            <div data-testid="twitch-last-disconnected">
              <Text strong>Последнее отключение: </Text>
              <Text>{formatTime(status.lastDisconnectedAt)}</Text>
            </div>
            {status.lastDisconnectReason && (
              <div data-testid="twitch-disconnect-reason">
                <Text strong>Причина отключения: </Text>
                <Text type="danger">{status.lastDisconnectReason}</Text>
              </div>
            )}
            <div data-testid="twitch-reconnect-attempts">
              <Text strong>Попытки переподключения: </Text>
              <Text>{status.reconnectAttempts}</Text>
            </div>
            {status.isReconnecting && (
              <Tag color="orange" data-testid="twitch-reconnecting-tag">
                Переподключение...
              </Tag>
            )}
          </Space>
        )}
      </Card>

      <Card
        title="Токен"
        className={styles.statusCard}
        data-testid="twitch-auth-token-card"
      >
        {status === null ? (
          <Spin data-testid="twitch-auth-token-loading" />
        ) : (
          <Space direction="vertical" size="middle">
            <div>
              <Text strong>Токен получен: </Text>
              <Tag
                color={status.hasToken ? "green" : "red"}
                data-testid="twitch-token-has-tag"
              >
                {status.hasToken ? "Да" : "Нет"}
              </Tag>
            </div>
            <div data-testid="twitch-token-created">
              <Text strong>Создан: </Text>
              <Text>{formatTime(status.tokenWhenCreated)}</Text>
            </div>
            <div data-testid="twitch-token-expires">
              <Text strong>Срок действия: </Text>
              <Text>
                {status.tokenExpiresIn
                  ? status.tokenExpiresIn
                  : "не ограничен"}
              </Text>
            </div>
            <div>
              <Text strong>Истёк: </Text>
              <Tag
                color={status.isTokenExpired ? "red" : "green"}
                data-testid="twitch-token-expired-tag"
              >
                {status.isTokenExpired ? "Да" : "Нет"}
              </Tag>
            </div>
          </Space>
        )}
      </Card>

      <Card
        title="Управление"
        className={styles.controlsCard}
        data-testid="twitch-auth-controls-card"
      >
        <Button
          type="primary"
          onClick={() => void handleReconnect()}
          loading={reconnecting}
          data-testid="button-reconnect"
        >
          Переподключить
        </Button>
      </Card>
    </div>
  );
};

export default TwitchAuthPage;