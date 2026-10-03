import { vi } from "vitest";

/**
 * Подмена одного клиента вместо всего модуля `@/shared/api`.
 *
 * Раньше здесь стоял сплошной `vi.mock` с одним экспортом `SoundRequest`.
 * Такой мок выбрасывает всё остальное содержимое модуля, и любой компонент,
 * импортирующий из `@/shared/api` хоть что-нибудь ещё, получал на тесте
 * «No export is defined on the mock». Из-за этого 59 тестов в четырёх файлах
 * падали до прихода сюда: smoke-покрытие OBS-компонентов, Header,
 * WelcomePage и useQueueActions.
 *
 * `importOriginal` возвращает настоящий модуль, а поверх него накладывается
 * единственная подмена. Исходная цель — не ходить в сеть из тестов —
 * сохраняется: `SoundRequest` по-прежнему заглушен.
 *
 * Сигналы и билдеры хабов из этого модуля при импорте конструируются, но
 * соединение не открывают: `HubConnectionBuilder.build()` не подключается к
 * сети, а адрес берётся из `VITE_BASE_PATH`, который задаёт корень.
 */
vi.mock("@/shared/api", async importOriginal => {
  const actual = await importOriginal<typeof import("@/shared/api")>();

  return {
    ...actual,
    SoundRequest: function () {
      return {
        soundRequestQueueReorderCreate: async (_: unknown) => ({
          data: { success: true },
        }),
      };
    },
  };
});

vi.mock("@/shared/api/api-config", () => ({
  defaultApiConfig: { baseURL: "" },
}));
