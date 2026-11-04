// Базовые generic-типы и интерфейсы для CRUD компонентов

export type SortDirection = "asc" | "desc";

export interface ListParams<
  TFilter extends Record<string, unknown> = Record<string, unknown>,
> {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: SortDirection;
  search?: string;
  filters?: TFilter;
}

export interface ListResult<T> {
  items: T[];
  total: number;
}

export interface DataSource<
  T,
  TCreate = Partial<T>,
  TUpdate = Partial<T>,
  TFilter extends Record<string, unknown> = Record<string, unknown>,
> {
  list: (parameters: ListParams<TFilter>) => Promise<ListResult<T>>;
  getById: (id: string | number) => Promise<T>;
  create: (payload: TCreate) => Promise<T>;
  update: (id: string | number, payload: TUpdate) => Promise<T>;
  remove: (id: string | number) => Promise<void>;
}

export type Accessor<T> =
  | keyof T
  | ((
      row: T
    ) => React.ReactNode | string | number | boolean | null | undefined);

export interface ColumnDef<T> {
  id: string;
  header: string;
  accessor: Accessor<T>;
  width?: string | number;
  sortable?: boolean;
  render?: (row: T) => React.ReactNode;
}

export type FieldType =
  | "text"
  | "textarea"
  | "number"
  | "date"
  | "boolean"
  | "select"
  | "file"
  | "slider";

export interface SelectOption {
  label: string;
  value: string | number | boolean;
}

export interface FormField<T> {
  name: keyof T & string;
  label: string;
  type: FieldType;
  placeholder?: string;
  required?: boolean;
  hidden?: boolean;
  disabled?: boolean;
  helpText?: string;
  defaultValue?: unknown;
  // Для select
  options?: SelectOption[] | (() => Promise<SelectOption[]>);
  // Для number/slider
  min?: number;
  max?: number;
  step?: number;
  // Для file
  accept?: string;
  multiple?: boolean;
}

export interface FormSchema<T> {
  fields: Array<FormField<T>>;
  layout?: "vertical" | "horizontal" | "grid";
}

export interface CrudPageProps<
  T,
  TCreate = Partial<T>,
  TUpdate = Partial<T>,
  TFilter extends Record<string, unknown> = Record<string, unknown>,
> {
  title?: string;
  dataSource: DataSource<T, TCreate, TUpdate, TFilter>;
  columns: Array<ColumnDef<T>>;
  formSchema: FormSchema<T>;
  idAccessor?: (row: T) => string | number;
  enableSearch?: boolean;
  defaultSort?: { field: string; dir: SortDirection };
  pageSizeOptions?: number[];
  initialPageSize?: number;
  toolbar?: React.ReactNode;
  rowActions?: (row: T) => React.ReactNode;
  renderEmpty?: React.ReactNode;
  filtersRenderer?: (
    value: TFilter | undefined,
    onChange: (v: TFilter | undefined) => void
  ) => React.ReactNode;
  onCreated?: (entity: T) => void;
  onUpdated?: (entity: T) => void;
  onDeleted?: (id: string | number) => void;
}

export interface CrudDetailPageProps<T, TUpdate = Partial<T>> {
  title?: string;
  id: string | number;
  dataSource: Pick<DataSource<T, Partial<T>, TUpdate>, "getById" | "update">;
  formSchema: FormSchema<T>;
  idAccessor?: (row: T) => string | number;
  onSaved?: (entity: T) => void;
}

export interface FormFieldRendererProps<T> {
  schema: FormSchema<T>;
  values: Record<string, unknown>;
  onChange: (name: string, value: unknown) => void;
  errors?: Record<string, string>;
  readOnly?: boolean;
}
