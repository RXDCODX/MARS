import { Alert, Button, Card, Flex, Spin } from "antd";
import {
  ArrowLeft,
  Download,
  Edit,
  Eye,
  EyeOff,
  Folder,
  Image,
  RefreshCw,
  Trash2,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { RandomMeme } from "@/shared/api";

import styles from "../RandomMemePage.module.scss";
import {
  RandomMemeOrderDetailsProps as RandomMemeOrderDetailsProperties,
  RandomMemeTypeDetailsProps as RandomMemeTypeDetailsProperties,
} from "../RandomMemePage.types";

const RandomMemeTypeDetails: React.FC<RandomMemeTypeDetailsProperties> = ({
  memeType,
  isLoading,
  onBack,
  onEdit,
  onDelete,
  onRefresh,
}) => (
  <div style={{ padding: "16px 0" }}>
    <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
      <Flex align="center" gap={12}>
        <Button
          size="small"
          onClick={onBack}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <ArrowLeft size={16} />
          Назад к списку
        </Button>
        <div>
          <h1
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              marginBottom: 2,
            }}
          >
            <Folder size={32} />
            Тип мема
          </h1>
          <p style={{ color: "#8c8c8c", marginBottom: 0 }}>{memeType.name}</p>
        </div>
      </Flex>

      <Flex gap={8}>
        <Button
          onClick={onRefresh}
          disabled={isLoading}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <RefreshCw size={16} />
          Обновить
        </Button>
        <Button
          onClick={onEdit}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <Edit size={16} />
          Редактировать
        </Button>
        <Button
          danger
          onClick={onDelete}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <Trash2 size={16} />
          Удалить
        </Button>
      </Flex>
    </Flex>

    <Card title="Основная информация" style={{ marginBottom: 16 }}>
      <Flex gap={12} wrap="wrap">
        <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
          <div
            style={{
              border: "1px solid #f0f0f0",
              borderRadius: 4,
              padding: 12,
            }}
          >
            <span
              style={{
                color: "#8c8c8c",
                fontSize: 12,
                display: "block",
                marginBottom: 4,
              }}
            >
              ID
            </span>
            <div style={{ fontWeight: 600 }}>{memeType.id}</div>
          </div>
        </div>
        <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
          <div
            style={{
              border: "1px solid #f0f0f0",
              borderRadius: 4,
              padding: 12,
            }}
          >
            <span
              style={{
                color: "#8c8c8c",
                fontSize: 12,
                display: "block",
                marginBottom: 4,
              }}
            >
              Название
            </span>
            <div style={{ fontWeight: 600 }}>{memeType.name}</div>
          </div>
        </div>
        <div style={{ flex: "0 0 100%" }}>
          <div
            style={{
              border: "1px solid #f0f0f0",
              borderRadius: 4,
              padding: 12,
            }}
          >
            <span
              style={{
                color: "#8c8c8c",
                fontSize: 12,
                display: "block",
                marginBottom: 4,
              }}
            >
              Папка
            </span>
            <code
              style={{
                padding: "0.25rem 0.5rem",
                borderRadius: "0.25rem",
                backgroundColor: "var(--site-bg-secondary)",
              }}
            >
              {memeType.folderPath}
            </code>
          </div>
        </div>
      </Flex>
    </Card>

    <Card title="Действия">
      <Flex gap={8} wrap="wrap">
        <Button
          type="primary"
          onClick={onEdit}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <Edit size={16} />
          Редактировать
        </Button>
        <Button
          danger
          onClick={onDelete}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <Trash2 size={16} />
          Удалить тип
        </Button>
        <Button
          onClick={onRefresh}
          disabled={isLoading}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <RefreshCw size={16} />
          Обновить данные
        </Button>
      </Flex>
    </Card>
  </div>
);

const RandomMemeOrderDetails: React.FC<RandomMemeOrderDetailsProperties> = ({
  memeOrder,
  isLoading,
  onBack,
  onEdit,
  onDelete,
  onRefresh,
}) => {
  const api = useMemo(() => new RandomMeme(), []);

  const [memeImageUrl, setMemeImageUrl] = useState<string | null>(null);
  const [isImageLoading, setIsImageLoading] = useState(false);
  const [imageError, setImageError] = useState<string | null>(null);
  const [showImage, setShowImage] = useState(true);

  const loadMemeMedia = useCallback(async () => {
    if (!memeOrder?.id) return;

    try {
      setIsImageLoading(true);
      setImageError(null);

      await api.randomMemeFileDetail(memeOrder.id);

      const mediaUrl = `/api/RandomMeme/file/${memeOrder.id}`;
      setMemeImageUrl(mediaUrl);
    } catch (error) {
      console.error("Ошибка загрузки медиа файла мема:", error);
      const errorMessage =
        error instanceof Error ? error.message : "Неизвестная ошибка";
      setImageError(errorMessage);
    } finally {
      setIsImageLoading(false);
    }
  }, [api, memeOrder?.id]);

  const getMediaType = useCallback((filePath: string) => {
    const extension = filePath.split(".").pop()?.toLowerCase();

    const imageExtensions = ["jpg", "jpeg", "png", "gif", "bmp", "webp", "svg"];
    const videoExtensions = ["mp4", "avi", "mov", "wmv", "flv", "webm", "mkv"];
    const audioExtensions = ["mp3", "wav", "ogg", "aac", "flac", "m4a"];

    if (imageExtensions.includes(extension || "")) {
      return "image";
    }
    if (videoExtensions.includes(extension || "")) {
      return "video";
    }
    if (audioExtensions.includes(extension || "")) {
      return "audio";
    }

    return "unknown";
  }, []);

  useEffect(() => {
    if (memeOrder?.id) {
      loadMemeMedia();
    } else {
      setMemeImageUrl(null);
      setImageError(null);
    }
  }, [memeOrder?.id, loadMemeMedia]);

  if (isLoading) {
    return (
      <div style={{ textAlign: "center", padding: "40px 0" }}>
        <Spin size="large" />
        <h4 style={{ marginTop: 12 }}>Загрузка деталей...</h4>
        <p style={{ color: "#8c8c8c" }}>Получаем информацию об элементе</p>
      </div>
    );
  }

  if (!memeOrder) {
    return (
      <div>
        <Alert
          type="warning"
          showIcon
          message="Элемент не найден"
          description={
            <div>
              <p>Запрашиваемый элемент не существует или был удален.</p>
              <Button
                onClick={onBack}
                style={{ display: "flex", alignItems: "center", gap: 4 }}
              >
                <ArrowLeft size={16} />
                Вернуться к списку
              </Button>
            </div>
          }
        />
      </div>
    );
  }

  return (
    <div style={{ padding: "16px 0" }}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Flex align="center" gap={12}>
          <Button
            size="small"
            onClick={onBack}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <ArrowLeft size={16} />
            Назад к списку
          </Button>
          <div>
            <h1
              style={{
                display: "flex",
                alignItems: "center",
                gap: 12,
                marginBottom: 2,
              }}
            >
              <Image size={32} />
              Заказ мема
            </h1>
            <p style={{ color: "#8c8c8c", marginBottom: 0 }}>
              Заказ #{memeOrder.order}
            </p>
          </div>
        </Flex>

        <Flex gap={8}>
          <Button
            onClick={onRefresh}
            disabled={isLoading}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <RefreshCw size={16} />
            Обновить
          </Button>
          <Button
            onClick={onEdit}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Edit size={16} />
            Редактировать
          </Button>
          <Button
            danger
            onClick={onDelete}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Trash2 size={16} />
            Удалить
          </Button>
        </Flex>
      </Flex>

      {memeOrder && (
        <Card
          className={styles.memeDisplayCard}
          title={
            <Flex justify="space-between" align="center">
              <span>Просмотр медиа файла</span>
              <Flex gap={8}>
                <Button
                  size="small"
                  onClick={() => setShowImage(!showImage)}
                  style={{ display: "flex", alignItems: "center", gap: 6 }}
                >
                  {showImage ? <EyeOff size={16} /> : <Eye size={16} />}
                  {showImage ? "Скрыть" : "Показать"}
                </Button>
                <Button
                  size="small"
                  onClick={loadMemeMedia}
                  disabled={isImageLoading}
                  style={{ display: "flex", alignItems: "center", gap: 6 }}
                >
                  <RefreshCw
                    size={16}
                    className={isImageLoading ? styles.spin : ""}
                  />
                  Обновить
                </Button>
              </Flex>
            </Flex>
          }
          style={{ marginBottom: 16 }}
        >
          {isImageLoading ? (
            <div style={{ textAlign: "center", padding: "40px 0" }}>
              <Spin />
              <p style={{ color: "#8c8c8c", marginTop: 12 }}>
                Загружаем медиа файл мема...
              </p>
            </div>
          ) : imageError ? (
            <Alert
              type="error"
              showIcon
              message="Ошибка загрузки медиа файла"
              description={
                <div>
                  <p>{imageError}</p>
                  <Button size="small" onClick={loadMemeMedia}>
                    Попробовать снова
                  </Button>
                </div>
              }
            />
          ) : memeImageUrl && showImage ? (
            <div style={{ textAlign: "center" }}>
              <div style={{ marginBottom: 12 }}>
                {(() => {
                  const mediaType = getMediaType(memeOrder.filePath);

                  switch (mediaType) {
                    case "image": {
                      return (
                        <img
                          src={memeImageUrl}
                          alt={`Мем #${memeOrder.order}`}
                          style={{
                            maxWidth: "100%",
                            borderRadius: 4,
                          }}
                          className={styles.memeImage}
                          onError={() => {
                            setImageError("Не удалось загрузить изображение");
                          }}
                        />
                      );
                    }

                    case "video": {
                      return (
                        <video
                          src={memeImageUrl}
                          controls
                          style={{
                            maxWidth: "100%",
                            borderRadius: 4,
                          }}
                          className={styles.memeImage}
                          onError={() => {
                            setImageError("Не удалось загрузить видео");
                          }}
                        >
                          Ваш браузер не поддерживает видео элемент.
                        </video>
                      );
                    }

                    case "audio": {
                      return (
                        <Flex vertical align="center">
                          <div style={{ marginBottom: 12 }}>
                            <Image size={64} style={{ color: "#8c8c8c" }} />
                          </div>
                          <audio
                            src={memeImageUrl}
                            controls
                            style={{ width: "100%" }}
                            onError={() => {
                              setImageError("Не удалось загрузить аудио");
                            }}
                          >
                            Ваш браузер не поддерживает аудио элемент.
                          </audio>
                        </Flex>
                      );
                    }

                    default: {
                      return (
                        <Flex vertical align="center">
                          <div style={{ marginBottom: 12 }}>
                            <Image size={64} style={{ color: "#8c8c8c" }} />
                          </div>
                          <p style={{ color: "#8c8c8c" }}>
                            Неподдерживаемый тип файла:{" "}
                            {memeOrder.filePath.split(".").pop()}
                          </p>
                          <Button
                            size="small"
                            onClick={() => window.open(memeImageUrl, "_blank")}
                            style={{
                              display: "flex",
                              alignItems: "center",
                              gap: 6,
                            }}
                          >
                            <Eye size={16} />
                            Открыть файл
                          </Button>
                        </Flex>
                      );
                    }
                  }
                })()}
              </div>
              <div className={styles.memeActions}>
                <Button
                  size="small"
                  onClick={() => window.open(memeImageUrl, "_blank")}
                  style={{ display: "flex", alignItems: "center", gap: 6 }}
                >
                  <Eye size={16} />
                  Открыть в новой вкладке
                </Button>
                <Button
                  size="small"
                  onClick={() => {
                    const link = document.createElement("a");
                    link.href = memeImageUrl;
                    const extension =
                      memeOrder.filePath.split(".").pop() || "file";
                    link.download = `meme_${memeOrder.order}.${extension}`;
                    document.body.append(link);
                    link.click();
                    link.remove();
                  }}
                  style={{ display: "flex", alignItems: "center", gap: 6 }}
                >
                  <Download size={16} />
                  Скачать
                </Button>
              </div>
            </div>
          ) : (
            <div className={styles.memePlaceholder}>
              <Image size={64} className="placeholder-icon" />
              <p className="placeholder-text">Медиа файл скрыт</p>
              <Button
                size="small"
                onClick={() => setShowImage(true)}
                style={{ display: "flex", alignItems: "center", gap: 6 }}
              >
                <Eye size={16} />
                Показать медиа файл
              </Button>
            </div>
          )}
        </Card>
      )}

      <Card title="Основная информация" style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap="wrap">
          <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                ID
              </span>
              <div style={{ fontWeight: 600 }}>
                {memeOrder.id.slice(0, 8)}...
              </div>
            </div>
          </div>
          <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                Порядок
              </span>
              <div style={{ fontWeight: 600 }}>#{memeOrder.order}</div>
            </div>
          </div>
          <div style={{ flex: "0 0 100%" }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                Файл
              </span>
              <code
                style={{
                  display: "block",
                  padding: "0.25rem 0.5rem",
                  borderRadius: "0.25rem",
                  backgroundColor: "var(--site-bg-secondary)",
                }}
              >
                {memeOrder.filePath}
              </code>
            </div>
          </div>
        </Flex>
      </Card>

      {memeOrder.type && (
        <Card title="Информация о типе" style={{ marginBottom: 16 }}>
          <Flex gap={12} wrap="wrap">
            <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
              <div
                style={{
                  border: "1px solid #f0f0f0",
                  borderRadius: 4,
                  padding: 12,
                }}
              >
                <span
                  style={{
                    color: "#8c8c8c",
                    fontSize: 12,
                    display: "block",
                    marginBottom: 4,
                  }}
                >
                  Название типа
                </span>
                <div style={{ fontWeight: 600 }}>{memeOrder.type.name}</div>
              </div>
            </div>
            <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
              <div
                style={{
                  border: "1px solid #f0f0f0",
                  borderRadius: 4,
                  padding: 12,
                }}
              >
                <span
                  style={{
                    color: "#8c8c8c",
                    fontSize: 12,
                    display: "block",
                    marginBottom: 4,
                  }}
                >
                  ID типа
                </span>
                <div style={{ fontWeight: 600 }}>{memeOrder.type.id}</div>
              </div>
            </div>
            <div style={{ flex: "0 0 100%" }}>
              <div
                style={{
                  border: "1px solid #f0f0f0",
                  borderRadius: 4,
                  padding: 12,
                }}
              >
                <span
                  style={{
                    color: "#8c8c8c",
                    fontSize: 12,
                    display: "block",
                    marginBottom: 4,
                  }}
                >
                  Папка типа
                </span>
                <code
                  style={{
                    padding: "0.25rem 0.5rem",
                    borderRadius: "0.25rem",
                    backgroundColor: "var(--site-bg-secondary)",
                  }}
                >
                  {memeOrder.type.folderPath}
                </code>
              </div>
            </div>
          </Flex>
        </Card>
      )}

      <Card title="Свойства файла" style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap="wrap">
          <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                Расширение
              </span>
              <div style={{ fontWeight: 600 }}>
                {memeOrder.filePath.split(".").pop()?.toUpperCase() || "N/A"}
              </div>
            </div>
          </div>
          <div style={{ flex: "0 0 calc(50% - 6px)", minWidth: 200 }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                Размер файла
              </span>
              <div style={{ fontWeight: 600, color: "#8c8c8c" }}>
                Недоступно
              </div>
              <span style={{ color: "#8c8c8c", fontSize: 12 }}>
                Информация о размере файла недоступна через API
              </span>
            </div>
          </div>
          <div style={{ flex: "0 0 100%" }}>
            <div
              style={{
                border: "1px solid #f0f0f0",
                borderRadius: 4,
                padding: 12,
              }}
            >
              <span
                style={{
                  color: "#8c8c8c",
                  fontSize: 12,
                  display: "block",
                  marginBottom: 4,
                }}
              >
                Полный путь
              </span>
              <code
                style={{
                  display: "block",
                  wordBreak: "break-all",
                  padding: "0.25rem 0.5rem",
                  borderRadius: "0.25rem",
                  backgroundColor: "var(--site-bg-secondary)",
                }}
              >
                {memeOrder.filePath}
              </code>
            </div>
          </div>
        </Flex>
      </Card>

      <Card title="Действия">
        <Flex gap={8} wrap="wrap">
          <Button
            type="primary"
            onClick={onEdit}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Edit size={16} />
            Редактировать
          </Button>
          <Button
            danger
            onClick={onDelete}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <Trash2 size={16} />
            Удалить заказ
          </Button>
          <Button
            onClick={onRefresh}
            disabled={isLoading}
            style={{ display: "flex", alignItems: "center", gap: 6 }}
          >
            <RefreshCw size={16} />
            Обновить данные
          </Button>
        </Flex>
      </Card>
    </div>
  );
};

const RandomMemeDetails: React.FC<
  RandomMemeTypeDetailsProperties | RandomMemeOrderDetailsProperties
> = properties =>
  "memeType" in properties ? (
    <RandomMemeTypeDetails {...properties} />
  ) : (
    <RandomMemeOrderDetails {...properties} />
  );

export default RandomMemeDetails;
export { RandomMemeTypeDetails, RandomMemeOrderDetails };
