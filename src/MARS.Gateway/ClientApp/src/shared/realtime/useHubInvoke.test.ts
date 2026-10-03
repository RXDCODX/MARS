import { renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { FakeHubAdapter } from "./FakeHubAdapter";
import { setOverlayAdapter } from "./overlayHub";
import { resetReportedHubFailures, useHubInvoke } from "./useHubInvoke";

/**
 * Вызов клиентского метода хаба из компонента.
 *
 * Тест закрывает реальный отказ: методов `MuteAll`, `UnmuteSessions` и прочих у
 * хаба нет вообще — серверная часть не реализована, звать gRPC-сервис неоткуда.
 * Вызов отказывал, а вызывающий почти нигде не ждал промис, и каждый алерт
 * оставлял в консоли «Unhandled promise rejection».
 */
describe("вызов метода хаба из компонента", () => {
  beforeEach(() => {
    resetReportedHubFailures();
  });

  afterEach(() => {
    setOverlayAdapter(null);
    resetReportedHubFailures();
    vi.restoreAllMocks();
  });

  it("не отклоняет промис, когда хаб не подключён", async () => {
    setOverlayAdapter(null);

    const { result } = renderHook(() => useHubInvoke());

    // Оверлей — фон поверх видео: отсутствие канала не должно ронять компонент.
    await expect(result.current("MuteAll")).resolves.toBeUndefined();
  });

  it("не отклоняет промис, когда метода у хаба нет", async () => {
    const adapter = new FakeHubAdapter();
    vi.spyOn(adapter, "invoke").mockRejectedValue(
      new Error("Method does not exist")
    );
    setOverlayAdapter(adapter);

    const { result } = renderHook(() => useHubInvoke());

    await expect(result.current("MuteAll")).resolves.toBeUndefined();
  });

  it("сообщает о неудаче один раз на метод", async () => {
    const adapter = new FakeHubAdapter();
    vi.spyOn(adapter, "invoke").mockRejectedValue(
      new Error("Method does not exist")
    );
    setOverlayAdapter(adapter);

    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    const { result } = renderHook(() => useHubInvoke());

    await result.current("MuteAll");
    await result.current("MuteAll");
    await result.current("MuteAll");

    // Оверлей зовёт MuteAll на каждом алерте. Три вызова — одна запись,
    // иначе консоль засорялась бы быстрее, чем её читают.
    expect(warn).toHaveBeenCalledTimes(1);
  });

  it("не сообщает об успешном вызове", async () => {
    const adapter = new FakeHubAdapter();
    setOverlayAdapter(adapter);

    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    const { result } = renderHook(() => useHubInvoke());

    await result.current("TwitchMsg", "привет");

    expect(warn).not.toHaveBeenCalled();
    expect(adapter.sent).toEqual([{ method: "TwitchMsg", args: ["привет"] }]);
  });

  it("печатает уровнем ниже error, а не error", async () => {
    // E2E-проверка маршрутов падает на console.error. Неудачный вызов
    // нереализованной кнопки не должен ронять проверку маршрутов, но и молчать
    // нельзя — об этом узнают из консоли.
    const adapter = new FakeHubAdapter();
    vi.spyOn(adapter, "invoke").mockRejectedValue(new Error("nope"));
    setOverlayAdapter(adapter);

    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    const error = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    const { result } = renderHook(() => useHubInvoke());

    await result.current("MuteAll");

    expect(warn).toHaveBeenCalled();
    expect(error).not.toHaveBeenCalled();
  });
});
