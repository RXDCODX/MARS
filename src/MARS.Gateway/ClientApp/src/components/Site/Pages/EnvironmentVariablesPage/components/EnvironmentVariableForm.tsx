import { Badge, Button, Input, Modal, Typography } from "antd";
import { Save, Trash2 } from "lucide-react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "../EnvironmentVariablesPage.module.scss";
import { useEnvironmentVariablesStore } from "../store/useEnvironmentVariablesStore";
import { formatDateTime } from "../utils/formatDateTime";

const EnvironmentVariableForm: React.FC = () => {
  const { showToast } = useToastModal();

  const showForm = useEnvironmentVariablesStore(state => state.showForm);
  const formMode = useEnvironmentVariablesStore(state => state.formMode);
  const formValues = useEnvironmentVariablesStore(state => state.formValues);
  const isSubmitting = useEnvironmentVariablesStore(
    state => state.isSubmitting
  );
  const selectedKey = useEnvironmentVariablesStore(state => state.selectedKey);
  const selectedVariable = useEnvironmentVariablesStore(state =>
    state.variables.find(variable => variable.key === state.selectedKey)
  );

  const updateFormField = useEnvironmentVariablesStore(
    state => state.updateFormField
  );
  const cancelForm = useEnvironmentVariablesStore(state => state.cancelForm);
  const submitForm = useEnvironmentVariablesStore(state => state.submitForm);
  const openDeleteConfirm = useEnvironmentVariablesStore(
    state => state.openDeleteConfirm
  );

  const handleSubmit = async () => {
    const result = await submitForm();
    if (result) {
      showToast(result);
    }
  };

  const handleDelete = () => {
    openDeleteConfirm();
  };

  if (!showForm) {
    return null;
  }

  return (
    <Modal
      open={showForm}
      onCancel={cancelForm}
      centered
      className={styles.formModal}
      footer={
        <div className={styles.formModalFooter}>
          {formMode === "edit" && (
            <div
              style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}
            >
              <Button
                danger
                size="small"
                onClick={handleDelete}
                icon={<Trash2 size={16} />}
              >
                Удалить
              </Button>
            </div>
          )}

          <div className={styles.formModalActions}>
            <Button size="small" onClick={cancelForm}>
              Закрыть
            </Button>
            <Button
              type="primary"
              size="small"
              onClick={handleSubmit}
              loading={isSubmitting}
              icon={isSubmitting ? undefined : <Save size={16} />}
            >
              Сохранить
            </Button>
          </div>
        </div>
      }
    >
      <div className={styles.formModalHeader}>
        <div className={styles.formModalTitle}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            {formMode === "create"
              ? "Создание переменной"
              : "Редактирование переменной"}
          </Typography.Title>
          {formMode === "edit" && selectedKey && selectedVariable && (
            <Badge
              className={styles.formModalBadge}
              count={`Обновлено ${formatDateTime(selectedVariable.updatedAt)}`}
              showZero
              style={{ backgroundColor: "#8c8c8c" }}
            />
          )}
        </div>
      </div>

      <div className={styles.formModalBody}>
        <div className={styles.formFields}>
          <div>
            <Typography.Text>Ключ</Typography.Text>
            <Input
              value={formValues.key}
              onChange={event => updateFormField("key", event.target.value)}
              placeholder="Например: Api:BaseUrl"
              disabled={formMode === "edit" && Boolean(selectedKey)}
            />
          </div>

          <div>
            <Typography.Text>Значение</Typography.Text>
            <Input.TextArea
              value={formValues.value}
              onChange={event => updateFormField("value", event.target.value)}
              placeholder="Введите значение переменной"
            />
          </div>

          <div>
            <Typography.Text>Описание</Typography.Text>
            <Input.TextArea
              value={formValues.description}
              onChange={event =>
                updateFormField("description", event.target.value)
              }
              placeholder="Добавьте описание для удобства"
            />
          </div>
        </div>
      </div>
    </Modal>
  );
};

export default EnvironmentVariableForm;
