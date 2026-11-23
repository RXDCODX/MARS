/**
 * Хук для работы с ToastModal
 */

import { createContext, useCallback, useContext, useState } from "react";

import { OperationResult } from "@/shared/types/OperationResult";

import { ToastModalData } from "./ToastModal.types";

/**
 * Вид устаревшего вызова тоста: текст и вид, без конверта.
 *
 * Принимается `showToast` в дополнение к конверту: сорок четыре места в
 * проекте вызывают его в этой форме, и без её поддержки они показывали не тот
 * текст, что передали.
 */
export type LegacyToastArgument = [message: string, kind?: string];

/**
 * Контекст для управления модальным окном и тостами
 */
interface ToastModalContextType {
  /** Данные для модального окна */
  modalData: ToastModalData | null;

  /** Состояние видимости модального окна */
  showModal: boolean;

  /** Открыть модальное окно */
  openModal: (data: ToastModalData) => void;

  /** Закрыть модальное окно */
  closeModal: () => void;

  /**
   * Показать тост.
   *
   * Обычная форма — конверт операции, и вид тоста выводится из `success`.
   * Форма `(сообщение, вид)` сохранена для существующих вызовов и помечена
   * устаревшей.
   */
  showToast: {
    <TData = unknown>(result: OperationResult<TData>): void;
    /** Устаревшая форма: текст и вид. См. LegacyToastArgument. */
    (...args: LegacyToastArgument): void;
  };
}

const ToastModalContext = createContext<ToastModalContextType | null>(null);

/**
 * Хук для использования ToastModal в компонентах
 *
 * Возвращает функцию showToast, которая принимает OperationResult
 * и автоматически показывает тост (success/error) на основе result.success
 *
 * @example
 * ```tsx
 * const { showToast } = useToastModal();
 *
 * const result = await api.createUser(data);
 * showToast(result); // Автоматически success или error тост
 * ```
 */
export const useToastModal = () => {
  const context = useContext(ToastModalContext);
  if (!context) {
    throw new Error(
      "useToastModal должен использоваться внутри ToastModalProvider"
    );
  }
  return context;
};

/**
 * Хук для управления состоянием модального окна
 * Используется внутри провайдера
 */
export const useToastModalState = () => {
  const [modalData, setModalData] = useState<ToastModalData | null>(null);
  const [showModal, setShowModal] = useState(false);

  const openModal = useCallback((data: ToastModalData) => {
    setModalData(data);
    setShowModal(true);
  }, []);

  const closeModal = useCallback(() => {
    setShowModal(false);
    setModalData(null);
  }, []);

  return {
    modalData,
    showModal,
    openModal,
    closeModal,
  };
};

export { ToastModalContext };
