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
  BulkMoveRequest,
  BulkRestoreRequest,
  BulkStorageRequest,
  MediaGitSyncRequest,
  OperationResult,
  ProblemDetails,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class MediaStorage<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageEntriesList
   * @request GET:/api/MediaStorage/entries
   * @response `200` `OperationResult<MediaStorageEntryDto[]>` OK
   */
  mediaStorageEntriesList = (
    query?: {
      /** @default false */
      includeDeleted: boolean;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaStorageEntryDto[]>, any>({
      path: `/api/MediaStorage/entries`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageSearchList
   * @request GET:/api/MediaStorage/search
   * @response `200` `OperationResult<MediaStorageSearchResultDto>` OK
   */
  mediaStorageSearchList = (
    query?: {
      q: string;
      /**
       * @format int32
       * @default 1
       */
      page: number;
      /**
       * @format int32
       * @default 50
       */
      pageSize: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaStorageSearchResultDto>, any>({
      path: `/api/MediaStorage/search`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageGitStatusList
   * @request GET:/api/MediaStorage/git/status
   * @response `200` `OperationResult<GitStatusResult>` OK
   */
  mediaStorageGitStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<GitStatusResult>, any>({
      path: `/api/MediaStorage/git/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageIndexCreate
   * @request POST:/api/MediaStorage/index
   * @response `200` `OperationResult<IndexResultDto>` OK
   */
  mediaStorageIndexCreate = (params: RequestParams = {}) =>
    this.request<OperationResult<IndexResultDto>, any>({
      path: `/api/MediaStorage/index`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageDeleteCreate
   * @request POST:/api/MediaStorage/delete
   * @response `200` `OperationResult<BulkOperationResultDto>` OK
   */
  mediaStorageDeleteCreate = (
    data: BulkStorageRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BulkOperationResultDto>, any>({
      path: `/api/MediaStorage/delete`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageFileDelete
   * @request DELETE:/api/MediaStorage/file
   * @response `200` `OperationResult<BulkOperationResultDto>` OK
   */
  mediaStorageFileDelete = (
    query?: {
      path: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BulkOperationResultDto>, any>({
      path: `/api/MediaStorage/file`,
      method: "DELETE",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageFileList
   * @request GET:/api/MediaStorage/file
   * @response `200` `void` OK
   * @response `400` `ProblemDetails` Bad Request
   * @response `404` `ProblemDetails` Not Found
   */
  mediaStorageFileList = (
    query?: {
      path: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, ProblemDetails>({
      path: `/api/MediaStorage/file`,
      method: "GET",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageReplaceCreate
   * @request POST:/api/MediaStorage/replace
   * @response `200` `OperationResult<BulkOperationResultDto>` OK
   */
  mediaStorageReplaceCreate = (
    data: {
      path: string;
      /** @format binary */
      file: File;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BulkOperationResultDto>, any>({
      path: `/api/MediaStorage/replace`,
      method: "POST",
      body: data,
      type: ContentType.FormData,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageRestoreCreate
   * @request POST:/api/MediaStorage/restore
   * @response `200` `OperationResult<number>` OK
   */
  mediaStorageRestoreCreate = (
    data: BulkRestoreRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<number>, any>({
      path: `/api/MediaStorage/restore`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageUploadCreate
   * @request POST:/api/MediaStorage/upload
   * @response `200` `OperationResult<BulkOperationResultDto>` OK
   */
  mediaStorageUploadCreate = (
    data: {
      files: File[];
      targetDirectory: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BulkOperationResultDto>, any>({
      path: `/api/MediaStorage/upload`,
      method: "POST",
      body: data,
      type: ContentType.FormData,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageMoveCreate
   * @request POST:/api/MediaStorage/move
   * @response `200` `OperationResult<BulkOperationResultDto>` OK
   */
  mediaStorageMoveCreate = (
    data: BulkMoveRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BulkOperationResultDto>, any>({
      path: `/api/MediaStorage/move`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageSyncCreate
   * @request POST:/api/MediaStorage/sync
   * @response `200` `OperationResult<MediaGitSyncResultDto>` OK
   */
  mediaStorageSyncCreate = (
    data: MediaGitSyncRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaGitSyncResultDto>, any>({
      path: `/api/MediaStorage/sync`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageBrowseList
   * @request GET:/api/MediaStorage/browse
   * @response `200` `OperationResult<MediaStorageBrowseResult>` OK
   * @response `400` `ProblemDetails` Bad Request
   * @response `404` `ProblemDetails` Not Found
   */
  mediaStorageBrowseList = (
    query?: {
      directory: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaStorageBrowseResult>, ProblemDetails>({
      path: `/api/MediaStorage/browse`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStoragePreviewList
   * @request GET:/api/MediaStorage/preview
   * @response `200` `void` OK
   * @response `400` `ProblemDetails` Bad Request
   * @response `404` `ProblemDetails` Not Found
   * @response `415` `ProblemDetails` Unsupported Media Type
   */
  mediaStoragePreviewList = (
    query?: {
      path: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, ProblemDetails>({
      path: `/api/MediaStorage/preview`,
      method: "GET",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags MediaStorage
   * @name MediaStorageTextPreviewList
   * @request GET:/api/MediaStorage/text-preview
   * @response `200` `OperationResult<MediaStorageTextPreviewDto>` OK
   * @response `400` `ProblemDetails` Bad Request
   * @response `404` `ProblemDetails` Not Found
   * @response `415` `ProblemDetails` Unsupported Media Type
   */
  mediaStorageTextPreviewList = (
    query?: {
      path: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<MediaStorageTextPreviewDto>, ProblemDetails>({
      path: `/api/MediaStorage/text-preview`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
}
