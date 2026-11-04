import { Alert, Button, Card, Input, Space, Spin, Tag, Typography } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import type { VerificationCodeRequest } from "@/shared/api";
import { WTelegram } from "@/shared/api/http-clients/WTelegram";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./WTelegramPage.module.scss";

const { Text, Title } = Typography;

const POLL_INTERVAL_MS = 3000;

interface WTelegramClientStatus {
  isAuthenticated: boolean;
  userId?: number | null;
  username?: string | null;
  phone?: string | null;
  errorMessage?: string | null;
  isAwaitingCode: boolean;
}

interface WTelegramOperationResult {
  success: boolean;
  message: string;
  errorDetails?: string | null;
  clientStatus?: WTelegramClientStatus | null;
}

const WTelegramPage: React.FC = () => {
  const { showToast } = useToastModal();
  const api = useMemo(() => new WTelegram(), []);

  const [status, setStatus] = useState<WTelegramClientStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const fetchStatus = useCallback(async () => {
    try {
      const res = await api.wTelegramStatusList();
      setStatus(res.data as WTelegramClientStatus);
    } catch {
      // silent on poll
    }
  }, [api]);

  useEffect(() => {
    void fetchStatus();
    const interval = setInterval(() => void fetchStatus(), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [fetchStatus]);

  const handleRelogin = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await api.wTelegramReloginCreate();
      const result = res.data as WTelegramOperationResult;
      showToast(result.message || "Переавторизация запущена", "success");
      await fetchStatus();
    } catch (error_: unknown) {
      const message =
        error_ instanceof Error ? error_.message : "Ошибка переавторизации";
      setError(message);
      showToast(message, "error");
    } finally {
      setLoading(false);
    }
  }, [api, fetchStatus, showToast]);

  const handleSubmitCode = useCallback(async () => {
    if (!code.trim()) {
      return;
    }
    setSubmitting(true);
    setError("");
    try {
      const payload: VerificationCodeRequest = { code: code.trim() };
      const res = await api.wTelegramVerificationCodeCreate(payload);
      const result = res.data as WTelegramOperationResult;
      showToast(result.message || "Код отправлен", "success");
      setCode("");
      await fetchStatus();
    } catch (error_: unknown) {
      const message =
        error_ instanceof Error ? error_.message : "Ошибка отправки кода";
      setError(message);
      showToast(message, "error");
    } finally {
      setSubmitting(false);
    }
  }, [api, code, fetchStatus, showToast]);

  return (
    <div data-testid="page-wtelegram" className={styles.page}>
      <Title level={2}>WTelegram Клиент</Title>

      {error && (
        <Alert
          type="error"
          message={error}
          closable
          onClose={() => setError("")}
          style={{ marginBottom: "1rem" }}
          data-testid="wtelegram-error-alert"
        />
      )}

      <Card
        title="Статус"
        className={styles.statusCard}
        data-testid="wtelegram-status-card"
      >
        {status === null ? (
          <Spin data-testid="wtelegram-loading" />
        ) : (
          <Space direction="vertical" size="middle">
            <div>
              <Text strong>Авторизован: </Text>
              <Tag
                color={status.isAuthenticated ? "green" : "red"}
                data-testid="wtelegram-auth-tag"
              >
                {status.isAuthenticated ? "Да" : "Нет"}
              </Tag>
            </div>
            {status.userId != undefined && (
              <div data-testid="wtelegram-user-id">
                <Text strong>User ID: </Text>
                <Text>{status.userId}</Text>
              </div>
            )}
            {status.username && (
              <div data-testid="wtelegram-username">
                <Text strong>Username: </Text>
                <Text>@{status.username}</Text>
              </div>
            )}
            {status.phone && (
              <div data-testid="wtelegram-phone">
                <Text strong>Телефон: </Text>
                <Text>{status.phone}</Text>
              </div>
            )}
            {status.errorMessage && (
              <Alert
                type="error"
                message={status.errorMessage}
                data-testid="wtelegram-status-error"
              />
            )}
            {status.isAwaitingCode && (
              <Tag color="orange" data-testid="wtelegram-awaiting-code-tag">
                Ожидает код верификации
              </Tag>
            )}
          </Space>
        )}
      </Card>

      {status?.isAwaitingCode && (
        <Card
          title="Код верификации"
          className={styles.codeCard}
          data-testid="wtelegram-code-card"
        >
          <Space>
            <Input
              placeholder="Введите код из Telegram"
              value={code}
              onChange={e => setCode(e.target.value)}
              onPressEnter={() => void handleSubmitCode()}
              disabled={submitting}
              className={styles.codeInput}
              data-testid="input-verification-code"
            />
            <Button
              type="primary"
              onClick={() => void handleSubmitCode()}
              loading={submitting}
              disabled={!code.trim()}
              data-testid="button-submit-code"
            >
              Отправить
            </Button>
          </Space>
        </Card>
      )}

      <Card
        title="Управление"
        className={styles.controlsCard}
        data-testid="wtelegram-controls-card"
      >
        <Button
          onClick={() => void handleRelogin()}
          loading={loading}
          data-testid="button-relogin"
        >
          Переавторизация
        </Button>
      </Card>
    </div>
  );
};

export default WTelegramPage;
