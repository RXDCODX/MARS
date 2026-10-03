/**
 * TypeScript типы для OperationResult из серверной части
 * Соответствует MARS.Server.Services.OperationResult
 */

/**
 * Результат операции с типизированными данными
 */
export interface OperationResult<TData = unknown> {
  /** Флаг успеха операции */
  success: boolean;

  /** Сообщение о результате операции */
  message?: string | null;

  /** Хранимый объект данных */
  data?: TData;
}

/**
 * Проверяет, является ли объект OperationResult
 */
export function isOperationResult(object: unknown): object is OperationResult {
  return (
    typeof object === "object" &&
    object !== null &&
    "success" in object &&
    typeof (object as OperationResult).success === "boolean"
  );
}

/**
 * Создает успешный результат
 */
export function createSuccessResult<TData = unknown>(
  message?: string | null,
  data?: TData
): OperationResult<TData> {
  return {
    success: true,
    message,
    data,
  };
}

/**
 * Создает негативный результат
 */
export function createErrorResult<TData = unknown>(
  message?: string | null,
  data?: TData
): OperationResult<TData> {
  return {
    success: false,
    message,
    data,
  };
}
