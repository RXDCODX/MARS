import { useInjectStyles } from "@/shared/hooks/useInjectStyles";

export interface InjectStylesProps {
  /**
   * CSS строка со стилями для внедрения в head
   */
  styles: string;

  /**
   * Уникальный идентификатор для style элемента
   * Если указан, то при наличии элемента с таким id, он будет заменен
   */
  id?: string;
}

/**
 * Компонент для декларативного внедрения CSS стилей в head страницы
 *
 * @example
 * ```tsx
 * <InjectStyles
 *   styles={`
 *     .my-class {
 *       color: red;
 *     }
 *   `}
 *   id="my-custom-styles"
 * />
 * ```
 */
export const InjectStyles = ({ styles, id }: InjectStylesProps) => {
  useInjectStyles(styles, id);

  return null;
};

export default InjectStyles;
