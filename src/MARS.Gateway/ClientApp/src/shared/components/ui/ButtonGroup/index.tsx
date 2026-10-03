import { Flex } from "antd";

interface ButtonGroupProperties {
  children: React.ReactNode;
  className?: string;
}

const ButtonGroup = ({ children, className }: ButtonGroupProperties) => (
  <Flex gap={8} className={className}>
    {children}
  </Flex>
);

export default ButtonGroup;
