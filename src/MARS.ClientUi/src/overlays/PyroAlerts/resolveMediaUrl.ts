/**
 * Склеивает корень приложения и локальный путь к файлу из MARS.MediaStorage.
 *
 * Маршруты MediaStorage возвращают путь с ведущим «/» (например
 * `/Alerts/uploaded_mems/a.png`), а корень приложения тоже задаётся со слешем
 * (`VITE_BASE_PATH=/`). Прямая конкатенация давала `//Alerts/...` —
 * протокол-относительный URL, который браузер резолвил в хост «Alerts», и
 * файл не грузился. Эта функция схлопывает двойной слеш на стыке и не трогает
 * внешние URL.
 */
export function resolveLocalMediaUrl(
  basePath: string | undefined,
  filePath: string
): string {
  if (/^https?:\/\//i.test(filePath) || filePath.startsWith("//")) {
    return filePath;
  }

  const base = (basePath ?? "").replace(/\/+$/, "");
  const path = filePath.startsWith("/") ? filePath : `/${filePath}`;

  return `${base}${path}`;
}