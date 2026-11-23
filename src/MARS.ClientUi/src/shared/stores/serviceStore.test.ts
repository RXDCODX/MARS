import axios from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { ServiceInfo } from "@/shared/api";

import { useServiceStore } from "./serviceStore";

/**
 * Адреса, по которым стор ходит в `MARS.Admin`.
 *
 * `VITE_API_BASE_URL` задан как `/` — это относительный корень, и в этом
 * выборе есть смысл: значение попадает в бандл и обязано работать на любом
 * origin за Gateway. Но конкатенация `API + "/api/..."` при `API === "/"`
 * даёт `//api/ServiceManager/services`, а браузер читает `//host/path` как
 * protocol-relative URL и отправляет запрос на хост `api`.
 *
 * На стенде такой хост не резолвится, и страница `/services/details` писала в
 * консоль `Failed to load resource: net::ERR_NAME_NOT_RESOLVED`. Навигационный
 * тест клиента проверяет консоль на ошибки и падал на этом маршруте в каждом
 * прогоне CI.
 *
 * Проверяется не «есть ли слеш», а точное равенство адреса: строка с
 * `//api` и строка с `/api` отличаются поведением в браузере, и проверка на
 * отсутствие подстроки пропустила бы, например, `https://host//api`.
 */
vi.mock("axios");

const mockedAxios = vi.mocked(axios);

describe("адреса ServiceManager в serviceStore", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useServiceStore.setState({
      services: [],
      error: null,
      selectedService: null,
      logs: [],
      logsService: null,
    });

    mockedAxios.get.mockResolvedValue({
      data: { success: true, data: [] },
    } as never);
    mockedAxios.post.mockResolvedValue({
      data: { success: true, message: "ok", data: null },
    } as never);
  });

  it("список сервисов запрашивается по корню сайта, а не по хосту «api»", async () => {
    await useServiceStore.getState().fetchServices();

    expect(mockedAxios.get).toHaveBeenCalledWith(
      "/api/ServiceManager/services"
    );
  });

  it("включение сервиса не собирает адрес с двойным слешем", async () => {
    // `toggle` ищет сервис в уже загруженном списке и на отсутствии молча
    // выходит: без предварительно засеянного сервиса адрес не собирался бы
    // вовсе, и проверка прошла бы, ничего не доказав.
    useServiceStore.setState({
      services: [
        { name: "twitch-core", isEnabled: false } as unknown as ServiceInfo,
      ],
    });

    await useServiceStore.getState().handleAction("twitch-core", "toggle");

    expect(mockedAxios.post).toHaveBeenCalledWith(
      "/api/ServiceManager/service/twitch-core/active",
      expect.anything(),
      expect.anything()
    );
  });

  it("запуск и остановка сервиса не собирают адрес с двойным слешем", async () => {
    await useServiceStore.getState().handleAction("twitch-core", "restart");

    expect(mockedAxios.post).toHaveBeenCalledWith(
      "/api/ServiceManager/service/twitch-core/restart"
    );
  });

  it("логи сервиса запрашиваются по корню сайта", async () => {
    await useServiceStore.getState().handleShowLogs("twitch-core");

    expect(mockedAxios.get).toHaveBeenCalledWith(
      "/api/ServiceManager/service/twitch-core/logs"
    );
  });
});