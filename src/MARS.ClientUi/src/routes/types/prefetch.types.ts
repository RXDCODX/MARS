import { ComponentType } from "react";

/**
 * Тип для функции загрузки lazy компонента
 */
export type LazyComponentLoader = () => Promise<{
  default: ComponentType<any>;
}>;

/**
 * Статус предзагрузки компонентов
 */
export interface PrefetchStatus {
  /** Идет ли загрузка в данный момент */
  isLoading: boolean;
  /** Количество загруженных компонентов */
  loaded: number;
  /** Общее количество компонентов для загрузки */
  total: number;
  /** Список ошибок при загрузке */
  errors: Error[];
}

/**
 * Результат операции предзагрузки
 */
export interface PrefetchResult {
  /** Количество успешно загруженных компонентов */
  loaded: number;
  /** Общее количество компонентов */
  total: number;
  /** Список ошибок */
  errors: Error[];
}
