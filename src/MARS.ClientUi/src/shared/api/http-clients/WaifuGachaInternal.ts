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
  AutoHelloRequest,
  OperationResult,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class WaifuGachaInternal<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalHusbandsWaifuNameList
   * @request GET:/api/internal/husbands/{twitchId}/waifu-name
   * @response `200` `OperationResult<string>` OK
   */
  internalHusbandsWaifuNameList = (
    twitchId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<string>, any>({
      path: `/api/internal/husbands/${twitchId}/waifu-name`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalAutoHelloCreate
   * @request POST:/api/internal/auto-hello/{twitchId}
   * @response `200` `OperationResult<string>` OK
   */
  internalAutoHelloCreate = (
    twitchId: string,
    data: AutoHelloRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<string>, any>({
      path: `/api/internal/auto-hello/${twitchId}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalAutoHelloToggleCreate
   * @request POST:/api/internal/auto-hello/{twitchId}/toggle
   * @response `200` `OperationResult<boolean>` OK
   */
  internalAutoHelloToggleCreate = (
    twitchId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<boolean>, any>({
      path: `/api/internal/auto-hello/${twitchId}/toggle`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalCollectionsFumoList
   * @request GET:/api/internal/collections/fumo
   * @response `200` `OperationResult<CollectionInventory>` OK
   */
  internalCollectionsFumoList = (
    query?: {
      twitchId: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CollectionInventory>, any>({
      path: `/api/internal/collections/fumo`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalCollectionsMikuList
   * @request GET:/api/internal/collections/miku
   * @response `200` `OperationResult<CollectionInventory>` OK
   */
  internalCollectionsMikuList = (
    query?: {
      twitchId: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CollectionInventory>, any>({
      path: `/api/internal/collections/miku`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalShikimoriRandomAnimeList
   * @request GET:/api/internal/shikimori/random-anime
   * @response `200` `OperationResult<ShikimoriTitleRef>` OK
   */
  internalShikimoriRandomAnimeList = (params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriTitleRef>, any>({
      path: `/api/internal/shikimori/random-anime`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuGachaInternal
   * @name InternalShikimoriRandomMangaList
   * @request GET:/api/internal/shikimori/random-manga
   * @response `200` `OperationResult<ShikimoriTitleRef>` OK
   */
  internalShikimoriRandomMangaList = (params: RequestParams = {}) =>
    this.request<OperationResult<ShikimoriTitleRef>, any>({
      path: `/api/internal/shikimori/random-manga`,
      method: "GET",
      format: "json",
      ...params,
    });
}
