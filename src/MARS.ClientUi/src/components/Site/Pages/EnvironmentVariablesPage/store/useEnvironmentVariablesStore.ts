import { create } from "zustand";

import { defaultApiConfig } from "@/shared/api/api-config";
import { EnvironmentVariable } from "@/shared/api/http-clients/EnvironmentVariable";
import type {
  EnvironmentVariable as EnvironmentVariableDto,
  SetEnvironmentVariableRequest,
} from "@/shared/api/types/data-contracts";
import {
  createErrorResult,
  createSuccessResult,
  type OperationResult,
} from "@/shared/types/OperationResult";

import {
  createEnvironmentVariableViewModel,
  EnvironmentVariableFormMode,
  EnvironmentVariableFormValues,
  EnvironmentVariableViewModel,
  LoadVariablesOptions,
} from "../EnvironmentVariablesPage.types";

const defaultFormValues: EnvironmentVariableFormValues = {
  key: "",
  value: "",
  description: "",
};

const filterVariables = (
  variables: EnvironmentVariableViewModel[],
  searchQuery: string,
  sortDirection: "asc" | "desc"
) => {
  const normalizedQuery = searchQuery.trim().toLowerCase();
  const sorted = [...variables].sort((a, b) =>
    sortDirection === "asc"
      ? a.key.localeCompare(b.key)
      : b.key.localeCompare(a.key)
  );

  if (!normalizedQuery) {
    return sorted;
  }

  return sorted.filter(variable => {
    const keyMatch = variable.key.toLowerCase().includes(normalizedQuery);
    const descriptionMatch = variable.description
      .toLowerCase()
      .includes(normalizedQuery);
    const valueMatch = variable.value.toLowerCase().includes(normalizedQuery);

    return keyMatch || descriptionMatch || valueMatch;
  });
};

interface EnvironmentVariablesStoreState {
  variables: EnvironmentVariableViewModel[];
  filteredVariables: EnvironmentVariableViewModel[];
  searchQuery: string;
  sortDirection: "asc" | "desc";
  selectedKey: string | null;
  showForm: boolean;
  formMode: EnvironmentVariableFormMode;
  formValues: EnvironmentVariableFormValues;
  isLoading: boolean;
  isSubmitting: boolean;
  isReloading: boolean;
  isDeleting: boolean;
  error: string;
  confirmDeleteKey: string | null;

  loadVariables: (
    options?: LoadVariablesOptions
  ) => Promise<OperationResult | null>;
  reloadVariables: () => Promise<OperationResult | null>;
  selectVariable: (key: string) => void;
  startCreate: () => void;
  setSearchQuery: (query: string) => void;
  clearSearch: () => void;
  toggleSortDirection: () => void;
  updateFormField: (
    field: keyof EnvironmentVariableFormValues,
    value: string
  ) => void;
  cancelForm: () => void;
  submitForm: () => Promise<OperationResult | null>;
  openDeleteConfirm: () => void;
  closeDeleteConfirm: () => void;
  deleteVariable: () => Promise<OperationResult | null>;
}

const findLastUpdated = (variables: EnvironmentVariableViewModel[]) =>
  variables
    .map(variable => variable.updatedAt)
    .sort((a, b) => (a > b ? -1 : 1))[0] ?? "";

const mapVariables = (data?: EnvironmentVariableDto[]) => {
  if (!Array.isArray(data) || data.length === 0) {
    return [];
  }
  return data.map(e => createEnvironmentVariableViewModel(e));
};

export const useEnvironmentVariablesStore =
  create<EnvironmentVariablesStoreState>((set, get) => ({
    variables: [],
    filteredVariables: [],
    searchQuery: "",
    sortDirection: "asc",
    selectedKey: null,
    showForm: false,
    formMode: "create",
    formValues: defaultFormValues,
    isLoading: true,
    isSubmitting: false,
    isReloading: false,
    isDeleting: false,
    error: "",
    confirmDeleteKey: null,

    loadVariables: async (options?: LoadVariablesOptions) => {
      const { showToast = false, focusKey } = options ?? {};

      set(state => ({
        ...state,
        isLoading: true,
        error: "",
      }));

      try {
        const client = new EnvironmentVariable(defaultApiConfig);
        const response = await client.environmentVariableList();
        const operation = response.data as OperationResult<
          EnvironmentVariableDto[] | undefined
        >;

        if (!operation.success) {
          const message =
            operation.message ?? "Не удалось загрузить переменные окружения";
          set(state => ({
            ...state,
            isLoading: false,
            error: message,
            variables: [],
            filteredVariables: [],
            selectedKey: null,
            showForm: false,
            formMode: "create",
            formValues: defaultFormValues,
          }));
          return showToast ? operation : createErrorResult(message);
        }

        const variables = mapVariables(operation.data);

        set(state => {
          const nextSelectedKey = focusKey ?? state.selectedKey ?? null;
          const selectedExists =
            nextSelectedKey !== null &&
            variables.some(variable => variable.key === nextSelectedKey);

          const resolvedSelectedKey = selectedExists ? nextSelectedKey : null;

          let formValues = state.formValues;
          let formMode = state.formMode;
          let showForm = state.showForm;

          if (resolvedSelectedKey && state.showForm) {
            const selected = variables.find(
              variable => variable.key === resolvedSelectedKey
            );
            if (selected) {
              formMode = "edit";
              formValues = {
                key: selected.key,
                value: selected.value,
                description: selected.description,
              };
            }
          }

          if (state.showForm && formMode === "edit" && !resolvedSelectedKey) {
            showForm = false;
            formMode = "create";
            formValues = defaultFormValues;
          }

          const filteredVariables = filterVariables(
            variables,
            state.searchQuery,
            state.sortDirection
          );

          return {
            ...state,
            variables,
            filteredVariables,
            isLoading: false,
            error: "",
            selectedKey: resolvedSelectedKey,
            showForm,
            formMode,
            formValues,
          };
        });

        return showToast ? operation : null;
      } catch (error) {
        const message =
          "Ошибка при загрузке переменных окружения: " +
          (error as Error).message;
        set(state => ({
          ...state,
          isLoading: false,
          error: message,
          variables: [],
          filteredVariables: [],
          selectedKey: null,
          showForm: false,
          formMode: "create",
          formValues: defaultFormValues,
        }));
        return createErrorResult(message);
      }
    },

    reloadVariables: async () => {
      set(state => ({
        ...state,
        isReloading: true,
      }));

      try {
        const client = new EnvironmentVariable(defaultApiConfig);
        const response = await client.environmentVariableReloadCreate();
        const operation = response.data;

        if (!operation.success) {
          return operation;
        }

        await get().loadVariables({
          showToast: false,
          focusKey: get().selectedKey,
        });

        return operation;
      } catch (error) {
        return createErrorResult(
          "Ошибка при перезагрузке переменных: " + (error as Error).message
        );
      } finally {
        set(state => ({
          ...state,
          isReloading: false,
        }));
      }
    },

    selectVariable: key => {
      set(state => {
        const selected = state.variables.find(variable => variable.key === key);
        if (!selected) {
          return state;
        }

        return {
          ...state,
          selectedKey: key,
          showForm: true,
          formMode: "edit" as EnvironmentVariableFormMode,
          formValues: {
            key: selected.key,
            value: selected.value,
            description: selected.description,
          },
        };
      });
    },

    startCreate: () => {
      set(state => ({
        ...state,
        selectedKey: null,
        showForm: true,
        formMode: "create",
        formValues: defaultFormValues,
      }));
    },

    setSearchQuery: query => {
      set(state => ({
        ...state,
        searchQuery: query,
        filteredVariables: filterVariables(
          state.variables,
          query,
          state.sortDirection
        ),
      }));
    },

    clearSearch: () => {
      set(state => ({
        ...state,
        searchQuery: "",
        filteredVariables: filterVariables(
          state.variables,
          "",
          state.sortDirection
        ),
      }));
    },

    toggleSortDirection: () => {
      set(state => {
        const nextDirection = state.sortDirection === "asc" ? "desc" : "asc";
        return {
          ...state,
          sortDirection: nextDirection,
          filteredVariables: filterVariables(
            state.variables,
            state.searchQuery,
            nextDirection
          ),
        };
      });
    },

    updateFormField: (field, value) => {
      set(state => ({
        ...state,
        formValues: {
          ...state.formValues,
          [field]: value,
        },
      }));
    },

    cancelForm: () => {
      set(state => ({
        ...state,
        showForm: false,
        selectedKey: null,
        formMode: "create",
        formValues: defaultFormValues,
      }));
    },

    submitForm: async () => {
      const { formValues, formMode } = get();
      const trimmedKey = formValues.key.trim();

      if (!trimmedKey) {
        return createErrorResult("Ключ переменной не может быть пустым");
      }

      const payload: SetEnvironmentVariableRequest = {
        key: trimmedKey,
        value: formValues.value,
        description: formValues.description.trim()
          ? formValues.description.trim()
          : undefined,
      };

      set(state => ({
        ...state,
        isSubmitting: true,
      }));

      try {
        const client = new EnvironmentVariable(defaultApiConfig);
        const response = await client.environmentVariableCreate(payload);
        const operation = response.data;

        if (!operation.success) {
          return operation;
        }

        await get().loadVariables({
          showToast: false,
          focusKey: payload.key,
        });

        return createSuccessResult(
          formMode === "create"
            ? "Переменная окружения создана"
            : "Переменная окружения обновлена"
        );
      } catch (error) {
        return createErrorResult(
          "Ошибка при сохранении переменной: " + (error as Error).message
        );
      } finally {
        set(state => ({
          ...state,
          isSubmitting: false,
        }));
      }
    },

    openDeleteConfirm: () => {
      set(state => ({
        ...state,
        confirmDeleteKey: state.selectedKey,
      }));
    },

    closeDeleteConfirm: () => {
      set(state => ({
        ...state,
        confirmDeleteKey: null,
      }));
    },

    deleteVariable: async () => {
      const key = get().confirmDeleteKey;
      if (!key) {
        return createErrorResult("Не удалось определить ключ для удаления");
      }

      set(state => ({
        ...state,
        isDeleting: true,
      }));

      try {
        const client = new EnvironmentVariable(defaultApiConfig);
        const response = await client.environmentVariableDelete(key);
        const operation = response.data;

        if (!operation.success) {
          return operation;
        }

        await get().loadVariables({
          showToast: false,
          focusKey: null,
        });

        set(state => ({
          ...state,
          showForm: false,
          selectedKey: null,
          formMode: "create",
          formValues: defaultFormValues,
        }));

        return createSuccessResult("Переменная окружения удалена");
      } catch (error) {
        return createErrorResult(
          "Ошибка при удалении переменной: " + (error as Error).message
        );
      } finally {
        set(state => ({
          ...state,
          isDeleting: false,
          confirmDeleteKey: null,
        }));
      }
    },
  }));

export const selectFilteredVariables = (
  state: EnvironmentVariablesStoreState
) => state.filteredVariables;

export const selectTotalVariables = (state: EnvironmentVariablesStoreState) =>
  state.variables.length;

export const selectLastUpdated = (state: EnvironmentVariablesStoreState) =>
  findLastUpdated(state.variables);
