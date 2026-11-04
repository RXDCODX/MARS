import { Alert } from "antd";
import type { ReactNode } from "react";

/**
 * Ошибка в виде alert antd с текстом-пояснением.
 *
 * Обёртка существует из-за коллизии в типах antd: у `AlertProps` есть и
 * `description: string` самого компонента, и `description: CSSProperties` из
 * `React.AriaAttributes`. Последнее выигрывает при слиянии, и передать в
 * `description` узел React нельзя — тип не принимает ни текст, ни элемент.
 *
 * Приведение сосредоточено здесь, а не расставлено по местам: иначе пришлось
 * бы повторять одно и то же пояснение и рискнуть забыть его, а коллизия в
 * типах библиотеки никуда не денется до обновления antd.
 */
export type ErrorAlertProperties = {
  /** Заголовок: что именно произошло. */
  message: ReactNode;
  /** Пояснение: детали, текст ошибки или список. */
  description?: ReactNode;
  kind?: "error" | "warning" | "info" | "success";
  /** Действие внизу блока: обычно кнопка закрытия. */
  action?: ReactNode;
  style?: React.CSSProperties;
};

/**
 * Alert с пояснением.
 *
 * `key` задаётся явно, потому что antd держит состояние закрытия внутри себя
 * по позиции в списке, и без ключа повторный показ того же места закрывал бы
 * его по старой памяти.
 */
export const ErrorAlert = ({
  message,
  description,
  kind = "error",
  action,
  style,
}: ErrorAlertProperties) => (
  <Alert
    key={`${kind}-${typeof message === "string" ? message : ""}`}
    type={kind}
    message={message}
    // Приведение — обход коллизии в типах antd, описанной выше: поле
    // description в них перекрыто значением из ARIA-описания.
    description={description as never}
    style={style}
    action={action}
  />
);
