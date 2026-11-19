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
}
