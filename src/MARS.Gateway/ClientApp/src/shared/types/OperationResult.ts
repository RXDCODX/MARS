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

/**
 * Текст ошибки из исключения, с запасным вариантом.
 *
 * Транспорт превращает отказ сервиса в `HTTP 200 + success: false` в
 * `throw new Error(errorMessage)`, и это единственный живой путь отказа для
 * конвертных ответов. `catch` без параметра этот текст выбрасывал, и пользователь
 * вместо причины видал «Ошибка сети»: сервис отвечает внятно, например «Вайфу с
 * ID {id} уже существует» или «Тело запроса не может быть пустым».
 *
 * Помощник общий, потому что такой `catch` повторялся десять раз в сторе вайфу и
 * ещё в нескольких местах, и в каждом текст терялся одинаково.
 */
export function messageOf(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message.length > 0) {
    return error.message;
  }

  if (
    typeof error === "object" &&
    error !== null &&
    "message" in error &&
    typeof (error as { message: unknown }).message === "string"
  ) {
    const text = (error as { message: string }).message;

    if (text.length > 0) {
      return text;
    }
  }

  return fallback;
}
