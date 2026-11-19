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

export class RxdcodxViewers<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags RxdcodxViewers
   * @name RxdcodxViewersAllList
   * @request GET:/api/RxdcodxViewers/all
   * @response `200` `OperationResult<FollowerInfo[]>` OK
   */
  rxdcodxViewersAllList = (
    query?: {
      forceUseCash: boolean;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<FollowerInfo[]>, any>({
      path: `/api/RxdcodxViewers/all`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RxdcodxViewers
   * @name RxdcodxViewersWithoutAvatarsList
   * @request GET:/api/RxdcodxViewers/without-avatars
   * @response `200` `OperationResult<FollowerInfo[]>` OK
   */
  rxdcodxViewersWithoutAvatarsList = (params: RequestParams = {}) =>
    this.request<OperationResult<FollowerInfo[]>, any>({
      path: `/api/RxdcodxViewers/without-avatars`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RxdcodxViewers
   * @name RxdcodxViewersWithoutAvatarsCountList
   * @request GET:/api/RxdcodxViewers/without-avatars/count
   * @response `200` `OperationResult<number>` OK
   */
  rxdcodxViewersWithoutAvatarsCountList = (params: RequestParams = {}) =>
    this.request<OperationResult<number>, any>({
      path: `/api/RxdcodxViewers/without-avatars/count`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RxdcodxViewers
   * @name RxdcodxViewersUpdateAvatarsCreate
   * @request POST:/api/RxdcodxViewers/update-avatars
   * @response `200` `OperationResult<number>` OK
   */
  rxdcodxViewersUpdateAvatarsCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<number>, any>({
      path: `/api/RxdcodxViewers/update-avatars`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RxdcodxViewers
   * @name RxdcodxViewersDebugAvatarsList
   * @request GET:/api/RxdcodxViewers/debug/avatars
   * @response `200` `OperationResult<RxdcodxAvatarsDebugDto>` OK
   */
  rxdcodxViewersDebugAvatarsList = (params: RequestParams = {}) =>
    this.request<OperationResult<RxdcodxAvatarsDebugDto>, any>({
      path: `/api/RxdcodxViewers/debug/avatars`,
      method: "GET",
      format: "json",
      ...params,
    });
}
