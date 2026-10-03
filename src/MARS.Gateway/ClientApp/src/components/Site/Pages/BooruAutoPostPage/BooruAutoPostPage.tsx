import {
  Alert,
  Badge,
  Button,
  Card,
  Input,
  Modal,
  Select,
  Space,
  Spin,
  Tag,
  Tooltip,
} from "antd";
import {
  Check,
  Clock,
  Edit3,
  Hash,
  MessageCircle,
  Play,
  Plus,
  Send,
  Trash2,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import type {
  BooruAutoPostConfigDto,
  BooruAutoPostCreateRequest,
  BooruAutoPostUpdateRequest,
  DiscordChannelOptionDto,
  TelegramChannelOptionDto,
} from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { BooruAutoPost } from "@/shared/api/http-clients/BooruAutoPost";
import {
  BooruAutoPostConfigDtoSourceEnum,
  BooruAutoPostConfigDtoTargetPlatformEnum,
  BooruAutoPostConfigDtoTelegramParseModeEnum,
  BooruAutoPostCreateRequestSourceEnum,
  BooruAutoPostCreateRequestTargetPlatformEnum,
  BooruAutoPostCreateRequestTelegramParseModeEnum,
  BooruAutoPostUpdateRequestTargetPlatformEnum,
} from "@/shared/api/types/data-contracts";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./BooruAutoPostPage.module.scss";

type Platform = "Discord" | "Telegram";
type FilterTab = "all" | "discord" | "telegram";
type SourceFilterTab = "all" | "danbooru" | "rule34";

interface FormState {
  source: "Danbooru" | "Rule34";
  targetPlatform: Platform;
  discordChannelId: string;
  telegramChannelId: string;
  tags: string[];
  cronExpression: string;
  planningHorizonDays: number;
  message: string;
  telegramParseMode: "Default" | "Html" | "Markdown";
}

const defaultForm: FormState = {
  source: "Danbooru",
  targetPlatform: "Discord",
  discordChannelId: "",
  telegramChannelId: "",
  tags: [],
  cronExpression: "",
  planningHorizonDays: 60,
  message: "",
  telegramParseMode: "Html",
};

interface CronParts {
  minute: string;
  hour: string;
  dayOfMonth: string;
  month: string;
  dayOfWeek: string;
}

interface CronField {
  label: string;
  value: string;
}

const cronMinutes: CronField[] = [
  { label: "каждую минуту", value: "*" },
  { label: "каждые 5 мин", value: "*/5" },
  { label: "каждые 10 мин", value: "*/10" },
  { label: "каждые 15 мин", value: "*/15" },
  { label: "каждые 30 мин", value: "*/30" },
  { label: "0", value: "0" },
  { label: "15", value: "15" },
  { label: "30", value: "30" },
  { label: "45", value: "45" },
];

const cronHours: CronField[] = [
  { label: "каждый час", value: "*" },
  { label: "каждые 2 часа", value: "*/2" },
  { label: "каждые 3 часа", value: "*/3" },
  { label: "каждые 6 часов", value: "*/6" },
  { label: "каждые 12 часов", value: "*/12" },
  ...Array.from({ length: 24 }, (_, index) => ({
    label: String(index).padStart(2, "0"),
    value: String(index),
  })),
];

const cronDaysOfMonth: CronField[] = [
  { label: "каждый день", value: "*" },
  ...Array.from({ length: 31 }, (_, index) => ({
    label: String(index + 1),
    value: String(index + 1),
  })),
];

const cronMonths: CronField[] = [
  { label: "каждый месяц", value: "*" },
  { label: "янв", value: "1" },
  { label: "фев", value: "2" },
  { label: "мар", value: "3" },
  { label: "апр", value: "4" },
  { label: "май", value: "5" },
  { label: "июн", value: "6" },
  { label: "июл", value: "7" },
  { label: "авг", value: "8" },
  { label: "сен", value: "9" },
  { label: "окт", value: "10" },
  { label: "ноя", value: "11" },
  { label: "дек", value: "12" },
];

const cronDaysOfWeek: CronField[] = [
  { label: "любой день", value: "*" },
  { label: "будни", value: "1-5" },
  { label: "выходные", value: "0,6" },
  { label: "пн", value: "1" },
  { label: "вт", value: "2" },
  { label: "ср", value: "3" },
  { label: "чт", value: "4" },
  { label: "пт", value: "5" },
  { label: "сб", value: "6" },
  { label: "вс", value: "0" },
];

const BooruAutoPostPage: React.FC = () => {
  const { showToast } = useToastModal();
  const api = useMemo(() => new BooruAutoPost(defaultApiConfig), []);

  const [configs, setConfigs] = useState<BooruAutoPostConfigDto[]>([]);
  const [discordChannels, setDiscordChannels] = useState<
    DiscordChannelOptionDto[]
  >([]);
  const [telegramChannels, setTelegramChannels] = useState<
    TelegramChannelOptionDto[]
  >([]);
  const [form, setForm] = useState<FormState>({ ...defaultForm });
  const [loading, setLoading] = useState(true);
  const [loadingDiscord, setLoadingDiscord] = useState(false);
  const [loadingTelegram, setLoadingTelegram] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState<string | undefined>(undefined);
  const [filter, setFilter] = useState("");
  const [filterTab, setFilterTab] = useState<FilterTab>("all");
  const [sourceFilterTab, setSourceFilterTab] =
    useState<SourceFilterTab>("all");
  const [processingIds, setProcessingIds] = useState<Record<string, boolean>>(
    {}
  );
  const [error, setError] = useState("");
  const [showChannelPicker, setShowChannelPicker] = useState(false);
  const [channelPickerType, setChannelPickerType] =
    useState<Platform>("Discord");
  const [channelSearch, setChannelSearch] = useState("");
  const [cronMode, setCronMode] = useState<"visual" | "manual">("visual");
  const [cronParts, setCronParts] = useState({
    minute: "0",
    hour: "*",
    dayOfMonth: "*",
    month: "*",
    dayOfWeek: "*",
  });

  const cronExpression = useMemo(
    () =>
      `${cronParts.minute} ${cronParts.hour} ${cronParts.dayOfMonth} ${cronParts.month} ${cronParts.dayOfWeek}`,
    [cronParts]
  );

  const discordChannelMap = useMemo(
    () =>
      new Map(
        discordChannels.map(ch => [
          String(ch.id),
          `${ch.guildName} / #${ch.name}`,
        ])
      ),
    [discordChannels]
  );

  const telegramChannelMap = useMemo(
    () => new Map(telegramChannels.map(ch => [String(ch.id), ch.title])),
    [telegramChannels]
  );

  const filteredPickerChannels = useMemo(() => {
    const query = channelSearch.trim().toLowerCase();
    if (channelPickerType === "Discord") {
      if (!query) return discordChannels;
      return discordChannels.filter(
        ch =>
          ch.name.toLowerCase().includes(query) ||
          ch.guildName.toLowerCase().includes(query) ||
          String(ch.id).includes(query)
      );
    }
    if (!query) return telegramChannels;
    return telegramChannels.filter(
      ch =>
        ch.title.toLowerCase().includes(query) || String(ch.id).includes(query)
    );
  }, [discordChannels, telegramChannels, channelSearch, channelPickerType]);

  const filteredConfigs = useMemo(() => {
    let result = configs;

    if (sourceFilterTab === "danbooru") {
      result = result.filter(
        c => c.source === BooruAutoPostConfigDtoSourceEnum.Danbooru
      );
    } else if (sourceFilterTab === "rule34") {
      result = result.filter(
        c => c.source === BooruAutoPostConfigDtoSourceEnum.Rule34
      );
    }

    if (filterTab === "discord") {
      result = result.filter(
        c =>
          c.targetPlatform === BooruAutoPostConfigDtoTargetPlatformEnum.Discord
      );
    } else if (filterTab === "telegram") {
      result = result.filter(
        c =>
          c.targetPlatform === BooruAutoPostConfigDtoTargetPlatformEnum.Telegram
      );
    }

    const query = filter.trim().toLowerCase();
    if (query) {
      result = result.filter(config => {
        const channelId = String(
          config.discordChannelId ?? config.telegramChannelId ?? ""
        );
        const channelName = getChannelDisplayName(config).toLowerCase();
        const tags = (config.tags ?? "").toLowerCase();
        const cron = (config.cronExpression ?? "").toLowerCase();

        return (
          channelId.includes(query) ||
          channelName.includes(query) ||
          tags.includes(query) ||
          cron.includes(query)
        );
      });
    }

    return result;
  }, [configs, filterTab, filter]);

  const getChannelDisplayName = useCallback(
    (config: BooruAutoPostConfigDto) => {
      if (
        config.targetPlatform ===
        BooruAutoPostConfigDtoTargetPlatformEnum.Telegram
      ) {
        return (
          telegramChannelMap.get(String(config.telegramChannelId ?? "")) ??
          "Неизвестный канал"
        );
      }
      return (
        discordChannelMap.get(String(config.discordChannelId)) ??
        "Неизвестный канал"
      );
    },
    [discordChannelMap, telegramChannelMap]
  );

  const loadConfigs = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const result = await api.booruAutoPostConfigsList();
      setConfigs(Array.isArray(result.data.data) ? result.data.data : []);
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось загрузить конфигурации";
      setError(message);
      showToast({ success: false, message });
    } finally {
      setLoading(false);
    }
  }, [api, showToast]);

  const loadDiscordChannels = useCallback(async () => {
    setLoadingDiscord(true);

    try {
      const result = await api.booruAutoPostDiscordChannelsList();
      setDiscordChannels(
        Array.isArray(result.data.data) ? result.data.data : []
      );
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось загрузить Discord каналы";
      showToast({ success: false, message });
    } finally {
      setLoadingDiscord(false);
    }
  }, [api, showToast]);

  const loadTelegramChannels = useCallback(async () => {
    setLoadingTelegram(true);

    try {
      const result = await api.booruAutoPostTelegramChannelsList();
      setTelegramChannels(
        Array.isArray(result.data.data) ? result.data.data : []
      );
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось загрузить Telegram каналы";
      showToast({ success: false, message });
    } finally {
      setLoadingTelegram(false);
    }
  }, [api, showToast]);

  useEffect(() => {
    void loadConfigs();
  }, [loadConfigs]);

  useEffect(() => {
    void loadDiscordChannels();
  }, [loadDiscordChannels]);

  useEffect(() => {
    void loadTelegramChannels();
  }, [loadTelegramChannels]);

  useEffect(() => {
    if (showModal) {
      setForm(previous => ({
        ...previous,
        cronExpression:
          cronMode === "visual" ? cronExpression : previous.cronExpression,
      }));
    }
  }, [cronExpression, cronMode, showModal]);

  const openCreateModal = useCallback(() => {
    setForm({ ...defaultForm });
    setEditingId(undefined);
    setCronMode("visual");
    setCronParts({
      minute: "0",
      hour: "*",
      dayOfMonth: "*",
      month: "*",
      dayOfWeek: "*",
    });
    setShowModal(true);
  }, []);

  const openEditModal = useCallback((config: BooruAutoPostConfigDto) => {
    const tags = (config.tags ?? "").split(/\s+/).filter(Boolean);
    const parts = (config.cronExpression ?? "").split(/\s+/);
    const parsed = {
      minute: parts[0] ?? "0",
      hour: parts[1] ?? "*",
      dayOfMonth: parts[2] ?? "*",
      month: parts[3] ?? "*",
      dayOfWeek: parts[4] ?? "*",
    };
    setCronParts(parsed);

    const isTelegram =
      config.targetPlatform ===
      BooruAutoPostConfigDtoTargetPlatformEnum.Telegram;

    setForm({
      source:
        config.source === BooruAutoPostConfigDtoSourceEnum.Danbooru
          ? "Danbooru"
          : "Rule34",
      targetPlatform: isTelegram ? "Telegram" : "Discord",
      discordChannelId: isTelegram ? "" : String(config.discordChannelId),
      telegramChannelId: isTelegram
        ? String(config.telegramChannelId ?? "")
        : "",
      tags,
      cronExpression: config.cronExpression ?? "",
      planningHorizonDays: config.planningHorizonDays ?? 60,
      message: config.message ?? "",
      telegramParseMode:
        (config.telegramParseMode as "Default" | "Html" | "Markdown") ?? "Html",
    });
    setEditingId(config.id);
    setCronMode("visual");
    setShowModal(true);
  }, []);

  const openChannelPicker = useCallback((type: Platform) => {
    setChannelPickerType(type);
    setChannelSearch("");
    setShowChannelPicker(true);
  }, []);

  const handleSubmit = useCallback(
    async (event: React.FormEvent) => {
      event.preventDefault();
      setSubmitting(true);
      setError("");

      const isTelegram = form.targetPlatform === "Telegram";

      if (isTelegram && !form.telegramChannelId) {
        const message = "Выберите Telegram канал";
        setError(message);
        showToast({ success: false, message });
        setSubmitting(false);
        return;
      }

      if (!isTelegram && !form.discordChannelId) {
        const message = "Выберите Discord канал";
        setError(message);
        showToast({ success: false, message });
        setSubmitting(false);
        return;
      }

      const finalCron =
        cronMode === "visual" ? cronExpression : form.cronExpression;
      const hasCron = finalCron.trim().length > 0;

      if (!hasCron) {
        const message = "Укажите CRON выражение";
        setError(message);
        showToast({ success: false, message });
        setSubmitting(false);
        return;
      }

      if (form.tags.length > 2) {
        const message = "Максимум 2 тега (ограничение платформы)";
        setError(message);
        showToast({ success: false, message });
        setSubmitting(false);
        return;
      }

      if (
        !form.planningHorizonDays ||
        form.planningHorizonDays < 1 ||
        form.planningHorizonDays > 365
      ) {
        const message = "Горизонт планирования должен быть от 1 до 365 дней";
        setError(message);
        showToast({ success: false, message });
        setSubmitting(false);
        return;
      }

      try {
        const targetPlatform = isTelegram
          ? BooruAutoPostCreateRequestTargetPlatformEnum.Telegram
          : BooruAutoPostCreateRequestTargetPlatformEnum.Discord;

        const source =
          form.source === "Danbooru"
            ? BooruAutoPostCreateRequestSourceEnum.Danbooru
            : BooruAutoPostCreateRequestSourceEnum.Rule34;

        const baseData = {
          source,
          targetPlatform,
          discordChannelId: isTelegram ? "0" : form.discordChannelId,
          telegramChannelId: isTelegram ? form.telegramChannelId : undefined,
          tags: form.tags.join(" ").trim(),
          cronExpression: hasCron ? finalCron.trim() : "",
          planningHorizonDays: form.planningHorizonDays,
          message: form.message,
          telegramParseMode: isTelegram
            ? (BooruAutoPostCreateRequestTelegramParseModeEnum[
                form.telegramParseMode
              ] ?? BooruAutoPostCreateRequestTelegramParseModeEnum.Html)
            : BooruAutoPostCreateRequestTelegramParseModeEnum.Html,
        };

        if (editingId) {
          const updateData: BooruAutoPostUpdateRequest = {
            ...baseData,
            id: editingId,
          };
          const result = await api.booruAutoPostConfigsUpdate(
            editingId,
            updateData
          );
          showToast(result.data);
        } else {
          const createData: BooruAutoPostCreateRequest = baseData;
          const result = await api.booruAutoPostConfigsCreate(createData);
          showToast(result.data);
        }

        setForm({ ...defaultForm });
        setShowModal(false);
        setEditingId(undefined);
        await loadConfigs();
      } catch (error_) {
        const message =
          error_ instanceof Error
            ? error_.message
            : "Не удалось сохранить конфигурацию";
        setError(message);
        showToast({ success: false, message });
      } finally {
        setSubmitting(false);
      }
    },
    [api, form, editingId, showToast, loadConfigs, cronMode, cronExpression]
  );

  const handleDelete = useCallback(
    async (id: string) => {
      setProcessingIds(previous => ({ ...previous, [id]: true }));
      try {
        const result = await api.booruAutoPostConfigsDelete(id);
        showToast(result.data);
        await loadConfigs();
      } catch (error_) {
        const message =
          error_ instanceof Error
            ? error_.message
            : "Не удалось удалить конфигурацию";
        setError(message);
        showToast({ success: false, message });
      } finally {
        setProcessingIds(previous => ({ ...previous, [id]: false }));
      }
    },
    [api, loadConfigs, showToast]
  );

  const handleToggleEnabled = useCallback(
    async (config: BooruAutoPostConfigDto) => {
      setProcessingIds(previous => ({
        ...previous,
        [config.id]: true,
      }));
      try {
        const result = await api.booruAutoPostConfigsEnabledUpdate(config.id, {
          isEnabled: !config.isEnabled,
        });
        showToast(result.data);
        await loadConfigs();
      } catch (error_) {
        const message =
          error_ instanceof Error
            ? error_.message
            : "Не удалось обновить статус";
        setError(message);
        showToast({ success: false, message });
      } finally {
        setProcessingIds(previous => ({
          ...previous,
          [config.id]: false,
        }));
      }
    },
    [api, loadConfigs, showToast]
  );

  const handleTriggerNow = useCallback(
    async (id: string) => {
      setProcessingIds(previous => ({ ...previous, [id]: true }));
      try {
        const result = await api.booruAutoPostConfigsTriggerCreate(id);
        showToast(result.data);
      } catch (error_) {
        const message =
          error_ instanceof Error
            ? error_.message
            : "Не удалось выполнить триггер";
        setError(message);
        showToast({ success: false, message });
      } finally {
        setProcessingIds(previous => ({ ...previous, [id]: false }));
      }
    },
    [api, showToast]
  );

  const renderTags = (tagsString: string) => {
    if (!tagsString) {
      return <span style={{ color: "#999" }}>—</span>;
    }
    const tags = tagsString.split(/\s+/).filter(Boolean);
    return (
      <Space size={[4, 4]} wrap>
        {tags.map(tag => (
          <Tag key={tag} color="blue">
            {tag}
          </Tag>
        ))}
      </Space>
    );
  };

  const renderConfigCard = (config: BooruAutoPostConfigDto) => {
    const isProcessing = Object.hasOwn(processingIds, config.id);
    const channelName = getChannelDisplayName(config);
    const isTelegram =
      config.targetPlatform ===
      BooruAutoPostConfigDtoTargetPlatformEnum.Telegram;
    const isDanbooru =
      config.source === BooruAutoPostConfigDtoSourceEnum.Danbooru;

    return (
      <Card
        key={config.id}
        className={styles.configCard}
        data-testid={`card-${config.id}`}
        hoverable
      >
        <div className={styles.cardHeader}>
          <div className={styles.cardChannel}>
            <div
              className={`${styles.platformIcon} ${isTelegram ? styles.platformTelegram : styles.platformDiscord}`}
            >
              {isTelegram ? <Send size={14} /> : <MessageCircle size={14} />}
            </div>
            <div className={styles.channelInfo}>
              <span className={styles.channelName}>{channelName}</span>
              <span className={styles.channelId}>
                {isTelegram
                  ? `ID: ${config.telegramChannelId}`
                  : `ID: ${config.discordChannelId}`}
              </span>
            </div>
          </div>
          <Space>
            <Tag color={isDanbooru ? "blue" : "orange"}>
              {isDanbooru ? "Danbooru" : "Rule34"}
            </Tag>
            <Badge
              color={config.isEnabled ? "green" : "default"}
              text={config.isEnabled ? "Включён" : "Выключен"}
            />
          </Space>
        </div>

        <div className={styles.cardBody}>
          <div className={styles.cardField}>
            <span className={styles.fieldLabel}>Теги</span>
            <div>{renderTags(config.tags ?? "")}</div>
          </div>

          <div className={styles.cardField}>
            <span className={styles.fieldLabel}>Расписание</span>
            <code className={styles.cronValue}>
              {config.cronExpression || "—"}
            </code>
          </div>

          {config.planningHorizonDays > 0 && (
            <div className={styles.cardField}>
              <span className={styles.fieldLabel}>Горизонт планирования</span>
              <span className={styles.fieldValue}>
                {config.planningHorizonDays} дн.
              </span>
            </div>
          )}

          {config.message && (
            <div className={styles.cardField}>
              <span className={styles.fieldLabel}>Сообщение</span>
              <span className={styles.fieldValue}>{config.message}</span>
            </div>
          )}

          {isTelegram && (
            <div className={styles.cardField}>
              <span className={styles.fieldLabel}>ParseMode</span>
              <span className={styles.fieldValue}>
                {config.telegramParseMode}
              </span>
            </div>
          )}

          <div className={styles.cardField}>
            <span className={styles.fieldLabel}>Отложено</span>
            <span
              className={styles.fieldValue}
              data-testid={`field-pending-count-${config.id}`}
            >
              {config.pendingPostsCount > 0 ? config.pendingPostsCount : "—"}
            </span>
          </div>

          <div className={styles.cardField}>
            <span className={styles.fieldLabel}>Следующий пост</span>
            <span
              className={styles.fieldValue}
              data-testid={`field-next-scheduled-${config.id}`}
            >
              {config.nextScheduledAtUtc
                ? new Date(config.nextScheduledAtUtc).toLocaleString()
                : "—"}
            </span>
          </div>

          <div className={styles.cardField}>
            <span className={styles.fieldLabel}>Последний запуск</span>
            <span className={styles.fieldValue}>
              {config.lastExecutedAtUtc
                ? new Date(config.lastExecutedAtUtc).toLocaleString()
                : "—"}
            </span>
          </div>
        </div>

        <div className={styles.cardActions}>
          <Tooltip title="Триггер сейчас">
            <Button
              size="small"
              type="primary"
              ghost
              disabled={isProcessing}
              onClick={() => void handleTriggerNow(config.id)}
              data-testid={`button-trigger-${config.id}`}
              icon={<Play size={14} />}
            />
          </Tooltip>
          <Button
            size="small"
            type={config.isEnabled ? "default" : "primary"}
            disabled={isProcessing}
            onClick={() => void handleToggleEnabled(config)}
            data-testid={`button-toggle-${config.id}`}
          >
            {config.isEnabled ? "Выключить" : "Включить"}
          </Button>
          <Tooltip title="Редактировать">
            <Button
              size="small"
              type="default"
              disabled={isProcessing}
              onClick={() => openEditModal(config)}
              data-testid={`button-edit-${config.id}`}
              icon={<Edit3 size={14} />}
            />
          </Tooltip>
          <Tooltip title="Удалить">
            <Button
              size="small"
              danger
              ghost
              disabled={isProcessing}
              onClick={() => void handleDelete(config.id)}
              data-testid={`button-delete-${config.id}`}
              icon={<Trash2 size={14} />}
            />
          </Tooltip>
        </div>
      </Card>
    );
  };

  const renderChannelPickerModal = () => {
    const isTelegram = channelPickerType === "Telegram";
    const isLoading = isTelegram ? loadingTelegram : loadingDiscord;

    return (
      <Modal
        open={showChannelPicker}
        onCancel={() => setShowChannelPicker(false)}
        title={`Выберите ${isTelegram ? "Telegram" : "Discord"} канал`}
        footer={undefined}
        centered
        width={560}
        data-testid="modal-channel-picker"
      >
        <Input
          placeholder={
            isTelegram
              ? "Поиск по названию канала"
              : "Поиск по названию канала или сервера"
          }
          value={channelSearch}
          onChange={event_ => setChannelSearch(event_.target.value)}
          style={{ marginBottom: 12 }}
          allowClear
          data-testid="input-channel-search"
        />
        {isLoading ? (
          <div style={{ textAlign: "center", padding: 24 }}>
            <Spin />
          </div>
        ) : filteredPickerChannels.length === 0 ? (
          <Alert
            type="info"
            message="Каналы не найдены"
            data-testid="empty-channels"
          />
        ) : (
          <div className={styles.channelGrid} data-testid="channel-grid">
            {filteredPickerChannels.map(ch => {
              const channelId = String(ch.id);

              if (isTelegram) {
                const tgCh = ch as TelegramChannelOptionDto;
                const isSelected = form.telegramChannelId === channelId;
                return (
                  <div
                    key={tgCh.id}
                    className={`${styles.channelCard} ${isSelected ? styles.channelCardSelected : ""}`}
                    onClick={() => {
                      setForm(previous => ({
                        ...previous,
                        telegramChannelId: channelId,
                      }));
                      setShowChannelPicker(false);
                    }}
                    data-testid={`channel-card-${tgCh.id}`}
                  >
                    <div className={styles.channelCardHeader}>
                      <span className={styles.telegramChannelTitle}>
                        {tgCh.title}
                      </span>
                      {isSelected && <Check size={16} />}
                    </div>
                    <div className={styles.channelCardId}>ID: {tgCh.id}</div>
                  </div>
                );
              }

              const dcCh = ch as DiscordChannelOptionDto;
              const isSelected = form.discordChannelId === channelId;
              return (
                <div
                  key={dcCh.id}
                  className={`${styles.channelCard} ${isSelected ? styles.channelCardSelected : ""}`}
                  onClick={() => {
                    setForm(previous => ({
                      ...previous,
                      discordChannelId: channelId,
                    }));
                    setShowChannelPicker(false);
                  }}
                  data-testid={`channel-card-${dcCh.id}`}
                >
                  <div className={styles.channelCardHeader}>
                    <span className={styles.channelCardGuild}>
                      {dcCh.guildName}
                    </span>
                    {isSelected && <Check size={16} />}
                  </div>
                  <div className={styles.channelCardName}>
                    <Hash size={14} />
                    {dcCh.name}
                  </div>
                  <div className={styles.channelCardId}>ID: {dcCh.id}</div>
                </div>
              );
            })}
          </div>
        )}
      </Modal>
    );
  };

  const renderPlatformSelector = (
    value: Platform,
    onChange: (p: Platform) => void
  ) => (
    <div className={styles.platformSelector}>
      <button
        type="button"
        className={`${styles.platformButton} ${styles.platformButtonDiscord} ${value === "Discord" ? styles.platformButtonActive : ""}`}
        onClick={() => onChange("Discord")}
        data-testid="platform-discord"
      >
        <MessageCircle size={16} />
        Discord
      </button>
      <button
        type="button"
        className={`${styles.platformButton} ${styles.platformButtonTelegram} ${value === "Telegram" ? styles.platformButtonActive : ""}`}
        onClick={() => onChange("Telegram")}
        data-testid="platform-telegram"
      >
        <Send size={16} />
        Telegram
      </button>
    </div>
  );

  const renderCronBuilder = () => (
    <div className={styles.cronBuilder}>
      <div className={styles.cronRow}>
        <span className={styles.cronLabel}>Минута</span>
        <Select
          value={cronParts.minute}
          onChange={value =>
            setCronParts(previous => ({
              ...previous,
              minute: value,
            }))
          }
          options={cronMinutes.map(o => ({
            value: o.value,
            label: `${o.value} — ${o.label}`,
          }))}
          style={{ flex: 1 }}
          disabled={submitting}
          data-testid="cron-minute"
        />
      </div>
      <div className={styles.cronRow}>
        <span className={styles.cronLabel}>Час</span>
        <Select
          value={cronParts.hour}
          onChange={value =>
            setCronParts(previous => ({
              ...previous,
              hour: value,
            }))
          }
          options={cronHours.map(o => ({
            value: o.value,
            label: `${o.value} — ${o.label}`,
          }))}
          style={{ flex: 1 }}
          disabled={submitting}
          data-testid="cron-hour"
          showSearch
        />
      </div>
      <div className={styles.cronRow}>
        <span className={styles.cronLabel}>День месяца</span>
        <Select
          value={cronParts.dayOfMonth}
          onChange={value =>
            setCronParts(previous => ({
              ...previous,
              dayOfMonth: value,
            }))
          }
          options={cronDaysOfMonth.map(o => ({
            value: o.value,
            label: o.label,
          }))}
          style={{ flex: 1 }}
          disabled={submitting}
          data-testid="cron-day"
        />
      </div>
      <div className={styles.cronRow}>
        <span className={styles.cronLabel}>Месяц</span>
        <Select
          value={cronParts.month}
          onChange={value =>
            setCronParts(previous => ({
              ...previous,
              month: value,
            }))
          }
          options={cronMonths.map(o => ({
            value: o.value,
            label: o.label,
          }))}
          style={{ flex: 1 }}
          disabled={submitting}
          data-testid="cron-month"
        />
      </div>
      <div className={styles.cronRow}>
        <span className={styles.cronLabel}>День недели</span>
        <Select
          value={cronParts.dayOfWeek}
          onChange={value =>
            setCronParts(previous => ({
              ...previous,
              dayOfWeek: value,
            }))
          }
          options={cronDaysOfWeek.map(o => ({
            value: o.value,
            label: `${o.value} — ${o.label}`,
          }))}
          style={{ flex: 1 }}
          disabled={submitting}
          data-testid="cron-weekday"
        />
      </div>
      <div className={styles.cronPreview}>
        <code>{cronExpression}</code>
      </div>
    </div>
  );

  const selectedChannelDisplay = useMemo(() => {
    if (form.targetPlatform === "Telegram") {
      if (!form.telegramChannelId) return "Выберите Telegram канал";
      return (
        telegramChannelMap.get(form.telegramChannelId) ??
        "Выберите Telegram канал"
      );
    }
    if (!form.discordChannelId) return "Выберите Discord канал";
    return (
      discordChannelMap.get(form.discordChannelId) ?? "Выберите Discord канал"
    );
  }, [
    form.targetPlatform,
    form.discordChannelId,
    form.telegramChannelId,
    discordChannelMap,
    telegramChannelMap,
  ]);

  return (
    <div className={styles.page} data-testid="booru-auto-post-page">
      <div className={styles.pageHeader}>
        <h1 className={styles.pageTitle}>Booru автопостинг</h1>
        <div className={styles.headerActions}>
          <Button
            type="primary"
            onClick={openCreateModal}
            data-testid="button-create"
            icon={<Plus size={14} />}
          >
            Добавить
          </Button>
        </div>
      </div>

      {!!error && (
        <Alert
          type="error"
          title={error}
          style={{ marginBottom: 12 }}
          closable
          onClose={() => setError("")}
          data-testid="error-alert"
        />
      )}

      {/* Create/Edit Modal */}
      <Modal
        open={showModal}
        onCancel={() => {
          setShowModal(false);
          setEditingId(undefined);
        }}
        title={editingId ? "Редактировать" : "Добавить конфигурацию"}
        footer={false}
        centered
        width={600}
        data-testid="modal-config-form"
      >
        <form onSubmit={event_ => void handleSubmit(event_)}>
          <div className={styles.formField}>
            <label className={styles.formLabel}>Источник</label>
            <Select
              value={form.source}
              onChange={value =>
                setForm(previous => ({ ...previous, source: value }))
              }
              options={[
                { value: "Danbooru", label: "Danbooru" },
                { value: "Rule34", label: "Rule34" },
              ]}
              style={{ width: "100%" }}
              disabled={submitting || !!editingId}
              data-testid="select-source"
            />
          </div>

          {renderPlatformSelector(form.targetPlatform, p =>
            setForm(previous => ({
              ...previous,
              targetPlatform: p,
            }))
          )}

          <div className={styles.formField}>
            <label className={styles.formLabel}>
              {form.targetPlatform === "Telegram"
                ? "Telegram канал"
                : "Discord канал"}
            </label>
            <Button
              block
              disabled={submitting}
              onClick={() => openChannelPicker(form.targetPlatform)}
              data-testid="button-open-channel-picker"
            >
              {selectedChannelDisplay}
            </Button>
          </div>

          <div className={styles.formField}>
            <label className={styles.formLabel}>Теги</label>
            <Select
              mode="tags"
              value={form.tags}
              onChange={(values: string[]) =>
                setForm(previous => ({
                  ...previous,
                  tags: values.slice(0, 2),
                }))
              }
              placeholder="Введите тег и нажмите Enter"
              disabled={submitting}
              style={{ width: "100%" }}
              data-testid="input-tags"
              tokenSeparators={[" "]}
              maxCount={2}
              maxTagCount="responsive"
            />
            <div className={styles.cronHint}>
              Примеры: 1girl, solo, landscape, anime, scenery
            </div>
          </div>

          <div className={styles.formField}>
            <label className={styles.formLabel}>Расписание (CRON)</label>
            <Space style={{ marginBottom: 8 }}>
              <Button
                size="small"
                type={cronMode === "visual" ? "primary" : "default"}
                onClick={() => setCronMode("visual")}
                data-testid="cron-mode-visual"
              >
                Конструктор
              </Button>
              <Button
                size="small"
                type={cronMode === "manual" ? "primary" : "default"}
                onClick={() => setCronMode("manual")}
                data-testid="cron-mode-manual"
              >
                Вручную
              </Button>
            </Space>

            {cronMode === "visual" ? (
              renderCronBuilder()
            ) : (
              <Input
                value={form.cronExpression}
                onChange={event_ =>
                  setForm(previous => ({
                    ...previous,
                    cronExpression: event_.target.value,
                  }))
                }
                placeholder="*/30 * * * *"
                disabled={submitting}
                data-testid="input-cron"
              />
            )}
          </div>

          <div className={styles.formField}>
            <label className={styles.formLabel}>
              Горизонт планирования (дней)
            </label>
            <Input
              type="number"
              min={1}
              max={365}
              value={form.planningHorizonDays}
              onChange={event_ =>
                setForm(previous => ({
                  ...previous,
                  planningHorizonDays: Number(event_.target.value) || 60,
                }))
              }
              disabled={submitting}
              data-testid="input-planning-horizon"
            />
          </div>

          <div className={styles.formField}>
            <label className={styles.formLabel}>Сообщение (капшн)</label>
            <Input.TextArea
              value={form.message}
              onChange={event_ =>
                setForm(previous => ({
                  ...previous,
                  message: event_.target.value,
                }))
              }
              placeholder="Текст сообщения. Переменные: {tags}, {character}, {artist}, {copyright}, {id}, {rating}, {score}, {source}"
              disabled={submitting}
              rows={3}
              data-testid="input-message"
            />
            <div className={styles.cronHint}>
              Используйте переменные: {"{tags}"} {"{character}"} {"{artist}"}{" "}
              {"{copyright}"} {"{id}"} {"{rating}"} {"{score}"} {"{source}"}
            </div>
          </div>

          {form.targetPlatform === "Telegram" && (
            <div className={styles.formField}>
              <label className={styles.formLabel}>Telegram ParseMode</label>
              <Select
                value={form.telegramParseMode}
                onChange={value =>
                  setForm(previous => ({
                    ...previous,
                    telegramParseMode: value,
                  }))
                }
                options={[
                  { value: "Html", label: "HTML" },
                  { value: "Markdown", label: "Markdown" },
                  { value: "Default", label: "Без форматирования" },
                ]}
                style={{ width: "100%" }}
                disabled={submitting}
                data-testid="select-parse-mode"
              />
            </div>
          )}

          <div
            style={{
              display: "flex",
              justifyContent: "flex-end",
              marginTop: 16,
              gap: 8,
            }}
          >
            <Button
              type="default"
              onClick={() => {
                setShowModal(false);
                setEditingId(undefined);
              }}
              disabled={submitting}
              data-testid="button-cancel"
            >
              Отмена
            </Button>
            <Button
              type="primary"
              htmlType="submit"
              disabled={submitting}
              data-testid="button-submit"
            >
              {submitting ? (
                <>
                  <Spin size="small" style={{ marginRight: 8 }} />
                  {editingId ? "Сохранение..." : "Создание..."}
                </>
              ) : editingId ? (
                "Сохранить"
              ) : (
                "Создать"
              )}
            </Button>
          </div>
        </form>
      </Modal>

      {renderChannelPickerModal()}

      <Card>
        <div className={styles.toolbar}>
          <h5 style={{ marginBottom: 0 }}>Конфигурации</h5>
          <div className={styles.filterTabs}>
            <Button
              size="small"
              type={sourceFilterTab === "all" ? "primary" : "default"}
              onClick={() => setSourceFilterTab("all")}
              data-testid="filter-source-all"
            >
              Все
            </Button>
            <Button
              size="small"
              type={sourceFilterTab === "danbooru" ? "primary" : "default"}
              onClick={() => setSourceFilterTab("danbooru")}
              data-testid="filter-source-danbooru"
            >
              Danbooru
            </Button>
            <Button
              size="small"
              type={sourceFilterTab === "rule34" ? "primary" : "default"}
              onClick={() => setSourceFilterTab("rule34")}
              data-testid="filter-source-rule34"
            >
              Rule34
            </Button>
          </div>
          <div className={styles.filterTabs}>
            <Button
              size="small"
              type={filterTab === "all" ? "primary" : "default"}
              onClick={() => setFilterTab("all")}
              data-testid="filter-all"
            >
              Все
            </Button>
            <Button
              size="small"
              type={filterTab === "discord" ? "primary" : "default"}
              onClick={() => setFilterTab("discord")}
              data-testid="filter-discord"
              icon={<MessageCircle size={12} />}
            >
              Discord
            </Button>
            <Button
              size="small"
              type={filterTab === "telegram" ? "primary" : "default"}
              onClick={() => setFilterTab("telegram")}
              data-testid="filter-telegram"
              icon={<Send size={12} />}
            >
              Telegram
            </Button>
          </div>
          <Input
            className={styles.searchBar}
            placeholder="Поиск: канал, теги, CRON"
            value={filter}
            onChange={event_ => setFilter(event_.target.value)}
            style={{ width: 300 }}
            data-testid="input-search"
          />
        </div>
        <div className={styles.cardsWrap}>
          {loading ? (
            <div
              style={{ textAlign: "center", padding: "16px 0" }}
              data-testid="loading-spinner"
            >
              <Spin />
            </div>
          ) : filteredConfigs.length === 0 ? (
            <Alert
              type="info"
              title={
                filterTab !== "all" || filter
                  ? "Конфигурации не найдены"
                  : "Нет конфигураций. Создайте первую!"
              }
              style={{ marginBottom: 0, marginTop: 12 }}
              data-testid="empty-state"
            />
          ) : (
            <div className={styles.cardsGrid}>
              {filteredConfigs.map(config => renderConfigCard(config))}
            </div>
          )}
        </div>
      </Card>
    </div>
  );
};

export default BooruAutoPostPage;
