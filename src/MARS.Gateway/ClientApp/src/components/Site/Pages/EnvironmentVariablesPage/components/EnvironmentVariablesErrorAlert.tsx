import { Alert } from "antd";

import { useEnvironmentVariablesStore } from "../store/useEnvironmentVariablesStore";

const EnvironmentVariablesErrorAlert: React.FC = () => {
  const error = useEnvironmentVariablesStore(state => state.error);

  if (!error) {
    return null;
  }

  return (
    <Alert
      message={error}
      type="error"
      showIcon
      style={{ marginBottom: "0.75rem" }}
    />
  );
};

export default EnvironmentVariablesErrorAlert;
