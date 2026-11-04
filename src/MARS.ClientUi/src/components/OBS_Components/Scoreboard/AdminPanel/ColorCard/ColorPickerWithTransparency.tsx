import { Button, Flex, Input } from "antd";
import { Eye, EyeOff as EyeSlash } from "lucide-react";
import { useState } from "react";

type ColorPickerWithTransparencyProperties = {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
};

const ColorPickerWithTransparency: React.FC<
  ColorPickerWithTransparencyProperties
> = ({ value, onChange, placeholder = "hex или rgba" }) => {
  const [isTransparent, setIsTransparent] = useState(false);

  const isTransparentColor = (color: string) => {
    if (color.startsWith("rgba")) {
      const match = color.match(/rgba\([^)]+\)/);
      if (match) {
        const parts = match[0].match(/[\d.]+/g);
        if (parts && parts.length >= 4) {
          const alpha = Number.parseFloat(parts[3]);
          return alpha === 0;
        }
      }
    }
    return false;
  };

  const setTransparent = () => {
    onChange("rgba(255, 255, 255, 0)");
    setIsTransparent(true);
  };

  const setOpaque = () => {
    if (isTransparentColor(value)) {
      onChange("#ffffff");
    }
    setIsTransparent(false);
  };

  const handleColorChange = (newValue: string) => {
    onChange(newValue);
    setIsTransparent(isTransparentColor(newValue));
  };

  const handleTextChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    handleColorChange(newValue);
  };

  const handleColorPickerChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    handleColorChange(newValue);
  };

  return (
    <Flex gap={8}>
      <input
        type="color"
        value={isTransparent ? "#ffffff" : value}
        onChange={handleColorPickerChange}
        style={{
          width: 50,
          height: 38,
          padding: 1,
          borderRadius: 8,
          border: "1px solid var(--site-border-primary)",
        }}
      />
      <Input
        value={value}
        onChange={handleTextChange}
        placeholder={placeholder}
        style={{ fontSize: 12, fontWeight: 700, borderRadius: 12 }}
      />
      <Button
        type={isTransparent ? "primary" : "default"}
        size="small"
        onClick={isTransparent ? setOpaque : setTransparent}
        style={{
          width: 38,
          height: 38,
          minWidth: 38,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
        title={isTransparent ? "Сделать непрозрачным" : "Сделать прозрачным"}
      >
        {isTransparent ? <EyeSlash size={14} /> : <Eye size={14} />}
      </Button>
    </Flex>
  );
};

export default ColorPickerWithTransparency;
