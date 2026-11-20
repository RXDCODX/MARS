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

export class TwitchAuth<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TwitchAuth
   * @name TwitchAuthUserAuthList
   * @request GET:/api/TwitchAuth/user-auth
   * @response `200` `OperationResult<any>` OK
   */
  twitchAuthUserAuthList = (
    query?: {
      code: string;
      error: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<any>, any>({
      path: `/api/TwitchAuth/user-auth`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });

  /**
   * No description
   *
   * @tags TwitchAuth
   * @name TwitchAuthStatusList
   * @request GET:/api/TwitchAuth/status
   * @response `200` `OperationResult<TwitchAuthStatusResponse>` OK
   */
  twitchAuthStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<TwitchAuthStatusResponse>, any>({
      path: `/api/TwitchAuth/status`,
      method: "GET",
      format: "json",
      ...params,
    });

  /**
   * No description
   *
   * @tags TwitchAuth
   * @name TwitchAuthReconnectCreate
   * @request POST:/api/TwitchAuth/reconnect
   * @response `200` `OperationResult<string>` OK
   */
  twitchAuthReconnectCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<string>, any>({
      path: `/api/TwitchAuth/reconnect`,
      method: "POST",
      format: "json",
      ...params,
    });

  /**
   * No description
   *
   * @tags TwitchAuth
   * @name TwitchAuthAuthorizeUrlList
   * @request GET:/api/TwitchAuth/authorize-url
   * @response `200` `OperationResult<string>` OK
   */
  twitchAuthAuthorizeUrlList = (params: RequestParams = {}) =>
    this.request<OperationResult<string>, any>({
      path: `/api/TwitchAuth/authorize-url`,
      method: "GET",
      format: "json",
      ...params,
    });
}
