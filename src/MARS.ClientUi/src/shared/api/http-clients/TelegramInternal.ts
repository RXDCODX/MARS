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

import type {
  OperationResult,
  TelegramAdminMessageRequest,
  TelegramLogEntryRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class TelegramInternal<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TelegramInternal
   * @name InternalTelegramAdminMessagesCreate
   * @request POST:/api/internal/telegram/admin-messages
   * @response `200` `OperationResult<TelegramAdminMessageResult>` OK
   */
  internalTelegramAdminMessagesCreate = (
    data: TelegramAdminMessageRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TelegramAdminMessageResult>, any>({
      path: `/api/internal/telegram/admin-messages`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramInternal
   * @name InternalTelegramLogEntriesCreate
   * @request POST:/api/internal/telegram/log-entries
   * @response `200` `OperationResult<TelegramLogEntryResult>` OK
   */
  internalTelegramLogEntriesCreate = (
    data: TelegramLogEntryRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TelegramLogEntryResult>, any>({
      path: `/api/internal/telegram/log-entries`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
