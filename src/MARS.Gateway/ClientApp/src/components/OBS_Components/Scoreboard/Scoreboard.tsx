import { AnimatePresence, motion } from "framer-motion";
import { type CSSProperties } from "react";

import InjectStyles from "@/shared/components/InjectStyles";

import commonStyles from "../OBSCommon.module.scss";
import { useScoreboardStore } from "./AdminPanel";
import styles from "./Scoreboard.module.scss";

const ScoreboardContent: React.FC = () => {
  // Функция для проверки валидности тега
  const isValidTag = (tag: string): boolean => {
    if (!tag || tag.trim() === "") return false;
    const hasLetter = /[a-zA-Zа-яА-Я]/.test(tag);
    return hasLetter;
  };

  // Функция для получения пути к флагу
  const getFlagPath = (countryCode: string): string => {
    if (!countryCode) return "";
    return `/flags/${countryCode.toLowerCase()}.svg`;
  };

  // Состояние берётся из стора, а не держится локально.
  //
  // Раньше компонент подписывался на соединение сам и складывал ответы в
  // собственные useState, то есть состояние табло жило в двух местах: в сторе и
  // здесь. Стор при этом обновлялся отдельно, и правка одного поля в панели
  // администратора доезжала до экрана через две независимые подписки — одна из
  // них молча переставала работать, и на табло оставалось старое значение.
  const player1 = useScoreboardStore(state => state.player1);
  const player2 = useScoreboardStore(state => state.player2);
  const meta = useScoreboardStore(state => state.meta);
  const colors = useScoreboardStore(state => state.color);
  const isVisible = useScoreboardStore(state => state.isVisible);
  const animationDuration = useScoreboardStore(
    state => state.animationDuration
  );
  const layout = useScoreboardStore(state => state.layout);

  // Признак «сервер уже прислал состояние». Отличать его от значений по
  // умолчанию нужно, чтобы не мигать демонстрационным табло до первого ответа.
  const hasReceivedInitialState = useScoreboardStore(
    state => state.hasReceivedInitialState
  );

  // В Production окружении не показываем скорборд до первого получения данных
  if (import.meta.env.PROD && !hasReceivedInitialState) {
    return null;
  }

  if (!isVisible) {
    return null;
  }

  // Функция для проверки, нужно ли отображать режим боя
  const shouldShowFightMode = () =>
    meta.fightRule &&
    meta.fightRule.trim() !== "" &&
    meta.fightRule.toLowerCase() !== "none" &&
    meta.fightRule.toLowerCase() !== "n/a";

  // Анимационные варианты
  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        duration: animationDuration / 1000,
        staggerChildren: 0.1,
        delayChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: animationDuration / 1000 },
    },
  };

  const headerVariants = {
    hidden: { opacity: 0, y: -20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: animationDuration / 1000 },
      transform: "translateX(-50%)",
    },
  };

  // Dynamic border calculations based on header height
  const baseHeaderHeight = 60; // reference height matching SCSS defaults
  const effectiveHeaderHeight = layout?.headerHeight ?? baseHeaderHeight;
  const delta = baseHeaderHeight - effectiveHeaderHeight;

  // Derived values from provided samples:
  // top: -2px @60 -> -11px @20  => slope ≈ 0.225 per px
  // height: 130% @60 -> 211% @20 => slope ≈ 2.025% per px
  // left rotate: 148deg @60 -> 131deg @20 => slope ≈ 0.425deg per px (decreasing)
  // right rotate: 32deg @60 -> 49deg @20 => slope ≈ 0.425deg per px (increasing)
  const borderTopPx = -2 - 0.225 * delta;
  const borderHeightPercent = 130 + 2.025 * delta;
  const leftRotateDeg = 148 - 0.425 * delta;
  const rightRotateDeg = 32 + 0.425 * delta;

  const leftBorderStyle: CSSProperties = {
    borderColor: colors.borderColor,
    top: `${borderTopPx}px`,
    height: `${borderHeightPercent}%`,
    transform: `rotate(${leftRotateDeg}deg)`,
  };

  const rightBorderStyle: CSSProperties = {
    borderColor: colors.borderColor,
    top: `${borderTopPx}px`,
    height: `${borderHeightPercent}%`,
    transform: `rotate(${rightRotateDeg}deg)`,
  };

  return (
    <AnimatePresence>
      <InjectStyles
        styles={`
          :root {
            --banner-skew: 10px;
          }
        `}
        id="scoreboard-styles"
      />
      {isVisible && (
        <motion.div
          className={styles.scoreboardContainer}
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          exit="hidden"
          data-testid="obs-scoreboard"
        >
          {/* Заголовок турнира с режимом боя */}
          {layout.showHeader && (
            <motion.div
              className={styles.tournamentHeader}
              variants={headerVariants}
              style={{
                position: "absolute",
                top: `${layout.headerTop}px`,
                left: `${layout.headerLeft}%`,
                transform: "translateX(-50%)",
                width: `${layout.headerWidth}px`,
                height: `${layout.headerHeight}px`,
                backgroundColor: colors.backgroundColor,
                borderColor: colors.borderColor || colors.mainColor,
                padding: `${layout.padding}px`,
              }}
              data-testid="scoreboard-tournament-header"
            >
              <div
                className={styles.headerLeftBorder}
                style={leftBorderStyle}
              ></div>
              <h1
                className={commonStyles.textStrokeShadow}
                style={{ color: colors.tournamentTitleColor }}
                data-testid="text-tournament-title"
              >
                {meta.title}
              </h1>
              {shouldShowFightMode() && (
                <div
                  className={`${styles.fightMode} ${commonStyles.textStrokeShadow}`}
                  style={{ color: colors.fightModeColor }}
                  data-testid="text-fight-mode"
                >
                  {meta.fightRule}
                </div>
              )}
              <div
                className={styles.headerRightBorder}
                style={rightBorderStyle}
              ></div>
            </motion.div>
          )}

          {/* Контейнер игроков */}
          <motion.div
            className={styles.playersContainer}
            variants={itemVariants}
            style={{
              position: "absolute",
              top: `${layout.playersTop}px`,
              left: `${layout.playersLeft}px`,
              right: `${layout.playersRight}px`,
              gap: `${layout.spacing}px`,
            }}
            data-testid="scoreboard-players"
          >
            {/* Левый игрок */}
            <motion.div
              className={styles.playerLeft}
              style={{
                width: `${layout.playerBarWidth}px`,
                height: `${layout.playerBarHeight}px`,
                backgroundColor: colors.backgroundColor,
                borderColor: colors.borderColor || colors.mainColor,
                padding: `${layout.padding}px`,
              }}
              data-testid="scoreboard-player-1"
            >
              {layout.showFlags && player1.flag && player1.flag !== "none" && (
                <div
                  className={styles.flag}
                  style={{
                    height: "auto",
                  }}
                >
                  <img
                    src={getFlagPath(player1.flag)}
                    alt="Player 1 flag"
                    onError={e => {
                      e.currentTarget.style.display = "none";
                    }}
                    data-testid="img-player-1-flag"
                  />
                </div>
              )}

              {/* Счет левого игрока */}
              <div
                className={styles.score}
                style={{
                  width: layout.scoreSize,
                  backgroundColor: colors.borderColor,
                  height: layout.playerBarHeight,
                }}
              >
                <h3
                  className={commonStyles.textStrokeShadow}
                  style={{ color: colors.scoreColor }}
                  data-testid="text-player-1-score"
                >
                  {player1.score}
                </h3>
              </div>
              <div className={styles.playerInfo}>
                <h2
                  className={commonStyles.textStrokeShadow}
                  style={{ color: colors.playerNamesColor }}
                  data-testid="text-player-1-name"
                >
                  {player1.final === "winner" && "[W] "}
                  {player1.final === "loser" && "[L] "}
                  {layout.showTags && isValidTag(player1.tag) && (
                    <span
                      className={styles.playerTag}
                      style={{ color: colors.mainColor }}
                    >
                      {player1.tag}
                    </span>
                  )}
                  {layout.showTags && isValidTag(player1.tag) && " | "}
                  {player1.name}
                </h2>
              </div>
            </motion.div>

            {/* Правый игрок */}
            <motion.div
              className={styles.playerRight}
              style={{
                width: `${layout.playerBarWidth}px`,
                height: `${layout.playerBarHeight}px`,
                backgroundColor: colors.backgroundColor,
                borderColor: colors.borderColor || colors.mainColor,
                padding: `${layout.padding}px`,
              }}
              data-testid="scoreboard-player-2"
            >
              {layout.showFlags && player2.flag && player2.flag !== "none" && (
                <div
                  className={styles.flag}
                  style={{
                    height: `auto`,
                  }}
                >
                  <img
                    src={getFlagPath(player2.flag)}
                    alt="Player 2 flag"
                    onError={e => {
                      e.currentTarget.style.display = "none";
                    }}
                    data-testid="img-player-2-flag"
                  />
                </div>
              )}

              <div className={styles.playerInfo}>
                <h2
                  className={commonStyles.textStrokeShadow}
                  style={{ color: colors.playerNamesColor }}
                  data-testid="text-player-2-name"
                >
                  {player2.final === "winner" && "[W] "}
                  {player2.final === "loser" && "[L] "}
                  {player2.name}
                  {layout.showTags && isValidTag(player2.tag) && " | "}
                  {layout.showTags && isValidTag(player2.tag) && (
                    <span
                      className={styles.playerTag}
                      style={{ color: colors.mainColor }}
                    >
                      {player2.tag}
                    </span>
                  )}
                </h2>
              </div>

              {/* Счет правого игрока */}
              <div
                className={styles.score}
                style={{
                  width: layout.scoreSize,
                  backgroundColor: colors.borderColor,
                  height: layout.playerBarHeight,
                }}
              >
                <h3
                  className={commonStyles.textStrokeShadow}
                  style={{ color: colors.scoreColor }}
                  data-testid="text-player-2-score"
                >
                  {player2.score}
                </h3>
              </div>
            </motion.div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};

const Scoreboard: React.FC = () => <ScoreboardContent />;

export default Scoreboard;
