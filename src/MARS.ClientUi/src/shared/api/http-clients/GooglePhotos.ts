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

export class GooglePhotos<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags GooglePhotos
   * @name GooglePhotosAuthorizeCreate
   * @request POST:/api/GooglePhotos/authorize
   * @response `200` `OperationResult<string>` OK
   */
  googlePhotosAuthorizeCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<string>, any>({
      path: `/api/GooglePhotos/authorize`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags GooglePhotos
   * @name GooglePhotosOauthCallbackList
   * @request GET:/api/GooglePhotos/oauth-callback
   * @response `200` `void` OK
   */
  googlePhotosOauthCallbackList = (
    query?: {
      code: string;
      error: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, any>({
      path: `/api/GooglePhotos/oauth-callback`,
      method: "GET",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags GooglePhotos
   * @name GooglePhotosStatusList
   * @request GET:/api/GooglePhotos/status
   * @response `200` `OperationResult<any>` OK
   */
  googlePhotosStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<any>, any>({
      path: `/api/GooglePhotos/status`,
      method: "GET",
      format: "json",
      ...params,
    });
}
