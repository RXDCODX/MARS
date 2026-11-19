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

export class Discord<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Discord
   * @name DiscordStatusList
   * @request GET:/api/Discord/status
   * @response `200` `OperationResult<any>` OK
   */
  discordStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<any>, any>({
      path: `/api/Discord/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Discord
   * @name DiscordSendCreate
   * @request POST:/api/Discord/send
   * @response `200` `OperationResult` OK
   */
  discordSendCreate = (
    query?: {
      /** @format int64 */
      channelId: number;
      message: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/Discord/send`,
      method: "POST",
      query: query,
      format: "json",
      ...params,
    });
}
