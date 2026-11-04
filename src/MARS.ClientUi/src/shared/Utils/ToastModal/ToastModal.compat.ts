/**
 * Функции обратной совместимости со старым кодом
 * Эти функции конвертируют вызовы в OperationResult формат
 */

import {
  createErrorResult,
  createSuccessResult,
  OperationResult,
} from "@/shared/types/OperationResult";

/**
 * Создает OperationResult для успешной операции
 * @deprecated Используйте прямой вызов showToast с OperationResult
 */
export function createSuccessToast(
  message: string,
  data?: unknown,
  _title?: string
): OperationResult {
  return createSuccessResult(message, data);
}

/**
 * Создает OperationResult для ошибки
 * @deprecated Используйте прямой вызов showToast с OperationResult
 */
export function createErrorToast(
  message: string,
  error?: Error | unknown,
  _title?: string
): OperationResult {
  const errorData =
    error instanceof Error
      ? { message: error.message, stack: error.stack, name: error.name }
      : error;

  return createErrorResult(message, errorData);
}

/**
 * Создает OperationResult для информационного сообщения
 * @deprecated Используйте прямой вызов showToast с OperationResult
 */
export function createInfoToast(
  message: string,
  data?: unknown,
  _title?: string
): OperationResult {
  return createSuccessResult(message, data);
}

/**
 * Создает OperationResult для предупреждения
 * @deprecated Используйте прямой вызов showToast с OperationResult
 */
export function createWarningToast(
  message: string,
  data?: unknown,
  _title?: string
): OperationResult {
  return createErrorResult(message, data);
}
