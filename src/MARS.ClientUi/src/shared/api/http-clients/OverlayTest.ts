import type { OperationResult } from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export interface OverlayTestEventRequest {
  eventName: string;
  payloadJson?: string | null;
}

export class OverlayTest<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * Отправить тестовое событие в оверлейный хаб.
   * @request POST:/api/overlay-test/event
   */
  overlayTestEventCreate = (
    data: OverlayTestEventRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<string>, any>({
      path: `/api/overlay-test/event`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
