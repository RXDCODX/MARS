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
  CreateMediaItemRequest,
  MediaStatus,
  OperationResult,
  UpdateMediaItemRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class CinemaQueue<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueList
   * @request GET:/api/CinemaQueue
   * @response `200` `OperationResult<CinemaMediaItemDto[]>` OK
   */
  cinemaQueueList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto[]>, any>({
      path: `/api/CinemaQueue`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueCreate
   * @request POST:/api/CinemaQueue
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueCreate = (
    data: CreateMediaItemRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueDetail
   * @request GET:/api/CinemaQueue/{id}
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueDetail = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueUpdate
   * @request PUT:/api/CinemaQueue/{id}
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueUpdate = (
    id: string,
    data: UpdateMediaItemRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueDelete
   * @request DELETE:/api/CinemaQueue/{id}
   * @response `200` `OperationResult` OK
   */
  cinemaQueueDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueNextList
   * @request GET:/api/CinemaQueue/next
   * @response `200` `OperationResult<CinemaMediaItemDto>` OK
   */
  cinemaQueueNextList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto>, any>({
      path: `/api/CinemaQueue/next`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatusDetail
   * @request GET:/api/CinemaQueue/status/{status}
   * @response `200` `OperationResult<CinemaMediaItemDto[]>` OK
   */
  cinemaQueueStatusDetail = (status: MediaStatus, params: RequestParams = {}) =>
    this.request<OperationResult<CinemaMediaItemDto[]>, any>({
      path: `/api/CinemaQueue/status/${status}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueMarkAsNextCreate
   * @request POST:/api/CinemaQueue/{id}/mark-as-next
   * @response `200` `OperationResult` OK
   */
  cinemaQueueMarkAsNextCreate = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/mark-as-next`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatusPartialUpdate
   * @request PATCH:/api/CinemaQueue/{id}/status
   * @response `200` `OperationResult` OK
   */
  cinemaQueueStatusPartialUpdate = (
    id: string,
    data: MediaStatus,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/status`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueuePriorityPartialUpdate
   * @request PATCH:/api/CinemaQueue/{id}/priority
   * @response `200` `OperationResult` OK
   */
  cinemaQueuePriorityPartialUpdate = (
    id: string,
    data: number,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/CinemaQueue/${id}/priority`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags CinemaQueue
   * @name CinemaQueueStatisticsList
   * @request GET:/api/CinemaQueue/statistics
   * @response `200` `OperationResult<CinemaQueueStatistics>` OK
   */
  cinemaQueueStatisticsList = (params: RequestParams = {}) =>
    this.request<OperationResult<CinemaQueueStatistics>, any>({
      path: `/api/CinemaQueue/statistics`,
      method: "GET",
      format: "json",
      ...params,
    });
}
