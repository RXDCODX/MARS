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
  SetEnvironmentVariableRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class EnvironmentVariable<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags EnvironmentVariable
   * @name EnvironmentVariableList
   * @request GET:/api/EnvironmentVariable
   * @response `200` `EnvironmentVariableListOperationResult` OK
   */
  environmentVariableList = (params: RequestParams = {}) =>
    this.request<EnvironmentVariableListOperationResult, any>({
      path: `/api/EnvironmentVariable`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EnvironmentVariable
   * @name EnvironmentVariableCreate
   * @request POST:/api/EnvironmentVariable
   * @response `200` `OperationResult` OK
   */
  environmentVariableCreate = (
    data: SetEnvironmentVariableRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/EnvironmentVariable`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EnvironmentVariable
   * @name EnvironmentVariableDetail
   * @request GET:/api/EnvironmentVariable/{key}
   * @response `200` `EnvironmentVariableOperationResult` OK
   */
  environmentVariableDetail = (key: string, params: RequestParams = {}) =>
    this.request<EnvironmentVariableOperationResult, any>({
      path: `/api/EnvironmentVariable/${key}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EnvironmentVariable
   * @name EnvironmentVariableDelete
   * @request DELETE:/api/EnvironmentVariable/{key}
   * @response `200` `OperationResult` OK
   */
  environmentVariableDelete = (key: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/EnvironmentVariable/${key}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags EnvironmentVariable
   * @name EnvironmentVariableReloadCreate
   * @request POST:/api/EnvironmentVariable/reload
   * @response `200` `OperationResult` OK
   */
  environmentVariableReloadCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/EnvironmentVariable/reload`,
      method: "POST",
      format: "json",
      ...params,
    });
}
