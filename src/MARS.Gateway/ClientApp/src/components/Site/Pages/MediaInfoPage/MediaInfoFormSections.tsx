import { Button, Flex, Form, Input, InputNumber, Select, Switch } from "antd";

import {
  ApiMediaInfo,
  MediaFileInfoTypeEnum,
  MediaMetaInfoPriorityEnum,
} from "@/shared/api";

import {
  mediaInfoFileTypes,
  mediaInfoPriorities,
} from "./mediaInfoPageHelpers";

type Properties = {
  formData: ApiMediaInfo;
  onChange: (path: string, value: unknown) => void;
  onGenerateRewardId: () => void;
  onClearRewardId: () => void;
  onFileSelected: (file: File | null) => void;
  helpText?: string;
  previewUrl?: string | null;
};

export const MediaInfoFormSections: React.FC<Properties> = ({
  formData,
  onChange,
  onGenerateRewardId,
  onClearRewardId,
  onFileSelected,
  helpText,
  previewUrl,
}) => (
  <Flex vertical gap={18} data-testid="media-info-form-sections">
    <section className="form-section-card">
      <div className="section-heading">
        <h2>Основные данные</h2>
        <p>Название, триггер и сам текст алерта.</p>
      </div>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Отображаемое имя"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
          required
        >
          <Input
            value={formData.metaInfo.displayName}
            onChange={event =>
              onChange("metaInfo.displayName", event.target.value)
            }
            placeholder="Например: Welcome clip"
            data-testid="input-display-name"
          />
        </Form.Item>

        <Form.Item
          label="Триггер"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
        >
          <Input
            value={formData.textInfo.triggerWord ?? ""}
            onChange={event =>
              onChange("textInfo.triggerWord", event.target.value)
            }
            placeholder="Команда или слово"
            data-testid="input-trigger-word"
          />
        </Form.Item>
      </Flex>

      <Form.Item label="Текст алерта" layout="vertical">
        <Input.TextArea
          value={formData.textInfo.text ?? ""}
          onChange={event => onChange("textInfo.text", event.target.value)}
          rows={4}
          placeholder="Текст, который увидит зритель"
          data-testid="input-alert-text"
        />
      </Form.Item>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Цвет текста"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Input
            value={formData.textInfo.textColor ?? ""}
            onChange={event =>
              onChange("textInfo.textColor", event.target.value)
            }
            placeholder="#ffffff"
            data-testid="input-text-color"
          />
        </Form.Item>

        <Form.Item
          label="Цвет ключевых слов"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Input
            value={formData.textInfo.keyWordsColor ?? ""}
            onChange={event =>
              onChange("textInfo.keyWordsColor", event.target.value)
            }
            placeholder="#f7c948"
            data-testid="input-keywords-color"
          />
        </Form.Item>

        <Form.Item
          label="Разделитель ключевых слов"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Input
            value={formData.textInfo.keyWordSybmolDelimiter ?? ""}
            onChange={event =>
              onChange("textInfo.keyWordSybmolDelimiter", event.target.value)
            }
            placeholder="#"
            maxLength={1}
            data-testid="input-keyword-delimiter"
          />
        </Form.Item>
      </Flex>
    </section>

    <section className="form-section-card reward-card">
      <div className="section-heading">
        <h2>Своя награда</h2>
        <p>Привяжи Twitch reward или сгенерируй новый идентификатор.</p>
      </div>

      <div className="reward-state">
        <span
          className={`reward-pill ${formData.metaInfo.twitchGuid ? "linked" : "empty"}`}
        >
          {formData.metaInfo.twitchGuid
            ? "Награда привязана"
            : "Награда не привязана"}
        </span>
        <span className="reward-hint">
          {formData.metaInfo.twitchGuid
            ? formData.metaInfo.twitchGuid
            : "Сейчас можно добавить свой reward id"}
        </span>
      </div>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Reward ID"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
        >
          <Input
            value={formData.metaInfo.twitchGuid ?? ""}
            onChange={event =>
              onChange(
                "metaInfo.twitchGuid",
                event.target.value.trim() ? event.target.value : undefined
              )
            }
            placeholder="UUID кастомной награды"
            data-testid="input-reward-id"
          />
        </Form.Item>

        <Form.Item
          label="Стоимость"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
        >
          <InputNumber
            value={formData.metaInfo.twitchPointsCost}
            onChange={value => {
              if (value !== null) {
                onChange("metaInfo.twitchPointsCost", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-points-cost"
          />
        </Form.Item>
      </Flex>

      <Flex gap={12} wrap="wrap" className="reward-actions">
        <Button
          type="default"
          onClick={onGenerateRewardId}
          data-testid="button-generate-reward"
        >
          Сгенерировать новый reward id
        </Button>
        <Button onClick={onClearRewardId} data-testid="button-clear-reward">
          Очистить привязку
        </Button>
      </Flex>
    </section>

    <section className="form-section-card">
      <div className="section-heading">
        <h2>Файл</h2>
        <p>Путь, тип и локальность медиа.</p>
        {helpText ? <p>{helpText}</p> : null}
      </div>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Имя файла"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
          required
        >
          <Input
            value={formData.fileInfo.fileName}
            onChange={event =>
              onChange("fileInfo.fileName", event.target.value)
            }
            data-testid="input-file-name"
          />
        </Form.Item>

        <Form.Item
          label="Расширение"
          layout="vertical"
          style={{ flex: 1, minWidth: 200 }}
          required
        >
          <Input
            value={formData.fileInfo.extension}
            onChange={event =>
              onChange("fileInfo.extension", event.target.value)
            }
            data-testid="input-file-extension"
          />
        </Form.Item>
      </Flex>

      <Form.Item label="Путь к файлу" layout="vertical" required>
        <Input
          value={formData.fileInfo.filePath}
          onChange={event => onChange("fileInfo.filePath", event.target.value)}
          placeholder="/Alerts/uploaded_mems/..."
          data-testid="input-file-path"
        />
      </Form.Item>

      <Form.Item label="Загрузить файл" layout="vertical">
        <input
          type="file"
          data-testid="input-file-upload"
          onChange={event => {
            const file = event.target.files && event.target.files[0];
            if (file) {
              onFileSelected(file);
            }

            if (event.target) {
              event.target.value = "";
            }
          }}
        />
      </Form.Item>

      {(previewUrl || formData.fileInfo.filePath) && (
        <div className="field">
          <span>Предпросмотр</span>
          <div>
            {(() => {
              const extension = (
                formData.fileInfo.extension || ""
              ).toLowerCase();
              const fp = previewUrl || formData.fileInfo.filePath;
              if (
                [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"].includes(
                  extension
                )
              ) {
                return (
                  <img
                    src={fp}
                    alt={formData.metaInfo.displayName}
                    style={{ maxWidth: "100%", borderRadius: 8 }}
                  />
                );
              }
              if ([".mp3", ".wav", ".ogg"].includes(extension)) {
                return <audio controls src={fp} />;
              }
              if ([".mp4", ".webm", ".mov", ".avi"].includes(extension)) {
                return <video controls src={fp} style={{ maxWidth: "100%" }} />;
              }

              return (
                <div className="muted-line">
                  Файл: {formData.fileInfo.fileName}
                </div>
              );
            })()}
          </div>
        </div>
      )}

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Тип файла"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Select
            value={formData.fileInfo.type}
            onChange={value => onChange("fileInfo.type", value)}
            options={mediaInfoFileTypes.map(type => ({
              label: type,
              value: type,
            }))}
            data-testid="select-file-type"
          />
        </Form.Item>

        <Form.Item
          label="Локальный файл"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Select
            value={formData.fileInfo.isLocalFile ? "true" : "false"}
            onChange={value =>
              onChange("fileInfo.isLocalFile", value === "true")
            }
            options={[
              { label: "Да", value: "true" },
              { label: "Нет", value: "false" },
            ]}
            data-testid="select-local-file"
          />
        </Form.Item>

        <Form.Item
          label="Формат preview"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Input
            value={formData.fileInfo.extension}
            disabled
            data-testid="input-preview-format"
          />
        </Form.Item>
      </Flex>
    </section>

    <section className="form-section-card">
      <div className="section-heading">
        <h2>Позиция и размер</h2>
        <p>Координаты, масштаб и флаги поведения.</p>
      </div>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="X"
          layout="vertical"
          style={{ flex: 1, minWidth: 120 }}
        >
          <InputNumber
            value={formData.positionInfo.xCoordinate}
            onChange={value => {
              if (value !== null) {
                onChange("positionInfo.xCoordinate", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-x-coordinate"
          />
        </Form.Item>

        <Form.Item
          label="Y"
          layout="vertical"
          style={{ flex: 1, minWidth: 120 }}
        >
          <InputNumber
            value={formData.positionInfo.yCoordinate}
            onChange={value => {
              if (value !== null) {
                onChange("positionInfo.yCoordinate", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-y-coordinate"
          />
        </Form.Item>

        <Form.Item
          label="Ширина"
          layout="vertical"
          style={{ flex: 1, minWidth: 120 }}
        >
          <InputNumber
            value={formData.positionInfo.width}
            onChange={value => {
              if (value !== null) {
                onChange("positionInfo.width", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-width"
          />
        </Form.Item>

        <Form.Item
          label="Высота"
          layout="vertical"
          style={{ flex: 1, minWidth: 120 }}
        >
          <InputNumber
            value={formData.positionInfo.height}
            onChange={value => {
              if (value !== null) {
                onChange("positionInfo.height", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-height"
          />
        </Form.Item>
      </Flex>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Поворот"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <InputNumber
            value={formData.positionInfo.rotation}
            onChange={value => {
              if (value !== null) {
                onChange("positionInfo.rotation", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-rotation"
          />
        </Form.Item>

        <Form.Item
          label="Рандомные координаты"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Select
            value={formData.positionInfo.randomCoordinates ? "true" : "false"}
            onChange={value =>
              onChange("positionInfo.randomCoordinates", value === "true")
            }
            options={[
              { label: "Да", value: "true" },
              { label: "Нет", value: "false" },
            ]}
            data-testid="select-random-coordinates"
          />
        </Form.Item>

        <Form.Item
          label="Пропорционально"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Select
            value={formData.positionInfo.isProportion ? "true" : "false"}
            onChange={value =>
              onChange("positionInfo.isProportion", value === "true")
            }
            options={[
              { label: "Да", value: "true" },
              { label: "Нет", value: "false" },
            ]}
            data-testid="select-proportion"
          />
        </Form.Item>
      </Flex>

      <div className="toggle-grid">
        <label className="toggle-field">
          <Switch
            checked={formData.positionInfo.isRotated}
            onChange={checked => onChange("positionInfo.isRotated", checked)}
            data-testid="switch-rotated"
          />
          <span>Рандомный поворот</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.positionInfo.isResizeRequires}
            onChange={checked =>
              onChange("positionInfo.isResizeRequires", checked)
            }
            data-testid="switch-resize-requires"
          />
          <span>Требует ресайз</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.positionInfo.isHorizontalCenter}
            onChange={checked =>
              onChange("positionInfo.isHorizontalCenter", checked)
            }
            data-testid="switch-horizontal-center"
          />
          <span>Центр по горизонтали</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.positionInfo.isVerticallCenter}
            onChange={checked =>
              onChange("positionInfo.isVerticallCenter", checked)
            }
            data-testid="switch-vertical-center"
          />
          <span>Центр по вертикали</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.positionInfo.isUseOriginalWidthAndHeight}
            onChange={checked =>
              onChange("positionInfo.isUseOriginalWidthAndHeight", checked)
            }
            data-testid="switch-original-size"
          />
          <span>Оригинальный размер</span>
        </label>
      </div>
    </section>

    <section className="form-section-card">
      <div className="section-heading">
        <h2>Поведение</h2>
        <p>Длительность, приоритет, громкость и флаги отображения.</p>
      </div>

      <Flex gap={14} wrap="wrap">
        <Form.Item
          label="Длительность, сек"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <InputNumber
            min={1}
            max={3600}
            value={formData.metaInfo.duration}
            onChange={value => {
              if (value !== null) {
                onChange("metaInfo.duration", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-duration"
          />
        </Form.Item>

        <Form.Item
          label="Приоритет"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <Select
            value={formData.metaInfo.priority}
            onChange={value => onChange("metaInfo.priority", value)}
            options={mediaInfoPriorities.map(priority => ({
              label: priority,
              value: priority,
            }))}
            data-testid="select-priority"
          />
        </Form.Item>

        <Form.Item
          label="Громкость"
          layout="vertical"
          style={{ flex: 1, minWidth: 160 }}
        >
          <InputNumber
            min={0}
            max={400}
            value={formData.metaInfo.volume}
            onChange={value => {
              if (value !== null) {
                onChange("metaInfo.volume", value);
              }
            }}
            style={{ width: "100%" }}
            data-testid="input-volume"
          />
        </Form.Item>
      </Flex>

      <div className="toggle-grid">
        <label className="toggle-field">
          <Switch
            checked={formData.metaInfo.isLooped}
            onChange={checked => onChange("metaInfo.isLooped", checked)}
            data-testid="switch-looped"
          />
          <span>Зациклить</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.metaInfo.isEnabled}
            onChange={checked => onChange("metaInfo.isEnabled", checked)}
            data-testid="switch-enabled"
          />
          <span>Включено</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.metaInfo.vip}
            onChange={checked => onChange("metaInfo.vip", checked)}
            data-testid="switch-vip"
          />
          <span>VIP</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.metaInfo.isFreezeRequired}
            onChange={checked => onChange("metaInfo.isFreezeRequired", checked)}
            disabled={
              formData.metaInfo.priority !== MediaMetaInfoPriorityEnum.High
            }
            data-testid="switch-freeze-required"
          />
          <span>Заморозить OBS</span>
        </label>
      </div>
    </section>

    <section className="form-section-card">
      <div className="section-heading">
        <h2>Стили</h2>
        <p>Визуальные флаги для рамки и letterbox.</p>
      </div>

      <div className="toggle-grid">
        <label className="toggle-field">
          <Switch
            checked={formData.stylesInfo.isBorder}
            onChange={checked => onChange("stylesInfo.isBorder", checked)}
            data-testid="switch-border"
          />
          <span>Рамка</span>
        </label>

        <label className="toggle-field">
          <Switch
            checked={formData.stylesInfo.isShowLetterbox}
            onChange={checked =>
              onChange("stylesInfo.isShowLetterbox", checked)
            }
            data-testid="switch-letterbox"
          />
          <span>Letterbox</span>
        </label>
      </div>
    </section>
  </Flex>
);
