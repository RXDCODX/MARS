import type { CardProps as AntCardProperties } from "antd";
import { Card as AntCard } from "antd";

const Card = (properties: AntCardProperties) => <AntCard {...properties} />;

export const CardBody = AntCard.Body;

export default Card;

export {
  Card as CardHeader,
  Card as CardFooter,
  Card as CardTitle,
} from "antd";
