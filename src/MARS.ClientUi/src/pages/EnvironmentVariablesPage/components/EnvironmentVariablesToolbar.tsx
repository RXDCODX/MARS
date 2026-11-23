import { Button, Input, Space, Spin } from "antd";
import { Plus, RefreshCcw, Search } from "lucide-react";
import { useCallback } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "../EnvironmentVariablesPage.module.scss";
import { useEnvironmentVariablesStore } from "../store/useEnvironmentVariablesStore";

const EnvironmentVariablesToolbar: React.FC = () => {
  const { showToast } = useToastModal();
  const searchQuery = useEnvironmentVariablesStore(state => state.searchQuery);
  const sortDirection = useEnvironmentVariablesStore(
    state => state.sortDirection
  );
  const isLoading = useEnvironmentVariablesStore(state => state.isLoading);
  const isReloading = useEnvironmentVariablesStore(state => state.isReloading);
  const selectedKey = useEnvironmentVariablesStore(state => state.selectedKey);

  const setSearchQuery = useEnvironmentVariablesStore(
    state => state.setSearchQuery
  );
  const clearSearch = useEnvironmentVariablesStore(state => state.clearSearch);
  const toggleSortDirection = useEnvironmentVariablesStore(
    state => state.toggleSortDirection
  );
  const loadVariables = useEnvironmentVariablesStore(
    state => state.loadVariables
  );
  const reloadVariables = useEnvironmentVariablesStore(
    state => state.reloadVariables
  );
  const startCreate = useEnvironmentVariablesStore(state => state.startCreate);

  const handleRefresh = useCallback(async () => {
    const result = await loadVariables({
      showToast: true,
      focusKey: selectedKey,
    });
    if (result) {
      showToast(result);
    }
  }, [loadVariables, selectedKey, showToast]);

  const handleReload = useCallback(async () => {
    const result = await reloadVariables();
    if (result) {
      showToast(result);
    }
  }, [reloadVariables, showToast]);

  return (
    <div className={styles.toolbar}>
      <div className={styles.toolbarLeft}>
        <Space className={styles.searchControl}>
          <Input
            prefix={<Search size={16} />}
            placeholder="Поиск по ключу, описанию или значению..."
            value={searchQuery}
            onChange={event => setSearchQuery(event.target.value)}
            allowClear
          />
          <Button onClick={clearSearch} disabled={!searchQuery.trim()}>
            Очистить
          </Button>
        </Space>
      </div>
      <div className={styles.toolbarRight}>
        <Button onClick={toggleSortDirection}>
          Сортировка: {sortDirection === "asc" ? "A → Я" : "Я → A"}
        </Button>
        <Button
          onClick={handleRefresh}
          disabled={isLoading}
          icon={<RefreshCcw size={16} />}
        >
          Обновить список
        </Button>
        <Button
          onClick={handleReload}
          disabled={isReloading}
          icon={isReloading ? <Spin size="small" /> : <RefreshCcw size={16} />}
        >
          Перезагрузить с сервера
        </Button>
        <Button type="primary" onClick={startCreate} icon={<Plus size={16} />}>
          Новая переменная
        </Button>
      </div>
    </div>
  );
};

export default EnvironmentVariablesToolbar;
