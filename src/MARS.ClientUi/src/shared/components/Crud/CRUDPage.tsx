import { useCallback, useEffect, useState } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import { ConfirmDeleteModal, EditCreateModal } from "./CrudModals";
import {
  CrudPageProps as CrudPageProperties,
  ListParams as ListParameters,
  SortDirection,
} from "./types";

interface RowWithId {
  id?: string | number;
  Id?: string | number;
  key?: string | number;
  Key?: string | number;
}

export default function CRUDPage<
  T,
  TCreate = Partial<T>,
  TUpdate = Partial<T>,
  TFilter extends Record<string, unknown> = Record<string, unknown>,
>({
  title,
  dataSource,
  columns,
  formSchema,
  idAccessor,
  enableSearch = true,
  defaultSort,
  pageSizeOptions = [10, 20, 50],
  initialPageSize = 10,
  toolbar,
  rowActions,
  renderEmpty,
  filtersRenderer,
  onCreated,
  onUpdated,
  onDeleted,
}: CrudPageProperties<T, TCreate, TUpdate, TFilter>) {
  const { showToast } = useToastModal();
  const getId = useCallback(
    (row: T): string | number => {
      if (idAccessor) return idAccessor(row);
      const rowWithId = row as unknown as RowWithId;
      return (
        rowWithId.id ?? rowWithId.Id ?? rowWithId.key ?? rowWithId.Key ?? 0
      );
    },
    [idAccessor]
  );

  const [items, setItems] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [sortBy, setSortBy] = useState<string | undefined>(defaultSort?.field);
  const [sortDir, setSortDir] = useState<SortDirection | undefined>(
    defaultSort?.dir
  );
  const [search, setSearch] = useState("");
  const [filters, setFilters] = useState<TFilter | undefined>(undefined);

  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<T | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<T | null>(null);

  const load = useCallback(
    async (currentParameters: ListParameters<TFilter>) => {
      setLoading(true);
      try {
        const res = await dataSource.list(currentParameters);
        setItems(res.items);
        setTotal(res.total);
      } catch (error) {
        const errorMessage =
          error instanceof Error ? error.message : "Ошибка загрузки";
        showToast({
          success: false,
          message: errorMessage,
        });
      } finally {
        setLoading(false);
      }
    },
    [dataSource, showToast]
  );

  useEffect(() => {
    const parameters: ListParameters<TFilter> = {
      page,
      pageSize,
      sortBy,
      sortDir,
      search: enableSearch ? search : undefined,
      filters,
    };
    load(parameters);
  }, [load, page, pageSize, sortBy, sortDir, search, filters, enableSearch]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const handleCreate = async (values: Record<string, unknown>) => {
    const parameters: ListParameters<TFilter> = {
      page,
      pageSize,
      sortBy,
      sortDir,
      search: enableSearch ? search : undefined,
      filters,
    };
    const created = await dataSource.create(values as TCreate);
    onCreated?.(created);
    await load(parameters);
  };

  const handleUpdate = async (values: Record<string, unknown>) => {
    if (!editTarget) return;
    const parameters: ListParameters<TFilter> = {
      page,
      pageSize,
      sortBy,
      sortDir,
      search: enableSearch ? search : undefined,
      filters,
    };
    const id = getId(editTarget);
    const updated = await dataSource.update(id, values as TUpdate);
    onUpdated?.(updated);
    await load(parameters);
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const parameters: ListParameters<TFilter> = {
      page,
      pageSize,
      sortBy,
      sortDir,
      search: enableSearch ? search : undefined,
      filters,
    };
    const id = getId(deleteTarget);
    await dataSource.remove(id);
    onDeleted?.(id);
    await load(parameters);
  };

  const renderHeaderCell = (
    header: string,
    columnId: string,
    sortable?: boolean
  ) => {
    if (!sortable) return header;
    const isActive = sortBy === columnId;
    const dir = isActive ? (sortDir ?? "asc") : undefined;
    return (
      <button
        type="button"
        className="btn btn-link p-0 text-decoration-none"
        onClick={() => {
          if (isActive) {
            setSortDir(dir === "asc" ? "desc" : "asc");
          } else {
            setSortBy(columnId);
            setSortDir("asc");
          }
        }}
      >
        {header} {isActive ? (dir === "asc" ? "▲" : "▼") : null}
      </button>
    );
  };

  return (
    <div className="container-fluid py-3">
      {title ? <h2 className="mb-3 site-text-primary">{title}</h2> : null}

      <div className="d-flex align-items-center gap-2 mb-3">
        {enableSearch ? (
          <input
            className="form-control"
            style={{ maxWidth: 320 }}
            placeholder="Поиск..."
            value={search}
            onChange={e => {
              setPage(1);
              setSearch(e.target.value);
            }}
          />
        ) : null}
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
          Создать
        </button>
        <div className="ms-auto" />
        {toolbar}
      </div>

      {filtersRenderer ? (
        <div className="card mb-3">
          <div className="card-body">
            {filtersRenderer(filters, v => {
              setPage(1);
              setFilters(v);
            })}
          </div>
        </div>
      ) : null}

      <div className="table-responsive">
        <table className="table table-hover align-middle">
          <thead>
            <tr>
              {columns.map(c => (
                <th key={c.id} style={{ width: c.width }}>
                  {renderHeaderCell(c.header, c.id, c.sortable)}
                </th>
              ))}
              <th style={{ width: 1 }} />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length + 1}>Загрузка...</td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={columns.length + 1}>
                  {renderEmpty ?? "Нет данных"}
                </td>
              </tr>
            ) : (
              items.map(row => (
                <tr key={String(getId(row))}>
                  {columns.map(c => (
                    <td key={c.id}>
                      {c.render
                        ? c.render(row)
                        : typeof c.accessor === "function"
                          ? c.accessor(row)
                          : String(
                              (row as Record<string, unknown>)[
                                c.accessor as string
                              ]
                            )}
                    </td>
                  ))}
                  <td className="text-nowrap">
                    {rowActions ? (
                      rowActions(row)
                    ) : (
                      <div className="btn-group btn-group-sm">
                        <button
                          className="btn btn-outline-secondary"
                          onClick={() => setEditTarget(row)}
                        >
                          Изменить
                        </button>
                        <button
                          className="btn btn-outline-danger"
                          onClick={() => setDeleteTarget(row)}
                        >
                          Удалить
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="d-flex align-items-center gap-2">
        <div className="input-group" style={{ maxWidth: 160 }}>
          <span className="input-group-text">На странице</span>
          <select
            className="form-select"
            value={pageSize}
            onChange={e => {
              setPage(1);
              setPageSize(Number(e.target.value));
            }}
          >
            {pageSizeOptions.map(s => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>

        <div className="ms-auto d-flex align-items-center gap-2">
          <button
            className="btn btn-outline-secondary"
            disabled={page <= 1}
            onClick={() => setPage(1)}
          >
            «
          </button>
          <button
            className="btn btn-outline-secondary"
            disabled={page <= 1}
            onClick={() => setPage(p => Math.max(1, p - 1))}
          >
            ‹
          </button>
          <span>
            Стр. {page} / {totalPages}
          </span>
          <button
            className="btn btn-outline-secondary"
            disabled={page >= totalPages}
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
          >
            ›
          </button>
          <button
            className="btn btn-outline-secondary"
            disabled={page >= totalPages}
            onClick={() => setPage(totalPages)}
          >
            »
          </button>
        </div>
      </div>

      <EditCreateModal<T>
        title="Создать запись"
        show={showCreate}
        onClose={() => setShowCreate(false)}
        onSubmit={handleCreate}
        schema={formSchema}
        initialValues={{}}
      />

      <EditCreateModal<T>
        title="Изменить запись"
        show={!!editTarget}
        onClose={() => setEditTarget(null)}
        onSubmit={handleUpdate}
        schema={formSchema}
        initialValues={
          editTarget ? (editTarget as unknown as Record<string, unknown>) : {}
        }
      />

      <ConfirmDeleteModal
        show={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        text="Действительно удалить запись?"
      />
    </div>
  );
}
