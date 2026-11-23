import { Flex } from "antd";

interface ContainerProperties {
  children: React.ReactNode;
  className?: string;
}

const Container = ({ children, className }: ContainerProperties) => (
  <Flex vertical gap={16} className={className} style={{ width: "100%" }}>
    {children}
  </Flex>
);

export default Container;
