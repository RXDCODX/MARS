/**
 * ToastModal - система уведомлений на основе OperationResult
 *
 * === УСТАНОВКА ===
 *
 * Оберните приложение в ToastModalProvider (в App.tsx):
 *
 * ```tsx
 * import { ToastModalProvider } from '@/shared/Utils/ToastModal';
 *
 * function App() {
 *   return (
 *     <ToastModalProvider>
 *       <YourApp />
 *     </ToastModalProvider>
 *   );
 * }
 * ```
 *
 * === ИСПОЛЬЗОВАНИЕ ===
 *
 * Одна функция для всего:
 *
 * ```tsx
 * import { useToastModal } from '@/shared/Utils/ToastModal';
 *
 * function MyComponent() {
 *   const { showToast } = useToastModal();
 *
 *   const handleAction = async () => {
 *     // result.success определяет тип тоста (success/error)
 *     // result.message - отображается в тосте
 *     // result.data - отображается в модальном окне при клике
 *     const result = await api.someOperation();
 *     showToast(result);
 *   };
 *
 *   return <button onClick={handleAction}>Выполнить</button>;
 * }
 * ```
 *
 * === ПРИМЕРЫ ===
 *
 * Создание:
 * ```tsx
 * const result = await api.createUser(data);
 * showToast(result);
 * ```
 *
 * Обновление:
 * ```tsx
 * const result = await api.updateUser(id, data);
 * showToast(result);
 * ```
 *
 * Удаление:
 * ```tsx
 * const result = await api.deleteUser(id);
 * showToast(result);
 * ```
 *
 * С типизацией:
 * ```tsx
 * interface UserData {
 *   id: number;
 *   name: string;
 * }
 *
 * const result: OperationResult<UserData> = await api.getUser(123);
 * showToast(result); // result.data будет типа UserData
 * ```
 *
 * === КЛИК НА ТОСТ ===
 *
 * При клике на тост автоматически открывается модальное окно с:
 * - Полным сообщением (result.message)
 * - Данными из result.data (если есть)
 * - Статусом операции (успех/ошибка)
 * - Временной меткой
 */

export { ToastModalProvider } from "./ToastModal";
export { useToastModal } from "./ToastModal.hooks";
export type { ToastModalData, ToastType } from "./ToastModal.types";

// Функции обратной совместимости (deprecated)
export {
  createSuccessToast,
  createErrorToast,
  createInfoToast,
  createWarningToast,
} from "./ToastModal.compat";
