/**
 * Типы для ToastModal
 */

import { OperationResult } from "@/shared/types/OperationResult";

/**
 * Тип тоста (автоматически определяется на основе OperationResult.success)
 */
export type ToastType = "success" | "error";

/**
 * Данные для отображения тоста
 */
export interface ToastModalData<TData = unknown> {
  /** Тип тоста */
  type: ToastType;

  /** Сообщение для отображения в тосте */
  message: string;

  /** Данные для отображения в модальном окне */
  data?: TData;

  /** Временная метка */
  timestamp: Date;

  /** Исходный результат операции */
  operationResult: OperationResult<TData>;
}
