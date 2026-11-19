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
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class MediaInfoApi<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiList
   * @request GET:/api/MediaInfoApi
   * @response `200` `OperationResult<ApiMediaInfo[]>` OK
   */
  mediaInfoApiList = (params: RequestParams = {}) =>
    this.request<OperationResult<ApiMediaInfo[]>, any>({
      path: `/api/MediaInfoApi`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiCreate
   * @request POST:/api/MediaInfoApi
   * @response `200` `OperationResult<ApiMediaInfo>` OK
   */
  mediaInfoApiCreate = (
    data: {
      AlertJson: string;
      /** @format binary */
      File: File;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ApiMediaInfo>, any>({
      path: `/api/MediaInfoApi`,
      method: "POST",
      body: data,
      type: ContentType.FormData,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiDetail
   * @request GET:/api/MediaInfoApi/{id}
   * @response `200` `OperationResult<ApiMediaInfo>` OK
   */
  mediaInfoApiDetail = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult<ApiMediaInfo>, any>({
      path: `/api/MediaInfoApi/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiUpdate
   * @request PUT:/api/MediaInfoApi/{id}
   * @response `200` `OperationResult<ApiMediaInfo>` OK
   */
  mediaInfoApiUpdate = (
    id: string,
    data: {
      AlertJson: string;
      /** @format binary */
      File: File;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<ApiMediaInfo>, any>({
      path: `/api/MediaInfoApi/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.FormData,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiDelete
   * @request DELETE:/api/MediaInfoApi/{id}
   * @response `200` `OperationResult` OK
   */
  mediaInfoApiDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/MediaInfoApi/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaInfoApi
   * @name MediaInfoApiFileList
   * @request GET:/api/MediaInfoApi/{id}/file
   * @response `200` `void` OK
   */
  mediaInfoApiFileList = (id: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/MediaInfoApi/${id}/file`,
      method: "GET",
      ...params,
    });
}
