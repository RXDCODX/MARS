import type { EnvironmentVariable as EnvironmentVariableDto } from "@/shared/api/types/data-contracts";

export type EnvironmentVariableFormMode = "create" | "edit";

export interface EnvironmentVariableViewModel {
  key: string;
  value: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface EnvironmentVariableFormValues {
  key: string;
  value: string;
  description: string;
}

export interface LoadVariablesOptions {
  showToast?: boolean;
  focusKey?: string | null;
}

export const createEnvironmentVariableViewModel = (
  variable: EnvironmentVariableDto
): EnvironmentVariableViewModel => ({
  key: variable.key,
  value: variable.value ?? "",
  description: variable.description ?? "",
  createdAt: variable.createdAt,
  updatedAt: variable.updatedAt,
});
