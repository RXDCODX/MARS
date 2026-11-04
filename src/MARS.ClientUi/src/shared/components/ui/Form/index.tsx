import type { FormProps as AntFormProperties } from "antd";
import { Form as AntForm, Input, Select, Switch } from "antd";
import type { ReactNode } from "react";

// antd различает форму с обязательным содержимым и без него, и наш тип
// соответствовал второму: `FormProps` без содержимого нельзя передать в
// `AntForm`. Форма без содержимого — обычный случай при условном рендере,
// поэтому содержимое здесь объявлено необязательным.
type FormProperties = Omit<AntFormProperties, "children"> & {
  children?: ReactNode;
};

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

/**
 * Общие поля однострочного и многострочного ввода.
 *
 * Общими они названы с оговоркой: обработчик изменения у них разный, поэтому
 * он вынесен в каждую ветвь отдельно. Один тип на оба случая утверждал бы,
 * что `onChange` примет событие `<input>`, и код текстовой области с обработчиком
 * `<textarea>` прошёл бы типы, а работал бы с чужим событием.
 */
interface CommonInputProperties {
  placeholder?: string;
  value?: string;
  disabled?: boolean;
  className?: string;
}

/** Однострочный ввод. */
interface SingleLineInputProperties extends CommonInputProperties {
  type?: string;
  as?: string;
  rows?: number;
  onChange?: React.ChangeEventHandler<HTMLInputElement>;
}

/** Многострочный ввод. У него нет `type`, а событие — от `textarea`. */
interface TextAreaInputProperties extends CommonInputProperties {
  as?: "textarea";
  rows?: number;
  onChange?: React.ChangeEventHandler<HTMLTextAreaElement>;
}

type FormInputProperties = SingleLineInputProperties | TextAreaInputProperties;

function isTextArea(
  properties: FormInputProperties
): properties is TextAreaInputProperties {
  return properties.as === "textarea";
}

/**
 * Ввод или текстовая область.
 *
 * Ветви разведены по типам, а не сведены приведением: у textarea нет `type`,
 * а событие изменения у неё другое. Раньше всё это передавалось в
 * `Input.TextArea` как есть, и обработчик `<input>` объявлялся принимаемым
 * текстовой областью.
 */
const FormInput = (properties: FormInputProperties) => {
  if (isTextArea(properties)) {
    const { as: _unusedAs, ...textAreaProperties } = properties;

    return <Input.TextArea {...textAreaProperties} />;
  }

  const { as: _unusedAs, ...inputProperties } = properties;

  return <Input {...inputProperties} />;
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
