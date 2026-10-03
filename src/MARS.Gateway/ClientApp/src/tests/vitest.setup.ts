import { vi } from "vitest";

// Global mocks to avoid loading large API/http-client modules during unit tests
vi.mock("@/shared/api", () => ({
  SoundRequest: function () {
    return {
      soundRequestQueueReorderCreate: async (_: any) => ({
        data: { success: true },
      }),
    };
  },
  // export any types used at runtime as undefined/mocks
}));

vi.mock("@/shared/api/api-config", () => ({
  defaultApiConfig: { baseURL: "" },
}));
