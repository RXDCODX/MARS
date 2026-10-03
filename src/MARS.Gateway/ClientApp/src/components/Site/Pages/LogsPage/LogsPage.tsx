import { Alert } from "antd";
import { AlertCircle } from "lucide-react";

import {
  LogsFilters,
  LogsModeToggle,
  LogsPageHeader,
  LogsStatistics as LogsStatisticsComponent,
  LogsTable,
  LogsTestButtons,
} from "./components";
import { useLogsData } from "./hooks";
import styles from "./LogsPage.module.scss";

const LogsPage: React.FC = () => {
  const {
    state,
    filters,
    isRealtime,
    updateState,
    updateFilters,
    loadLogs,
    handlePageChange,
    handlePageSizeChange,
    handleSearch,
    handleResetFilters,
    handleModeChange,
  } = useLogsData();

  return (
    <div className={styles.logsPage}>
      <div style={{ width: "100%" }}>
        <LogsPageHeader />

        {state.error && (
          <Alert
            type="error"
            className={styles.errorAlert}
            closable
            onClose={() => updateState({ error: "" })}
            message={
              <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <AlertCircle size={20} />
                {state.error}
              </span>
            }
          />
        )}

        <LogsFilters
          filters={filters}
          onFiltersChange={updateFilters}
          onSearch={handleSearch}
          onReset={handleResetFilters}
          isLoading={state.isLoading}
        />

        <LogsTestButtons onLogsRefresh={loadLogs} disabled={state.isLoading} />

        <LogsStatisticsComponent
          statistics={state.statistics}
          isLoading={state.isLoadingStats}
        />

        <LogsModeToggle
          isRealtime={isRealtime}
          onModeChange={handleModeChange}
          disabled={state.isLoading}
        />

        <LogsTable
          logs={state.logs}
          isLoading={state.isLoading}
          currentPage={state.currentPage}
          pageSize={state.pageSize}
          totalPages={state.totalPages}
          totalCount={state.totalCount}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
        />
      </div>
    </div>
  );
};

export default LogsPage;
