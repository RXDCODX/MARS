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
  Space,
  Spin,
} from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import { ChannelRewardRecord } from "@/shared/api";
import { messageOf } from "@/shared/types/OperationResult";
import { ChannelRewardsManager } from "@/shared/api/http-clients/ChannelRewardsManager";

import styles from "./TwitchRewardsPage.module.scss";

const TwitchRewardsPage: React.FC = () => {
  const api = useMemo(() => new ChannelRewardsManager(), []);

  const [rewards, setRewards] = useState<ChannelRewardRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState("");
  const [syncing, setSyncing] = useState(false);
  const [savingId, setSavingId] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<Record<string, Partial<ChannelRewardRecord>>>({});

  const loadRewards = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await api.channelRewardsManagerLocalList();
      const data = result.data?.result ?? [];
      setRewards((data as ChannelRewardRecord[]).filter(r => !r.isDeleted));
    } catch (error_) {
      const message = messageOf(error_, "Не удалось загрузить награды");
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [api]);

  useEffect(() => {
    loadRewards();
  }, [loadRewards]);

  const setDraft = (id: string, patch: Partial<ChannelRewardRecord>) => {
    setDrafts(prev => ({ ...prev, [id]: { ...prev[id], ...patch } }));
  };

  const effective = (reward: ChannelRewardRecord) => ({
    ...reward,
    ...drafts[reward.id],
  });

  const saveSettings = async (reward: ChannelRewardRecord) => {
    const current = effective(reward);
    setSavingId(reward.id);
    setError("");
    try {
      await api.channelRewardsManagerLocalUpdate(reward.id, {
        isEnabled: current.isEnabled,
        isMaxPerStreamEnabled: current.isMaxPerStreamEnabled,
        maxPerStream: current.isMaxPerStreamEnabled ? current.maxPerStream : undefined,
        isMaxPerUserPerStreamEnabled: current.isMaxPerUserPerStreamEnabled,
        maxPerUserPerStream: current.isMaxPerUserPerStreamEnabled
          ? current.maxPerUserPerStream
          : undefined,
        isGlobalCooldownEnabled: current.isGlobalCooldownEnabled,
        globalCooldownSeconds: current.isGlobalCooldownEnabled
          ? current.globalCooldownSeconds
          : undefined,
        shouldRedemptionsSkipRequestQueue:
          current.shouldRedemptionsSkipRequestQueue,
      });
      await api.channelRewardsManagerSyncCreate();
      setDrafts(prev => {
        const next = { ...prev };
        delete next[reward.id];
        return next;
      });
      await loadRewards();
    } catch (error_) {
      setError(messageOf(error_, "Не удалось сохранить настройки"));
    } finally {
      setSavingId(null);
    }
  };

  const disableReward = async (reward: ChannelRewardRecord) => {
    setSavingId(reward.id);
    setError("");
    try {
      await api.channelRewardsManagerLocalUpdate(reward.id, { isEnabled: false });
      await api.channelRewardsManagerSyncCreate();
      await loadRewards();
    } catch (error_) {
      setError(messageOf(error_, "Не удалось отключить награду"));
    } finally {
      setSavingId(null);
    }
  };

  const syncAll = async () => {
    setSyncing(true);
    setError("");
    try {
      await api.channelRewardsManagerSyncCreate();
      await loadRewards();
    } catch (error_) {
      setError(messageOf(error_, "Не удалось синхронизировать награды"));
    } finally {
      setSyncing(false);
    }
  };

  const filteredRewards = useMemo(() => {
    if (!filter.trim()) return rewards;
    const q = filter.toLowerCase();
    return rewards.filter(r =>
      [r.title, r.prompt, r.id].some(v =>
        (v || "").toLowerCase().includes(q)
      )
    );
  }, [rewards, filter]);

  return (
    <div className={styles.page}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 12 }}>
        <h1>Твич награды</h1>
        <Space>
          <Input
            placeholder="Поиск"
            value={filter}
            onChange={e => setFilter(e.target.value)}
            style={{ width: 220 }}
          />
          <Button onClick={syncAll} disabled={syncing || loading}>
            {syncing ? <Spin size="small" /> : "Синхронизировать"}
          </Button>
          <Button onClick={loadRewards} disabled={loading}>
            {loading ? <Spin size="small" /> : "Обновить"}
          </Button>
        </Space>
      </Flex>

      {!!error && <Alert type="error" message={error} style={{ marginBottom: 12 }} />}

      {loading ? (
        <Spin />
      ) : filteredRewards.length === 0 ? (
        <Alert type="info" message="Награды не найдены" />
      ) : (
        <Space direction="vertical" style={{ width: "100%" }}>
          {filteredRewards.map(reward => {
            const current = effective(reward);
            return (
              <Card key={reward.id} className={styles.card}>
                <Flex justify="space-between" align="start">
                  <Space direction="vertical" style={{ width: "100%" }}>
                    <Flex justify="space-between" align="center">
                      <strong>{reward.title}</strong>
                      <Badge
                        status={current.isEnabled ? "success" : "default"}
                        text={current.isEnabled ? "Включена" : "Отключена"}
                      />
                    </Flex>
                    <div>{reward.prompt}</div>
                    <Space wrap>
                      <Badge color={reward.backgroundColor} text={`Стоимость: ${reward.cost}`} />
                      <Badge text={`ID: ${reward.twitchRewardId || "—"}`} />
                      <Badge text={reward.isUserInputRequired ? "Требует ввод" : "Без ввода"} />
                    </Space>

                    <Row gutter={12}>
                      <Col span={6}>
                        <Checkbox
                          checked={current.isEnabled}
                          onChange={e =>
                            setDraft(reward.id, { isEnabled: e.target.checked })
                          }
                        >
                          Включена
                        </Checkbox>
                      </Col>
                      <Col span={6}>
                        <Checkbox
                          checked={current.isMaxPerStreamEnabled}
                          onChange={e =>
                            setDraft(reward.id, {
                              isMaxPerStreamEnabled: e.target.checked,
                            })
                          }
                        >
                          Лимит на стрим
                        </Checkbox>
                        <InputNumber
                          value={current.maxPerStream}
                          onChange={v => setDraft(reward.id, { maxPerStream: v ?? undefined })}
                          disabled={!current.isMaxPerStreamEnabled}
                          min={1}
                          style={{ width: "100%" }}
                        />
                      </Col>
                      <Col span={6}>
                        <Checkbox
                          checked={current.isMaxPerUserPerStreamEnabled}
                          onChange={e =>
                            setDraft(reward.id, {
                              isMaxPerUserPerStreamEnabled: e.target.checked,
                            })
                          }
                        >
                          Лимит на пользователя
                        </Checkbox>
                        <InputNumber
                          value={current.maxPerUserPerStream}
                          onChange={v =>
                            setDraft(reward.id, { maxPerUserPerStream: v ?? undefined })
                          }
                          disabled={!current.isMaxPerUserPerStreamEnabled}
                          min={1}
                          style={{ width: "100%" }}
                        />
                      </Col>
                      <Col span={6}>
                        <Checkbox
                          checked={current.isGlobalCooldownEnabled}
                          onChange={e =>
                            setDraft(reward.id, {
                              isGlobalCooldownEnabled: e.target.checked,
                            })
                          }
                        >
                          Cooldown
                        </Checkbox>
                        <InputNumber
                          value={current.globalCooldownSeconds}
                          onChange={v =>
                            setDraft(reward.id, { globalCooldownSeconds: v ?? undefined })
                          }
                          disabled={!current.isGlobalCooldownEnabled}
                          min={0}
                          style={{ width: "100%" }}
                        />
                      </Col>
                    </Row>

                    <Checkbox
                      checked={current.shouldRedemptionsSkipRequestQueue}
                      onChange={e =>
                        setDraft(reward.id, {
                          shouldRedemptionsSkipRequestQueue: e.target.checked,
                        })
                      }
                    >
                      Пропускать очередь запросов
                    </Checkbox>

                    <Space>
                      <Button
                        type="primary"
                        onClick={() => saveSettings(reward)}
                        loading={savingId === reward.id}
                      >
                        Сохранить
                      </Button>
                      <Button
                        danger
                        onClick={() => disableReward(reward)}
                        loading={savingId === reward.id}
                      >
                        Отключить
                      </Button>
                    </Space>
                  </Space>
                </Flex>
              </Card>
            );
          })}
        </Space>
      )}
    </div>
  );
};

export default TwitchRewardsPage;