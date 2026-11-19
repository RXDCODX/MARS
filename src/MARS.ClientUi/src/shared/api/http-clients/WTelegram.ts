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
  VerificationCodeRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class WTelegram<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags WTelegram
   * @name WTelegramReloginCreate
   * @request POST:/api/WTelegram/relogin
   * @response `200` `WTelegramOperationResult` OK
   */
  wTelegramReloginCreate = (params: RequestParams = {}) =>
    this.request<WTelegramOperationResult, any>({
      path: `/api/WTelegram/relogin`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WTelegram
   * @name WTelegramStatusList
   * @request GET:/api/WTelegram/status
   * @response `200` `WTelegramOperationResult` OK
   */
  wTelegramStatusList = (params: RequestParams = {}) =>
    this.request<WTelegramOperationResult, any>({
      path: `/api/WTelegram/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WTelegram
   * @name WTelegramVerificationCodeCreate
   * @request POST:/api/WTelegram/verification-code
   * @response `200` `WTelegramOperationResult` OK
   */
  wTelegramVerificationCodeCreate = (
    data: VerificationCodeRequest,
    params: RequestParams = {}
  ) =>
    this.request<WTelegramOperationResult, any>({
      path: `/api/WTelegram/verification-code`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
