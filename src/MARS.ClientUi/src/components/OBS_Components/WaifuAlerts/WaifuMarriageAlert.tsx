import { WaifuAlertProps } from "@/components/OBS_Components/WaifuAlerts/helper";

import WaifuReminderAlert from "./WaifuReminderAlert";
import WaifuWeddingAlert from "./WaifuWeddingAlert";

interface Properties {
  message: WaifuAlertProps;
  onRemove: () => void;
  onMuteAll: () => void;
  onUnmuteAll: () => void;
  onSendMessage: (text: string) => void;
}

export default function WaifuMarriageAlert({
  message,
  onRemove,
  onMuteAll,
  onUnmuteAll,
  onSendMessage,
}: Properties) {
  if (message.isReminder) {
    return <WaifuReminderAlert message={message} onRemove={onRemove} />;
  }

  return (
    <WaifuWeddingAlert
      message={message}
      onRemove={onRemove}
      onMuteAll={onMuteAll}
      onUnmuteAll={onUnmuteAll}
      onSendMessage={onSendMessage}
    />
  );
}
