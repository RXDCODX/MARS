import { Input } from "antd";

interface InputGroupProperties {
  children: React.ReactNode;
  className?: string;
}

const InputGroup = ({ children, className }: InputGroupProperties) => (
  <Input.Group compact className={className}>
    {children}
  </Input.Group>
);

export default InputGroup;
