import { MemeOrderDto, MemeTypeDto } from "@/shared/api";

// Основные представления страницы
export type RandomMemeView =
  | "list"
  | "type-details"
  | "order-details"
  | "create-type"
  | "create-order"
  | "edit-type"
  | "edit-order";

// Состояние навигации
export interface NavigationState {
  currentView: RandomMemeView;
  selectedType: MemeTypeDto | null;
  selectedOrder: MemeOrderDto | null;
  isLoading: boolean;
  error: string;
}

// Пропсы для списка типов мемов
export interface RandomMemeTypesListProps {
  memeTypes: MemeTypeDto[];
  isLoading: boolean;
  error: string;
  onRefresh: () => void;
  onViewDetails: (memeType: MemeTypeDto) => void;
  onEdit: (memeType: MemeTypeDto) => void;
  onDelete: (memeType: MemeTypeDto) => void;
  onCreate: () => void;
}

// Пропсы для списка заказов мемов
export interface RandomMemeOrdersListProps {
  memeOrders: MemeOrderDto[];
  memeTypes: MemeTypeDto[];
  isLoading: boolean;
  error: string;
  onRefresh: () => void;
  onViewDetails: (memeOrder: MemeOrderDto) => void;
  onEdit: (memeOrder: MemeOrderDto) => void;
  onDelete: (memeOrder: MemeOrderDto) => void;
  onCreate: () => void;
  showToast?: <TData = unknown>(
    result: import("@/shared/types/OperationResult").OperationResult<TData>
  ) => void;
}

// Пропсы для деталей типа мема
export interface RandomMemeTypeDetailsProps {
  memeType: MemeTypeDto;
  isLoading: boolean;
  onBack: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onRefresh: () => void;
}

// Пропсы для деталей заказа мема
export interface RandomMemeOrderDetailsProps {
  memeOrder: MemeOrderDto;
  isLoading: boolean;
  onBack: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onRefresh: () => void;
}

// Пропсы для формы
export interface RandomMemeFormProps {
  memeType?: MemeTypeDto | null;
  memeOrder?: MemeOrderDto | null;
  memeTypes?: MemeTypeDto[];
  isSubmitting: boolean;
  mode: "create" | "edit";
  onSubmit: (data: Record<string, unknown>) => Promise<void>;
  onCancel: () => void;
}

// Пропсы для модального окна удаления
export interface DeleteConfirmationModalProps {
  isOpen: boolean;
  itemType: "type" | "order";
  itemName: string;
  isDeleting: boolean;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
}

// Данные формы для типа мема
export interface MemeTypeFormData {
  name: string;
  folderPath: string;
}

// Данные формы для заказа мема
export interface MemeOrderFormData {
  filePath: string;
  memeTypeId: number;
  order: number;
}

// Объединенный тип данных формы
export type MemeFormData = MemeTypeFormData | MemeOrderFormData;
