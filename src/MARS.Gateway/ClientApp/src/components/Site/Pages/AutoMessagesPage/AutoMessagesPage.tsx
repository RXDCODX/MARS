import { Alert, Button, Card, Col, Input, Row, Spin, Table } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import {
  AutoMessageDto,
  AutoMessages,
  CreateAutoMessageRequest,
  UpdateAutoMessageRequest,
} from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./AutoMessagesPage.module.scss";

const emptyMessage = "";

const AutoMessagesPage: React.FC = () => {
  const { showToast } = useToastModal();
  const api = useMemo(() => new AutoMessages(defaultApiConfig), []);

  const [items, setItems] = useState<AutoMessageDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [message, setMessage] = useState(emptyMessage);

  const loadItems = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const result = await api.autoMessagesList();
      const data = Array.isArray(result.data.data) ? result.data.data : [];
      setItems(data);
    } catch (error_) {
      const text =
        error_ instanceof Error
          ? error_.message
          : "Не удалось загрузить автосообщения";
      setError(text);
      showToast({ success: false, message: text });
    } finally {
      setLoading(false);
    }
  }, [api, showToast]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const resetForm = () => {
    setMessage(emptyMessage);
    setEditingId(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim()) {
      showToast({
        success: false,
        message: "Текст сообщения не может быть пустым",
      });
      return;
    }

    setSaving(true);
    setError("");

    try {
      if (editingId) {
        const payload: UpdateAutoMessageRequest = { message: message.trim() };
        const result = await api.autoMessagesUpdate(editingId, payload);
        showToast(result.data);
      } else {
        const payload: CreateAutoMessageRequest = { message: message.trim() };
        const result = await api.autoMessagesCreate(payload);
        showToast(result.data);
      }

      resetForm();
      await loadItems();
    } catch (error_) {
      const text =
        error_ instanceof Error
          ? error_.message
          : editingId
            ? "Не удалось обновить автосообщение"
            : "Не удалось создать автосообщение";
      setError(text);
      showToast({ success: false, message: text });
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (item: AutoMessageDto) => {
    setEditingId(item.id);
    setMessage(item.message || emptyMessage);
  };

  const handleDelete = async (id: string) => {
    const isConfirmed = globalThis.confirm("Удалить это автосообщение?");
    if (!isConfirmed) {
      return;
    }

    setDeletingId(id);
    setError("");

    try {
      const result = await api.autoMessagesDelete(id);
      showToast(result.data);

      if (editingId === id) {
        resetForm();
      }

      await loadItems();
    } catch (error_) {
      const text =
        error_ instanceof Error
          ? error_.message
          : "Не удалось удалить автосообщение";
      setError(text);
      showToast({ success: false, message: text });
    } finally {
      setDeletingId(null);
    }
  };

  const columns = [
    { title: "ID", dataIndex: "id", key: "id" },
    { title: "Сообщение", dataIndex: "message", key: "message" },
    {
      title: "Действия",
      key: "actions",
      align: "right" as const,
      render: (_: unknown, record: AutoMessageDto) => (
        <div className={styles.actionsRight}>
          <Button
            size="small"
            type="primary"
            ghost
            onClick={() => handleEdit(record)}
          >
            Изменить
          </Button>
          <Button
            size="small"
            danger
            ghost
            disabled={deletingId === record.id}
            onClick={() => void handleDelete(record.id)}
          >
            {deletingId === record.id ? "Удаление..." : "Удалить"}
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1>Управление автосообщениями</h1>
        <Button onClick={() => void loadItems()}>Обновить</Button>
      </div>

      {!!error && (
        <Alert type="error" message={error} style={{ marginBottom: 12 }} />
      )}

      <Row gutter={16}>
        <Col xl={8}>
          <Card className={styles.card}>
            <h5 style={{ marginBottom: 0 }}>
              {editingId ? "Редактирование" : "Создание"}
            </h5>
            <form onSubmit={handleSubmit} style={{ marginTop: 16 }}>
              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", marginBottom: 4 }}>
                  Текст сообщения
                </label>
                <Input.TextArea
                  rows={4}
                  value={message}
                  onChange={e => setMessage(e.target.value)}
                  placeholder="Введите текст автосообщения"
                />
              </div>

              <div className={styles.actions}>
                <Button type="primary" htmlType="submit" disabled={saving}>
                  {saving ? (
                    <>
                      <Spin size="small" style={{ marginRight: 8 }} />
                      Сохранение...
                    </>
                  ) : editingId ? (
                    "Сохранить"
                  ) : (
                    "Создать"
                  )}
                </Button>

                {editingId && (
                  <Button type="default" onClick={resetForm}>
                    Отмена
                  </Button>
                )}
              </div>
            </form>
          </Card>
        </Col>

        <Col xl={16}>
          <Card className={styles.card}>
            <h5 style={{ marginBottom: 0 }}>Список автосообщений</h5>
            <div style={{ marginTop: 16 }}>
              {loading ? (
                <div className={styles.loading}>
                  <Spin />
                </div>
              ) : (
                <Table
                  columns={columns}
                  dataSource={items}
                  rowKey="id"
                  size="small"
                  pagination={false}
                  className={styles.tableWrap}
                />
              )}
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default AutoMessagesPage;
