import { Button, Form, Input, Modal } from "antd";
import { memo, useCallback, useState } from "react";
import { useShallow } from "zustand/react/shallow";

import styles from "./AddTrackModal.module.scss";
import { useAddTrackModal } from "./hooks";
import { useAddTrackModalStore } from "./stores/useAddTrackModalStore";

function AddTrackModalComponent() {
  const { isOpen, close } = useAddTrackModalStore(
    useShallow(state => ({
      isOpen: state.isOpen,
      close: state.close,
    }))
  );
  const { addTrack } = useAddTrackModal();
  const [query, setQuery] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();

      if (!query.trim()) {
        return;
      }

      setIsSubmitting(true);
      try {
        const success = await addTrack(query);
        if (success) {
          setQuery("");
          close();
        }
      } finally {
        setIsSubmitting(false);
      }
    },
    [query, addTrack, close]
  );

  const handleClose = useCallback(() => {
    if (isSubmitting) {
      return;
    }

    setQuery("");
    close();
  }, [isSubmitting, close]);

  return (
    <Modal
      open={isOpen}
      onCancel={handleClose}
      title="Добавить трек"
      footer={[
        <Button
          key="cancel"
          className={`${styles.modalButton} ${styles.secondary}`}
          onClick={handleClose}
          disabled={isSubmitting}
          data-testid="button-add-track-cancel"
        >
          Отмена
        </Button>,
        <Button
          key="submit"
          className={`${styles.modalButton} ${styles.primary}`}
          type="primary"
          onClick={handleSubmit}
          disabled={!query.trim() || isSubmitting}
          data-testid="button-add-track-submit"
        >
          {isSubmitting ? "Добавление..." : "Добавить"}
        </Button>,
      ]}
      data-testid="modal-add-track"
    >
      <Form onSubmitCapture={handleSubmit}>
        <Form.Item label="Введите название трека или ссылку">
          <Input
            placeholder="Например: Rick Astley - Never Gonna Give You Up"
            value={query}
            onChange={e => setQuery(e.target.value)}
            autoFocus
            disabled={isSubmitting}
            data-testid="input-add-track-query"
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

export const AddTrackModal = memo(AddTrackModalComponent);
