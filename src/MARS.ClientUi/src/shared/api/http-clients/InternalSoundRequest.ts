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

export class InternalSoundRequest<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags InternalSoundRequest
   * @name InternalSoundRequestMuteCreate
   * @request POST:/api/internal/sound-request/mute
   * @response `200` `OperationResult` OK
   */
  internalSoundRequestMuteCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/internal/sound-request/mute`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags InternalSoundRequest
   * @name InternalSoundRequestUnmuteCreate
   * @request POST:/api/internal/sound-request/unmute
   * @response `200` `OperationResult` OK
   */
  internalSoundRequestUnmuteCreate = (params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/internal/sound-request/unmute`,
      method: "POST",
      format: "json",
      ...params,
    });
}
