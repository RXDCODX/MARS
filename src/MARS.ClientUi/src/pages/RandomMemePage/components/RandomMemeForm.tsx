import {
  Alert,
  Button,
  Card,
  Flex,
  Form,
  Input,
  InputNumber,
  Select,
  Spin,
} from "antd";
import { ArrowLeft, Save } from "lucide-react";
import { useEffect, useState } from "react";

import {
  MemeFormData,
  MemeOrderFormData,
  MemeTypeFormData,
  RandomMemeFormProps as RandomMemeFormProperties,
} from "../RandomMemePage.types";

const RandomMemeForm: React.FC<RandomMemeFormProperties> = ({
  memeType,
  memeOrder,
  memeTypes = [],
  isSubmitting,
  mode,
  onSubmit,
  onCancel,
}) => {
  const isType = !!memeType;
  const isEdit = mode === "edit";

  const [formData, setFormData] = useState<MemeFormData>(
    isType
      ? {
          name: memeType?.name || "",
          folderPath: memeType?.folderPath || "",
        }
      : {
          filePath: memeOrder?.filePath || "",
          memeTypeId: memeOrder?.memeTypeId || 0,
          order: memeOrder?.order || 1,
        }
  );

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string>("");

  useEffect(() => {
    if (isType && memeType) {
      setFormData({
        name: memeType.name,
        folderPath: memeType.folderPath,
      });
    } else if (!isType && memeOrder) {
      setFormData({
        filePath: memeOrder.filePath,
        memeTypeId: memeOrder.memeTypeId || 0,
        order: memeOrder.order,
      });
    }
  }, [memeType, memeOrder, isType]);

  const handleInputChange = (field: string, value: string | number) => {
    setFormData(previous => ({
      ...previous,
      [field]: value,
    }));

    if (errors[field]) {
      setErrors(previous => ({
        ...previous,
        [field]: "",
      }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (isType) {
      const typeData = formData as MemeTypeFormData;
      if (!typeData.name.trim()) {
        newErrors.name = "Название типа обязательно";
      } else if (typeData.name.length > 50) {
        newErrors.name = "Название не может быть длиннее 50 символов";
      }

      if (!typeData.folderPath.trim()) {
        newErrors.folderPath = "Путь к папке обязателен";
      }
    } else {
      const orderData = formData as MemeOrderFormData;
      if (!orderData.filePath.trim()) {
        newErrors.filePath = "Путь к файлу обязателен";
      }

      if (!orderData.memeTypeId || orderData.memeTypeId <= 0) {
        newErrors.memeTypeId = "Необходимо выбрать тип мема";
      }

      if (orderData.order < 1) {
        newErrors.order = "Порядок должен быть положительным числом";
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError("");

    if (!validateForm()) {
      return;
    }

    try {
      await onSubmit(formData as unknown as Record<string, unknown>);
    } catch (error) {
      setSubmitError(
        error instanceof Error
          ? error.message
          : `Ошибка ${isEdit ? "обновления" : "создания"} ${isType ? "типа" : "заказа"}`
      );
    }
  };

  const renderTypeForm = () => {
    const data = formData as MemeTypeFormData;

    return (
      <Flex gap={12} wrap="wrap">
        <div style={{ flex: "0 0 100%" }}>
          <Form.Item
            label={
              <span style={{ fontWeight: 600 }}>
                Название типа <span style={{ color: "#ff4d4f" }}>*</span>
              </span>
            }
            validateStatus={errors.name ? "error" : undefined}
            help={errors.name}
          >
            <Input
              value={data.name}
              onChange={e => handleInputChange("name", e.target.value)}
              placeholder="Введите название типа мема"
              maxLength={50}
              status={errors.name ? "error" : undefined}
            />
            <span style={{ color: "#8c8c8c", fontSize: 12 }}>
              Максимум 50 символов
            </span>
          </Form.Item>
        </div>

        <div style={{ flex: "0 0 100%" }}>
          <Form.Item
            label={
              <span style={{ fontWeight: 600 }}>
                Путь к папке <span style={{ color: "#ff4d4f" }}>*</span>
              </span>
            }
            validateStatus={errors.folderPath ? "error" : undefined}
            help={errors.folderPath}
          >
            <Input
              value={data.folderPath}
              onChange={e => handleInputChange("folderPath", e.target.value)}
              placeholder="Введите путь к папке с мемами"
              status={errors.folderPath ? "error" : undefined}
            />
            <span style={{ color: "#8c8c8c", fontSize: 12 }}>
              Абсолютный или относительный путь к папке
            </span>
          </Form.Item>
        </div>
      </Flex>
    );
  };

  const renderOrderForm = () => {
    const data = formData as MemeOrderFormData;

    return (
      <Flex gap={12} wrap="wrap">
        <div style={{ flex: "0 0 100%" }}>
          <Form.Item
            label={
              <span style={{ fontWeight: 600 }}>
                Путь к файлу <span style={{ color: "#ff4d4f" }}>*</span>
              </span>
            }
            validateStatus={errors.filePath ? "error" : undefined}
            help={errors.filePath}
          >
            <Input
              value={data.filePath}
              onChange={e => handleInputChange("filePath", e.target.value)}
              placeholder="Введите путь к файлу мема"
              status={errors.filePath ? "error" : undefined}
            />
            <span style={{ color: "#8c8c8c", fontSize: 12 }}>
              Полный путь к файлу мема
            </span>
          </Form.Item>
        </div>

        <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
          <Form.Item
            label={
              <span style={{ fontWeight: 600 }}>
                Тип мема <span style={{ color: "#ff4d4f" }}>*</span>
              </span>
            }
            validateStatus={errors.memeTypeId ? "error" : undefined}
            help={errors.memeTypeId}
          >
            <Select
              value={data.memeTypeId || undefined}
              onChange={value => handleInputChange("memeTypeId", value)}
              placeholder="Выберите тип мема"
              status={errors.memeTypeId ? "error" : undefined}
              options={[
                { label: "Выберите тип мема", value: 0, disabled: true },
                ...memeTypes.map(type => ({
                  label: `${type.name} (ID: ${type.id})`,
                  value: type.id,
                })),
              ]}
            />
          </Form.Item>
        </div>

        <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
          <Form.Item
            label={
              <span style={{ fontWeight: 600 }}>
                Порядок <span style={{ color: "#ff4d4f" }}>*</span>
              </span>
            }
            validateStatus={errors.order ? "error" : undefined}
            help={errors.order}
          >
            <InputNumber
              min={1}
              value={data.order}
              onChange={value => handleInputChange("order", value || 1)}
              placeholder="1"
              style={{ width: "100%" }}
              status={errors.order ? "error" : undefined}
            />
            <span style={{ color: "#8c8c8c", fontSize: 12 }}>
              Порядок отображения в рандомизации
            </span>
          </Form.Item>
        </div>
      </Flex>
    );
  };

  return (
    <div style={{ padding: "16px 0" }}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <div>
          <h1
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              marginBottom: 8,
            }}
          >
            <Save size={32} />
            {isEdit ? "Редактирование" : "Создание"}{" "}
            {isType ? "типа мема" : "заказа мема"}
          </h1>
          <p style={{ color: "#8c8c8c", marginBottom: 0 }}>
            {isEdit
              ? `Редактирование ${isType ? "типа мема" : "заказа мема"}`
              : `Создание нового ${isType ? "типа мема" : "заказа мема"}`}
          </p>
        </div>

        <Button
          onClick={onCancel}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <ArrowLeft size={16} />
          Отмена
        </Button>
      </Flex>

      {submitError && (
        <Alert
          type="error"
          showIcon
          message="Ошибка сохранения"
          description={submitError}
          style={{ marginBottom: 16 }}
        />
      )}

      <Card title={isType ? "Параметры типа мема" : "Параметры заказа мема"}>
        <Form onSubmitCapture={handleSubmit}>
          {isType ? renderTypeForm() : renderOrderForm()}

          <Flex justify="flex-end" gap={12} style={{ marginTop: 16 }}>
            <Button onClick={onCancel} disabled={isSubmitting}>
              Отмена
            </Button>
            <Button
              type="primary"
              htmlType="submit"
              disabled={isSubmitting}
              style={{ display: "flex", alignItems: "center", gap: 6 }}
            >
              {isSubmitting ? (
                <>
                  <Spin size="small" />
                  Сохранение...
                </>
              ) : (
                <>
                  <Save size={16} />
                  {isEdit ? "Сохранить изменения" : "Создать"}
                </>
              )}
            </Button>
          </Flex>
        </Form>
      </Card>

      <Alert
        type="info"
        showIcon
        message="Правила валидации"
        description={
          <ul style={{ marginBottom: 0 }}>
            {isType ? (
              <>
                <li>
                  Название типа обязательно и не может быть длиннее 50 символов
                </li>
                <li>Путь к папке обязателен</li>
              </>
            ) : (
              <>
                <li>Путь к файлу обязателен</li>
                <li>Необходимо выбрать тип мема</li>
                <li>Порядок должен быть положительным числом</li>
              </>
            )}
          </ul>
        }
        style={{ marginTop: 16 }}
      />
    </div>
  );
};

export default RandomMemeForm;
