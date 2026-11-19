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
import { HttpClient, RequestParams } from "./http-client";

export class TelegramClipboardCopy<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TelegramClipboardCopy
   * @name TelegramClipboardCopyDetail
   * @request GET:/api/TelegramClipboardCopy/{requestId}
   * @response `200` `OperationResult<string[]>` OK
   */
  telegramClipboardCopyDetail = (
    requestId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<string[]>, any>({
      path: `/api/TelegramClipboardCopy/${requestId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramClipboardCopy
   * @name TelegramClipboardCopyCompleteCreate
   * @request POST:/api/TelegramClipboardCopy/complete/{requestId}
   * @response `200` `OperationResult` OK
   */
  telegramClipboardCopyCompleteCreate = (
    requestId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/TelegramClipboardCopy/complete/${requestId}`,
      method: "POST",
      format: "json",
      ...params,
    });
}
