import { useEffect } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import EnvironmentVariableDeleteModal from "./components/EnvironmentVariableDeleteModal";
import EnvironmentVariableForm from "./components/EnvironmentVariableForm";
import EnvironmentVariablesErrorAlert from "./components/EnvironmentVariablesErrorAlert";
import EnvironmentVariablesHeader from "./components/EnvironmentVariablesHeader";
import EnvironmentVariablesTable from "./components/EnvironmentVariablesTable";
import EnvironmentVariablesToolbar from "./components/EnvironmentVariablesToolbar";
import styles from "./EnvironmentVariablesPage.module.scss";
import { useEnvironmentVariablesStore } from "./store/useEnvironmentVariablesStore";

const EnvironmentVariablesPage: React.FC = () => {
  const { showToast } = useToastModal();
  const loadVariables = useEnvironmentVariablesStore(
    state => state.loadVariables
  );
  useEffect(() => {
    void loadVariables({ showToast: false }).then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
  }, [loadVariables, showToast]);

  return (
    <div className={styles.pageWrapper}>
      <EnvironmentVariablesHeader />
      <EnvironmentVariablesToolbar />
      <EnvironmentVariablesErrorAlert />
      <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
        <div style={{ flex: "0 0 100%", maxWidth: "100%" }}>
          <EnvironmentVariablesTable />
        </div>
      </div>
      <EnvironmentVariableForm />
      <EnvironmentVariableDeleteModal />
    </div>
  );
};

export default EnvironmentVariablesPage;
