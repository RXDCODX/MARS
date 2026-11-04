import type { CardProps as AntCardProperties } from "antd";
import { Card as AntCard } from "antd";

/**
 * Обёртка поверх `Card` из antd.
 *
 * Подкомпонентов у antd нет: `Card.Header`, `Card.Body` и `Card.Footer` в v5 не
 * существуют, и прежние статические экспорты `CardBody` равнялись `undefined` —
 * импорт проходил, типы проходили, а отрисовывалась пустота. Заголовок и
 * содержимое передаются обычными пропсами `title` и `children`.
 *
 * Поиск по проекту не находит ни одного использования обёртки; она оставлена на
 * случай, если понадобится, но без выдуманных подкомпонентов.
 */
const Card = (properties: AntCardProperties) => <AntCard {...properties} />;

export default Card;
