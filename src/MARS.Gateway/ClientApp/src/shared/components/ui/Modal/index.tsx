import type { ModalProps as AntModalProperties } from "antd";
import { Modal as AntModal } from "antd";

interface ModalProperties extends Omit<AntModalProperties, "open"> {
  show?: boolean;
  onHide?: () => void;
  size?: "sm" | "lg" | "xl";
}

const sizeMap: Record<string, AntModalProperties["width"]> = {
  sm: 400,
  lg: 800,
  xl: 1000,
};

const Modal = ({
  show,
  onHide,
  size,
  width,
  onCancel,
  children,
  ...properties
}: ModalProperties) => (
  <AntModal
    open={show}
    onCancel={onCancel ?? onHide}
    width={width ?? sizeMap[size ?? "lg"]}
    footer={null}
    {...properties}
  >
    {children}
  </AntModal>
);

/**
 * Обёртка поверх `Modal` из antd.
 *
 * Раньше здесь были ещё четыре экспорта — `ModalHeader`, `ModalTitle`,
 * `ModalBody`, `ModalFooter` — взятые из Mantine. В antd таких статических
 * свойств нет, поэтому все четыре равнялись `undefined`: импорт проходил, типы
 * проходили, а отрисован был пустой элемент. Поиск по проекту не находит ни
 * одного использования, так что удалено, а не «приведено в соответствие».
 *
 * Заголовок и подвал передаются обычными пропсами `title` и `footer`.
 */
export default Modal;
