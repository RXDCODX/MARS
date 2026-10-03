declare module "react-bits/src/content/Animations/ElectricBorder/ElectricBorder" {
  import type { CSSProperties, PropsWithChildren } from "react";
  export interface ElectricBorderProps {
    color?: string;
    speed?: number;
    chaos?: number;
    thickness?: number;
    className?: string;
    style?: CSSProperties;
  }
  const ElectricBorder: (
    properties: PropsWithChildren<ElectricBorderProps>
  ) => JSX.Element;
  export default ElectricBorder;
}

declare module "react-bits/src/ts-default/TextAnimations/GradientText/GradientText" {
  import type { PropsWithChildren, ReactNode } from "react";

  export interface GradientTextProps {
    children: ReactNode;
    className?: string;
    colors?: string[];
    animationSpeed?: number;
    showBorder?: boolean;
  }

  const GradientText: (
    properties: PropsWithChildren<GradientTextProps>
  ) => JSX.Element;
  export default GradientText;
}
