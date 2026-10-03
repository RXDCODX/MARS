import {
  Button,
  Card,
  Checkbox,
  Flex,
  InputNumber,
  Slider,
  Typography,
} from "antd";
import { Minimize2, Move } from "lucide-react";

import { useLayout, useLayoutActions } from "./store/scoreboardStore";
import { defaultLayout } from "./types";

const LayoutCard: React.FC = () => {
  const layout = useLayout();
  const { setLayout } = useLayoutActions();

  const handleChange = (
    field: keyof typeof layout,
    value: number | boolean
  ) => {
    setLayout({
      ...layout,
      [field]: value,
    });
  };

  const resetToDefaults = () => {
    setLayout(defaultLayout);
  };

  return (
    <Card
      style={{ marginBottom: 16 }}
      title={
        <Flex justify="space-between" align="center">
          <Typography.Title level={5} style={{ margin: 0 }}>
            <Move style={{ marginRight: 8 }} />
            Настройка макета
          </Typography.Title>
          <Button size="small" onClick={resetToDefaults}>
            <Minimize2 style={{ marginRight: 4 }} />
            Сброс
          </Button>
        </Flex>
      }
    >
      <Flex vertical gap={16} align="center">
        <Flex gap={16} style={{ width: "100%" }}>
          <div style={{ flex: 1 }}>
            <Typography.Title level={6} style={{ marginBottom: 12 }}>
              Позиционирование
            </Typography.Title>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Заголовок - отступ сверху (px)</Typography.Text>
              <Slider
                min={0}
                max={200}
                value={layout.headerTop}
                onChange={value => handleChange("headerTop", value)}
              />
              <Typography.Text type="secondary">
                {layout.headerTop}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>
                Заголовок - позиция по горизонтали (%)
              </Typography.Text>
              <Slider
                min={0}
                max={100}
                value={layout.headerLeft}
                onChange={value => handleChange("headerLeft", value)}
              />
              <Typography.Text type="secondary">
                {layout.headerLeft}%
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Игроки - отступ сверху (px)</Typography.Text>
              <Slider
                min={0}
                max={200}
                value={layout.playersTop}
                onChange={value => handleChange("playersTop", value)}
              />
              <Typography.Text type="secondary">
                {layout.playersTop}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Игроки - отступ слева (px)</Typography.Text>
              <Slider
                min={0}
                max={200}
                value={layout.playersLeft}
                onChange={value => handleChange("playersLeft", value)}
              />
              <Typography.Text type="secondary">
                {layout.playersLeft}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Игроки - отступ справа (px)</Typography.Text>
              <Slider
                min={0}
                max={200}
                value={layout.playersRight}
                onChange={value => handleChange("playersRight", value)}
              />
              <Typography.Text type="secondary">
                {layout.playersRight}px
              </Typography.Text>
            </div>
          </div>

          <div style={{ flex: 1 }}>
            <Typography.Title level={6} style={{ marginBottom: 12 }}>
              Размеры
            </Typography.Title>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Ширина заголовка (px)</Typography.Text>
              <Slider
                min={200}
                max={800}
                value={layout.headerWidth}
                onChange={value => handleChange("headerWidth", value)}
              />
              <Typography.Text type="secondary">
                {layout.headerWidth}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Высота заголовка (px)</Typography.Text>
              <Slider
                min={10}
                max={120}
                value={layout.headerHeight}
                onChange={value => handleChange("headerHeight", value)}
              />
              <Typography.Text type="secondary">
                {layout.headerHeight}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Ширина карточки игрока (px)</Typography.Text>
              <Slider
                min={200}
                max={600}
                value={layout.playerBarWidth}
                onChange={value => handleChange("playerBarWidth", value)}
              />
              <Typography.Text type="secondary">
                {layout.playerBarWidth}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Высота карточки игрока (px)</Typography.Text>
              <Slider
                min={10}
                max={150}
                value={layout.playerBarHeight}
                onChange={value => handleChange("playerBarHeight", value)}
              />
              <Typography.Text type="secondary">
                {layout.playerBarHeight}px
              </Typography.Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Typography.Text>Размер счета (px)</Typography.Text>
              <Slider
                min={30}
                max={80}
                value={layout.scoreSize}
                onChange={value => handleChange("scoreSize", value)}
              />
              <Typography.Text type="secondary">
                {layout.scoreSize}px
              </Typography.Text>
            </div>
          </div>
        </Flex>

        <div style={{ width: "100%" }}>
          <Typography.Title level={6} style={{ marginBottom: 12 }}>
            Дополнительные настройки
          </Typography.Title>
          <Flex gap={16}>
            <div style={{ flex: 1 }}>
              <div style={{ marginBottom: 12 }}>
                <Typography.Text>Отступы (px)</Typography.Text>
                <Slider
                  min={5}
                  max={30}
                  value={layout.padding}
                  onChange={value => handleChange("padding", value)}
                />
                <Typography.Text type="secondary">
                  {layout.padding}px
                </Typography.Text>
              </div>
            </div>
            <div style={{ flex: 1 }}>
              <div style={{ marginBottom: 12 }}>
                <Typography.Text>
                  Расстояние между элементами (px)
                </Typography.Text>
                <Slider
                  min={5}
                  max={50}
                  value={layout.spacing}
                  onChange={value => handleChange("spacing", value)}
                />
                <Typography.Text type="secondary">
                  {layout.spacing}px
                </Typography.Text>
              </div>
            </div>
            <div style={{ flex: 1 }}>
              <Flex vertical gap={8}>
                <Checkbox
                  checked={layout.showHeader}
                  onChange={e => handleChange("showHeader", e.target.checked)}
                >
                  Показывать заголовок
                </Checkbox>
                <Checkbox
                  checked={layout.showFlags}
                  onChange={e => handleChange("showFlags", e.target.checked)}
                >
                  Показывать флаги
                </Checkbox>
                <Checkbox
                  checked={layout.showTags}
                  onChange={e => handleChange("showTags", e.target.checked)}
                >
                  Показывать теги
                </Checkbox>
              </Flex>
            </div>
          </Flex>
        </div>

        <iframe
          width={"1920"}
          style={{
            width: "100%",
            background: "var(--site-bg-primary)",
            border: "3px solid var(--site-border-accent)",
            borderRadius: "20px",
            display: "block",
            margin: "5vh 5px",
            boxShadow: "0 2px 8px var(--site-shadow-heavy)",
          }}
          src="/scoreboard"
        ></iframe>
      </Flex>
    </Card>
  );
};

export default LayoutCard;
