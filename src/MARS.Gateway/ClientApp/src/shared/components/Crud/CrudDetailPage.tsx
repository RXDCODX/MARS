import { FormEvent, useCallback, useEffect, useState } from "react";

import { useToastModal } from "@/shared/Utils/ToastModal";

import FormFieldRenderer from "./FormFieldRenderer";
import { CrudDetailPageProps as CrudDetailPageProperties } from "./types";

export default function CrudDetailPage<T, TUpdate = Partial<T>>({
  title,
  id,
  dataSource,
  formSchema,
  onSaved,
}: CrudDetailPageProperties<T, TUpdate>) {
  const { showToast } = useToastModal();
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const entity = await dataSource.getById(id);
      setValues(entity as unknown as Record<string, unknown>);
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
  }, [dataSource, id, showToast]);

  useEffect(() => {
    load();
  }, [load]);

  const handleChange = (name: string, value: unknown) => {
    setValues(previous => ({ ...previous, [name]: value }));
  };

  const handleSave = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await dataSource.update(id, values as TUpdate);
      onSaved?.(updated);
      showToast({
        success: true,
        message: "Сохранено",
      });
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Ошибка сохранения";
      showToast({
        success: false,
        message: errorMessage,
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="container py-3">
      {title ? <h2 className="mb-3 site-text-primary">{title}</h2> : null}
      {loading ? (
        <div>Загрузка...</div>
      ) : (
        <form onSubmit={handleSave}>
          <div className="card mb-3">
            <div className="card-body">
              <FormFieldRenderer<T>
                schema={formSchema}
                values={values}
                onChange={handleChange}
              />
            </div>
          </div>
          <div className="d-flex gap-2">
            <button className="btn btn-primary" disabled={saving} type="submit">
              {saving ? "Сохранение..." : "Сохранить"}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
