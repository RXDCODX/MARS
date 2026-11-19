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

export class Leaderboard<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Leaderboard
   * @name LeaderboardTopList
   * @request GET:/api/leaderboard/top
   * @response `200` `OperationResult<LeaderboardTop>` OK
   */
  leaderboardTopList = (
    query?: {
      /**
       * @format int32
       * @default 3
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<LeaderboardTop>, any>({
      path: `/api/leaderboard/top`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Leaderboard
   * @name LeaderboardUserDetail
   * @request GET:/api/leaderboard/user/{twitchId}
   * @response `200` `OperationResult<LeaderboardStats>` OK
   */
  leaderboardUserDetail = (twitchId: string, params: RequestParams = {}) =>
    this.request<OperationResult<LeaderboardStats>, any>({
      path: `/api/leaderboard/user/${twitchId}`,
      method: "GET",
      format: "json",
      ...params,
    });
}
