import {
  Alert,
  Badge,
  Button,
  Card,
  Checkbox,
  Col,
  Flex,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Spin,
} from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import {
  CreateCustomRewardsRequest,
  CustomReward,
  UpdateCustomRewardRequest,
} from "@/shared/api";
import { TwitchRewards } from "@/shared/api/http-clients/TwitchRewards";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./TwitchRewardsPage.module.scss";

interface RewardFormState {
  title: string;
  prompt: string;
  cost: number;
  isEnabled: boolean;
  backgroundColor: string;
  isUserInputRequired: boolean;
  isMaxPerStreamEnabled: boolean;
  maxPerStream?: number;
  isMaxPerUserPerStreamEnabled: boolean;
  maxPerUserPerStream?: number;
  isGlobalCooldownEnabled: boolean;
  globalCooldownSeconds?: number;
  shouldRedemptionsSkipRequestQueue: boolean;
}

const defaultForm: RewardFormState = {
  title: "",
  prompt: "",
  cost: 100,
  isEnabled: true,
  backgroundColor: "#9146FF",
  isUserInputRequired: false,
  isMaxPerStreamEnabled: false,
  maxPerStream: undefined,
  isMaxPerUserPerStreamEnabled: false,
  maxPerUserPerStream: undefined,
  isGlobalCooldownEnabled: false,
  globalCooldownSeconds: undefined,
  shouldRedemptionsSkipRequestQueue: false,
};

const TwitchRewardsPage: React.FC = () => {
  const { showToast } = useToastModal();
  const api = useMemo(() => new TwitchRewards(), []);

  const [rewards, setRewards] = useState<CustomReward[]>([]);
  const [onlyManageable, setOnlyManageable] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");
  const [form, setForm] = useState<RewardFormState>({ ...defaultForm });
  const [creating, setCreating] = useState(false);
  const [filter, setFilter] = useState("");

  const loadRewards = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await api.twitchRewardsList({ onlyManageable });
      const data = Array.isArray(result.data.data) ? result.data.data : [];
      setRewards(data);
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось загрузить награды";
      setError(message);
      showToast({ success: false, message: message });
    } finally {
      setLoading(false);
    }
  }, [api, onlyManageable, showToast]);

  useEffect(() => {
    loadRewards();
  }, [loadRewards]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreating(true);
    setError("");

    const payload: CreateCustomRewardsRequest = {
      title: form.title,
      prompt: form.prompt,
      cost: Number(form.cost) || 0,
      isEnabled: form.isEnabled,
      backgroundColor: form.backgroundColor || undefined,
      isUserInputRequired: form.isUserInputRequired,
      isMaxPerStreamEnabled: form.isMaxPerStreamEnabled,
      maxPerStream: form.isMaxPerStreamEnabled ? form.maxPerStream : undefined,
      isMaxPerUserPerStreamEnabled: form.isMaxPerUserPerStreamEnabled,
      maxPerUserPerStream: form.isMaxPerUserPerStreamEnabled
        ? form.maxPerUserPerStream
        : undefined,
      isGlobalCooldownEnabled: form.isGlobalCooldownEnabled,
      globalCooldownSeconds: form.isGlobalCooldownEnabled
        ? form.globalCooldownSeconds
        : undefined,
      shouldRedemptionsSkipRequestQueue: form.shouldRedemptionsSkipRequestQueue,
    };

    try {
      const result = await api.twitchRewardsCreate(payload);
      showToast(result.data);
      setForm({ ...defaultForm });
      await loadRewards();
    } catch (error_) {
      const message =
        error_ instanceof Error ? error_.message : "Не удалось создать награду";
      showToast({ success: false, message: message });
      setError(message);
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (rewardId?: string) => {
    if (!rewardId) return;
    try {
      const result = await api.twitchRewardsDelete(rewardId);
      showToast(result.data);
      await loadRewards();
    } catch (error_) {
      const message =
        error_ instanceof Error ? error_.message : "Не удалось удалить награду";
      showToast({ success: false, message: message });
      setError(message);
    }
  };

  const handleRecreate = async (reward: CustomReward) => {
    try {
      if (reward.id) {
        const deleteResult = await api.twitchRewardsDelete(reward.id);
        showToast(deleteResult.data);
      }

      const recreatePayload: CreateCustomRewardsRequest = {
        title: reward.title || "",
        prompt: reward.prompt || "",
        cost: reward.cost ?? 0,
        isEnabled: reward.isEnabled,
        backgroundColor: reward.backgroundColor,
        isUserInputRequired: reward.isUserInputRequired,
        isMaxPerStreamEnabled: !!reward.maxPerStreamSetting?.isEnabled,
        maxPerStream: reward.maxPerStreamSetting?.maxPerStream,
        isMaxPerUserPerStreamEnabled:
          !!reward.maxPerUserPerStreamSetting?.isEnabled,
        maxPerUserPerStream:
          reward.maxPerUserPerStreamSetting?.maxPerUserPerStream,
        isGlobalCooldownEnabled: !!reward.globalCooldownSetting?.isEnabled,
        globalCooldownSeconds:
          reward.globalCooldownSetting?.globalCooldownSeconds,
        shouldRedemptionsSkipRequestQueue: reward.shouldRedemptionsSkipQueue,
      };

      const result = await api.twitchRewardsCreate(recreatePayload);
      showToast(result.data);
      await loadRewards();
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось пересоздать награду";
      showToast({ success: false, message: message });
      setError(message);
    }
  };

  const handleQuickDisable = async (reward: CustomReward) => {
    if (!reward.id) return;
    try {
      const payload: UpdateCustomRewardRequest = { isEnabled: false };
      const result = await api.twitchRewardsPartialUpdate(reward.id, payload);
      showToast(result.data);
      await loadRewards();
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось обновить награду";
      showToast({ success: false, message: message });
      setError(message);
    }
  };

  const filteredRewards = useMemo(() => {
    if (!filter.trim()) return rewards;
    const q = filter.toLowerCase();
    return rewards.filter(r =>
      [r.title, r.prompt, r.id].some(v => (v || "").toLowerCase().includes(q))
    );
  }, [rewards, filter]);

  return (
    <div className={styles.page}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 12 }}>
        <h1>Твич награды</h1>
        <Space>
          <Space>
            <Checkbox
              checked={onlyManageable}
              onChange={e => setOnlyManageable(e.target.checked)}
            >
              Только управляемые
            </Checkbox>
          </Space>
          <Button
            type="default"
            size="small"
            onClick={loadRewards}
            disabled={loading}
          >
            {loading ? (
              <Spin size="small" />
            ) : (
              <i className="bi bi-arrow-clockwise" />
            )}
          </Button>
        </Space>
      </Flex>

      {!!error && (
        <Alert type="error" message={error} style={{ marginBottom: 12 }} />
      )}

      <Card className={styles.card} style={{ marginBottom: 16 }}>
        <h5 style={{ marginBottom: 12 }}>Создать награду</h5>
        <form onSubmit={handleCreate}>
          <Row gutter={16}>
            <Col md={16}>
              <div className={styles.formGrid}>
                <Input
                  placeholder="Заголовок"
                  value={form.title}
                  onChange={e =>
                    setForm(s => ({ ...s, title: e.target.value }))
                  }
                />
                <Input
                  placeholder="Описание (prompt)"
                  value={form.prompt}
                  onChange={e =>
                    setForm(s => ({ ...s, prompt: e.target.value }))
                  }
                />
                <InputNumber
                  min={1}
                  placeholder="Стоимость"
                  value={form.cost}
                  onChange={value => setForm(s => ({ ...s, cost: value ?? 0 }))}
                  style={{ width: "100%" }}
                />
                <Space.Compact style={{ width: "100%" }}>
                  <span
                    style={{
                      padding: "4px 11px",
                      background: "#f5f5f5",
                      border: "1px solid #d9d9d9",
                      borderRight: "none",
                      borderRadius: "6px 0 0 6px",
                      display: "flex",
                      alignItems: "center",
                    }}
                  >
                    Цвет
                  </span>
                  <Input
                    placeholder="#9146FF"
                    value={form.backgroundColor}
                    onChange={e =>
                      setForm(s => ({
                        ...s,
                        backgroundColor: e.target.value,
                      }))
                    }
                    style={{ borderRadius: "0 6px 6px 0" }}
                  />
                </Space.Compact>
              </div>
            </Col>
            <Col md={8}>
              <Flex vertical gap={8}>
                <Checkbox
                  checked={form.isEnabled}
                  onChange={e =>
                    setForm(s => ({ ...s, isEnabled: e.target.checked }))
                  }
                >
                  Включена
                </Checkbox>
                <Checkbox
                  checked={form.isUserInputRequired}
                  onChange={e =>
                    setForm(s => ({
                      ...s,
                      isUserInputRequired: e.target.checked,
                    }))
                  }
                >
                  Требует ввод пользователя
                </Checkbox>
                <Checkbox
                  checked={form.isMaxPerStreamEnabled}
                  onChange={e =>
                    setForm(s => ({
                      ...s,
                      isMaxPerStreamEnabled: e.target.checked,
                    }))
                  }
                >
                  Лимит на стрим
                </Checkbox>
                {form.isMaxPerStreamEnabled && (
                  <InputNumber
                    min={1}
                    placeholder="maxPerStream"
                    value={form.maxPerStream ?? 1}
                    onChange={value =>
                      setForm(s => ({
                        ...s,
                        maxPerStream: value ?? undefined,
                      }))
                    }
                    style={{ width: "100%" }}
                  />
                )}
                <Checkbox
                  checked={form.isMaxPerUserPerStreamEnabled}
                  onChange={e =>
                    setForm(s => ({
                      ...s,
                      isMaxPerUserPerStreamEnabled: e.target.checked,
                    }))
                  }
                >
                  Лимит на пользователя
                </Checkbox>
                {form.isMaxPerUserPerStreamEnabled && (
                  <InputNumber
                    min={1}
                    placeholder="maxPerUserPerStream"
                    value={form.maxPerUserPerStream ?? 1}
                    onChange={value =>
                      setForm(s => ({
                        ...s,
                        maxPerUserPerStream: value ?? undefined,
                      }))
                    }
                    style={{ width: "100%" }}
                  />
                )}
                <Checkbox
                  checked={form.isGlobalCooldownEnabled}
                  onChange={e =>
                    setForm(s => ({
                      ...s,
                      isGlobalCooldownEnabled: e.target.checked,
                    }))
                  }
                >
                  Глобальный кулдаун
                </Checkbox>
                {form.isGlobalCooldownEnabled && (
                  <InputNumber
                    min={1}
                    placeholder="cooldownSeconds"
                    value={form.globalCooldownSeconds ?? 60}
                    onChange={value =>
                      setForm(s => ({
                        ...s,
                        globalCooldownSeconds: value ?? undefined,
                      }))
                    }
                    style={{ width: "100%" }}
                  />
                )}
                <Checkbox
                  checked={form.shouldRedemptionsSkipRequestQueue}
                  onChange={e =>
                    setForm(s => ({
                      ...s,
                      shouldRedemptionsSkipRequestQueue: e.target.checked,
                    }))
                  }
                >
                  Пропуск очереди
                </Checkbox>
              </Flex>
            </Col>
          </Row>

          <div
            style={{
              display: "flex",
              justifyContent: "flex-end",
              marginTop: 12,
            }}
          >
            <Button type="primary" htmlType="submit" disabled={creating}>
              {creating ? (
                <>
                  <Spin size="small" style={{ marginRight: 8 }} />
                  Создание...
                </>
              ) : (
                "Создать"
              )}
            </Button>
          </div>
        </form>
      </Card>

      <Card className={styles.card}>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: 12,
          }}
        >
          <h5 style={{ marginBottom: 0 }}>Список наград</h5>
          <Input
            className={styles.searchBar}
            placeholder="Поиск по названию, описанию, id"
            value={filter}
            onChange={e => setFilter(e.target.value)}
            style={{ width: 300 }}
          />
        </div>
        {loading ? (
          <div style={{ textAlign: "center", padding: "20px 0" }}>
            <Spin />
            <div style={{ marginTop: 8 }}>Загрузка...</div>
          </div>
        ) : filteredRewards.length === 0 ? (
          <Alert type="info" message="Награды не найдены" />
        ) : (
          <Row gutter={[16, 16]}>
            {filteredRewards.map(r => (
              <Col key={r.id || r.title} xs={24} sm={12} md={8} lg={6}>
                <Card className={styles.card} size="small">
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "flex-start",
                    }}
                  >
                    <div>
                      <h6 style={{ marginBottom: 4 }}>{r.title}</h6>
                      <div style={{ color: "#8c8c8c", fontSize: 12 }}>
                        {r.prompt}
                      </div>
                    </div>
                    <Badge color={r.isEnabled ? "green" : "default"}>
                      {r.isEnabled ? "Включена" : "Выключена"}
                    </Badge>
                  </div>
                  <div
                    style={{
                      marginTop: 8,
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <div className={styles.badgeRow}>
                      <Badge color="blue">{r.cost} pts</Badge>
                      {r.backgroundColor && (
                        <Badge color="default">{r.backgroundColor}</Badge>
                      )}
                      {r.isUserInputRequired && (
                        <Badge color="gold" style={{ color: "#000" }}>
                          Требует ввод
                        </Badge>
                      )}
                      {r.maxPerStreamSetting?.isEnabled && (
                        <Badge color="cyan">
                          max/stream: {r.maxPerStreamSetting.maxPerStream}
                        </Badge>
                      )}
                      {r.maxPerUserPerStreamSetting?.isEnabled && (
                        <Badge color="cyan">
                          max/user:{" "}
                          {r.maxPerUserPerStreamSetting.maxPerUserPerStream}
                        </Badge>
                      )}
                      {r.globalCooldownSetting?.isEnabled && (
                        <Badge color="default">
                          cd: {r.globalCooldownSetting.globalCooldownSeconds}s
                        </Badge>
                      )}
                    </div>
                  </div>
                  <div className={styles.actionsRow} style={{ marginTop: 12 }}>
                    <Button
                      size="small"
                      type="primary"
                      ghost
                      onClick={() => handleRecreate(r)}
                    >
                      Пересоздать
                    </Button>
                    <Button
                      size="small"
                      type="default"
                      onClick={() => handleQuickDisable(r)}
                      disabled={!r.isEnabled}
                    >
                      Выключить
                    </Button>
                    <Button
                      size="small"
                      danger
                      ghost
                      onClick={() => handleDelete(r.id)}
                    >
                      Удалить
                    </Button>
                  </div>
                </Card>
              </Col>
            ))}
          </Row>
        )}
      </Card>
    </div>
  );
};

export default TwitchRewardsPage;
