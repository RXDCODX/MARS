import { useEffect } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import DeleteConfirmModal from "./components/DeleteConfirmModal";
import HusbandForm from "./components/HusbandForm";
import HusbandList from "./components/HusbandList";
import UnmergeConfirmModal from "./components/UnmergeConfirmModal";
import WaifuForm from "./components/WaifuForm";
import WaifuList from "./components/WaifuList";
import WaifuRollHeader from "./components/WaifuRollHeader";
import WaifuRollSlider from "./components/WaifuRollSlider";
import { useWaifuRollStore } from "./store/useWaifuRollStore";
import styles from "./WaifuRollPage.module.scss";

const WaifuRollPage: React.FC = () => {
  const { showToast } = useToastModal();
  const mode = useWaifuRollStore(s => s.mode);
  const loadWaifus = useWaifuRollStore(s => s.loadWaifus);
  const loadHusbands = useWaifuRollStore(s => s.loadHusbands);

  useEffect(() => {
    void loadWaifus({ showToast: false }).then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
    void loadHusbands({ showToast: false });
  }, [loadWaifus, loadHusbands, showToast]);

  return (
    <div className={styles.pageWrapper} data-testid="waifu-roll-page">
      <WaifuRollHeader />
      <WaifuRollSlider />
      {mode === "waifu" ? <WaifuList /> : <HusbandList />}
      {mode === "waifu" ? <WaifuForm /> : <HusbandForm />}
      <DeleteConfirmModal />
      <UnmergeConfirmModal />
    </div>
  );
};

export default WaifuRollPage;
