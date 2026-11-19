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
  RootState,
  UpdateValueRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class RootState<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags RootState
   * @name RootStateList
   * @request GET:/api/RootState
   * @response `200` `RootStateListOperationResult` OK
   */
  rootStateList = (params: RequestParams = {}) =>
    this.request<RootStateListOperationResult, any>({
      path: `/api/RootState`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RootState
   * @name RootStateCreate
   * @request POST:/api/RootState
   * @response `200` `RootStateOperationResult` OK
   */
  rootStateCreate = (data: RootState, params: RequestParams = {}) =>
    this.request<RootStateOperationResult, any>({
      path: `/api/RootState`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RootState
   * @name RootStateDetail
   * @request GET:/api/RootState/{name}
   * @response `200` `RootStateOperationResult` OK
   */
  rootStateDetail = (name: string, params: RequestParams = {}) =>
    this.request<RootStateOperationResult, any>({
      path: `/api/RootState/${name}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RootState
   * @name RootStateDelete
   * @request DELETE:/api/RootState/{name}
   * @response `200` `OperationResult` OK
   */
  rootStateDelete = (name: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/RootState/${name}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags RootState
   * @name RootStateValuePartialUpdate
   * @request PATCH:/api/RootState/{name}/value
   * @response `200` `RootStateOperationResult` OK
   */
  rootStateValuePartialUpdate = (
    name: string,
    data: UpdateValueRequest,
    params: RequestParams = {}
  ) =>
    this.request<RootStateOperationResult, any>({
      path: `/api/RootState/${name}/value`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
