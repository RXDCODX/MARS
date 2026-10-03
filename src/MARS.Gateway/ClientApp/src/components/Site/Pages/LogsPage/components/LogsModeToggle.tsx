import { Button } from "antd";
import { RadioTower } from "lucide-react";

interface LogsModeToggleProperties {
  isRealtime: boolean;
  onModeChange: (isRealtime: boolean) => void;
  disabled?: boolean;
}

const LogsModeToggle: React.FC<LogsModeToggleProperties> = ({
  isRealtime,
  onModeChange,
  disabled = false,
}) => (
  <div style={{ display: "flex", alignItems: "center", marginBottom: 24 }}>
    <Button
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        ...(isRealtime && {
          backgroundColor: "#52c41a",
          color: "white",
          borderColor: "#52c41a",
        }),
      }}
      onClick={() => onModeChange(true)}
      disabled={true}
      title="Реальное время (временно отключено)"
    >
      <RadioTower size={16} />
      Реальное время
    </Button>
    <Button
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        marginLeft: 8,
      }}
      type={isRealtime ? "default" : "primary"}
      onClick={() => onModeChange(false)}
      disabled={disabled}
      title="Режим запросов"
    >
      Опрос (REST)
    </Button>
  </div>
);

export default LogsModeToggle;
