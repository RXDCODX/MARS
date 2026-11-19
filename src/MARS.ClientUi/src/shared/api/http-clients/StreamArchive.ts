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
  StreamArchiveConfig,
  ValidateFolderRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class StreamArchive<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveList
   * @request GET:/api/StreamArchive
   * @response `200` `StreamArchiveConfigListOperationResult` OK
   */
  streamArchiveList = (params: RequestParams = {}) =>
    this.request<StreamArchiveConfigListOperationResult, any>({
      path: `/api/StreamArchive`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveCreate
   * @request POST:/api/StreamArchive
   * @response `200` `StreamArchiveConfigOperationResult` OK
   */
  streamArchiveCreate = (
    data: StreamArchiveConfig,
    params: RequestParams = {}
  ) =>
    this.request<StreamArchiveConfigOperationResult, any>({
      path: `/api/StreamArchive`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveDetail
   * @request GET:/api/StreamArchive/{id}
   * @response `200` `StreamArchiveConfigOperationResult` OK
   */
  streamArchiveDetail = (id: string, params: RequestParams = {}) =>
    this.request<StreamArchiveConfigOperationResult, any>({
      path: `/api/StreamArchive/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveUpdate
   * @request PUT:/api/StreamArchive/{id}
   * @response `200` `OperationResult` OK
   */
  streamArchiveUpdate = (
    id: string,
    data: StreamArchiveConfig,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/StreamArchive/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveDelete
   * @request DELETE:/api/StreamArchive/{id}
   * @response `200` `OperationResult` OK
   */
  streamArchiveDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/StreamArchive/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveValidateFolderCreate
   * @request POST:/api/StreamArchive/validate-folder
   * @response `200` `ValidateFolderResponseOperationResult` OK
   */
  streamArchiveValidateFolderCreate = (
    data: ValidateFolderRequest,
    params: RequestParams = {}
  ) =>
    this.request<ValidateFolderResponseOperationResult, any>({
      path: `/api/StreamArchive/validate-folder`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveFilesList
   * @request GET:/api/StreamArchive/{configId}/files
   * @response `200` `OperationResult<any>` OK
   */
  streamArchiveFilesList = (configId: string, params: RequestParams = {}) =>
    this.request<OperationResult<any>, any>({
      path: `/api/StreamArchive/${configId}/files`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags StreamArchive
   * @name StreamArchiveStatisticsList
   * @request GET:/api/StreamArchive/statistics
   * @response `200` `OperationResult<any>` OK
   */
  streamArchiveStatisticsList = (params: RequestParams = {}) =>
    this.request<OperationResult<any>, any>({
      path: `/api/StreamArchive/statistics`,
      method: "GET",
      format: "json",
      ...params,
    });
}
