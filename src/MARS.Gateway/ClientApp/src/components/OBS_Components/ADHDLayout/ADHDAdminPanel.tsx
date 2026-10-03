import { Button, Card, Flex, Slider, Switch, Typography } from "antd";

import { useSiteColors } from "@/shared/Utils/useSiteColors";

import ThemeToggle from "../../ThemeToggle";
import styles from "./ADHDAdminPanel.module.scss";
import {
  type AdhdBooleanComponentKey,
  useAdhdConfig,
  useAdhdConfigActions,
} from "./store/adhdLayoutStore";

interface ComponentToggle {
  key: AdhdBooleanComponentKey;
  label: string;
  description: string;
}

const componentToggles: ComponentToggle[] = [
  { key: "showRainEffect", label: "Дождь", description: "Canvas-эффект дождя" },
  {
    key: "showDVDLogos",
    label: "DVD-логотипы",
    description: "Прыгающие логотипы DVD",
  },
  {
    key: "showBreakingNews",
    label: "Срочные новости",
    description: "Бегущая строка новостей",
  },
  {
    key: "showStreamerVideo",
    label: "Видео стримера",
    description: "Видео стримера снизу слева",
  },
  {
    key: "showFitnessVideo",
    label: "Фитнес-видео",
    description: "Фитнес-видео снизу по центру",
  },
  {
    key: "showGTAVideo",
    label: "GTA-видео",
    description: "Геймплей GTA слева",
  },
  {
    key: "showHydraulicMobileVideo",
    label: "Гидравлический пресс",
    description: "Видео гидравлического пресса",
  },
  {
    key: "showSlimeVideo",
    label: "Слайм-видео",
    description: "Слайм ASMR видео",
  },
  {
    key: "showMukbangVideo",
    label: "Мукбанг-видео",
    description: "Мукбанг видео сверху слева",
  },
  {
    key: "showQuiz",
    label: "Викторина",
    description: "Викторина в стиле Duolingo",
  },
  { key: "showSurfer", label: "Сёрфер", description: "Видео сёрфера справа" },
  {
    key: "showLOFIGirl",
    label: "Lo-fi girl",
    description: "Анимированная картинка",
  },
  {
    key: "showCatisa",
    label: "Котик",
    description: "Полупрозрачный кот на весь экран",
  },
  {
    key: "showNotifications",
    label: "Уведомления",
    description: "Фейковые email-уведомления",
  },
  {
    key: "showTimer",
    label: "Таймер",
    description: "Отображение таймера (счёт продолжается)",
  },
];

export function ADHDAdminPanel() {
  const colors = useSiteColors();
  const config = useAdhdConfig();
  const {
    toggleComponent,
    setAllComponents,
    setDvdLogosCount,
    resetToDefaults,
  } = useAdhdConfigActions();

  const enabledCount = componentToggles.filter(
    toggle => config[toggle.key]
  ).length;

  return (
    <div
      className={`py-4 admin-panel ${styles.adminPanel}`}
      style={{
        backgroundColor: colors.background.primary,
        color: colors.text.primary,
        maxWidth: 1200,
        margin: "0 auto",
        padding: "1.5rem",
      }}
      data-testid="adhd-admin-panel"
    >
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Typography.Title
          level={2}
          className={styles.adminTitle}
          style={{ ...colors.utils.getTextStyle("primary"), margin: 0 }}
        >
          Панель управления ADHD
        </Typography.Title>
        <ThemeToggle variant="admin" size="md" />
      </Flex>

      <Card
        className="card"
        style={{ marginBottom: 16 }}
        data-testid="card-adhd-controls"
      >
        <Flex
          justify="space-between"
          align="center"
          style={{ marginBottom: 16 }}
        >
          <Typography.Text style={{ color: colors.text.secondary }}>
            Включено компонентов: {enabledCount} / {componentToggles.length}
          </Typography.Text>
          <Flex gap={8}>
            <Button
              size="small"
              onClick={() => setAllComponents(true)}
              data-testid="button-adhd-select-all"
            >
              Включить все
            </Button>
            <Button
              size="small"
              onClick={() => setAllComponents(false)}
              data-testid="button-adhd-deselect-all"
            >
              Выключить все
            </Button>
            <Button
              size="small"
              onClick={resetToDefaults}
              data-testid="button-adhd-reset"
            >
              Сброс
            </Button>
          </Flex>
        </Flex>

        <Flex
          align="center"
          justify="space-between"
          className={styles.toggleRow}
          style={{ marginBottom: 16 }}
          data-testid="row-adhd-dvd-count"
        >
          <div>
            <Typography.Text strong style={{ color: colors.text.primary }}>
              Количество DVD-логотипов
            </Typography.Text>
            <br />
            <Typography.Text
              type="secondary"
              style={{ color: colors.text.secondary }}
            >
              Летающих логотипов: {config.dvdLogosCount} (от 1 до 20)
            </Typography.Text>
          </div>
          <Slider
            min={1}
            max={20}
            value={config.dvdLogosCount}
            onChange={setDvdLogosCount}
            style={{ width: 240 }}
            data-testid="slider-adhd-dvd-count"
          />
        </Flex>

        <Flex vertical gap={8}>
          {componentToggles.map(toggle => {
            const isEnabled = config[toggle.key];

            return (
              <Flex
                key={toggle.key}
                align="center"
                justify="space-between"
                className={styles.toggleRow}
                data-testid={`row-adhd-${toggle.key}`}
              >
                <div>
                  <Typography.Text
                    strong
                    style={{ color: colors.text.primary }}
                  >
                    {toggle.label}
                  </Typography.Text>
                  <br />
                  <Typography.Text
                    type="secondary"
                    style={{ color: colors.text.secondary }}
                  >
                    {toggle.description}
                  </Typography.Text>
                </div>
                <Switch
                  checked={isEnabled}
                  onChange={() => toggleComponent(toggle.key)}
                  data-testid={`switch-adhd-${toggle.key}`}
                />
              </Flex>
            );
          })}
        </Flex>
      </Card>
    </div>
  );
}
