import { ChangeEvent, useEffect, useState } from "react";

import {
  FormField,
  FormFieldRendererProps as FormFieldRendererProperties,
  SelectOption,
} from "./types";

function isPromise<T>(value: unknown): value is Promise<T> {
  return (
    !!value &&
    typeof value === "object" &&
    "then" in value &&
    typeof value.then === "function"
  );
}

export default function FormFieldRenderer<T>({
  schema,
  values,
  onChange,
  errors,
  readOnly,
}: FormFieldRendererProperties<T>) {
  const [asyncOptions, setAsyncOptions] = useState<
    Record<string, SelectOption[]>
  >({});

  useEffect(() => {
    let isMounted = true;
    const loadOptions = async () => {
      const fieldsNeedingOptions = schema.fields.filter(
        f => typeof f.options === "function"
      );
      for (const field of fieldsNeedingOptions) {
        try {
          const result = (field.options as () => Promise<SelectOption[]>)();
          const options = isPromise<SelectOption[]>(result) ? await result : [];
          if (isMounted) {
            setAsyncOptions(previous => ({
              ...previous,
              [field.name]: options,
            }));
          }
        } catch {
          // swallow to avoid breaking the form
        }
      }
    };
    loadOptions();
    return () => {
      isMounted = false;
    };
  }, [schema.fields]);

  const renderInput = (field: FormField<T>) => {
    if (field.hidden) {
      return null;
    }
    const commonProperties = {
      id: field.name,
      name: field.name,
      placeholder: field.placeholder,
      disabled: readOnly || field.disabled,
      required: field.required,
      className: "form-control",
      value: String(values[field.name] ?? ""),
      onChange: (
        e: ChangeEvent<
          HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
      ) => {
        const target = e.target as HTMLInputElement &
          HTMLTextAreaElement &
          HTMLSelectElement;
        let newValue: unknown = target.value;
        if (field.type === "number" || field.type === "slider") {
          const parsed = Number(newValue);
          newValue = Number.isNaN(parsed) ? undefined : parsed;
        }
        if (field.type === "boolean") {
          newValue = (target as HTMLInputElement).checked;
        }
        if (field.type === "file") {
          const files = (target as HTMLInputElement).files;
          newValue = field.multiple ? files : (files?.[0] ?? null);
        }
        onChange(field.name, newValue);
      },
    };

    switch (field.type) {
      case "text": {
        return <input type="text" {...commonProperties} />;
      }
      case "textarea": {
        return <textarea {...commonProperties} rows={4} />;
      }
      case "number": {
        return (
          <input
            type="number"
            {...commonProperties}
            min={field.min}
            max={field.max}
            step={field.step ?? 1}
          />
        );
      }
      case "date": {
        return <input type="date" {...commonProperties} />;
      }
      case "boolean": {
        return (
          <div className="form-check">
            <input
              id={field.name}
              name={field.name}
              className="form-check-input"
              type="checkbox"
              checked={Boolean(values[field.name])}
              disabled={readOnly || field.disabled}
              onChange={e => onChange(field.name, e.target.checked)}
            />
          </div>
        );
      }
      case "select": {
        const options = Array.isArray(field.options)
          ? field.options
          : (asyncOptions[field.name] ?? []);
        return (
          <select {...commonProperties} className="form-select">
            <option value="">Выберите...</option>
            {options.map(o => (
              <option key={String(o.value)} value={String(o.value)}>
                {o.label}
              </option>
            ))}
          </select>
        );
      }
      case "file": {
        return (
          <input
            type="file"
            id={field.name}
            name={field.name}
            className="form-control"
            multiple={field.multiple}
            accept={field.accept}
            disabled={readOnly || field.disabled}
            onChange={e => {
              const files = e.target.files;
              onChange(
                field.name,
                field.multiple ? files : (files?.[0] ?? null)
              );
            }}
          />
        );
      }
      case "slider": {
        return (
          <input
            type="range"
            id={field.name}
            name={field.name}
            className="form-range"
            min={field.min}
            max={field.max}
            step={field.step ?? 1}
            value={Number(values[field.name] ?? field.min ?? 0)}
            disabled={readOnly || field.disabled}
            onChange={e => onChange(field.name, Number(e.target.value))}
          />
        );
      }
      default: {
        return <input type="text" {...commonProperties} />;
      }
    }
  };

  return (
    <div className={`crud-form crud-form--${schema.layout ?? "vertical"}`}>
      {schema.fields.map(field => (
        <div key={field.name} className="mb-3">
          <label htmlFor={field.name} className="form-label">
            {field.label}
            {field.required ? " *" : ""}
          </label>
          {renderInput(field)}
          {field.helpText ? (
            <div className="form-text">{field.helpText}</div>
          ) : null}
          {errors?.[field.name] ? (
            <div className="text-danger small">{errors[field.name]}</div>
          ) : null}
        </div>
      ))}
    </div>
  );
}
