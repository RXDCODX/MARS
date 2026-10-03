import type { ButtonProps as AntButtonProperties } from "antd";
import { Button as AntButton } from "antd";

interface ButtonProperties extends Omit<AntButtonProperties, "variant"> {
  variant?:
    | "primary"
    | "secondary"
    | "outline-primary"
    | "outline-secondary"
    | "success"
    | "danger"
    | "warning"
    | "link"
    | "ghost";
  size?: "sm" | "md" | "lg";
}

const sizeMap: Record<string, AntButtonProperties["size"]> = {
  sm: "small",
  md: "middle",
  lg: "large",
};

const Button = ({
  variant = "primary",
  size = "md",
  type,
  danger,
  ghost,
  ...properties
}: ButtonProperties) => {
  const isOutline = variant?.startsWith("outline-");
  const variantBase = isOutline ? variant.replace("outline-", "") : variant;

  const antdType: AntButtonProperties["type"] = isOutline
    ? "default"
    : variant === "link"
      ? "link"
      : "primary";

  const isDanger = variantBase === "danger";
  const isGhost = variant === "ghost" || variantBase === "secondary";

  return (
    <AntButton
      type={type ?? antdType}
      danger={danger ?? isDanger}
      ghost={ghost ?? isGhost}
      size={sizeMap[size] ?? "middle"}
      bordered={isOutline ? true : undefined}
      {...properties}
    />
  );
};

export default Button;
