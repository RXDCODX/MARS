import { Button, Card, Flex, Input, Typography } from "antd";
import {
  ArrowDown,
  ArrowUp,
  RotateCw as ArrowRepeat,
  Trophy as TrophyFill,
  User as PersonFill,
  XCircle as XCircleFill,
} from "lucide-react";

import { useSiteColors } from "../../../../../shared/Utils";
import FlagSelector from "../FlagCard/FlagSelector";
import {
  useColor,
  usePlayer1,
  usePlayer2,
  usePlayerActions,
} from "../store/scoreboardStore";

type PlayerCardProperties = {
  playerIndex: 1 | 2;
  label: string;
  accent: string;
};

const PlayerCard: React.FC<PlayerCardProperties> = ({
  playerIndex,
  label,
  accent,
}) => {
  const colors = useSiteColors();
  const player1 = usePlayer1();
  const player2 = usePlayer2();
  const { setPlayer1, setPlayer2 } = usePlayerActions();
  const scoreboardColors = useColor();

  const player = playerIndex === 1 ? player1 : player2;
  const setPlayer = playerIndex === 1 ? setPlayer1 : setPlayer2;

  const handleNameChange = (name: string) => {
    try {
      setPlayer({ ...player, name });
    } catch (error) {
      console.error(`Error updating player${playerIndex} name:`, error);
    }
  };

  const handleScoreChange = (score: number) => {
    try {
      setPlayer({
        ...player,
        score: Math.max(0, Math.min(99, score)),
      });
    } catch (error) {
      console.error(`Error updating player${playerIndex} score:`, error);
    }
  };

  const handleWin = () => {
    try {
      setPlayer({ ...player, final: "winner" });
    } catch (error) {
      console.error(`Error setting player${playerIndex} as winner:`, error);
    }
  };

  const handleLose = () => {
    try {
      setPlayer({ ...player, final: "loser" });
    } catch (error) {
      console.error(`Error setting player${playerIndex} as loser:`, error);
    }
  };

  const handleTagChange = (tag: string) => {
    try {
      setPlayer({ ...player, tag });
    } catch (error) {
      console.error(`Error updating player${playerIndex} tag:`, error);
    }
  };

  const handleFlagChange = (flag: string) => {
    try {
      setPlayer({ ...player, flag });
    } catch (error) {
      console.error(`Error updating player${playerIndex} flag:`, error);
    }
  };

  const handleClearFinal = () => {
    try {
      setPlayer({ ...player, final: "none" });
    } catch (error) {
      console.error(`Error clearing player${playerIndex} final status:`, error);
    }
  };

  return (
    <Card
      style={{
        minWidth: 280,
        borderRadius: 18,
        border: `2px solid ${accent}`,
        backgroundColor: colors.background.card,
        boxShadow: "var(--site-shadow-heavy)",
      }}
    >
      <Flex vertical align="center" gap={8} style={{ marginBottom: 12 }}>
        <PersonFill color={accent} size={22} />
        <Typography.Text
          strong
          style={{
            color: accent,
            letterSpacing: 1,
            textTransform: "uppercase",
            textAlign: "center",
            width: "100%",
          }}
        >
          {label}
        </Typography.Text>
      </Flex>
      <Flex align="center" gap={8} style={{ width: "100%", marginBottom: 12 }}>
        <Input
          placeholder="Tag"
          value={player.tag}
          onChange={e => handleTagChange(e.target.value)}
          size="small"
          style={{
            maxWidth: 90,
            backgroundColor: colors.background.card,
            color: scoreboardColors?.playerNamesColor || colors.text.primary,
            borderColor: scoreboardColors?.mainColor || colors.border.info,
            fontWeight: 700,
            borderRadius: 12,
          }}
        />
        <Input
          placeholder="Name"
          value={
            (player.final === "winner"
              ? "[W] "
              : player.final === "loser"
                ? "[L] "
                : "") + player.name
          }
          onChange={e => {
            const value = e.target.value.replace(/^\[W\] |^\[L\] /, "");
            handleNameChange(value);
          }}
          size="small"
          style={{
            backgroundColor: colors.background.card,
            color: scoreboardColors?.playerNamesColor || colors.text.primary,
            borderColor: scoreboardColors?.mainColor || colors.border.primary,
            fontWeight: 700,
            borderRadius: 12,
            flex: 1,
          }}
        />
      </Flex>
      <div style={{ marginBottom: 12 }}>
        <FlagSelector
          selectedFlag={player.flag}
          onFlagChange={handleFlagChange}
          placeholder="Флаг"
        />
      </div>
      <Flex
        justify="space-between"
        align="center"
        gap={8}
        style={{ marginBottom: 12 }}
      >
        <Button
          size="small"
          onClick={() => handleScoreChange(player.score + 1)}
        >
          <ArrowUp />
        </Button>
        <span
          style={{
            fontSize: "2.5rem",
            fontWeight: 700,
            color: scoreboardColors?.scoreColor || colors.text.accent,
            minWidth: 48,
            textAlign: "center",
            width: "100%",
          }}
        >
          {player.score}
        </span>
        <Button
          size="small"
          onClick={() => handleScoreChange(player.score - 1)}
        >
          <ArrowDown />
        </Button>
        <Button size="small" onClick={() => handleScoreChange(0)}>
          <ArrowRepeat />
        </Button>
      </Flex>
      <Flex gap={8} justify="center" align="center" style={{ marginTop: 8 }}>
        <Button
          size="small"
          style={{
            paddingInline: 12,
            display: "flex",
            alignItems: "center",
            gap: 4,
            backgroundColor: colors.background.success,
            borderColor: colors.border.success,
            color: colors.text.light,
          }}
          onClick={handleWin}
        >
          <TrophyFill style={{ color: colors.text.warning }} /> W
        </Button>
        <Button
          size="small"
          danger
          style={{
            paddingInline: 12,
            display: "flex",
            alignItems: "center",
            gap: 4,
            backgroundColor: colors.background.danger,
            borderColor: colors.border.danger,
            color: colors.text.light,
          }}
          onClick={handleLose}
        >
          <XCircleFill style={{ color: colors.text.light }} /> L
        </Button>
        {player.final !== "none" && (
          <Button
            size="small"
            style={{
              marginLeft: 8,
              paddingInline: 8,
              paddingBlock: 0,
              fontSize: 14,
            }}
            onClick={handleClearFinal}
            title="Убрать статус W/L"
          >
            ✕
          </Button>
        )}
      </Flex>
    </Card>
  );
};

export default PlayerCard;
