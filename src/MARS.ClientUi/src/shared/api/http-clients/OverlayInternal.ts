/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import type { OperationResult } from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class OverlayInternal<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags OverlayInternal
   * @name InternalOverlayEventsCreate
   * @request POST:/api/internal/overlay/events
   * @response `200` `OperationResult<OverlayFireResult>` OK
   */
  internalOverlayEventsCreate = (data: any, params: RequestParams = {}) =>
    this.request<OperationResult<OverlayFireResult>, any>({
      path: `/api/internal/overlay/events`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
