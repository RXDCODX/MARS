import type { AlertProps as AntAlertProperties } from "antd";
import { Alert as AntAlert } from "antd";

// Из исключаемых — и closable, и variant: оба поля переопределяются
// своими значениями. У antd variant появился вместе с типом AlertVariant,
// и без исключения объявление ниже конфликтовало бы с ним.
interface AlertProperties extends Omit<
  AntAlertProperties,
  "closable" | "variant"
> {
  variant?: "primary" | "secondary" | "success" | "danger" | "warning" | "info";
  dismissible?: boolean;
  onClose?: () => void;
}

const typeMap: Record<string, AntAlertProperties["type"]> = {
  primary: "info",
  secondary: "info",
  success: "success",
  danger: "error",
  warning: "warning",
  info: "info",
};

const Alert = ({
  variant = "info",
  dismissible,
  onClose,
  ...properties
}: AlertProperties) => (
  <AntAlert
    type={typeMap[variant] ?? "info"}
    closable={dismissible}
    onClose={onClose}
    {...properties}
  />
);

export default Alert;
