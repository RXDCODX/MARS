import { Alert, Button, Card, Spin, Tag, Typography } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";

import { TwitchAuth } from "@/shared/api/http-clients/TwitchAuth";

import styles from "./TwitchAuthCallbackPage.module.scss";

const { Text, Title } = Typography;

const POLL_INTERVAL_MS = 3000;
const AUTO_CLOSE_DELAY_MS = 4000;

interface TwitchAuthStatus {
  hasToken: boolean;
  isTokenExpired: boolean;
}

const TwitchAuthCallbackPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const api = useMemo(() => new TwitchAuth(), []);

  const status = searchParams.get("status");
  const message = searchParams.get("message");

  const [authStatus, setAuthStatus] = useState<TwitchAuthStatus | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchStatus = useCallback(async () => {
    try {
      const res = await api.twitchAuthStatusList();
      const result = res.data?.result as TwitchAuthStatus | undefined;
      if (result) {
        setAuthStatus(result);
      }
    } catch {
      // silent on poll
    } finally {
      setLoading(false);
    }
  }, [api]);

  useEffect(() => {
    void fetchStatus();
    const interval = setInterval(() => void fetchStatus(), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [fetchStatus]);

  useEffect(() => {
    if (status !== "success") {
      return;
    }
    // После успешного OAuth поднимаем чат сразу, не дожидаясь 30-секундного
    // health-check: токен уже в БД, достаточно попросить сервис переподключиться.
    void api.twitchAuthReconnectCreate().catch(() => {
      // молчим: health-check поднимет чат сам
    });
    // Вкладка открыта скриптом (window.open) — закрываем её сами после того,
    // как результат показан. Для захода по ссылке из Telegram close() не сработает,
    // поэтому дополнительно показываем кнопку «Закрыть вкладку».
    if (window.opener) {
      const timer = window.setTimeout(() => {
        try {
          window.close();
        } catch {
          // ignore
        }
      }, AUTO_CLOSE_DELAY_MS);
      return () => window.clearTimeout(timer);
    }
    return undefined;
  }, [status, api]);

  const handleManualClose = useCallback(() => {
    try {
      window.close();
    } catch {
      // ignore
    }
  }, []);

  const isSuccess = status === "success";

  return (
    <div data-testid="page-twitch-auth-callback" className={styles.page}>
      <Title level={2}>Результат авторизации Twitch</Title>

      {status === null && (
        <Alert
          type="info"
          message="Результат авторизации неизвестен"
          description="Откройте страницу Twitch Auth и повторите вход."
          showIcon
          style={{ marginBottom: "1rem" }}
          data-testid="twitch-auth-callback-unknown"
        />
      )}

      {isSuccess && (
        <Alert
          type="success"
          message="Авторизация Twitch прошла успешно"
          description={
            message
              ? message
              : "Токен получен. Эту вкладку можно закрыть, статус обновится автоматически."
          }
          showIcon
          closable
          style={{ marginBottom: "1rem" }}
          data-testid="twitch-auth-callback-success"
        />
      )}

      {status === "error" && (
        <Alert
          type="error"
          message="Ошибка авторизации Twitch"
          description={message || "Не удалось завершить авторизацию."}
          showIcon
          closable
          style={{ marginBottom: "1rem" }}
          data-testid="twitch-auth-callback-error"
        />
      )}

      <Card title="Статус токена" className={styles.card} data-testid="twitch-auth-callback-status-card">
        {loading && authStatus === null ? (
          <Spin data-testid="twitch-auth-callback-loading" />
        ) : (
          <div>
            <Text strong>Токен получен: </Text>
            <Tag
              color={authStatus?.hasToken ? "green" : "red"}
              data-testid="twitch-auth-callback-has-token-tag"
            >
              {authStatus?.hasToken ? "Да" : "Нет"}
            </Tag>
            {authStatus?.isTokenExpired && (
              <Tag color="red" data-testid="twitch-auth-callback-expired-tag" style={{ marginLeft: 8 }}>
                Истёк
              </Tag>
            )}
          </div>
        )}
      </Card>

      <Card title="Действия" className={styles.card} data-testid="twitch-auth-callback-actions-card">
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <Link to="/twitch-auth">
            <Button type="primary" data-testid="button-go-to-twitch-auth">
              К странице Twitch Auth
            </Button>
          </Link>
          {window.opener && (
            <Button onClick={handleManualClose} data-testid="button-close-tab">
              Закрыть вкладку
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
};

export default TwitchAuthCallbackPage;