import type { BadgeProps as AntBadgeProperties } from "antd";
import { Badge as AntBadge } from "antd";

interface BadgeProperties extends AntBadgeProperties {
  bg?: string;
}

const Badge = ({ bg, color, ...properties }: BadgeProperties) => (
  <AntBadge color={color ?? bg} {...properties} />
);

export default Badge;
