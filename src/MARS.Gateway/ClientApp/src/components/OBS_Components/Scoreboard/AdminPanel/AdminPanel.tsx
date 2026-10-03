import { Flex } from "antd";
import { useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";

import { ScoreboardDto } from "@/shared/api";
import { useSiteColors } from "@/shared/Utils/useSiteColors";

import ThemeToggle from "../../../ThemeToggle";
import ActionButtons from "./ActionButtons";
import styles from "./AdminPanel.module.scss";
import ColorPresetCard from "./ColorCard/ColorPresetCard";
import LayoutCard from "./LayoutCard";
import MetaPanel from "./MetaPanel/MetaPanel";
import PlayerCard from "./PlayerCard/PlayerCard";
import {
  useGeneralActions,
  usePlayerActions,
  useScoreboardStore,
} from "./store/scoreboardStore";
import VisibilityCard from "./VisibilityCard/VisibilityCard";

const AdminPanelContent = () => {
  const colors = useSiteColors();
  const { swapPlayers } = usePlayerActions();
  const { handleReceiveState, reset } = useGeneralActions();

  const lastUpdateReference = useRef<number>(0);

  const handleReceiveStateCallback = useCallback(
    (state: ScoreboardDto) => {
      const now = Date.now();
      if (now - lastUpdateReference.current < 100) {
        console.log("Ignoring too frequent update");
        return;
      }
      lastUpdateReference.current = now;
      handleReceiveState(state);
    },
    [handleReceiveState]
  );

  const connection = useScoreboardStore(state => state._connection);

  useEffect(() => {
    connection.on("ReceiveState", handleReceiveStateCallback);
    connection.on("StateUpdated", handleReceiveStateCallback);

    return () => {
      connection.off("ReceiveState", handleReceiveStateCallback);
      connection.off("StateUpdated", handleReceiveStateCallback);
    };
  }, [connection, handleReceiveStateCallback]);

  const navigate = useNavigate();
  useEffect(() => {
    if (typeof window !== "undefined" && window.innerWidth < 768) {
      navigate("/scoreboard-admin");
    }
  }, [navigate]);

  const handleSwapNames = useCallback(async () => {
    try {
      await swapPlayers();
    } catch (error) {
      console.error("Error swapping names:", error);
    }
  }, [swapPlayers]);

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
    >
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <h2
          className={styles.adminTitle}
          style={colors.utils.getTextStyle("primary")}
        >
          Админ панель скорборда
        </h2>
        <ThemeToggle variant="admin" size="md" />
      </Flex>

      <Flex gap={16} style={{ marginBottom: 16 }}>
        <div style={{ flex: 1 }}>
          <VisibilityCard />
        </div>
        <div style={{ flex: 1 }}>
          <MetaPanel />
        </div>
      </Flex>

      <LayoutCard />

      <ColorPresetCard />

      <Flex
        justify="center"
        align="center"
        gap={16}
        style={{ flexWrap: "wrap" }}
      >
        <div style={{ flex: "0 0 33%", textAlign: "center", marginBottom: 12 }}>
          <PlayerCard playerIndex={1} label="Player 1" accent="#0dcaf0" />
        </div>
        <div
          style={{
            flex: "0 0 16%",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            gap: 12,
            marginBottom: 12,
          }}
        >
          <ActionButtons
            onSwapNames={handleSwapNames}
            onSwapPlayers={swapPlayers}
            onReset={reset}
          />
        </div>
        <div style={{ flex: "0 0 33%", textAlign: "center", marginBottom: 12 }}>
          <PlayerCard playerIndex={2} label="Player 2" accent="#6610f2" />
        </div>
      </Flex>
    </div>
  );
};

const AdminPanel = () => <AdminPanelContent />;

export default AdminPanel;
