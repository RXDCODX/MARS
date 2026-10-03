import { Button, Modal } from "antd";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { useWaifuRollStore } from "../store/useWaifuRollStore";

const UnmergeConfirmModal: React.FC = () => {
  const { showToast } = useToastModal();
  const confirmUnmergeId = useWaifuRollStore(s => s.confirmUnmergeId);
  const husbands = useWaifuRollStore(s => s.husbands);
  const waifus = useWaifuRollStore(s => s.waifus);
  const cancelUnmerge = useWaifuRollStore(s => s.cancelUnmerge);
  const unmergeHusband = useWaifuRollStore(s => s.unmergeHusband);

  if (!confirmUnmergeId) {
    return null;
  }

  const husband = husbands.find(h => h.twitchId === confirmUnmergeId);
  const waifu = husband?.waifuBrideId
    ? waifus.find(w => w.shikiId === husband.waifuBrideId)
    : undefined;

  const handleConfirm = async () => {
    const result = await unmergeHusband(confirmUnmergeId);
    if (result) {
      showToast(result);
    }
  };

  return (
    <Modal
      open
      title="Подтверждение развода"
      onCancel={cancelUnmerge}
      footer={[
        <Button
          key="cancel"
          onClick={cancelUnmerge}
          data-testid="button-cancel-unmerge"
        >
          Отмена
        </Button>,
        <Button
          key="confirm"
          type="primary"
          danger
          onClick={() => void handleConfirm()}
          data-testid="button-confirm-unmerge"
        >
          Развести
        </Button>,
      ]}
      data-testid="modal-confirm-unmerge"
    >
      <p>
        Вы уверены, что хотите развести{" "}
        <strong>{husband?.displayName ?? husband?.twitchId}</strong> и{" "}
        <strong>{waifu?.name ?? "вайфу"}</strong>?
      </p>
    </Modal>
  );
};

export default UnmergeConfirmModal;
