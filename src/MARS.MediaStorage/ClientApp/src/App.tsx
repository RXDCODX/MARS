import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  formatBytes,
  formatDate,
  indexEntries,
  listEntries,
  move,
  restore,
  softDelete,
  upload,
  typeName,
  typeClass,
  fileUrl,
  type BulkResult,
  type MediaType,
  type StorageEntry,
} from './api';

const ROW_HEIGHT = 44;
const OVERSCAN = 8;

type DialogKind = 'move' | 'delete' | 'upload' | null;

interface Outcome {
  title: string;
  result: BulkResult;
}

function isImage(mediaType: MediaType): boolean {
  return mediaType === 1 || mediaType === 6;
}

function isPlayable(mediaType: MediaType): boolean {
  return mediaType === 2 || mediaType === 3 || mediaType === 6;
}

export default function App() {
  const [entries, setEntries] = useState<StorageEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const [showDeleted, setShowDeleted] = useState(false);
  const [dryRun, setDryRun] = useState(true);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [dialog, setDialog] = useState<DialogKind>(null);
  const [targetDirectory, setTargetDirectory] = useState('Archive');
  const [uploadDirectory, setUploadDirectory] = useState('Alerts/random_meme/videos');
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [progress, setProgress] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  const scrollerRef = useRef<HTMLDivElement | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(600);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setEntries(await listEntries(showDeleted));
      setSelected(new Set());
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setLoading(false);
    }
  }, [showDeleted]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    const element = scrollerRef.current;
    if (!element) return;

    const onScroll = () => setScrollTop(element.scrollTop);
    element.addEventListener('scroll', onScroll, { passive: true });
    setViewportHeight(element.clientHeight);

    return () => element.removeEventListener('scroll', onScroll);
  }, []);

  const filtered = useMemo(() => {
    const needle = query.trim().toLowerCase();

    if (!needle) return entries;

    return entries.filter(
      (entry) =>
        entry.path.toLowerCase().includes(needle) ||
        entry.fileName.toLowerCase().includes(needle),
    );
  }, [entries, query]);

  const totals = useMemo(
    () => ({
      count: filtered.length,
      size: filtered.reduce((sum, entry) => sum + entry.sizeBytes, 0),
    }),
    [filtered],
  );

  // Окно отрисовки: 1984 строки в DOM держать нельзя, поэтому рисуем только
  // видимую часть с небольшим запасом.
  const window_ = useMemo(() => {
    const first = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN);
    const visible = Math.ceil(viewportHeight / ROW_HEIGHT) + OVERSCAN * 2;
    const last = Math.min(filtered.length, first + visible);

    return { first, last };
  }, [filtered.length, scrollTop, viewportHeight]);

  const visibleRows = filtered.slice(window_.first, window_.last);

  const allSelected =
    filtered.length > 0 && filtered.every((entry) => selected.has(entry.id));

  function toggleAll() {
    if (allSelected) {
      setSelected(new Set());
      return;
    }

    setSelected(new Set(filtered.map((entry) => entry.id)));
  }

  function toggleOne(id: string) {
    setSelected((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  const selectedIds = useMemo(() => [...selected], [selected]);

  // Прогресс приходит отдельным событием из слоя API: fetch его не отдаёт,
  // а XHR отдаёт, но на уровне компонента об этом знать не нужно.
  useEffect(() => {
    const onProgress = (event: Event) => {
      const { loaded, total } = (event as CustomEvent).detail as {
        loaded: number;
        total: number;
      };
      setProgress(`${Math.round((loaded / total) * 100)}% (${formatBytes(loaded)} из ${formatBytes(total)})`);
    };

    window.addEventListener('upload-progress', onProgress);
    return () => window.removeEventListener('upload-progress', onProgress);
  }, []);

  function addFiles(list: FileList | null) {
    if (!list || list.length === 0) return;
    setPendingFiles((previous) => {
      // Склеиваем по имени: повторный выбор того же файла не должен
      // приводить к тихой подмене уже выбранного.
      const merged = new Map(previous.map((file) => [file.name, file]));
      for (const file of Array.from(list)) merged.set(file.name, file);
      return [...merged.values()];
    });
    setDialog('upload');
  }

  async function runUpload() {
    if (pendingFiles.length === 0) return;

    setBusy(true);
    setError(null);
    setProgress('0%');

    try {
      const result = await upload(pendingFiles, uploadDirectory);
      setOutcome({ title: 'Загрузка', result });
      setPendingFiles([]);
      setDialog(null);
      await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setBusy(false);
      setProgress(null);
    }
  }

  async function runIndex() {
    setBusy(true);
    setError(null);
    setNotice(null);

    try {
      const result = await indexEntries();
      setNotice(`Добавлено: ${result.added}, обновлено: ${result.updated}`);
      await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setBusy(false);
    }
  }

  async function runDelete() {
    setBusy(true);
    setError(null);

    try {
      const result = await softDelete(selectedIds, dryRun);
      setOutcome({
        title: dryRun ? 'Пробное удаление' : 'Удаление',
        result,
      });
      setDialog(null);
      if (!dryRun) await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setBusy(false);
    }
  }

  async function runMove() {
    setBusy(true);
    setError(null);

    try {
      const result = await move(selectedIds, targetDirectory, dryRun);
      setOutcome({
        title: dryRun ? 'Пробный перенос' : 'Перенос',
        result,
      });
      setDialog(null);
      if (!dryRun) await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setBusy(false);
    }
  }

  async function runRestore() {
    setBusy(true);
    setError(null);

    try {
      const restored = await restore(selectedIds, dryRun);
      setOutcome({
        title: dryRun ? 'Пробное восстановление' : 'Восстановление',
        result: {
          requested: selectedIds.length,
          succeeded: restored,
          failed: selectedIds.length - restored,
          errors: [],
        },
      });
      if (!dryRun) await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app">
      <div className="toolbar">
        <h1>Хранилище медиа</h1>
        <input
          type="search"
          placeholder="Поиск по пути или имени"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        <label className="stat">
          <input
            type="checkbox"
            checked={showDeleted}
            onChange={(event) => setShowDeleted(event.target.checked)}
          />{' '}
          показывать удалённые
        </label>
        <button onClick={runIndex} disabled={busy}>
          Индексировать
        </button>
        <button
          className="primary"
          onClick={() => {
            setPendingFiles([]);
            setDialog('upload');
          }}
          disabled={busy}
        >
          Загрузить файлы
        </button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          hidden
          onChange={(event) => {
            addFiles(event.target.files);
            event.target.value = '';
          }}
        />
        <div className="spacer" />
        <span className="stat">
          {totals.count} файлов · {formatBytes(totals.size)}
        </span>
      </div>

      {dragging && (
        <div
          className="dropzone active"
          onDragOver={(event) => event.preventDefault()}
          onDragLeave={() => setDragging(false)}
          onDrop={(event) => {
            event.preventDefault();
            setDragging(false);
            addFiles(event.dataTransfer.files);
          }}
        >
          Отпустите файлы, чтобы загрузить их в хранилище
        </div>
      )}

      {selected.size > 0 && (
        <div className="bulk-bar">
          <strong>Выбрано: {selected.size}</strong>
          <label>
            <input
              type="checkbox"
              checked={dryRun}
              onChange={(event) => setDryRun(event.target.checked)}
            />
            пробный прогон (dry-run)
          </label>
          <button onClick={() => setDialog('move')} disabled={busy}>
            Перенести
          </button>
          <button className="danger" onClick={() => setDialog('delete')} disabled={busy}>
            Удалить в корзину
          </button>
          <button onClick={runRestore} disabled={busy}>
            Восстановить
          </button>
          <button onClick={() => setSelected(new Set())}>Снять выбор</button>
        </div>
      )}

      {notice && <div className="status ok">{notice}</div>}
      {error && <div className="status error">{error}</div>}

      {outcome && (
        <div className="status">
          {outcome.title}: обработано {outcome.result.succeeded} из{' '}
          {outcome.result.requested}, пропущено {outcome.result.failed}
          {outcome.result.errors.length > 0 && (
            <div className="result-errors">
              <ul>
                {outcome.result.errors.map((message) => (
                  <li key={message}>{message}</li>
                ))}
              </ul>
            </div>
          )}
          <button onClick={() => setOutcome(null)}>Скрыть</button>
        </div>
      )}

      <div className="table-head">
        <input type="checkbox" checked={allSelected} onChange={toggleAll} />
        <span>Тип</span>
        <span>Имя</span>
        <span>Путь</span>
        <span className="size">Размер</span>
        <span>Загружен</span>
        <span>Выгружен</span>
      </div>

      <div
        className="scroller"
        ref={scrollerRef}
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={(event) => {
          // Убираем подсветку только когда курсор действительно покинул
          // область: dragleave срабатывает и на переход между дочерними
          // элементами, и без проверки мигание было бы постоянным.
          if (!event.currentTarget.contains(event.relatedTarget as Node)) {
            setDragging(false);
          }
        }}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          addFiles(event.dataTransfer.files);
        }}
      >
        {loading && <div className="empty">Загрузка…</div>}
        {!loading && filtered.length === 0 && (
          <div className="empty">
            Ничего не найдено. Нажмите «Индексировать», чтобы зарегистрировать
            файлы в хранилище.
          </div>
        )}

        {!loading &&
          visibleRows.map((entry, index) => (
            <div
              key={entry.id}
              className={[
                'row',
                selected.has(entry.id) ? 'selected' : '',
                entry.isDeleted ? 'deleted' : '',
              ]
                .filter(Boolean)
                .join(' ')}
              style={{ transform: `translateY(${(window_.first + index) * ROW_HEIGHT}px)` }}
            >
              <input
                type="checkbox"
                checked={selected.has(entry.id)}
                onChange={() => toggleOne(entry.id)}
              />
              <span className={`tag ${typeClass(entry.mediaType)}`}>
                {typeName(entry.mediaType)}
              </span>
              <span className="name">
                {isImage(entry.mediaType) && (
                  <img className="preview" src={fileUrl(entry.path)} alt="" loading="lazy" />
                )}
                <span className="file-name" title={entry.fileName}>
                  {entry.fileName}
                  {isPlayable(entry.mediaType) && (
                    <a
                      href={fileUrl(entry.path)}
                      target="_blank"
                      rel="noreferrer"
                      title="Открыть"
                    >
                      ↗
                    </a>
                  )}
                </span>
              </span>
              <span className="path" title={entry.originalPath ?? entry.path}>
                {entry.isDeleted ? entry.originalPath : entry.path}
              </span>
              <span className="size">{formatBytes(entry.sizeBytes)}</span>
              <span className="date">{formatDate(entry.uploadedAt)}</span>
              <span className="date">{formatDate(entry.lastDownloadedAt)}</span>
            </div>
          ))}
      </div>

      {dialog === 'move' && (
        <div className="dialog-backdrop" onClick={() => setDialog(null)}>
          <div className="dialog" onClick={(event) => event.stopPropagation()}>
            <h2>
              Перенос {selectedIds.length} файлов в {dryRun ? '(пробный прогон)' : ''}
            </h2>
            <div className="field">
              <label htmlFor="target">Каталог назначения (относительно wwwroot)</label>
              <input
                id="target"
                type="text"
                value={targetDirectory}
                onChange={(event) => setTargetDirectory(event.target.value)}
              />
            </div>
            <div className="actions">
              <button onClick={() => setDialog(null)}>Отмена</button>
              <button className="primary" onClick={runMove} disabled={busy}>
                Перенести
              </button>
            </div>
          </div>
        </div>
      )}

      {dialog === 'upload' && (
        <div className="dialog-backdrop" onClick={() => setDialog(null)}>
          <div className="dialog" onClick={(event) => event.stopPropagation()}>
            <h2>Загрузка файлов в хранилище</h2>
            <div className="field">
              <label htmlFor="upload-dir">Каталог назначения (относительно wwwroot)</label>
              <input
                id="upload-dir"
                type="text"
                value={uploadDirectory}
                onChange={(event) => setUploadDirectory(event.target.value)}
              />
            </div>
            <div className="field">
              <label htmlFor="upload-files">Файлы</label>
              <input
                id="upload-files"
                type="file"
                multiple
                onChange={(event) => {
                  // Список файлов читаем в локальную переменную: внутри
                  // замыкания TypeScript больше не сужает event.target.files,
                  // и Array.from() не проходит проверку типов.
                  const chosen = event.target.files;
                  if (chosen) {
                    const incoming = Array.from(chosen);
                    setPendingFiles((previous) => {
                      const merged = new Map(previous.map((f) => [f.name, f]));
                      for (const f of incoming) merged.set(f.name, f);
                      return [...merged.values()];
                    });
                  }
                  event.target.value = '';
                }}
              />
            </div>

            {pendingFiles.length === 0 ? (
              <p className="stat">Файлы не выбраны. Можно также перетащить их в таблицу.</p>
            ) : (
              <ul className="file-list">
                {pendingFiles.map((file) => (
                  <li key={file.name}>
                    <span className="file-name">{file.name}</span>
                    <span className="stat">{formatBytes(file.size)}</span>
                    <button
                      onClick={() =>
                        setPendingFiles((previous) =>
                          previous.filter((item) => item.name !== file.name),
                        )
                      }
                    >
                      ×
                    </button>
                  </li>
                ))}
              </ul>
            )}

            {progress && <div className="status">Загрузка: {progress}</div>}

            <div className="actions">
              <button onClick={() => setDialog(null)} disabled={busy}>
                Отмена
              </button>
              <button
                className="primary"
                onClick={runUpload}
                disabled={busy || pendingFiles.length === 0}
              >
                Загрузить {pendingFiles.length > 0 ? `(${pendingFiles.length})` : ''}
              </button>
            </div>
          </div>
        </div>
      )}

      {dialog === 'delete' && (
        <div className="dialog-backdrop" onClick={() => setDialog(null)}>
          <div className="dialog" onClick={(event) => event.stopPropagation()}>
            <h2>
              Удалить {selectedIds.length} файлов в корзину{' '}
              {dryRun ? '(пробный прогон)' : ''}
            </h2>
            <p>
              Файлы будут перемещены в <code>_trash/</code> внутри хранилища и
              останутся там 30 дней. {dryRun ? 'Пробный прогон ничего не меняет.' : 'После удаления их можно восстановить.'}
            </p>
            <div className="actions">
              <button onClick={() => setDialog(null)}>Отмена</button>
              <button className="danger" onClick={runDelete} disabled={busy}>
                Удалить
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
