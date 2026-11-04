/**
 * Общие типы приложения
 */

export type {
  OperationResult,
  OperationResult as OperationResultGeneric,
} from "./OperationResult";

export {
  isOperationResult,
  createSuccessResult,
  createErrorResult,
} from "./OperationResult";
