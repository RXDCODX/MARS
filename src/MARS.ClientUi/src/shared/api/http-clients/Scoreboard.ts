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

import type { OperationResult, ScoreboardDto } from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class Scoreboard<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Scoreboard
   * @name ScoreboardList
   * @request GET:/api/Scoreboard
   * @response `200` `OperationResult<ScoreboardDto>` OK
   */
  scoreboardList = (params: RequestParams = {}) =>
    this.request<OperationResult<ScoreboardDto>, any>({
      path: `/api/Scoreboard`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Scoreboard
   * @name ScoreboardUpdate
   * @request PUT:/api/Scoreboard
   * @response `200` `OperationResult` OK
   */
  scoreboardUpdate = (data: ScoreboardDto, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/Scoreboard`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Scoreboard
   * @name ScoreboardVisibilityPartialUpdate
   * @request PATCH:/api/Scoreboard/visibility
   * @response `200` `OperationResult` OK
   */
  scoreboardVisibilityPartialUpdate = (
    data: boolean,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/Scoreboard/visibility`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Scoreboard
   * @name ScoreboardPlayerScorePartialUpdate
   * @request PATCH:/api/Scoreboard/player/{position}/score
   * @response `200` `OperationResult` OK
   */
  scoreboardPlayerScorePartialUpdate = (
    position: number,
    data: number,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/Scoreboard/player/${position}/score`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Scoreboard
   * @name ScoreboardPlayerFinalPartialUpdate
   * @request PATCH:/api/Scoreboard/player/{position}/final
   * @response `200` `OperationResult` OK
   */
  scoreboardPlayerFinalPartialUpdate = (
    position: number,
    data: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/Scoreboard/player/${position}/final`,
      method: "PATCH",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
