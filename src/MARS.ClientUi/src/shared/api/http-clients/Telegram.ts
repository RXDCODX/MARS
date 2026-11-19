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

export class Telegram<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Telegram
   * @name TelegramHealthList
   * @request GET:/api/Telegram/health
   * @response `200` `OperationResult<string>` OK
   */
  telegramHealthList = (params: RequestParams = {}) =>
    this.request<OperationResult<string>, any>({
      path: `/api/Telegram/health`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Telegram
   * @name TelegramUsersList
   * @request GET:/api/Telegram/users
   * @response `200` `OperationResult<number>` OK
   */
  telegramUsersList = (params: RequestParams = {}) =>
    this.request<OperationResult<number>, any>({
      path: `/api/Telegram/users`,
      method: "GET",
      format: "json",
      ...params,
    });
}
