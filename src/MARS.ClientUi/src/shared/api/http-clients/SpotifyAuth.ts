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
  SpotifyAuthStartRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class SpotifyAuth<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthStartCreate
   * @request POST:/api/SpotifyAuth/start
   * @response `200` `OperationResult<SpotifyAuthStartResult>` OK
   */
  spotifyAuthStartCreate = (
    data: SpotifyAuthStartRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<SpotifyAuthStartResult>, any>({
      path: `/api/SpotifyAuth/start`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthCallbackList
   * @request GET:/api/SpotifyAuth/callback
   * @response `200` `OperationResult<SpotifyAuthCompleteResult>` OK
   */
  spotifyAuthCallbackList = (
    query?: {
      code: string;
      state: string;
      error: string;
      error_description: string;
      redirectUri: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<SpotifyAuthCompleteResult>, any>({
      path: `/api/SpotifyAuth/callback`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthStatusList
   * @request GET:/api/SpotifyAuth/status
   * @response `200` `OperationResult<SpotifyAuthStatusResult>` OK
   */
  spotifyAuthStatusList = (params: RequestParams = {}) =>
    this.request<OperationResult<SpotifyAuthStatusResult>, any>({
      path: `/api/SpotifyAuth/status`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags SpotifyAuth
   * @name SpotifyAuthDisconnectCreate
   * @request POST:/api/SpotifyAuth/disconnect
   * @response `200` `OperationResult` OK
   */
  spotifyAuthDisconnectCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/SpotifyAuth/disconnect`,
      method: "POST",
      format: "json",
      ...params,
    });
}
