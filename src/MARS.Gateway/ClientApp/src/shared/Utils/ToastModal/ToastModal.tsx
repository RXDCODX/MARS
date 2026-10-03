/**
 * Компонент ToastModal для отображения уведомлений на основе OperationResult
 */

import { Button, Modal } from "antd";
import { useCallback } from "react";
import toast, { Toaster } from "react-hot-toast";

import { OperationResult } from "@/shared/types/OperationResult";

import { ToastModalContext, useToastModalState } from "./ToastModal.hooks";
import styles from "./ToastModal.module.scss";
import { ToastModalData } from "./ToastModal.types";

interface ToastModalProperties {
  data: ToastModalData;
  onClose?: () => void;
  show?: boolean;
}

const ToastModal: React.FC<ToastModalProperties> = ({
  data,
  onClose,
  show = false,
}) => {
  const handleClose = useCallback(() => {
    if (onClose) {
      onClose();
    }
  }, [onClose]);

  const renderData = () => {
    if (!data.data) return null;

    return (
      <div className={styles.dataSection}>
        <h6>Данные:</h6>
        <pre className={styles.jsonData}>
          {JSON.stringify(data.data, null, 2)}
        </pre>
      </div>
    );
  };

  const renderOperationInfo = () => {
    const { operationResult } = data;

    return (
      <div className={styles.metadataSection}>
        <h6>Информация:</h6>
        <div className={styles.metadataGrid}>
          <div className={styles.metadataItem}>
            <span className={styles.metadataKey}>Статус:</span>
            <span className={styles.metadataValue}>
              {operationResult.success ? "Успешно" : "Ошибка"}
            </span>
          </div>
          <div className={styles.metadataItem}>
            <span className={styles.metadataKey}>Время:</span>
            <span className={styles.metadataValue}>
              {data.timestamp.toLocaleString("ru-RU")}
            </span>
          </div>
        </div>
      </div>
    );
  };

  const getModalHeaderStyle = () =>
    data.type === "success"
      ? { backgroundColor: "#28a745", color: "white" }
      : { backgroundColor: "#dc3545", color: "white" };

  return (
    <Modal
      open={show}
      onCancel={handleClose}
      width={800}
      centered
      title={
        <span style={getModalHeaderStyle()}>
          {data.type === "success" ? "Успех" : "Ошибка"}
        </span>
      }
      footer={<Button onClick={handleClose}>Закрыть</Button>}
    >
      <div className={styles.modalBody}>
        <p className={styles.message}>{data.message}</p>
        {renderData()}
        {renderOperationInfo()}
      </div>
    </Modal>
  );
};

/**
 * Провайдер для ToastModal
 */
export const ToastModalProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { modalData, showModal, openModal, closeModal } = useToastModalState();

  const showToast = useCallback(
    <TData = unknown,>(result: OperationResult<TData>) => {
      const type = result.success ? "success" : "error";

      const message =
        result.message ||
        (result.success ? "Операция выполнена успешно" : "Произошла ошибка");

      const toastData: ToastModalData<TData> = {
        type,
        message,
        data: result.data,
        timestamp: new Date(),
        operationResult: result,
      };

      const toastOptions = {
        duration: 5000,
        position: "bottom-right" as const,
      };

      toast.custom(
        t => (
          <div
            onClick={() => {
              openModal(toastData);
              toast.dismiss(t.id);
            }}
            className={
              type === "success"
                ? `${styles.hotToast} ${styles.hotToastSuccess}`
                : `${styles.hotToast} ${styles.hotToastError}`
            }
          >
            <div className={styles.hotToastMessage}>{message}</div>
          </div>
        ),
        toastOptions
      );
    },
    [openModal]
  );

  const contextValue = {
    modalData,
    showModal,
    openModal,
    closeModal,
    showToast,
  };

  return (
    <ToastModalContext.Provider value={contextValue}>
      {children}
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 5000,
        }}
      />
      {modalData && (
        <ToastModal data={modalData} show={showModal} onClose={closeModal} />
      )}
    </ToastModalContext.Provider>
  );
};

export default ToastModal;
