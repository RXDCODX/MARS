import { Button, Input, InputNumber, Switch, Typography } from "antd";
import { Save, X } from "lucide-react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const HusbandForm: React.FC = () => {
  const { showToast } = useToastModal();
  const showForm = useWaifuRollStore(s => s.showForm);
  const husbandFormValues = useWaifuRollStore(s => s.husbandFormValues);
  const isSubmitting = useWaifuRollStore(s => s.isSubmitting);
  const setHusbandFormValues = useWaifuRollStore(s => s.setHusbandFormValues);
  const submitHusband = useWaifuRollStore(s => s.submitHusband);
  const cancelForm = useWaifuRollStore(s => s.cancelForm);

  if (!showForm) {
    return null;
  }

  const handleSubmit = async () => {
    const result = await submitHusband();
    if (result) {
      showToast(result);
    }
  };

  const handleChange = (field: string, value: unknown) => {
    setHusbandFormValues({ ...husbandFormValues, [field]: value });
  };

  return (
    <div className={styles.formOverlay} data-testid="husband-form-overlay">
      <div className={styles.formPanel} data-testid="husband-form">
        <Typography.Title level={4} className={styles.formTitle}>
          Редактировать мужа
        </Typography.Title>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>Waifu Bride ID</label>
            <Input
              value={husbandFormValues.waifuBrideId}
              onChange={e => handleChange("waifuBrideId", e.target.value)}
              placeholder="ShikiId вайфу"
              data-testid="input-waifu-bride-id"
            />
          </div>
          <div className={styles.formField}>
            <label>Waifu Roll ID</label>
            <Input
              value={husbandFormValues.waifuRollId}
              onChange={e => handleChange("waifuRollId", e.target.value)}
              placeholder="ShikiId последнего роула"
              data-testid="input-waifu-roll-id"
            />
          </div>
        </div>

        <div className={styles.formRow}>
          <div className={styles.formField}>
            <label>В браке</label>
            <Switch
              checked={husbandFormValues.isPrivated}
              onChange={checked => handleChange("isPrivated", checked)}
              data-testid="switch-is-privated"
            />
          </div>
          <div className={styles.formField}>
            <label>Месяцев поздравлено</label>
            <InputNumber
              value={husbandFormValues.lastWeddingCongratulatedMonths}
              onChange={value =>
                handleChange("lastWeddingCongratulatedMonths", value ?? 0)
              }
              style={{ width: "100%" }}
              data-testid="input-last-congratulated"
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
            Сохранить
          </Button>
        </div>
      </div>
    </div>
  );
};

export default HusbandForm;
