import {
  BaseCommand,
  BaseCommandAvailablePlatformsEnum,
  CommandParameterInfo,
} from "@/shared/api";

// Состояние выполнения команды
export interface CommandExecutionState {
  isExecuting: boolean;
  result: string;
  error: string;
}

// Состояние параметров
export interface ParameterState {
  [parameterName: string]: string;
}

// Основное состояние страницы команд
export interface CommandsPageState {
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

// Тип платформы
export type PlatformType = BaseCommandAvailablePlatformsEnum;

// Пропсы для компонента ListView
export interface ListViewProps {
  commands: BaseCommand[];
  selectedCommand: BaseCommand | null;
  onCommandSelect: (command: BaseCommand) => void;
  colors: any; // ReturnType<typeof useSiteColors>
  styles: Record<string, string>;
}

// Пропсы для компонента GridView
export interface GridViewProps {
  commands: BaseCommand[];
  selectedCommand: BaseCommand | null;
  onCommandSelect: (command: BaseCommand) => void;
  colors: any; // ReturnType<typeof useSiteColors>
  styles: Record<string, string>;
}

// Пропсы для компонента CommandParameters
export interface CommandParametersProps {
  command: BaseCommand;
  parameters: CommandParameterInfo[];
  parameterValues: Record<string, string>;
  onParameterChange: (name: string, value: string) => void;
  onExecute: () => void;
  onCancel: () => void;
  isExecuting: boolean;
  colors: any; // ReturnType<typeof useSiteColors>
}

// Пропсы для компонента CommandSearch
export interface CommandSearchProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  onClear: () => void;
  resultCount: number;
  colors: any; // ReturnType<typeof useSiteColors>
}

// Пропсы для компонента DisplayModeToggle
export interface DisplayModeToggleProps {
  displayMode: "list" | "grid";
  onModeChange: (mode: "list" | "grid") => void;
}

// Типы для событий
export type CommandSelectHandler = (command: BaseCommand) => void;
export type ParameterChangeHandler = (name: string, value: string) => void;
export type DisplayModeChangeHandler = (mode: "list" | "grid") => void;
export type SearchChangeHandler = (query: string) => void;

// Интерфейс для результатов поиска
export interface SearchResult {
  commands: BaseCommand[];
  totalCount: number;
  query: string;
}

// Интерфейс для статистики команд
export interface CommandsStats {
  totalCommands: number;
  userCommands: number;
  adminCommands: number;
  availablePlatforms: PlatformType[];
}
