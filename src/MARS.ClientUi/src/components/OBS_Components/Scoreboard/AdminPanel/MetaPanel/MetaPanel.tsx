import { Button, Card, Flex, Input, Typography } from "antd";
import { Info as InfoCircle } from "lucide-react";
import { useState } from "react";

import { useSiteColors } from "../../../../../shared/Utils/useSiteColors";
import { useColor, useMeta, useMetaActions } from "../store/scoreboardStore";

const MetaPanel: React.FC = () => {
  const colors = useSiteColors();
  const meta = useMeta();
  const { setMeta } = useMetaActions();
  const scoreboardColors = useColor();
  const [customFightRule, setCustomFightRule] = useState(meta.fightRule || "");

  const fightRules = ["FT1", "FT2", "FT3", "FT4", "FT5"];

  const handleFightRuleChange = (rule: string) => {
    setMeta({ fightRule: rule });
    if (fightRules.includes(rule) || rule === "None") {
      setCustomFightRule("");
    } else {
      setCustomFightRule(rule);
    }
  };

  const handleCustomFightRuleChange = (value: string) => {
    setCustomFightRule(value);
    setMeta({ fightRule: value });
  };

  return (
    <Card
      style={{
        backgroundColor: colors.background.card,
        borderRadius: 18,
        border: `2px solid ${colors.border.accent}`,
        height: "100%",
        boxShadow: "var(--site-shadow-heavy)",
      }}
    >
      <Flex
        vertical
        align="center"
        style={{ marginBottom: 12, gap: 8, textAlign: "center" }}
      >
        <InfoCircle color={colors.text.accent} size={20} />
        <Typography.Text
          strong
          style={{
            color: colors.text.accent,
            letterSpacing: 1,
            fontSize: 14,
            textTransform: "uppercase",
          }}
        >
          Meta Panel
        </Typography.Text>
      </Flex>

      <Flex vertical gap={12} style={{ flex: 1 }}>
        <div>
          <Typography.Text
            strong
            style={{
              fontSize: 12,
              color: colors.text.primary,
              display: "block",
              marginBottom: 4,
            }}
          >
            Название турнира
          </Typography.Text>
          <Input
            placeholder="Введите название турнира"
            value={meta.title}
            onChange={e => setMeta({ title: e.target.value })}
            style={{
              fontSize: 14,
              backgroundColor: colors.background.card,
              color:
                scoreboardColors?.tournamentTitleColor || colors.text.primary,
              borderColor: scoreboardColors?.mainColor || colors.border.primary,
              fontWeight: 700,
              borderRadius: 12,
            }}
          />
        </div>

        <div>
          <Typography.Text
            strong
            style={{
              fontSize: 12,
              color: colors.text.primary,
              display: "block",
              marginBottom: 4,
            }}
          >
            Режим драки
          </Typography.Text>
          <Flex gap={4} wrap="wrap" style={{ marginBottom: 8 }}>
            <Flex gap={4} style={{ width: "100%" }}>
              {fightRules.map(rule => (
                <Button
                  key={rule}
                  type={meta.fightRule === rule ? "primary" : "default"}
                  onClick={() => handleFightRuleChange(rule)}
                  style={{
                    minWidth: 50,
                    fontSize: 12,
                    fontWeight: 700,
                    background:
                      meta.fightRule === rule
                        ? colors.background.accent
                        : "transparent",
                    borderColor: colors.border.accent,
                    color:
                      meta.fightRule === rule
                        ? colors.text.light
                        : colors.text.accent,
                  }}
                >
                  {rule}
                </Button>
              ))}
            </Flex>
          </Flex>
          <Button
            type={meta.fightRule === "None" ? "default" : "default"}
            onClick={() => handleFightRuleChange("None")}
            style={{
              width: "100%",
              fontSize: 12,
              fontWeight: 700,
              background:
                meta.fightRule === "None"
                  ? colors.background.warning
                  : "transparent",
              borderColor: colors.border.warning,
              color:
                meta.fightRule === "None"
                  ? colors.text.light
                  : colors.text.warning,
              marginBottom: 8,
            }}
          >
            None
          </Button>
          <Input
            placeholder="Или введите свой режим"
            value={customFightRule}
            onChange={e => handleCustomFightRuleChange(e.target.value)}
            style={{
              fontSize: 14,
              backgroundColor: colors.background.card,
              color: scoreboardColors?.fightModeColor || colors.text.primary,
              borderColor:
                scoreboardColors?.mainColor || colors.border.secondary,
              fontWeight: 700,
              borderRadius: 12,
            }}
          />
        </div>
      </Flex>
    </Card>
  );
};

export default MetaPanel;
