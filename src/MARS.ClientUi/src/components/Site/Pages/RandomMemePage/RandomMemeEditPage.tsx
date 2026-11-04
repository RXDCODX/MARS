import { Alert, Button, Spin } from "antd";
import { ArrowLeft } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { RandomMeme } from "@/shared/api";
import { MemeOrderDto, MemeTypeDto } from "@/shared/api";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { RandomMemeForm } from "./components";
import styles from "./RandomMemePage.module.scss";

const RandomMemeEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const api = useMemo(() => new RandomMeme(), []);
  const { showToast } = useToastModal();

  const [memeOrder, setMemeOrder] = useState<MemeOrderDto | null>(null);
  const [memeTypes, setMemeTypes] = useState<MemeTypeDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  const loadData = useCallback(async () => {
    if (!id) {
      setError("ID мема не указан");
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setError("");

      const [orderResponse, typesResponse] = await Promise.all([
        api.randomMemeOrdersDetail(id),
        api.randomMemeTypesList(),
      ]);

      setMemeOrder(orderResponse.data.data ?? null);
      setMemeTypes(typesResponse.data.data ?? []);

      if (!orderResponse.data.data) {
        setError("Мем с указанным ID не найден");
      }
    } catch (error_) {
      console.error("Ошибка загрузки данных:", error_);
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
    loadData();
  }, [loadData]);

  const handleBack = useCallback(() => {
    if (memeOrder) {
      navigate(`/random-meme/${memeOrder.id}`);
    } else {
      navigate("/random-meme");
    }
  }, [navigate, memeOrder]);

  const handleSubmit = useCallback(
    async (data: Record<string, unknown>) => {
      if (!memeOrder) return;

      try {
        setIsSubmitting(true);

        const orderData = data as unknown as {
          filePath: string;
          memeTypeId: number;
          order: number;
        };

        const result = await api.randomMemeOrdersUpdate(
          memeOrder.id,
          orderData
        );

        showToast(result.data);

        navigate(`/random-meme/${memeOrder.id}`);
      } catch (error_) {
        console.error("Ошибка сохранения:", error_);
        showToast({
          success: false,
          message: "Ошибка сохранения данных",
        });
      } finally {
        setIsSubmitting(false);
      }
    },
    [memeOrder, api, showToast, navigate]
  );

  if (isLoading) {
    return (
      <div className={styles.randomMemePage}>
        <div style={{ textAlign: "center", padding: "40px 0" }}>
          <Spin size="large" />
          <h4 style={{ marginTop: 12 }}>
            Загрузка данных для редактирования...
          </h4>
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
                  Вернуться
                </Button>
                <Button onClick={loadData}>Попробовать снова</Button>
              </div>
            </div>
          }
        />
      </div>
    );
  }

  return (
    <div className={styles.randomMemePage}>
      <RandomMemeForm
        memeOrder={memeOrder}
        memeTypes={memeTypes}
        isSubmitting={isSubmitting}
        mode="edit"
        onSubmit={handleSubmit}
        onCancel={handleBack}
      />
    </div>
  );
};

export default RandomMemeEditPage;
