import { FormEvent, useEffect, useState } from "react";

import {
  createErrorToast,
  createSuccessToast,
  useToastModal,
} from "@/shared/Utils/ToastModal";

import FormFieldRenderer from "./FormFieldRenderer";
import { FormSchema } from "./types";

interface EditCreateModalProperties<T> {
  title: string;
  show: boolean;
  onClose: () => void;
  onSubmit: (values: Record<string, unknown>) => Promise<void>;
  schema: FormSchema<T>;
  initialValues?: Record<string, unknown>;
}

export function EditCreateModal<T>({
  title,
  show,
  onClose,
  onSubmit,
  schema,
  initialValues,
}: EditCreateModalProperties<T>) {
  const { showToast } = useToastModal();
  const [values, setValues] = useState<Record<string, unknown>>(
    initialValues ?? {}
  );
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setValues(initialValues ?? {});
  }, [initialValues]);

  const handleChange = (name: string, value: unknown) => {
    setValues(previous => ({ ...previous, [name]: value }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors({});
    try {
      await onSubmit(values);
      showToast(createSuccessToast("Сохранено"));
      onClose();
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : "Ошибка сохранения";
      showToast(createErrorToast(errorMessage, error));
    } finally {
      setSubmitting(false);
    }
  };

  if (!show) return null;

  return (
    <div
      className="modal d-block"
      role="dialog"
      style={{ background: "rgba(0,0,0,0.4)" }}
    >
      <div className="modal-dialog modal-lg" role="document">
        <div
          className="modal-content"
          style={{
            background: "var(--bs-card-bg)",
            color: "var(--bs-body-color)",
          }}
        >
          <div className="modal-header">
            <h5 className="modal-title">{title}</h5>
            <button
              type="button"
              className="btn-close"
              onClick={onClose}
              aria-label="Close"
            />
          </div>
          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              <FormFieldRenderer<T>
                schema={schema}
                values={values}
                onChange={handleChange}
                errors={errors}
              />
            </div>
            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={onClose}
                disabled={submitting}
              >
                Отмена
              </button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={submitting}
              >
                {submitting ? "Сохранение..." : "Сохранить"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

interface ConfirmDeleteModalProperties {
  title?: string;
  show: boolean;
  onClose: () => void;
  onConfirm: () => Promise<void>;
  text?: string;
}

export function ConfirmDeleteModal({
  title = "Удалить запись",
  show,
  onClose,
  onConfirm,
  text,
}: ConfirmDeleteModalProperties) {
  const { showToast } = useToastModal();
  const [submitting, setSubmitting] = useState(false);

  if (!show) return null;

  const handleConfirm = async () => {
    setSubmitting(true);
    try {
      await onConfirm();
      showToast(createSuccessToast("Удалено"));
      onClose();
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : "Ошибка удаления";
      showToast(createErrorToast(errorMessage, error));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      className="modal d-block"
      role="dialog"
      style={{ background: "rgba(0,0,0,0.4)" }}
    >
      <div className="modal-dialog" role="document">
        <div
          className="modal-content"
          style={{
            background: "var(--bs-card-bg)",
            color: "var(--bs-body-color)",
          }}
        >
          <div className="modal-header">
            <h5 className="modal-title">{title}</h5>
            <button
              type="button"
              className="btn-close"
              onClick={onClose}
              aria-label="Close"
            />
          </div>
          <div className="modal-body">
            <p>{text ?? "Вы уверены, что хотите удалить эту запись?"}</p>
          </div>
          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={submitting}
            >
              Отмена
            </button>
            <button
              type="button"
              className="btn btn-danger"
              onClick={handleConfirm}
              disabled={submitting}
            >
              {submitting ? "Удаление..." : "Удалить"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
