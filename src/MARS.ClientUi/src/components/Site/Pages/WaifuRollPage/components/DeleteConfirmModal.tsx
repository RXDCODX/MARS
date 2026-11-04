import { Button, Modal } from "antd";

import { useWaifuRollStore } from "../store/useWaifuRollStore";

const DeleteConfirmModal: React.FC = () => {
  const confirmDeleteId = useWaifuRollStore(s => s.confirmDeleteId);
  const mode = useWaifuRollStore(s => s.mode);
  const isDeleting = useWaifuRollStore(s => s.isDeleting);
  const cancelDelete = useWaifuRollStore(s => s.cancelDelete);
  const deleteWaifu = useWaifuRollStore(s => s.deleteWaifu);
  const deleteHusband = useWaifuRollStore(s => s.deleteHusband);
  const waifus = useWaifuRollStore(s => s.waifus);
  const husbands = useWaifuRollStore(s => s.husbands);

  if (!confirmDeleteId) {
    return null;
  }

  const handleConfirm = async () => {
    if (mode === "waifu") {
      await deleteWaifu(confirmDeleteId);
    } else {
      await deleteHusband(confirmDeleteId);
    }
  };

  const getItemName = () => {
    if (mode === "waifu") {
      const waifu = waifus.find(w => w.shikiId === confirmDeleteId);
      return waifu?.name ?? confirmDeleteId;
    }
    const husband = husbands.find(h => h.twitchId === confirmDeleteId);
    return husband?.displayName ?? confirmDeleteId;
  };

  return (
    <Modal
      open
      title="Подтверждение удаления"
      onCancel={cancelDelete}
      footer={[
        <Button
          key="cancel"
          onClick={cancelDelete}
          data-testid="button-cancel-delete"
        >
          Отмена
        </Button>,
        <Button
          key="confirm"
          type="primary"
          danger
          loading={isDeleting}
          onClick={() => void handleConfirm()}
          data-testid="button-confirm-delete"
        >
          Удалить
        </Button>,
      ]}
      data-testid="modal-confirm-delete"
    >
      <p>
        Вы уверены, что хотите удалить {mode === "waifu" ? "вайфу" : "мужа"}{" "}
        <strong>{getItemName()}</strong>?
      </p>
    </Modal>
  );
};

export default DeleteConfirmModal;
