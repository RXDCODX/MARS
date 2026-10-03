import { Button } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";

import { OperationResult } from "@/shared/types";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./TelegramClipboardCopyPage.module.scss";

interface ClipboardImageItem {
  sourceUrl: string;
  previewUrl: string;
  blob: Blob;
}

const TelegramClipboardCopyPage: React.FC = () => {
  const [searchParameters] = useSearchParams();
  const [items, setItems] = useState<ClipboardImageItem[]>([]);
  const [statusText, setStatusText] = useState(
    "Загружаю список изображений..."
  );
  const [isLoading, setIsLoading] = useState(false);
  const { showToast } = useToastModal();

  const requestId = useMemo(
    () => searchParameters.get("id") ?? "",
    [searchParameters]
  );

  const loadFiles = useCallback(async () => {
    let status = "ID не указан в параметре запроса";

    if (!requestId) {
      setItems([]);
      setStatusText(status);
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch(
        `/api/TelegramClipboardCopy/${encodeURIComponent(requestId)}`
      );
      const operation = (await response.json()) as OperationResult<string[]>;

      if (response.ok && operation.success && Array.isArray(operation.data)) {
        const blobs = await Promise.all(
          operation.data.map(async sourceUrl => {
            const fileResponse = await fetch(sourceUrl, { cache: "no-store" });
            if (!fileResponse.ok) {
              throw new Error(`Не удалось загрузить файл: ${sourceUrl}`);
            }

            const blob = await fileResponse.blob();
            const previewUrl = URL.createObjectURL(blob);
            return { sourceUrl, previewUrl, blob } as ClipboardImageItem;
          })
        );

        setItems(previousItems => {
          previousItems.forEach(item => URL.revokeObjectURL(item.previewUrl));
          return blobs;
        });
        status = `Получено файлов: ${blobs.length}`;
      } else {
        setItems(previousItems => {
          previousItems.forEach(item => URL.revokeObjectURL(item.previewUrl));
          return [];
        });
        status = operation.message ?? "Не удалось получить файлы";
      }
    } catch (error) {
      setItems(previousItems => {
        previousItems.forEach(item => URL.revokeObjectURL(item.previewUrl));
        return [];
      });
      status =
        error instanceof Error ? error.message : "Ошибка загрузки файлов";
    }

    setStatusText(status);
    setIsLoading(false);
  }, [requestId]);

  const copyAllImages = useCallback(async () => {
    if (items.length === 0) {
      showToast({ success: false, message: "Нет изображений для копирования" });
      return;
    }

    try {
      if (!navigator.clipboard || typeof ClipboardItem === "undefined") {
        showToast({
          success: false,
          message: "Браузер не поддерживает запись изображений в буфер",
        });
        return;
      }

      let successCount = 0;
      for (const item of items) {
        try {
          const clipboardItem = new ClipboardItem({
            [item.blob.type || "image/png"]: item.blob,
          });
          await navigator.clipboard.write([clipboardItem]);
          successCount++;
        } catch (itemError) {
          console.warn(
            `Не удалось скопировать изображение ${item.sourceUrl}:`,
            itemError
          );
        }
      }

      if (successCount > 0) {
        await fetch(
          `/api/TelegramClipboardCopy/complete/${encodeURIComponent(requestId)}`,
          {
            method: "POST",
          }
        );

        showToast({
          success: true,
          message: `Скопировано изображений: ${successCount} из ${items.length}`,
        });
      } else {
        showToast({
          success: false,
          message: "Не удалось скопировать ни одного изображения",
        });
      }
    } catch (error) {
      showToast({
        success: false,
        message:
          error instanceof Error
            ? `Не удалось скопировать изображения: ${error.message}`
            : "Не удалось скопировать изображения",
      });
    }
  }, [items, requestId, showToast]);

  const copyLinks = useCallback(async () => {
    if (items.length === 0) {
      showToast({ success: false, message: "Нет ссылок для копирования" });
      return;
    }

    try {
      const absoluteUrls = items.map(
        item => new URL(item.sourceUrl, location.href).href
      );
      await navigator.clipboard.writeText(absoluteUrls.join("\n"));

      showToast({ success: true, message: "Ссылки скопированы в буфер" });
    } catch (error) {
      showToast({
        success: false,
        message:
          error instanceof Error
            ? `Не удалось скопировать ссылки: ${error.message}`
            : "Не удалось скопировать ссылки",
      });
    }
  }, [items, showToast]);

  useEffect(() => {
    loadFiles();
  }, [loadFiles]);

  useEffect(
    () => () => {
      items.forEach(item => URL.revokeObjectURL(item.previewUrl));
    },
    [items]
  );

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <section className={styles.panel}>
          <header className={styles.header}>
            <h1 className={styles.title}>Telegram Copy</h1>
            <p className={styles.subtitle}>
              Нажми кнопку, чтобы скопировать изображения или ссылки
            </p>
          </header>

          <div className={styles.actions}>
            <Button
              type="primary"
              onClick={copyAllImages}
              disabled={isLoading || items.length === 0}
            >
              Скопировать все
            </Button>
            <Button
              onClick={copyLinks}
              disabled={isLoading || items.length === 0}
            >
              Скопировать ссылки
            </Button>
            <Button onClick={loadFiles} disabled={isLoading}>
              Обновить
            </Button>
          </div>

          <div className={styles.status}>{statusText}</div>

          {items.length > 0 ? (
            <div className={styles.grid}>
              {items.map(item => (
                <figure key={item.sourceUrl} className={styles.card}>
                  <img
                    src={item.previewUrl}
                    alt={item.sourceUrl}
                    className={styles.image}
                    loading="lazy"
                  />
                  <figcaption className={styles.caption}>
                    {item.sourceUrl}
                  </figcaption>
                </figure>
              ))}
            </div>
          ) : (
            <div className={styles.empty}>Изображения не найдены.</div>
          )}
        </section>
      </div>
    </div>
  );
};

export default TelegramClipboardCopyPage;
