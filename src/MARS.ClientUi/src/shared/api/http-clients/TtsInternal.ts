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
  TtsMuteStateRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class TtsInternal<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TtsInternal
   * @name InternalTtsMuteStateCreate
   * @request POST:/api/internal/tts/mute-state
   * @response `200` `OperationResult` OK
   */
  internalTtsMuteStateCreate = (
    data: TtsMuteStateRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/internal/tts/mute-state`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
