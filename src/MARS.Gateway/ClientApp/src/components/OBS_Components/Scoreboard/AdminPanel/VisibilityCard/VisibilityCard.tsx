import { Button, Card, Flex, Slider, Spin, Typography } from "antd";
import { Clock, Eye, EyeOff as EyeSlash } from "lucide-react";
import { useEffect, useState } from "react";

import { useSiteColors } from "../../../../../shared/Utils/useSiteColors";
import {
  useAnimationDuration,
  useVisibility,
  useVisibilityActions,
} from "../store/scoreboardStore";

const VisibilityCard: React.FC = () => {
  const colors = useSiteColors();
  const isVisible = useVisibility();
  const animationDuration = useAnimationDuration();
  const { setVisibility, setAnimationDuration } = useVisibilityActions();

  const [isTransitioning, setIsTransitioning] = useState(false);
  const [isShowing, setIsShowing] = useState(false);
  const [pageOpenTime] = useState<number>(Date.now());
  const [lastUpdateTime, setLastUpdateTime] = useState<number>(Date.now());
  const [currentTime, setCurrentTime] = useState<number>(Date.now());

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentTime(Date.now());
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    setLastUpdateTime(Date.now());
  }, [isVisible]);

  const handleVisibilityToggle = () => {
    const isNewVisibility = !isVisible;

    if (isTransitioning) return;

    if (isNewVisibility) {
      setIsTransitioning(true);
      setIsShowing(true);
      setVisibility(isNewVisibility);
      setTimeout(() => {
        setIsTransitioning(false);
        setIsShowing(false);
      }, animationDuration);
    } else {
      setIsTransitioning(true);
      setIsShowing(false);
      setVisibility(isNewVisibility);
      setTimeout(() => {
        setIsTransitioning(false);
      }, 100);
    }
  };

  const handleAnimationDurationChange = (value: number) => {
    if (!Number.isNaN(value) && value >= 100 && value <= 99_999) {
      setAnimationDuration(value);
    }
  };

  const handleDurationIncrease = () => {
    const newValue = Math.min(animationDuration + 500, 10_000);
    setAnimationDuration(newValue);
  };

  const handleDurationDecrease = () => {
    const newValue = Math.max(animationDuration - 500, 100);
    setAnimationDuration(newValue);
  };

  const formatPageOpenTime = () => {
    const elapsed = Math.floor((currentTime - pageOpenTime) / 1000);
    const hours = Math.floor(elapsed / 3600);
    const minutes = Math.floor((elapsed % 3600) / 60);
    const seconds = elapsed % 60;
    return `${hours.toString().padStart(2, "0")}:${minutes
      .toString()
      .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
  };

  const formatLastUpdateTime = () => {
    const elapsed = Math.floor((currentTime - lastUpdateTime) / 1000);
    if (elapsed < 60) {
      return `${elapsed}с назад`;
    }
    if (elapsed < 3600) {
      const minutes = Math.floor(elapsed / 60);
      return `${minutes}м ${elapsed % 60}с назад`;
    } else {
      const hours = Math.floor(elapsed / 3600);
      const minutes = Math.floor((elapsed % 3600) / 60);
      return `${hours}ч ${minutes}м назад`;
    }
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
        {isVisible ? (
          <Eye color={colors.text.accent} size={20} />
        ) : (
          <EyeSlash color={colors.text.accent} size={20} />
        )}
        <Typography.Text
          strong
          style={{
            color: colors.text.accent,
            letterSpacing: 1,
            fontSize: 14,
            textTransform: "uppercase",
          }}
        >
          Visibility Panel
        </Typography.Text>
      </Flex>

      <Flex vertical gap={12} style={{ flex: 1 }}>
        <Button
          type={isVisible ? "primary" : "primary"}
          danger={!isVisible}
          size="large"
          block
          onClick={handleVisibilityToggle}
          disabled={isTransitioning}
          style={{
            backgroundColor: isVisible
              ? colors.background.success
              : colors.background.danger,
            borderColor: isVisible
              ? colors.border.success
              : colors.border.danger,
            color: colors.text.light,
            fontSize: 16,
            letterSpacing: 1,
            fontWeight: 700,
          }}
        >
          {isTransitioning ? (
            <Flex align="center" gap={8}>
              <Spin size="small" />
              {isShowing ? "Показываем..." : "Скрываем..."}
            </Flex>
          ) : isVisible ? (
            <Flex align="center" gap={8}>
              <Eye />
              СКРЫТЬ
            </Flex>
          ) : (
            <Flex align="center" gap={8}>
              <EyeSlash />
              ПОКАЗАТЬ
            </Flex>
          )}
        </Button>

        <Flex vertical gap={8}>
          <Flex align="center" gap={8}>
            <Clock color={colors.text.secondary} size={16} />
            <Typography.Text
              strong
              style={{ fontSize: 12, color: colors.text.secondary }}
            >
              Время анимации: {animationDuration}мс
            </Typography.Text>
          </Flex>
          <Flex align="center" gap={8}>
            <Button size="small" onClick={handleDurationDecrease}>
              -
            </Button>
            <Slider
              min={100}
              max={10_000}
              step={100}
              value={animationDuration}
              onChange={handleAnimationDurationChange}
              style={{ flex: 1 }}
            />
            <Button size="small" onClick={handleDurationIncrease}>
              +
            </Button>
          </Flex>
        </Flex>

        <Flex vertical gap={4} style={{ marginTop: "auto" }}>
          <Flex justify="space-between" align="center">
            <Typography.Text
              strong
              style={{ fontSize: 11, color: colors.text.secondary }}
            >
              Страница открыта:
            </Typography.Text>
            <Typography.Text
              strong
              style={{ fontSize: 11, color: colors.text.accent }}
            >
              {formatPageOpenTime()}
            </Typography.Text>
          </Flex>
          <Flex justify="space-between" align="center">
            <Typography.Text
              strong
              style={{ fontSize: 11, color: colors.text.secondary }}
            >
              Последнее обновление:
            </Typography.Text>
            <Typography.Text
              strong
              style={{ fontSize: 11, color: colors.text.accent }}
            >
              {formatLastUpdateTime()}
            </Typography.Text>
          </Flex>
        </Flex>
      </Flex>
    </Card>
  );
};

export default VisibilityCard;
