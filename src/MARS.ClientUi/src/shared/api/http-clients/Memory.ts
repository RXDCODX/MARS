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

import { HttpClient, RequestParams } from "./http-client";

export class Memory<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Memory
   * @name MemoryDetail
   * @request GET:/memory/{escapedFileName}
   * @response `200` `void` OK
   */
  memoryDetail = (escapedFileName: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/memory/${escapedFileName}`,
      method: "GET",
      ...params,
    });
}
