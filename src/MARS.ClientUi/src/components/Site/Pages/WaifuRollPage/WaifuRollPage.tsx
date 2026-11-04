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
    // `showToast: false` здесь означал «не трогать тост» — и действительно не
    // трогал: стор на отказе при этом флаге возвращает `undefined`, а страница
    // ждала результат, чтобы показать его. То есть при отказе загрузки
    // пользователь видел пустой список без единого сообщения.
    //
    // Первичная загрузка идёт с дефолтным флагом, и отказ попадает в тост.
    void loadWaifus().then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
    void loadHusbands().then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
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
