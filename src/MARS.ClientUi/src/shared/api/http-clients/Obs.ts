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

import type { ObsPauseMode, OperationResult } from "../types/data-contracts";
import { HttpClient, RequestParams } from "./http-client";

export class Obs<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Obs
   * @name ObsStatusList
   * @request GET:/api/Obs/status
   * @response `200` `void` OK
   */
  obsStatusList = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/status`,
      method: "GET",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsConnectCreate
   * @request POST:/api/Obs/connect
   * @response `200` `void` OK
   */
  obsConnectCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/connect`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsDisconnectCreate
   * @request POST:/api/Obs/disconnect
   * @response `200` `void` OK
   */
  obsDisconnectCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/disconnect`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsScreenshotCreate
   * @request POST:/api/Obs/screenshot
   * @response `200` `void` OK
   */
  obsScreenshotCreate = (
    query?: {
      sourceName: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, any>({
      path: `/api/Obs/screenshot`,
      method: "POST",
      query: query,
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsFreezeCreate
   * @request POST:/api/Obs/freeze
   * @response `200` `void` OK
   */
  obsFreezeCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/freeze`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsUnfreezeCreate
   * @request POST:/api/Obs/unfreeze
   * @response `200` `void` OK
   */
  obsUnfreezeCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/unfreeze`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsPauseSceneCreate
   * @request POST:/api/Obs/pause-scene
   * @response `200` `void` OK
   */
  obsPauseSceneCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/pause-scene`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsUnpauseSceneCreate
   * @request POST:/api/Obs/unpause-scene
   * @response `200` `void` OK
   */
  obsUnpauseSceneCreate = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/Obs/unpause-scene`,
      method: "POST",
      ...params,
    });
  /**
   * No description
   *
   * @tags Obs
   * @name ObsToggleCreate
   * @request POST:/api/Obs/toggle
   * @response `200` `void` OK
   */
  obsToggleCreate = (
    query?: {
      mode: ObsPauseMode;
    },
    params: RequestParams = {}
  ) =>
    this.request<void, any>({
      path: `/api/Obs/toggle`,
      method: "POST",
      query: query,
      ...params,
    });
}
