import { create } from "zustand";

import { defaultApiConfig } from "@/shared/api/api-config";
import { TwitchUsers } from "@/shared/api/http-clients/TwitchUsers";
import type { TwitchUserDto } from "@/shared/api/types/data-contracts";
import {
  createErrorResult,
  createSuccessResult,
  type OperationResult,
} from "@/shared/types/OperationResult";

import {
  createTwitchUserViewModel,
  type TwitchUserViewModel,
} from "../TwitchUsersPage.types";

export type SortField = "displayName" | "userLogin" | "role";
export type SortDirection = "asc" | "desc";

interface TwitchUsersStoreState {
  users: TwitchUserViewModel[];
  searchQuery: string;
  sortField: SortField;
  sortDirection: SortDirection;
  hideFollowers: boolean;
  isLoading: boolean;
  isDeleting: boolean;
  error: string;
  confirmDeleteId: string | null;
  confirmDeleteName: string;

  loadUsers: () => Promise<OperationResult | null>;
  setSearchQuery: (query: string) => void;
  setSortField: (field: SortField) => void;
  toggleSortDirection: () => void;
  setHideFollowers: (hide: boolean) => void;
  openDeleteConfirm: (id: string, name: string) => void;
  closeDeleteConfirm: () => void;
  deleteUser: () => Promise<OperationResult | null>;
}

const filterAndSortUsers = (
  users: TwitchUserViewModel[],
  searchQuery: string,
  sortField: SortField,
  sortDirection: SortDirection,
  hideFollowers: boolean
): TwitchUserViewModel[] => {
  let result = [...users];

  const normalizedQuery = searchQuery.trim().toLowerCase();
  if (normalizedQuery) {
    result = result.filter(
      user =>
        user.displayName.toLowerCase().includes(normalizedQuery) ||
        user.userLogin.toLowerCase().includes(normalizedQuery) ||
        user.twitchId.includes(normalizedQuery)
    );
  }

  if (hideFollowers) {
    result = result.filter(
      user =>
        user.isBroadcaster ||
        user.isModerator ||
        user.isVip ||
        user.isInBlockList
    );
  }

  const roleWeight = (u: TwitchUserViewModel): number => {
    if (u.isBroadcaster) return 4;
    if (u.isModerator) return 3;
    if (u.isVip) return 2;
    if (u.isInBlockList) return 1;
    return 0;
  };

  result.sort((a, b) => {
    let cmp: number;

    switch (sortField) {
      case "displayName": {
        cmp = a.displayName.localeCompare(b.displayName);
        break;
      }
      case "userLogin": {
        cmp = a.userLogin.localeCompare(b.userLogin);
        break;
      }
      case "role": {
        cmp = roleWeight(b) - roleWeight(a);
        break;
      }
      default: {
        cmp = 0;
      }
    }

    return sortDirection === "asc" ? cmp : -cmp;
  });

  return result;
};

export const useTwitchUsersStore = create<TwitchUsersStoreState>(
  (set, get) => ({
    users: [],
    searchQuery: "",
    sortField: "displayName",
    sortDirection: "asc",
    hideFollowers: false,
    isLoading: false,
    isDeleting: false,
    error: "",
    confirmDeleteId: null,
    confirmDeleteName: "",

    loadUsers: async () => {
      set({ isLoading: true, error: "" });
      try {
        const client = new TwitchUsers(defaultApiConfig);
        const response = await client.twitchUsersList();
        const operation = response.data as OperationResult<TwitchUserDto[]>;

        if (!operation.success) {
          set({ isLoading: false, error: operation.message ?? "" });
          return createErrorResult(operation.message);
        }

        const users = (operation.data ?? []).map(createTwitchUserViewModel);
        set({ users, isLoading: false });
        return null;
      } catch {
        set({ isLoading: false, error: "Ошибка при загрузке пользователей" });
        return createErrorResult("Ошибка при загрузке пользователей");
      }
    },

    setSearchQuery: (query: string) => {
      set({ searchQuery: query });
    },

    setSortField: (field: SortField) => {
      set({ sortField: field });
    },

    toggleSortDirection: () => {
      set(state => ({
        sortDirection: state.sortDirection === "asc" ? "desc" : "asc",
      }));
    },

    setHideFollowers: (hide: boolean) => {
      set({ hideFollowers: hide });
    },

    openDeleteConfirm: (id: string, name: string) => {
      set({ confirmDeleteId: id, confirmDeleteName: name });
    },

    closeDeleteConfirm: () => {
      set({ confirmDeleteId: null, confirmDeleteName: "" });
    },

    deleteUser: async () => {
      const id = get().confirmDeleteId;
      if (!id) {
        return createErrorResult("Не выбран пользователь для удаления");
      }

      set({ isDeleting: true });
      try {
        const client = new TwitchUsers(defaultApiConfig);
        const response = await client.twitchUsersDelete(id);
        const operation = response.data;

        if (!operation.success) {
          set({ isDeleting: false });
          return operation as OperationResult;
        }

        await get().loadUsers();
        set({
          isDeleting: false,
          confirmDeleteId: null,
          confirmDeleteName: "",
        });
        return createSuccessResult("Пользователь удален");
      } catch {
        set({ isDeleting: false });
        return createErrorResult("Ошибка при удалении пользователя");
      }
    },
  })
);

export const selectProcessedUsers = (state: TwitchUsersStoreState) =>
  filterAndSortUsers(
    state.users,
    state.searchQuery,
    state.sortField,
    state.sortDirection,
    state.hideFollowers
  );
