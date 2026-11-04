import { Button, Flex } from "antd";
import { ArrowLeftRight, RotateCw as ArrowRepeat } from "lucide-react";

type ActionButtonsProperties = {
  onSwapNames: () => void;
  onSwapPlayers: () => void;
  onReset: () => void;
};

const ActionButtons: React.FC<ActionButtonsProperties> = ({
  onSwapNames,
  onSwapPlayers,
  onReset,
}) => (
  <Flex vertical gap={12} style={{ width: "100%", alignItems: "center" }}>
    <Button
      type="primary"
      style={{
        width: "100%",
        fontSize: 18,
        fontWeight: 700,
        background: "#0dcaf0",
        border: "none",
        boxShadow: "0 2px 8px #0dcaf055",
      }}
      onClick={onSwapNames}
      title="Поменять имена"
    >
      <ArrowLeftRight /> Name
    </Button>
    <Button
      type="primary"
      style={{
        width: "100%",
        fontSize: 18,
        fontWeight: 700,
        background: "#0dcaf0",
        border: "none",
        boxShadow: "0 2px 8px #0dcaf055",
      }}
      onClick={onSwapPlayers}
      title="Поменять игроков местами"
    >
      <ArrowLeftRight /> All
    </Button>
    <Button
      danger
      style={{
        width: "100%",
        fontSize: 18,
        fontWeight: 700,
        background: "#dc3545",
        border: "none",
        boxShadow: "0 2px 8px #dc354555",
      }}
      onClick={onReset}
      title="Сбросить всё"
    >
      <ArrowRepeat /> Reset
    </Button>
  </Flex>
);

export default ActionButtons;
