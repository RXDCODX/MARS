import { Button, Modal, Typography } from "antd";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { useEnvironmentVariablesStore } from "../store/useEnvironmentVariablesStore";

const EnvironmentVariableDeleteModal: React.FC = () => {
  const { showToast } = useToastModal();
  const confirmDeleteKey = useEnvironmentVariablesStore(
    state => state.confirmDeleteKey
  );
  const closeDeleteConfirm = useEnvironmentVariablesStore(
    state => state.closeDeleteConfirm
  );
  const deleteVariable = useEnvironmentVariablesStore(
    state => state.deleteVariable
  );
  const isDeleting = useEnvironmentVariablesStore(state => state.isDeleting);

  const handleDelete = async () => {
    const result = await deleteVariable();
    if (result) {
      showToast(result);
    }
  };

  return (
    <Modal
      open={Boolean(confirmDeleteKey)}
      onCancel={closeDeleteConfirm}
      title="Удаление переменной"
      footer={
        <div
          style={{ display: "flex", justifyContent: "flex-end", gap: "0.5rem" }}
        >
          <Button onClick={closeDeleteConfirm}>Отмена</Button>
          <Button danger onClick={handleDelete} loading={isDeleting}>
            Удалить
          </Button>
        </div>
      }
    >
      <Typography.Text>
        Вы уверены, что хотите удалить переменную{" "}
        <strong>{confirmDeleteKey}</strong>? Действие нельзя отменить.
      </Typography.Text>
    </Modal>
  );
};

export default EnvironmentVariableDeleteModal;
