import { Button, Modal, Typography } from "antd";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { useTwitchUsersStore } from "../store/useTwitchUsersStore";

const TwitchUserDeleteModal: React.FC = () => {
  const { showToast } = useToastModal();
  const confirmDeleteId = useTwitchUsersStore(state => state.confirmDeleteId);
  const confirmDeleteName = useTwitchUsersStore(
    state => state.confirmDeleteName
  );
  const closeDeleteConfirm = useTwitchUsersStore(
    state => state.closeDeleteConfirm
  );
  const deleteUser = useTwitchUsersStore(state => state.deleteUser);
  const isDeleting = useTwitchUsersStore(state => state.isDeleting);

  const handleDelete = async () => {
    const result = await deleteUser();
    if (result) {
      showToast(result);
    }
  };

  return (
    <Modal
      open={Boolean(confirmDeleteId)}
      onCancel={closeDeleteConfirm}
      title="Удаление пользователя"
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
        Вы уверены, что хотите удалить пользователя{" "}
        <strong>{confirmDeleteName}</strong>? Действие нельзя отменить.
      </Typography.Text>
    </Modal>
  );
};

export default TwitchUserDeleteModal;
