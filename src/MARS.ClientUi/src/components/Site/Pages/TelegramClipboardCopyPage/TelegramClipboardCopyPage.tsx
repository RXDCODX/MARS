import { Button } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";

import { TelegramClipboardCopy } from "@/shared/api/http-clients/TelegramClipboardCopy";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { readClipboardUrls } from "./clipboardCopyResponse";

import styles from "./TelegramClipboardCopyPage.module.scss";

interface ClipboardImageItem {
  sourceUrl: string;
  previewUrl: string;
  blob: Blob;
}

const TelegramClipboardCopyPage: React.FC = () => {
  const [searchParameters] = useSearchParams();
  const telegramClipboardCopy = useMemo(() => new TelegramClipboardCopy(), []);
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
      // Через сгенерированный клиент, а не сырой `fetch`: конверт
      // `{ success, result, errorMessage }` разворачивает транспорт один раз для
      // всех вызовов. Напрямую страница читала `operation.data`, которого на
      // проводе нет, и показывала «Не удалось получить файлы» при живом сервисе,
      // отдающем ссылки.
      const response = await telegramClipboardCopy.telegramClipboardCopyDetail(
        encodeURIComponent(requestId)
      );
      const read = readClipboardUrls(response.data);

      if (read.ok) {
        const blobs = await Promise.all(
          read.urls.map(async sourceUrl => {
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
        status = read.message;
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
