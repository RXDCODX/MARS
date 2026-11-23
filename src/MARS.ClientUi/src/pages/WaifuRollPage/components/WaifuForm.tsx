import { Button, Input, InputNumber, Select, Typography } from "antd";
import { Save, Upload, X } from "lucide-react";
import { useEffect, useRef } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const WaifuForm: React.FC = () => {
  const { showToast } = useToastModal();
  const showForm = useWaifuRollStore(s => s.showForm);
  const formMode = useWaifuRollStore(s => s.formMode);
  const waifuFormValues = useWaifuRollStore(s => s.waifuFormValues);
  const audios = useWaifuRollStore(s => s.audios);
  const isSubmitting = useWaifuRollStore(s => s.isSubmitting);
  const setWaifuFormValues = useWaifuRollStore(s => s.setWaifuFormValues);
  const submitWaifu = useWaifuRollStore(s => s.submitWaifu);
  const cancelForm = useWaifuRollStore(s => s.cancelForm);
  const loadAudios = useWaifuRollStore(s => s.loadAudios);
  const uploadAudio = useWaifuRollStore(s => s.uploadAudio);
  const fileInputReference = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (showForm && audios.length === 0) {
      void loadAudios({ showToast: false });
    }
  }, [showForm, audios.length, loadAudios, showToast]);

  if (!showForm) {
    return null;
  }

  const handleSubmit = async () => {
    const result = await submitWaifu();
    if (result) {
      showToast(result);
    }
  };

  const handleChange = (field: string, value: unknown) => {
    setWaifuFormValues({ ...waifuFormValues, [field]: value });
  };

  const handleAudioUpload = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const name = file.name.replace(/\.[^/.]+$/, "");
    const result = await uploadAudio(file, name);
    if (result) {
      showToast(result);
    }

    if (fileInputReference.current) {
      fileInputReference.current.value = "";
    }
  };

  return (
    <div className={styles.formOverlay} data-testid="waifu-form-overlay">
      <div className={styles.formPanel} data-testid="waifu-form">
        <Typography.Title level={4} className={styles.formTitle}>
          {formMode === "create" ? "Создать вайфу" : "Редактировать вайфу"}
        </Typography.Title>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>Shiki ID</label>
            <Input
              value={waifuFormValues.shikiId}
              onChange={e => handleChange("shikiId", e.target.value)}
              disabled={formMode === "edit"}
              placeholder="shikimori ID"
              data-testid="input-shiki-id"
            />
          </div>
          <div className={styles.formField}>
            <label>Имя</label>
            <Input
              value={waifuFormValues.name}
              onChange={e => handleChange("name", e.target.value)}
              placeholder="Имя персонажа"
              data-testid="input-name"
            />
          </div>
        </div>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>Возраст</label>
            <InputNumber
              value={waifuFormValues.age}
              onChange={value => handleChange("age", value ?? 0)}
              style={{ width: "100%" }}
              data-testid="input-age"
            />
          </div>
          <div className={styles.formField}>
            <label>Аудио</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <Select
                value={waifuFormValues.audioId || undefined}
                onChange={value => handleChange("audioId", value ?? "")}
                placeholder="Выбрать аудио"
                allowClear
                style={{ flex: 1 }}
                data-testid="select-audio"
                options={audios.map(a => ({
                  label: `${a.name} (${a.fileExtension})`,
                  value: a.id,
                }))}
              />
              <input
                ref={fileInputReference}
                type="file"
                accept=".mp3,.wav,.ogg,.m4a,.flac"
                onChange={handleAudioUpload}
                style={{ display: "none" }}
                data-testid="input-audio-file"
              />
              <Button
                icon={<Upload size={14} />}
                onClick={() => fileInputReference.current?.click()}
                data-testid="button-upload-audio"
              >
                Загрузить
              </Button>
            </div>
          </div>
        </div>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>Аниме</label>
            <Input
              value={waifuFormValues.anime}
              onChange={e => handleChange("anime", e.target.value)}
              placeholder="Название аниме"
              data-testid="input-anime"
            />
          </div>
          <div className={styles.formField}>
            <label>Манга</label>
            <Input
              value={waifuFormValues.manga}
              onChange={e => handleChange("manga", e.target.value)}
              placeholder="Название манги"
              data-testid="input-manga"
            />
          </div>
        </div>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>URL изображения</label>
            <Input
              value={waifuFormValues.imageUrl}
              onChange={e => handleChange("imageUrl", e.target.value)}
              placeholder="https://..."
              data-testid="input-image-url"
            />
          </div>
        </div>

        <div className={styles.formActions}>
          <Button
            icon={<X size={16} />}
            onClick={cancelForm}
            data-testid="button-cancel"
          >
            Отмена
          </Button>
          <Button
            type="primary"
            icon={<Save size={16} />}
            loading={isSubmitting}
            onClick={() => void handleSubmit()}
            data-testid="button-save"
          >
            {formMode === "create" ? "Создать" : "Сохранить"}
          </Button>
        </div>
      </div>
    </div>
  );
};

export default WaifuForm;
