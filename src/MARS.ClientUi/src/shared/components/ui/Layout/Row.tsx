import { Flex } from "antd";

interface RowProperties {
  children: React.ReactNode;
  className?: string;
  gap?: number;
}

const Row = ({ children, className, gap = 16 }: RowProperties) => (
  <Flex gap={gap} wrap="wrap" className={className}>
    {children}
  </Flex>
);

export default Row;
