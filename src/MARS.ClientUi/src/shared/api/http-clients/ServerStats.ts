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

export class ServerStats<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags ServerStats
   * @name ServerStatsList
   * @request GET:/api/ServerStats
   * @response `200` `OperationResult<ServerStatsResponse>` OK
   */
  serverStatsList = (params: RequestParams = {}) =>
    this.request<OperationResult<ServerStatsResponse>, any>({
      path: `/api/ServerStats`,
      method: "GET",
      format: "json",
      ...params,
    });
}
