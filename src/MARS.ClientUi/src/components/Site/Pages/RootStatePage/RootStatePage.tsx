import { Alert, Button, Card, Col, Input, Row, Spin, Table } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";

import { defaultApiConfig } from "@/shared/api/api-config";
import { RootState } from "@/shared/api/http-clients/RootState";
import type { RootState as RootStateDto } from "@/shared/api/types/data-contracts";
import type { OperationResult } from "@/shared/types/OperationResult";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./RootStatePage.module.scss";

interface RootStateForm {
  name: string;
  value: string;
  description: string;
  typeDescription: string;
}

const defaultForm: RootStateForm = {
  name: "",
  value: "",
  description: "",
  typeDescription: "",
};

const RootStatePage: React.FC = () => {
  const { showToast } = useToastModal();
  const appConfigService = useMemo(() => new RootState(defaultApiConfig), []);

  const [items, setItems] = useState<RootStateDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingName, setDeletingName] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [editingName, setEditingName] = useState<string | null>(null);
  const [form, setForm] = useState<RootStateForm>(defaultForm);

  const loadItems = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const response = await appConfigService!.rootStateList();
      const operation = response.data;

      if (operation.success) {
        setItems(Array.isArray(operation.data) ? operation.data : []);
      } else {
        const message = operation.message ?? "Не удалось загрузить RootState";
        setItems([]);
        setError(message);
        showToast(operation);
      }
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Ошибка при загрузке RootState";
      setItems([]);
      setError(message);
      showToast({ success: false, message });
    } finally {
      setLoading(false);
    }
  }, [appConfigService, showToast]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const resetForm = () => {
    setForm(defaultForm);
    setEditingName(null);
  };

  const handleEdit = (item: RootStateDto) => {
    setEditingName(item.name);
    setForm({
      name: item.name,
      value: item.value,
      description: item.description,
      typeDescription: item.typeDescription,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const trimmedName = form.name.trim();

    if (!trimmedName) {
      showToast({
        success: false,
        message: "Имя RootState не может быть пустым",
      });
      return;
    }

    setSaving(true);
    setError("");

    try {
      let operation: OperationResult;

      if (editingName) {
        const response = await appConfigService!.rootStateValuePartialUpdate(
          editingName,
          {
            value: form.value,
          }
        );
        operation = response.data;
      } else {
        const response = await appConfigService!.rootStateCreate({
          name: trimmedName,
          value: form.value,
          description: form.description,
          typeDescription: form.typeDescription,
        });
        operation = response.data;
      }

      showToast(operation);

      if (operation.success) {
        resetForm();
        await loadItems();
      }
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : editingName
            ? "Не удалось обновить значение RootState"
            : "Не удалось создать RootState";
      setError(message);
      showToast({ success: false, message });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (name: string) => {
    if (!globalThis.confirm(`Удалить RootState '${name}'?`)) {
      return;
    }

    setDeletingName(name);
    setError("");

    try {
      const response = await appConfigService!.rootStateDelete(name);
      const operation = response.data;
      showToast(operation);

      if (operation.success) {
        if (editingName === name) {
          resetForm();
        }
        await loadItems();
      }
    } catch (error_) {
      const message =
        error_ instanceof Error
          ? error_.message
          : "Не удалось удалить RootState";
      setError(message);
      showToast({ success: false, message });
    } finally {
      setDeletingName(null);
    }
  };

  const columns = [
    {
      title: "Имя",
      dataIndex: "name",
      key: "name",
      render: (text: string) => <span className={styles.nameCell}>{text}</span>,
    },
    {
      title: "Значение",
      dataIndex: "value",
      key: "value",
      render: (text: string) => (
        <span className={styles.valueCell}>{text}</span>
      ),
    },
    { title: "Описание", dataIndex: "description", key: "description" },
    {
      title: "Тип",
      dataIndex: "typeDescription",
      key: "typeDescription",
    },
    {
      title: "Действия",
      key: "actions",
      align: "right" as const,
      render: (_: unknown, record: RootStateDto) => (
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
            disabled={deletingName === record.name}
            onClick={() => void handleDelete(record.name)}
          >
            {deletingName === record.name ? "Удаление..." : "Удалить"}
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1 style={{ marginBottom: 0 }}>Управление RootState</h1>
        <Button onClick={() => void loadItems()}>Обновить</Button>
      </div>

      {!!error && (
        <Alert type="error" message={error} style={{ marginBottom: 12 }} />
      )}

      <Row gutter={16}>
        <Col xl={8}>
          <Card className={styles.card}>
            <h5 style={{ marginBottom: 0 }}>
              {editingName ? "Редактирование значения" : "Создание"}
            </h5>
            <form onSubmit={handleSubmit} style={{ marginTop: 16 }}>
              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", marginBottom: 4 }}>Имя</label>
                <Input
                  value={form.name}
                  onChange={e =>
                    setForm(state => ({ ...state, name: e.target.value }))
                  }
                  placeholder="Например: feature-toggle"
                  disabled={!!editingName}
                />
              </div>

              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", marginBottom: 4 }}>
                  Значение
                </label>
                <Input.TextArea
                  rows={3}
                  value={form.value}
                  onChange={e =>
                    setForm(state => ({ ...state, value: e.target.value }))
                  }
                  placeholder="Введите значение"
                />
              </div>

              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", marginBottom: 4 }}>
                  Описание
                </label>
                <Input
                  value={form.description}
                  onChange={e =>
                    setForm(state => ({
                      ...state,
                      description: e.target.value,
                    }))
                  }
                  placeholder="Описание параметра"
                  disabled={!!editingName}
                />
              </div>

              <div style={{ marginBottom: 12 }}>
                <label style={{ display: "block", marginBottom: 4 }}>
                  Type Description
                </label>
                <Input
                  value={form.typeDescription}
                  onChange={e =>
                    setForm(state => ({
                      ...state,
                      typeDescription: e.target.value,
                    }))
                  }
                  placeholder="Например: string"
                  disabled={!!editingName}
                />
              </div>

              <div className={styles.actions}>
                <Button type="primary" htmlType="submit" disabled={saving}>
                  {saving ? (
                    <>
                      <Spin size="small" style={{ marginRight: 8 }} />
                      Сохранение...
                    </>
                  ) : editingName ? (
                    "Обновить значение"
                  ) : (
                    "Создать"
                  )}
                </Button>

                {editingName && (
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
            <h5 style={{ marginBottom: 0 }}>Список RootState</h5>
            <div style={{ marginTop: 16 }}>
              {loading ? (
                <div style={{ textAlign: "center", padding: "16px 0" }}>
                  <Spin />
                </div>
              ) : (
                <Table
                  columns={columns}
                  dataSource={items}
                  rowKey="name"
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

export default RootStatePage;
