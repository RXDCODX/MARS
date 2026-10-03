import { DownOutlined } from "@ant-design/icons";
import { Button, Dropdown, Flex, Typography } from "antd";

import {
  AntdStyle,
  useAntdStyle,
} from "@/shared/components/ui/theme/AntdStyleContext";

const styleOptions: Array<{
  key: AntdStyle;
  label: string;
  icon: string;
  desc: string;
}> = [
  {
    key: "illustration",
    label: "Illustration",
    icon: "🎨",
    desc: "Толстые рамки, тени, тёплая палитра",
  },
  {
    key: "mui",
    label: "Material UI",
    icon: "💎",
    desc: "Material Design, тени, скругления 4px",
  },
  {
    key: "cartoon",
    label: "Cartoon",
    icon: "🎪",
    desc: "Скругления 18px, мягкие тона",
  },
  {
    key: "geek",
    label: "Geek",
    icon: "👾",
    desc: "Тёмный, неоновый зелёный, без скруглений",
  },
];

const StyleSelector: React.FC = () => {
  const { antdStyle, setAntdStyle } = useAntdStyle();

  const currentStyle =
    styleOptions.find(s => s.key === antdStyle) || styleOptions[0];

  const items = styleOptions.map(option => ({
    key: option.key,
    label: (
      <Flex align="center" gap={10} style={{ minWidth: 200 }}>
        <span style={{ fontSize: 18 }}>{option.icon}</span>
        <Flex vertical gap={0}>
          <Typography.Text strong style={{ fontSize: 13 }}>
            {option.label}
          </Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 11 }}>
            {option.desc}
          </Typography.Text>
        </Flex>
        {option.key === antdStyle && (
          <Typography.Text type="success" style={{ marginLeft: "auto" }}>
            ✓
          </Typography.Text>
        )}
      </Flex>
    ),
    onClick: () => setAntdStyle(option.key),
  }));

  return (
    <Dropdown menu={{ items }} trigger={["click"]} placement="bottomRight">
      <Button
        size="small"
        style={{ fontSize: 12 }}
        data-testid="button-style-selector"
      >
        {currentStyle.icon} {currentStyle.label}
        <DownOutlined style={{ fontSize: 10, marginLeft: 4 }} />
      </Button>
    </Dropdown>
  );
};

export default StyleSelector;
