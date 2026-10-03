import type { FormProps as AntFormProperties } from "antd";
import { Form as AntForm, Input, Select, Switch } from "antd";
import type { ReactNode } from "react";

type FormProperties = AntFormProperties;

const Form = (properties: FormProperties) => <AntForm {...properties} />;

interface FormItemProperties {
  label?: string;
  children: ReactNode;
  name?: string;
  rules?: Array<Record<string, unknown>>;
  className?: string;
  required?: boolean;
}

const FormItem = ({
  label,
  children,
  name,
  rules,
  className,
  required,
  ...properties
}: FormItemProperties) => (
  <AntForm.Item
    label={label}
    name={name}
    rules={rules}
    className={className}
    required={required}
    {...properties}
  >
    {children as React.ReactElement}
  </AntForm.Item>
);

interface FormInputProperties {
  type?: string;
  placeholder?: string;
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  disabled?: boolean;
  className?: string;
  as?: string;
  rows?: number;
}

const FormInput = ({ as, rows, ...properties }: FormInputProperties) => {
  if (as === "textarea") {
    return <Input.TextArea rows={rows} {...properties} />;
  }
  return <Input {...properties} />;
};

interface FormSelectProperties {
  value?: string;
  onChange?: (value: string) => void;
  children?: ReactNode;
  options?: Array<{ value: string; label: string }>;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

const FormSelect = ({
  children,
  options,
  value,
  onChange,
  ...properties
}: FormSelectProperties) => (
  <Select value={value} onChange={onChange} options={options} {...properties}>
    {children}
  </Select>
);

const FormTextArea = (
  properties: React.ComponentProps<typeof Input.TextArea>
) => <Input.TextArea {...properties} />;

interface FormSwitchProperties {
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  disabled?: boolean;
}

const FormSwitch = ({ checked, onChange, disabled }: FormSwitchProperties) => (
  <Switch checked={checked} onChange={onChange} disabled={disabled} />
);

export { FormItem, FormInput, FormSelect, FormTextArea, FormSwitch };

export default Form;
