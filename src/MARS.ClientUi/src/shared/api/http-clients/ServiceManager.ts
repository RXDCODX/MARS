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

export class ServiceManager<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerStatusList
   * @request GET:/api/ServiceManager/status
   * @response `200` `StringServiceStatusDictionaryOperationResult` OK
   */
  serviceManagerStatusList = (params: RequestParams = {}) =>
    this.request<StringServiceStatusDictionaryOperationResult, any>({
      path: `/api/ServiceManager/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceDetail
   * @request GET:/api/ServiceManager/service/{serviceName}
   * @response `200` `ServiceInfoOperationResult` OK
   */
  serviceManagerServiceDetail = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<ServiceInfoOperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceStartCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/start
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceStartCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/start`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceStopCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/stop
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceStopCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/stop`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceRestartCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/restart
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceRestartCreate = (
    serviceName: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/restart`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceLogsList
   * @request GET:/api/ServiceManager/service/{serviceName}/logs
   * @response `200` `ServiceLogIEnumerableOperationResult` OK
   */
  serviceManagerServiceLogsList = (
    serviceName: string,
    query?: {
      /**
       * @format int32
       * @default 100
       */
      count: number;
    },
    params: RequestParams = {}
  ) =>
    this.request<ServiceLogIEnumerableOperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/logs`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServiceActiveCreate
   * @request POST:/api/ServiceManager/service/{serviceName}/active
   * @response `200` `OperationResult` OK
   */
  serviceManagerServiceActiveCreate = (
    serviceName: string,
    data: boolean,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/ServiceManager/service/${serviceName}/active`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceManager
   * @name ServiceManagerServicesList
   * @request GET:/api/ServiceManager/services
   * @response `200` `ServiceInfoIEnumerableOperationResult` OK
   */
  serviceManagerServicesList = (params: RequestParams = {}) =>
    this.request<ServiceInfoIEnumerableOperationResult, any>({
      path: `/api/ServiceManager/services`,
      method: "GET",
      format: "json",
      ...params,
    });
}
