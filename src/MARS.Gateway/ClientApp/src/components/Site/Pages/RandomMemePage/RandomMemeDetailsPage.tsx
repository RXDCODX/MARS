import { Alert, Button, Spin } from "antd";
import { ArrowLeft } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { RandomMeme } from "@/shared/api";
import { MemeOrderDto } from "@/shared/api";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { RandomMemeDetails } from "./components";
import styles from "./RandomMemePage.module.scss";

const RandomMemeDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const api = useMemo(() => new RandomMeme(), []);
  const { showToast } = useToastModal();

  const [memeOrder, setMemeOrder] = useState<MemeOrderDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>("");

  const loadMemeDetails = useCallback(async () => {
    if (!id) {
      setError("ID мема не указан");
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setError("");

      const response = await api.randomMemeOrdersDetail(id);
      setMemeOrder(response.data.data ?? null);

      if (!response.data.data) {
        setError("Мем с указанным ID не найден");
      }
    } catch (error_) {
      console.error("Ошибка загрузки деталей мема:", error_);
      const errorMessage =
        error_ instanceof Error ? error_.message : "Неизвестная ошибка";
      setError(errorMessage);
      showToast({
        success: false,
        message: errorMessage,
      });
    } finally {
      setIsLoading(false);
    }
  }, [id, api, showToast]);

  useEffect(() => {
    loadMemeDetails();
  }, [loadMemeDetails]);

  const handleBack = useCallback(() => {
    navigate("/random-meme");
  }, [navigate]);

  const handleEdit = useCallback(() => {
    if (memeOrder) {
      navigate(`/random-meme/edit/${memeOrder.id}`);
    }
  }, [navigate, memeOrder]);

  const handleDelete = useCallback(async () => {
    if (!memeOrder) return;

    try {
      const result = await api.randomMemeOrdersDelete(memeOrder.id);
      showToast(result.data);
      navigate("/random-meme");
    } catch (error_) {
      console.error("Ошибка удаления заказа:", error_);
      showToast({
        success: false,
        message: "Ошибка удаления заказа мема",
      });
    }
  }, [memeOrder, api, showToast, navigate]);

  const handleRefresh = useCallback(() => {
    loadMemeDetails();
  }, [loadMemeDetails]);

  if (isLoading) {
    return (
      <div className={styles.randomMemePage}>
        <div style={{ textAlign: "center", padding: "40px 0" }}>
          <Spin size="large" />
          <h4 style={{ marginTop: 12 }}>Загрузка деталей мема...</h4>
          <p style={{ color: "#8c8c8c" }}>Получаем информацию о меме</p>
        </div>
      </div>
    );
  }

  if (error || !memeOrder) {
    return (
      <div className={styles.randomMemePage}>
        <Alert
          type="error"
          showIcon
          message="Ошибка загрузки"
          description={
            <div>
              <p style={{ marginBottom: 12 }}>{error || "Мем не найден"}</p>
              <div style={{ display: "flex", gap: 8 }}>
                <Button
                  onClick={handleBack}
                  style={{ display: "flex", alignItems: "center", gap: 4 }}
                >
                  <ArrowLeft size={16} />
                  Вернуться к списку
                </Button>
                <Button onClick={handleRefresh}>Попробовать снова</Button>
              </div>
            </div>
          }
        />
      </div>
    );
  }

  return (
    <div className={styles.randomMemePage}>
      <RandomMemeDetails
        memeOrder={memeOrder}
        isLoading={isLoading}
        onBack={handleBack}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onRefresh={handleRefresh}
      />
    </div>
  );
};

export default RandomMemeDetailsPage;
