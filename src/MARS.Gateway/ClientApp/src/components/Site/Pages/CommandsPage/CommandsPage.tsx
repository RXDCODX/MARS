import {
  Alert,
  Badge,
  Button,
  Card,
  Checkbox,
  Col,
  Flex,
  Input,
  InputNumber,
  Row,
  Space,
  Spin,
  Tabs,
  Typography,
} from "antd";
import {
  ArrowLeft,
  CheckCircle,
  Grid3x3,
  Info,
  List,
  Play,
  Search,
  Settings,
  Shield,
  Terminal,
  User,
  XCircle,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import {
  BaseCommand,
  CommandParameterInfo,
  Commands,
  CommandsAdminPlatformInfoListParamsEnum,
  CommandsUserPlatformInfoListParamsEnum,
} from "@/shared/api";
import { defaultApiConfig } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";
import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "./CommandsPage.module.scss";

interface CommandsPageState {
  userCommands: BaseCommand[];
  adminCommands: BaseCommand[];
  selectedCommand: BaseCommand | null;
  commandParameters: CommandParameterInfo[];
  parameterValues: Record<string, string>;
  isExecuting: boolean;
  error: string;
  isLoading: boolean;
  activeTab: "user" | "admin";
  displayMode: "list" | "grid";
  searchQuery: string;
  showParameters: boolean;
}

const ListView: React.FC<{
  commands: BaseCommand[];
  selectedCommand: BaseCommand | null;
  onCommandSelect: (command: BaseCommand) => void;
  colors: ReturnType<typeof useSiteColors>;
  styles: Record<string, string>;
}> = ({ commands, selectedCommand, onCommandSelect, colors, styles }) => {
  const renderCommandCard = (command: BaseCommand) => (
    <Card
      key={command.commandName}
      className={`${styles.commandCard} ${selectedCommand?.commandName === command.commandName ? styles.selectedCommand : ""}`}
      style={{
        marginBottom: 12,
        backgroundColor:
          selectedCommand?.commandName === command.commandName
            ? colors.background.accent
            : colors.background.card,
        borderColor: colors.border.primary,
        cursor: "pointer",
        transition: "all 0.2s ease-in-out",
      }}
      styles={{ body: { padding: 16 } }}
      onClick={() => onCommandSelect(command)}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
        }}
      >
        <div style={{ flex: 1 }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              marginBottom: 8,
            }}
          >
            <Terminal size={20} style={{ color: colors.text.primary }} />
            <h6
              style={{
                marginBottom: 0,
                fontWeight: "bold",
                ...colors.utils.getTextStyle("primary"),
              }}
            >
              /{command.commandName}
            </h6>
          </div>
          <p
            style={{
              marginBottom: 8,
              color: "#8c8c8c",
              ...colors.utils.getTextStyle("secondary"),
            }}
          >
            {command.description}
          </p>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Badge
              color={command.isAdminCommand ? "red" : "blue"}
              style={{ display: "flex", alignItems: "center", gap: 4 }}
            >
              {command.isAdminCommand ? (
                <Shield size={14} />
              ) : (
                <User size={14} />
              )}
              {command.isAdminCommand ? "Админ" : "Пользователь"}
            </Badge>
            <Badge
              color="default"
              style={{ display: "flex", alignItems: "center", gap: 4 }}
            >
              <Grid3x3 size={14} />
              {command.availablePlatforms.length} платформ
            </Badge>
          </div>
        </div>
        <div style={{ marginLeft: 12 }}>
          <Button
            size="small"
            type="primary"
            ghost
            style={{ display: "flex", alignItems: "center", gap: 4 }}
          >
            <Play size={16} />
            Выполнить
          </Button>
        </div>
      </div>
    </Card>
  );

  return <div>{commands.map(renderCommandCard)}</div>;
};

const GridView: React.FC<{
  commands: BaseCommand[];
  selectedCommand: BaseCommand | null;
  onCommandSelect: (command: BaseCommand) => void;
  colors: ReturnType<typeof useSiteColors>;
  styles: Record<string, string>;
}> = ({ commands, selectedCommand, onCommandSelect, colors, styles }) => {
  const renderCommandGridCard = (command: BaseCommand) => (
    <Card
      key={command.commandName}
      className={`${styles.commandCard} ${selectedCommand?.commandName === command.commandName ? styles.selectedCommand : ""}`}
      style={{
        height: "100%",
        backgroundColor:
          selectedCommand?.commandName === command.commandName
            ? colors.background.accent
            : colors.background.card,
        borderColor: colors.border.primary,
        cursor: "pointer",
        transition: "all 0.2s ease-in-out",
      }}
      styles={{
        body: {
          padding: 16,
          display: "flex",
          flexDirection: "column",
          textAlign: "center",
        },
      }}
      onClick={() => onCommandSelect(command)}
    >
      <div style={{ marginBottom: 12 }}>
        <div
          style={{
            display: "flex",
            justifyContent: "center",
            marginBottom: 8,
          }}
        >
          <Terminal size={32} style={{ color: colors.text.primary }} />
        </div>
        <h6
          style={{
            marginBottom: 8,
            fontWeight: "bold",
            ...colors.utils.getTextStyle("primary"),
          }}
        >
          /{command.commandName}
        </h6>
        {command.isAdminCommand && (
          <Badge
            color="red"
            style={{
              marginBottom: 8,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: 4,
              margin: "0 auto 8px",
            }}
          >
            <Shield size={14} />
            Админ
          </Badge>
        )}
      </div>
      <p
        style={{
          fontSize: 12,
          color: "#8c8c8c",
          flex: 1,
          marginBottom: 12,
          ...colors.utils.getTextStyle("secondary"),
        }}
      >
        {command.description}
      </p>
      <div style={{ marginTop: "auto" }}>
        <Badge
          color="default"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            gap: 4,
            margin: "0 auto",
          }}
        >
          <Grid3x3 size={14} />
          {command.availablePlatforms.length} платформ
        </Badge>
      </div>
    </Card>
  );

  return (
    <Row gutter={[16, 16]}>
      {commands.map(command => (
        <Col key={command.commandName} xs={24} sm={12} lg={8}>
          {renderCommandGridCard(command)}
        </Col>
      ))}
    </Row>
  );
};

const CommandParameters: React.FC<{
  command: BaseCommand;
  parameters: CommandParameterInfo[];
  parameterValues: Record<string, string>;
  onParameterChange: (name: string, value: string) => void;
  onExecute: () => void;
  onCancel: () => void;
  isExecuting: boolean;
  colors: ReturnType<typeof useSiteColors>;
}> = ({
  command,
  parameters,
  parameterValues,
  onParameterChange,
  onExecute,
  onCancel,
  isExecuting,
  colors,
}) => {
  const renderParameterInput = (parameter: CommandParameterInfo) => {
    const value =
      parameterValues[parameter.name] || parameter.defaultValue || "";

    switch (parameter.type.toLowerCase()) {
      case "bool": {
        return (
          <div
            style={{
              marginTop: 8,
              display: "flex",
              alignItems: "center",
              gap: 8,
            }}
          >
            <Checkbox
              checked={value === "true"}
              onChange={e =>
                onParameterChange(parameter.name, e.target.checked.toString())
              }
            />
            <span>{parameter.description}</span>
          </div>
        );
      }
      case "int":
      case "long": {
        return (
          <InputNumber
            placeholder={parameter.description}
            value={value ? Number(value) : undefined}
            onChange={value_ =>
              onParameterChange(parameter.name, value_?.toString() ?? "")
            }
            className={styles.parameterInput}
            style={{ width: "100%" }}
          />
        );
      }
      case "double": {
        return (
          <InputNumber
            step={0.1}
            placeholder={parameter.description}
            value={value ? Number(value) : undefined}
            onChange={value_ =>
              onParameterChange(parameter.name, value_?.toString() ?? "")
            }
            className={styles.parameterInput}
            style={{ width: "100%" }}
          />
        );
      }
      default: {
        return (
          <Input
            placeholder={parameter.description}
            value={value}
            onChange={e => onParameterChange(parameter.name, e.target.value)}
            className={styles.parameterInput}
          />
        );
      }
    }
  };

  return (
    <Card
      style={{
        backgroundColor: colors.background.card,
        borderColor: colors.border.primary,
        height: "100%",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "12px 16px",
          backgroundColor: colors.background.secondary,
          borderBottom: `1px solid ${colors.border.primary}`,
        }}
      >
        <div>
          <h5
            style={{
              marginBottom: 0,
              display: "flex",
              alignItems: "center",
              gap: 8,
              ...colors.utils.getTextStyle("primary"),
            }}
          >
            <Terminal size={20} />/{command.commandName}
          </h5>
          <small style={colors.utils.getTextStyle("secondary")}>
            {command.description}
          </small>
        </div>
        <Button
          size="small"
          type="default"
          style={{ display: "flex", alignItems: "center", gap: 4 }}
          onClick={onCancel}
        >
          <ArrowLeft size={16} />
          Назад
        </Button>
      </div>
      <div style={{ padding: 16 }}>
        {parameters.length > 0 ? (
          <>
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                marginBottom: 16,
              }}
            >
              <Settings size={20} style={{ color: colors.text.primary }} />
              <h6
                style={{
                  marginBottom: 0,
                  ...colors.utils.getTextStyle("primary"),
                }}
              >
                Параметры команды
              </h6>
            </div>

            {parameters.map(parameter => (
              <div key={parameter.name} style={{ marginBottom: 16 }}>
                <label
                  style={{
                    fontWeight: 600,
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    marginBottom: 4,
                    ...colors.utils.getTextStyle("primary"),
                  }}
                >
                  {parameter.name}
                  {parameter.required && <Badge color="red" count="*" />}
                  {parameter.defaultValue && (
                    <small style={{ color: "#8c8c8c", marginLeft: 8 }}>
                      (по умолчанию: {parameter.defaultValue})
                    </small>
                  )}
                </label>
                {renderParameterInput(parameter)}
                <Typography.Text
                  type="secondary"
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 4,
                    marginTop: 4,
                  }}
                >
                  <Info size={14} />
                  {parameter.description} (тип: {parameter.type})
                </Typography.Text>
              </div>
            ))}

            <div style={{ display: "flex", gap: 12, marginTop: 16 }}>
              <Button
                type="primary"
                className={styles.executeButton}
                style={{ display: "flex", alignItems: "center", gap: 8 }}
                onClick={onExecute}
                disabled={isExecuting}
              >
                {isExecuting ? (
                  <>
                    <Spin size="small" />
                    Выполняется...
                  </>
                ) : (
                  <>
                    <Play size={18} />
                    Выполнить команду
                  </>
                )}
              </Button>

              <Button
                type="default"
                size="large"
                className={styles.cancelButton}
                onClick={onCancel}
              >
                Отменить
              </Button>
            </div>
          </>
        ) : (
          <div style={{ textAlign: "center", padding: "20px 0" }}>
            <Settings
              size={48}
              style={{ color: "#8c8c8c", marginBottom: 12 }}
            />
            <h6 style={colors.utils.getTextStyle("primary")}>
              У этой команды нет параметров
            </h6>
            <p style={{ color: "#8c8c8c", marginBottom: 16 }}>
              Команда готова к выполнению без дополнительных настроек
            </p>
            <Button
              type="primary"
              size="small"
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                margin: "0 auto",
              }}
              onClick={onExecute}
              disabled={isExecuting}
            >
              {isExecuting ? (
                <>
                  <Spin size="small" />
                  Выполняется...
                </>
              ) : (
                <>
                  <Play size={18} />
                  Выполнить команду
                </>
              )}
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
};

const CommandSearch: React.FC<{
  searchQuery: string;
  onSearchChange: (query: string) => void;
  onClear: () => void;
  resultCount: number;
  colors: ReturnType<typeof useSiteColors>;
}> = ({ searchQuery, onSearchChange, onClear, resultCount, colors }) => (
  <Card
    style={{
      marginBottom: 16,
      backgroundColor: colors.background.card,
      borderColor: colors.border.primary,
    }}
  >
    <div style={{ padding: 16 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
        <div style={{ flex: 1 }}>
          <Input
            prefix={
              <Search size={20} style={{ color: colors.text.secondary }} />
            }
            placeholder="🔍 Поиск команд по названию или описанию..."
            value={searchQuery}
            onChange={e => onSearchChange(e.target.value)}
            style={{
              backgroundColor: colors.background.card,
              borderColor: colors.border.primary,
              color: colors.text.primary,
            }}
            className={styles.parameterInput}
          />
        </div>
        {searchQuery && (
          <Button
            size="small"
            type="default"
            style={{ display: "flex", alignItems: "center", gap: 4 }}
            onClick={onClear}
            title="Очистить поиск"
          >
            <XCircle size={16} />
            Очистить
          </Button>
        )}
      </div>
      {searchQuery && (
        <div style={{ marginTop: 12 }}>
          <small
            style={{
              ...colors.utils.getTextStyle("secondary"),
              display: "flex",
              alignItems: "center",
              gap: 4,
            }}
          >
            <CheckCircle size={14} />
            Найдено: {resultCount} команд
          </small>
        </div>
      )}
    </div>
  </Card>
);

const DisplayModeToggle: React.FC<{
  displayMode: "list" | "grid";
  onModeChange: (mode: "list" | "grid") => void;
}> = ({ displayMode, onModeChange }) => (
  <Space.Compact>
    <Button
      type={displayMode === "list" ? "primary" : "default"}
      style={{ display: "flex", alignItems: "center", gap: 8 }}
      onClick={() => onModeChange("list")}
    >
      <List size={16} />
      Список
    </Button>
    <Button
      type={displayMode === "grid" ? "primary" : "default"}
      style={{ display: "flex", alignItems: "center", gap: 8 }}
      onClick={() => onModeChange("grid")}
    >
      <Grid3x3 size={16} />
      Сетка
    </Button>
  </Space.Compact>
);

const CommandsPage: React.FC = () => {
  const colors = useSiteColors();
  const { showToast } = useToastModal();
  const [commandsService] = useState(() => new Commands(defaultApiConfig));
  const parametersPanelReference = useRef<HTMLDivElement>(null);

  const [state, setState] = useState<CommandsPageState>({
    userCommands: [],
    adminCommands: [],
    selectedCommand: null,
    commandParameters: [],
    parameterValues: {},
    isExecuting: false,
    error: "",
    isLoading: true,
    activeTab: "admin",
    displayMode: "grid",
    searchQuery: "",
    showParameters: false,
  });

  const updateState = useCallback((updates: Partial<CommandsPageState>) => {
    setState(previous => ({ ...previous, ...updates }));
  }, []);

  const loadCommands = useCallback(async () => {
    try {
      updateState({ isLoading: true, error: "" });

      const resultUser = await commandsService.commandsUserPlatformInfoList(
        CommandsUserPlatformInfoListParamsEnum.Api
      );
      const adminResult = await commandsService.commandsAdminPlatformInfoList(
        CommandsAdminPlatformInfoListParamsEnum.Api
      );

      const userCommandsData = resultUser.data.data;
      const adminCommandsData = adminResult.data.data;

      updateState({
        userCommands: userCommandsData,
        adminCommands: adminCommandsData,
        isLoading: false,
      });
    } catch (error) {
      updateState({
        error: "Ошибка при загрузке команд: " + (error as Error).message,
        isLoading: false,
      });
    }
  }, [commandsService, updateState]);

  useEffect(() => {
    loadCommands();
  }, [loadCommands]);

  const filteredCommands = useMemo(() => {
    const allCommands =
      state.activeTab === "user" ? state.userCommands : state.adminCommands;

    if (!state.searchQuery) return allCommands;

    const query = state.searchQuery.toLowerCase();
    return allCommands.filter(
      command =>
        command.commandName.toLowerCase().includes(query) ||
        command.description.toLowerCase().includes(query)
    );
  }, [
    state.activeTab,
    state.userCommands,
    state.adminCommands,
    state.searchQuery,
  ]);

  const handleCommandSelect = async (command: BaseCommand) => {
    try {
      updateState({
        selectedCommand: command,
        parameterValues: {},
        error: "",
        showParameters: true,
      });

      const result = await commandsService.commandsParametersList(
        command.commandName
      );
      const parameters = result.data.data;
      updateState({ commandParameters: parameters });

      setTimeout(() => {
        if (!parametersPanelReference.current) {
          return;
        }

        const panelElement = parametersPanelReference.current;
        const panelRect = panelElement.getBoundingClientRect();
        const viewportHeight = window.innerHeight;

        if (panelRect.height > viewportHeight) {
          panelElement.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest",
          });
        } else {
          panelElement.scrollIntoView({
            behavior: "smooth",
            block: "center",
            inline: "nearest",
          });
        }
      }, 100);
    } catch (error) {
      updateState({
        error:
          "Ошибка при загрузке параметров команды: " + (error as Error).message,
      });
    }
  };

  const handleParameterChange = (parameterName: string, value: string) => {
    updateState({
      parameterValues: {
        ...state.parameterValues,
        [parameterName]: value,
      },
    });
  };

  const buildCommandInput = (): string => {
    if (!state.selectedCommand || state.commandParameters.length === 0) {
      return "";
    }

    const inputParts: string[] = [];
    state.commandParameters.forEach(parameter => {
      const value =
        state.parameterValues[parameter.name] || parameter.defaultValue || "";
      if (value) {
        inputParts.push(value);
      }
    });

    return inputParts.join(" ");
  };

  const executeCommand = async () => {
    if (!state.selectedCommand) return;

    try {
      updateState({ isExecuting: true, error: "" });

      const input = buildCommandInput();
      const result = await commandsService.commandsExecuteCreate(
        state.selectedCommand.commandName,
        input
      );

      showToast(result.data);

      updateState({ isExecuting: false });
    } catch (error) {
      const errorMessage =
        "Ошибка при выполнении команды: " + (error as Error).message;

      showToast({ success: false, message: errorMessage });

      updateState({
        error: errorMessage,
        isExecuting: false,
      });
    }
  };

  const handleCancelCommand = () => {
    updateState({
      selectedCommand: null,
      parameterValues: {},
      commandParameters: [],
      showParameters: false,
    });
  };

  if (state.isLoading) {
    return (
      <div style={{ textAlign: "center", padding: "20px 0" }}>
        <Flex vertical align="center" gap={16}>
          <Spin size="large" />
          <h4 style={colors.utils.getTextStyle("primary")}>Загрузка команд</h4>
          <p style={colors.utils.getTextStyle("secondary")}>
            Получаем список доступных команд...
          </p>
        </Flex>
      </div>
    );
  }

  const currentCommands = filteredCommands;

  const tabItems = [
    {
      key: "user",
      label: (
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <User size={16} />
          Пользовательские ({state.userCommands.length})
        </div>
      ),
    },
    {
      key: "admin",
      label: (
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <Shield size={16} />
          Админские ({state.adminCommands.length})
        </div>
      ),
    },
  ];

  return (
    <div className="page-container">
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 16,
        }}
      >
        <div>
          <h1
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              marginBottom: 8,
              ...colors.utils.getTextStyle("primary"),
            }}
          >
            <Terminal size={32} />
            Выполнение команд
          </h1>
          <p style={{ color: "#8c8c8c", marginBottom: 0 }}>
            Управляйте системой MARS через командный интерфейс
          </p>
        </div>

        <DisplayModeToggle
          displayMode={state.displayMode}
          onModeChange={mode => updateState({ displayMode: mode })}
        />
      </div>

      {state.error && (
        <Alert
          type="error"
          message={
            <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <XCircle size={20} />
              {state.error}
            </span>
          }
          style={{ marginBottom: 16 }}
        />
      )}

      <Row gutter={16}>
        <Col lg={state.showParameters ? 16 : 24}>
          <CommandSearch
            searchQuery={state.searchQuery}
            onSearchChange={query => updateState({ searchQuery: query })}
            onClear={() => updateState({ searchQuery: "" })}
            resultCount={currentCommands.length}
            colors={colors}
          />

          <Card
            style={{
              backgroundColor: colors.background.card,
              borderColor: colors.border.primary,
            }}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                padding: "12px 16px",
                backgroundColor: colors.background.secondary,
                borderBottom: `1px solid ${colors.border.primary}`,
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                }}
              >
                <span
                  style={{
                    fontWeight: 600,
                    ...colors.utils.getTextStyle("primary"),
                  }}
                >
                  Доступные команды
                </span>
                <Badge color="default" count={currentCommands.length} />
              </div>
            </div>
            <div style={{ padding: 16 }}>
              <div style={{ marginBottom: 16 }}>
                <Tabs
                  activeKey={state.activeTab}
                  onChange={key =>
                    updateState({ activeTab: key as "user" | "admin" })
                  }
                  className={styles.tabGroup}
                  items={tabItems}
                />
              </div>

              {currentCommands.length > 0 ? (
                state.displayMode === "list" ? (
                  <ListView
                    commands={currentCommands}
                    selectedCommand={state.selectedCommand}
                    onCommandSelect={handleCommandSelect}
                    colors={colors}
                    styles={styles}
                  />
                ) : (
                  <GridView
                    commands={currentCommands}
                    selectedCommand={state.selectedCommand}
                    onCommandSelect={handleCommandSelect}
                    colors={colors}
                    styles={styles}
                  />
                )
              ) : (
                <div style={{ textAlign: "center", padding: "20px 0" }}>
                  <div className={styles.emptyState}>
                    <Terminal
                      size={64}
                      style={{ color: "#8c8c8c", marginBottom: 12 }}
                    />
                    <h5 style={colors.utils.getTextStyle("primary")}>
                      {state.searchQuery
                        ? "Команды не найдены"
                        : "Нет доступных команд"}
                    </h5>
                    <p style={colors.utils.getTextStyle("secondary")}>
                      {state.searchQuery ? (
                        <>
                          По запросу <strong>"{state.searchQuery}"</strong>{" "}
                          ничего не найдено.
                          <br />
                          Попробуйте изменить поисковый запрос.
                        </>
                      ) : (
                        `В данный момент нет ${state.activeTab === "user" ? "пользовательских" : "админских"} команд для выполнения.`
                      )}
                    </p>
                  </div>
                </div>
              )}
            </div>
          </Card>
        </Col>

        {state.showParameters && state.selectedCommand && (
          <Col lg={8}>
            <div
              ref={parametersPanelReference}
              style={{
                position: "sticky",
                top: 87,
              }}
            >
              <CommandParameters
                command={state.selectedCommand}
                parameters={state.commandParameters}
                parameterValues={state.parameterValues}
                onParameterChange={handleParameterChange}
                onExecute={executeCommand}
                onCancel={handleCancelCommand}
                isExecuting={state.isExecuting}
                colors={colors}
              />
            </div>
          </Col>
        )}
      </Row>
    </div>
  );
};

export default CommandsPage;
