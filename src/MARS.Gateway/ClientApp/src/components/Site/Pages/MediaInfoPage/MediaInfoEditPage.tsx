import "./MediaInfoPage.scss";

import { Alert, Button, Flex, Spin, Tag } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";

import {
  ApiMediaInfo,
  MediaFileInfoTypeEnum,
  MediaInfoApi,
} from "@/shared/api";

import { MediaInfoFormSections } from "./MediaInfoFormSections";
import {
  applySelectedFileToMediaInfo,
  buildMediaInfoFormData,
  formatMediaDuration,
  formatMediaRewardId,
  updateMediaInfoValue,
} from "./mediaInfoPageHelpers";

const getMediaPreviewKind = (
  item: ApiMediaInfo
): "image" | "audio" | "video" | "unsupported" => {
  if (
    item.fileInfo.type === MediaFileInfoTypeEnum.Image ||
    item.fileInfo.type === MediaFileInfoTypeEnum.Gif
  ) {
    return "image";
  }

  if (item.fileInfo.type === MediaFileInfoTypeEnum.Audio) {
    return "audio";
  }

  if (item.fileInfo.type === MediaFileInfoTypeEnum.Video) {
    return "video";
  }

  return "unsupported";
};

const MediaInfoPreviewCard: React.FC<{
  item: ApiMediaInfo;
  previewUrl?: string | null;
}> = ({ item, previewUrl }) => {
  const fileUrl = previewUrl ?? `/api/MediaInfoApi/${item.id}/file`;
  const previewKind = getMediaPreviewKind(item);

  return (
    <section className="preview-card card-shell">
      <div className="preview-card-header">
        <div>
          <p className="eyebrow">Сам алерт</p>
          <h2>{item.metaInfo.displayName || "Без названия"}</h2>
          <p className="preview-subtitle">
            {item.fileInfo.filePath || "Путь к файлу не указан"}
          </p>
        </div>

        <div className="preview-badges">
          <Tag
            color={
              item.metaInfo.priority === "High"
                ? "red"
                : item.metaInfo.priority === "Low"
                  ? "default"
                  : "blue"
            }
          >
            {item.metaInfo.priority}
          </Tag>
          <Tag color="cyan">{item.fileInfo.type}</Tag>
          {item.metaInfo.isFreezeRequired && <Tag color="orange">Freeze</Tag>}
        </div>
      </div>

      <div className="preview-stage">
        {previewKind === "image" && (
          <img
            className="preview-media"
            src={fileUrl}
            alt={item.metaInfo.displayName}
          />
        )}

        {previewKind === "audio" && (
          <audio
            className="preview-media preview-audio"
            controls
            src={fileUrl}
          />
        )}

        {previewKind === "video" && (
          <video
            className="preview-media preview-video"
            controls
            src={fileUrl}
          />
        )}

        {previewKind === "unsupported" && (
          <div className="preview-empty">
            <h3>Неподдерживаемый формат</h3>
            <p>{item.fileInfo.type}</p>
          </div>
        )}
      </div>

      <div className="preview-meta-grid">
        <div className="meta-chip">
          <span>Файл</span>
          <strong>{item.fileInfo.fileName || "Не указан"}</strong>
        </div>
        <div className="meta-chip">
          <span>Длительность</span>
          <strong>{formatMediaDuration(item.metaInfo.duration)}</strong>
        </div>
        <div className="meta-chip">
          <span>Reward</span>
          <strong>{formatMediaRewardId(item.metaInfo.twitchGuid)}</strong>
        </div>
        <div className="meta-chip">
          <span>Баллы</span>
          <strong>{item.metaInfo.twitchPointsCost}</strong>
        </div>
      </div>

      <div className="preview-description">
        <p>{item.textInfo.text || "Описание алерта не заполнено"}</p>
      </div>
    </section>
  );
};

export const MediaInfoEditPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [alert, setAlert] = useState<ApiMediaInfo | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [testingReward, setTestingReward] = useState(false);

  const mediaInfoApi = useMemo(() => new MediaInfoApi(), []);

  const loadAlert = useCallback(async () => {
    if (!id) {
      setError("Не указан идентификатор медиа");
      setLoading(false);
      return;
    }

    setLoading(true);

    try {
      const response = await mediaInfoApi.mediaInfoApiDetail(id);
      if (response.data.success && response.data.data) {
        setAlert(response.data.data);
        setSelectedFile(null);
        setPreviewUrl(previous => {
          if (previous) {
            URL.revokeObjectURL(previous);
          }

          return null;
        });
        setError(null);
      } else {
        setAlert(null);
        setError(response.data.message ?? "Медиа не найдено");
      }
    } catch (loadError) {
      setAlert(null);
      setError(
        loadError instanceof Error
          ? loadError.message
          : "Не удалось загрузить медиа"
      );
    } finally {
      setLoading(false);
    }
  }, [id, mediaInfoApi]);

  useEffect(() => {
    loadAlert();
  }, [loadAlert]);

  useEffect(
    () => () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    },
    [previewUrl]
  );

  const handleChange = useCallback((path: string, value: unknown) => {
    setAlert(previous => {
      if (!previous) {
        return previous;
      }

      return updateMediaInfoValue(previous, path, value);
    });
  }, []);

  const handleFileSelected = useCallback((file: File | null) => {
    setSelectedFile(file);

    setAlert(previous => {
      if (!previous) {
        return previous;
      }

      if (!file) {
        setPreviewUrl(previousPreview => {
          if (previousPreview) {
            URL.revokeObjectURL(previousPreview);
          }

          return null;
        });

        return previous;
      }

      setPreviewUrl(previousPreview => {
        if (previousPreview) {
          URL.revokeObjectURL(previousPreview);
        }

        return URL.createObjectURL(file);
      });

      return applySelectedFileToMediaInfo(previous, file);
    });
  }, []);

  const handleGenerateRewardId = useCallback(() => {
    handleChange("metaInfo.twitchGuid", crypto.randomUUID());
  }, [handleChange]);

  const handleClearRewardId = useCallback(() => {
    handleChange("metaInfo.twitchGuid", undefined);
    handleChange("metaInfo.twitchPointsCost", 0);
  }, [handleChange]);

  const handleSubmit = useCallback(
    async (event: React.FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!alert || !id) {
        return;
      }

      if (selectedFile && !alert.fileInfo.filePath.trim()) {
        setError("Укажи путь к файлу перед сохранением");
        return;
      }

      setSaving(true);

      try {
        const response = await fetch(`/api/MediaInfoApi/${id}`, {
          method: "PUT",
          body: buildMediaInfoFormData(alert, selectedFile),
        });
        const result = await response.json();

        if (response.ok && result?.success && result.data) {
          setAlert(result.data);
          setSelectedFile(null);
          setSuccess("Изменения сохранены");
          setError(null);
        } else {
          setError(result?.message ?? "Не удалось сохранить изменения");
        }
      } catch (saveError) {
        setError(
          saveError instanceof Error
            ? saveError.message
            : "Не удалось сохранить изменения"
        );
      } finally {
        setSaving(false);
      }
    },
    [alert, id, selectedFile]
  );

  const helpText =
    "Если выбран новый файл, он хранится в памяти клиента до сохранения. Укажи целевой путь внутри Alerts/uploaded_mems/ перед нажатием Сохранить.";

  const handleDelete = useCallback(async () => {
    if (!alert || !id) {
      return;
    }

    const confirmDelete = globalThis.confirm(
      `Удалить медиа «${alert.metaInfo.displayName || alert.fileInfo.fileName}»?`
    );

    if (!confirmDelete) {
      return;
    }

    setSaving(true);

    try {
      const response = await mediaInfoApi.mediaInfoApiDelete(id);
      if (response.data.success) {
        navigate("/media-info");
      } else {
        setError(response.data.message ?? "Не удалось удалить медиа");
      }
    } catch (deleteError) {
      setError(
        deleteError instanceof Error
          ? deleteError.message
          : "Не удалось удалить медиа"
      );
    } finally {
      setSaving(false);
    }
  }, [alert, id, mediaInfoApi, navigate]);

  const handleTestReward = useCallback(async () => {
    if (!alert || !id) {
      return;
    }

    setTestingReward(true);

    try {
      const response = await mediaInfoApi.mediaInfoApiTestCreate(id);
      if (response.data.success) {
        setSuccess("Награда протестирована: алерт отправлен");
        setError(null);
      } else {
        setError(response.data.message ?? "Не удалось протестировать награду");
      }
    } catch (testError) {
      setError(
        testError instanceof Error
          ? testError.message
          : "Не удалось протестировать награду"
      );
    } finally {
      setTestingReward(false);
    }
  }, [alert, id, mediaInfoApi]);

  const handleCancel = useCallback(() => {
    navigate("/media-info");
  }, [navigate]);

  if (loading) {
    return (
      <div
        className="media-info-page media-info-edit-page"
        data-testid="media-info-edit-page"
      >
        <div className="state-block">
          <Spin size="large" data-testid="loading-spinner" />
          <p>Загружаем карточку алерта...</p>
        </div>
      </div>
    );
  }

  if (!alert) {
    return (
      <div
        className="media-info-page media-info-edit-page"
        data-testid="media-info-edit-page"
      >
        <div className="state-block empty-state">
          <h2>Алерт не найден</h2>
          <p>Запрашиваемая запись отсутствует или была удалена.</p>
          <Link to="/media-info">
            <Button type="primary" data-testid="button-back-to-list-not-found">
              Вернуться к списку
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div
      className="media-info-page media-info-edit-page"
      data-testid="media-info-edit-page"
    >
      <section className="media-info-hero">
        <div className="hero-copy">
          <p className="eyebrow">Редактирование</p>
          <h1>Карточка медиа-алерта</h1>
          <p className="hero-description">
            Здесь видно сам алерт, его reward-привязку и все параметры
            поведения.
          </p>
        </div>

        <div className="hero-actions">
          <Button
            className="hero-button"
            onClick={handleTestReward}
            loading={testingReward}
            data-testid="button-test-reward"
          >
            Тест награды
          </Button>
          <Button
            className="hero-button"
            onClick={handleCancel}
            data-testid="button-back"
          >
            Назад
          </Button>
          <Button
            danger
            className="hero-button"
            onClick={handleDelete}
            loading={saving}
            data-testid="button-delete"
          >
            Удалить
          </Button>
        </div>
      </section>

      {error && (
        <Alert
          type="error"
          message={`Ошибка: ${error}`}
          closable
          onClose={() => setError(null)}
          style={{ marginBottom: 18, borderRadius: 18 }}
          data-testid="error-alert"
        />
      )}

      {success && (
        <Alert
          type="success"
          message={success}
          closable
          onClose={() => setSuccess(null)}
          style={{ marginBottom: 18, borderRadius: 18 }}
          data-testid="success-alert"
        />
      )}

      <div className="editor-layout">
        <MediaInfoPreviewCard item={alert} previewUrl={previewUrl} />

        <section className="card-shell editor-shell">
          <form className="editor-form" onSubmit={handleSubmit}>
            <div className="editor-form-header">
              <div>
                <h2>Редактирование полей</h2>
                <p>
                  Все изменения сохраняются одной операцией и сразу отражаются в
                  карточке.
                </p>
              </div>

              <Flex gap={12} wrap="wrap" className="editor-form-actions">
                <Button
                  onClick={handleGenerateRewardId}
                  data-testid="button-new-reward"
                >
                  Новый reward id
                </Button>
                <Button
                  type="primary"
                  htmlType="submit"
                  loading={saving}
                  data-testid="button-save"
                >
                  {saving ? "Сохраняем..." : "Сохранить"}
                </Button>
              </Flex>
            </div>

            <MediaInfoFormSections
              formData={alert}
              onChange={handleChange}
              onGenerateRewardId={handleGenerateRewardId}
              onClearRewardId={handleClearRewardId}
              onFileSelected={handleFileSelected}
              helpText={helpText}
              previewUrl={previewUrl}
            />

            <Flex
              gap={12}
              wrap="wrap"
              justify="flex-end"
              className="editor-footer-actions"
            >
              <Button
                type="primary"
                htmlType="submit"
                loading={saving}
                data-testid="button-save-footer"
              >
                {saving ? "Сохраняем..." : "Сохранить изменения"}
              </Button>
              <Button onClick={handleCancel} data-testid="button-cancel-edit">
                К списку
              </Button>
            </Flex>
          </form>
        </section>
      </div>
    </div>
  );
};
