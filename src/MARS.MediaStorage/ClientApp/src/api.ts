export type MediaType = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface StorageEntry {
  id: string;
  path: string;
  fileName: string;
  extension: string;
  mediaType: MediaType;
  sizeBytes: number;
  uploadedAt: string;
  lastDownloadedAt: string | null;
  deletedAt: string | null;
  originalPath: string | null;
  isDeleted: boolean;
}

export interface BulkResult {
  requested: number;
  succeeded: number;
  failed: number;
  errors: string[];
}

export interface IndexResult {
  added: number;
  updated: number;
}

/**
 * Конверт OperationResult<T>, который отдаёт MediaStorage.
 * Ошибки сервис возвращает внутри успешного HTTP 200, поэтому проверять
 * приходится поле success, а не код ответа.
 */
interface Envelope<T> {
  success: boolean;
  result: T | null;
  errorMessage: string | null;
}

async function request<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }

  const envelope = (await response.json()) as Envelope<T>;

  if (!envelope.success) {
    throw new Error(envelope.errorMessage ?? 'Неизвестная ошибка');
  }

  return envelope.result as T;
}

export function listEntries(includeDeleted: boolean): Promise<StorageEntry[]> {
  return request<StorageEntry[]>(
    `/api/MediaStorage/entries?includeDeleted=${includeDeleted}`,
  );
}

export function indexEntries(): Promise<IndexResult> {
  return request<IndexResult>('/api/MediaStorage/index', { method: 'POST' });
}

export function softDelete(ids: string[], dryRun: boolean): Promise<BulkResult> {
  return request<BulkResult>('/api/MediaStorage/delete', {
    method: 'POST',
    body: JSON.stringify({ ids, dryRun }),
  });
}

export function restore(ids: string[], dryRun: boolean): Promise<number> {
  return request<number>('/api/MediaStorage/restore', {
    method: 'POST',
    body: JSON.stringify({ ids, dryRun }),
  });
}

export function move(
  ids: string[],
  targetDirectory: string,
  dryRun: boolean,
): Promise<BulkResult> {
  return request<BulkResult>('/api/MediaStorage/move', {
    method: 'POST',
    body: JSON.stringify({ ids, targetDirectory, dryRun }),
  });
}

export function upload(files: File[], targetDirectory: string): Promise<BulkResult> {
  // Файлы уходят через FormData: имена полей повторяются, поэтому сервер
  // принимает их коллекцией, а не одиночным IFormFile.
  const form = new FormData();
  form.append('targetDirectory', targetDirectory);

  for (const file of files) {
    form.append('files', file, file.name);
  }

  return uploadWithProgress(form);
}

/**
 * Отправка через XHR: fetch не умеет отдавать прогресс загрузки, а файл
 * может быть десятки мегабайт — без индикатора пользователь не понимает,
 * завис ли запрос.
 */
function uploadWithProgress(form: FormData): Promise<BulkResult> {
  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    request.open('POST', '/api/MediaStorage/upload');
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        window.dispatchEvent(
          new CustomEvent('upload-progress', {
            detail: {
              loaded: event.loaded,
              total: event.total,
            },
          }),
        );
      }
    };
    request.onload = () => {
      try {
        const envelope = JSON.parse(request.responseText) as Envelope<BulkResult>;

        if (!envelope.success) {
          reject(new Error(envelope.errorMessage ?? 'Неизвестная ошибка'));
          return;
        }

        resolve(envelope.result as BulkResult);
      } catch (cause) {
        reject(cause instanceof Error ? cause : new Error(String(cause)));
      }
    };
    request.onerror = () => reject(new Error('Сеть недоступна'));
    request.send(form);
  });
}

/** URL для просмотра файла: путь хранилища отдаётся как есть. */
export function fileUrl(path: string): string {
  return `/api/MediaStorage/file?path=${encodeURIComponent(path)}`;
}

export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 Б';

  const units = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ'];
  const exponent = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    units.length - 1,
  );
  const value = bytes / 1024 ** exponent;

  return `${value.toFixed(exponent === 0 ? 0 : 1)} ${units[exponent]}`;
}

export function formatDate(value: string | null): string {
  if (!value) return '—';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function typeName(mediaType: MediaType): string {
  switch (mediaType) {
    case 1:
      return 'изображение';
    case 2:
      return 'аудио';
    case 3:
      return 'видео';
    case 4:
      return 'стикер';
    case 5:
      return 'голос';
    case 6:
      return 'гиф';
    default:
      return 'файл';
  }
}

/** CSS-класс типа: стабильные значения, а не обрезка русских слов. */
export function typeClass(mediaType: MediaType): string {
  switch (mediaType) {
    case 1:
      return 'image';
    case 2:
      return 'audio';
    case 3:
      return 'video';
    case 6:
      return 'image';
    default:
      return 'other';
  }
}
