import { Button, Modal, Spin } from "antd";
import { AlertTriangle, Trash2 } from "lucide-react";
import React from "react";

import { DeleteConfirmationModalProps as DeleteConfirmationModalProperties } from "../RandomMemePage.types";

const DeleteConfirmationModal: React.FC<DeleteConfirmationModalProperties> = ({
  isOpen,
  itemType,
  itemName,
  isDeleting,
  onConfirm,
  onCancel,
}) => {
  const handleConfirm = async () => {
    try {
      await onConfirm();
    } catch (error) {
      console.error("Ошибка при удалении:", error);
    }
  };

  const getItemTypeText = () =>
    itemType === "type" ? "тип мема" : "заказ мема";

  const getItemTypeTextAccusative = () =>
    itemType === "type" ? "тип мема" : "заказ мема";

  return (
    <Modal
      open={isOpen}
      onCancel={onCancel}
      centered
      title={
        <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <AlertTriangle size={20} style={{ color: "#faad14" }} />
          Подтверждение удаления
        </span>
      }
      footer={
        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <Button onClick={onCancel} disabled={isDeleting}>
            Отмена
          </Button>
          <Button
            danger
            onClick={handleConfirm}
            disabled={isDeleting}
            style={{ display: "flex", alignItems: "center", gap: 8 }}
          >
            {isDeleting ? (
              <>
                <Spin size="small" />
                Удаление...
              </>
            ) : (
              <>
                <Trash2 size={16} />
                Удалить {getItemTypeTextAccusative()}
              </>
            )}
          </Button>
        </div>
      }
    >
      <div style={{ textAlign: "center", marginBottom: 32 }}>
        <Trash2 size={48} style={{ color: "#ff4d4f", marginBottom: 12 }} />
        <h5 style={{ marginBottom: 12 }}>Вы уверены?</h5>
        <p style={{ color: "#8c8c8c", marginBottom: 0 }}>
          Вы собираетесь удалить {getItemTypeText()}{" "}
          <strong>"{itemName}"</strong>.
          <br />
          Это действие нельзя отменить.
        </p>
      </div>

      {itemType === "type" && (
        <div
          style={{
            padding: "8px 12px",
            background: "#fffbe6",
            border: "1px solid #ffe58f",
            borderRadius: 4,
          }}
        >
          <strong>Внимание!</strong> При удалении типа мема будут также удалены
          все связанные с ним заказы.
        </div>
      )}
    </Modal>
  );
};

export default DeleteConfirmationModal;
