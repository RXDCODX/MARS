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

export class Shikimori<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Shikimori
   * @name ShikimoriRandomAnimeList
   * @request GET:/api/Shikimori/random-anime
   * @response `200` `OperationResult<ShikimoriTitleRef>` OK
   */
  shikimoriRandomAnimeList = (params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriTitleRef>, any>({
      path: `/api/Shikimori/random-anime`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Shikimori
   * @name ShikimoriRandomMangaList
   * @request GET:/api/Shikimori/random-manga
   * @response `200` `OperationResult<ShikimoriTitleRef>` OK
   */
  shikimoriRandomMangaList = (params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriTitleRef>, any>({
      path: `/api/Shikimori/random-manga`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Shikimori
   * @name ShikimoriCharactersDetail
   * @request GET:/api/Shikimori/characters/{id}
   * @response `200` `OperationResult<ShikimoriCharacterRef>` OK
   */
  shikimoriCharactersDetail = (id: number, params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriCharacterRef>, any>({
      path: `/api/Shikimori/characters/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Shikimori
   * @name ShikimoriCharactersCachedList
   * @request GET:/api/Shikimori/characters/{id}/cached
   * @response `200` `OperationResult<ShikimoriCharacterRef>` OK
   */
  shikimoriCharactersCachedList = (id: number, params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriCharacterRef>, any>({
      path: `/api/Shikimori/characters/${id}/cached`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Shikimori
   * @name ShikimoriRateLimiterList
   * @request GET:/api/Shikimori/rate-limiter
   * @response `200` `OperationResult<ShikimoriRateLimiterInfo>` OK
   */
  shikimoriRateLimiterList = (params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriRateLimiterInfo>, any>({
      path: `/api/Shikimori/rate-limiter`,
      method: "GET",
      format: "json",
      ...params,
    });
}
