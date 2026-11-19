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
  CreateWaifuRequest,
  OperationResult,
  UpdateHusbandRequest,
  UpdateWaifuRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class WaifuRoll<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollWaifusList
   * @request GET:/api/WaifuRoll/waifus
   * @response `200` `OperationResult<WaifuDto[]>` OK
   */
  waifuRollWaifusList = (params: RequestParams = {}) =>
    this.request<OperationResult<WaifuDto[]>, any>({
      path: `/api/WaifuRoll/waifus`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollWaifusCreate
   * @request POST:/api/WaifuRoll/waifus
   * @response `200` `OperationResult<WaifuDto>` OK
   */
  waifuRollWaifusCreate = (
    data: CreateWaifuRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<WaifuDto>, any>({
      path: `/api/WaifuRoll/waifus`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollWaifusDetail
   * @request GET:/api/WaifuRoll/waifus/{shikiId}
   * @response `200` `OperationResult<WaifuDto>` OK
   */
  waifuRollWaifusDetail = (shikiId: string, params: RequestParams = {}) =>
    this.request<OperationResult<WaifuDto>, any>({
      path: `/api/WaifuRoll/waifus/${shikiId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollWaifusUpdate
   * @request PUT:/api/WaifuRoll/waifus/{shikiId}
   * @response `200` `OperationResult<WaifuDto>` OK
   */
  waifuRollWaifusUpdate = (
    shikiId: string,
    data: UpdateWaifuRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<WaifuDto>, any>({
      path: `/api/WaifuRoll/waifus/${shikiId}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollWaifusDelete
   * @request DELETE:/api/WaifuRoll/waifus/{shikiId}
   * @response `200` `OperationResult` OK
   */
  waifuRollWaifusDelete = (shikiId: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/WaifuRoll/waifus/${shikiId}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollHusbandsList
   * @request GET:/api/WaifuRoll/husbands
   * @response `200` `OperationResult<HusbandDto[]>` OK
   */
  waifuRollHusbandsList = (params: RequestParams = {}) =>
    this.request<OperationResult<HusbandDto[]>, any>({
      path: `/api/WaifuRoll/husbands`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollHusbandsDetail
   * @request GET:/api/WaifuRoll/husbands/{twitchId}
   * @response `200` `OperationResult<HusbandDto>` OK
   */
  waifuRollHusbandsDetail = (twitchId: string, params: RequestParams = {}) =>
    this.request<OperationResult<HusbandDto>, any>({
      path: `/api/WaifuRoll/husbands/${twitchId}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollHusbandsUpdate
   * @request PUT:/api/WaifuRoll/husbands/{twitchId}
   * @response `200` `OperationResult<HusbandDto>` OK
   */
  waifuRollHusbandsUpdate = (
    twitchId: string,
    data: UpdateHusbandRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<HusbandDto>, any>({
      path: `/api/WaifuRoll/husbands/${twitchId}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollHusbandsDelete
   * @request DELETE:/api/WaifuRoll/husbands/{twitchId}
   * @response `200` `OperationResult` OK
   */
  waifuRollHusbandsDelete = (twitchId: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/WaifuRoll/husbands/${twitchId}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollHusbandsUnmergeCreate
   * @request POST:/api/WaifuRoll/husbands/{twitchId}/unmerge
   * @response `200` `OperationResult<any>` OK
   */
  waifuRollHusbandsUnmergeCreate = (
    twitchId: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<any>, any>({
      path: `/api/WaifuRoll/husbands/${twitchId}/unmerge`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollAudiosList
   * @request GET:/api/WaifuRoll/audios
   * @response `200` `OperationResult<WaifuRollAudioDto[]>` OK
   */
  waifuRollAudiosList = (params: RequestParams = {}) =>
    this.request<OperationResult<WaifuRollAudioDto[]>, any>({
      path: `/api/WaifuRoll/audios`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollAudiosCreate
   * @request POST:/api/WaifuRoll/audios
   * @response `200` `OperationResult<WaifuRollAudioDto>` OK
   */
  waifuRollAudiosCreate = (
    data: {
      /** @format binary */
      file: File;
    },
    query?: {
      name: string;
    },
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<WaifuRollAudioDto>, any>({
      path: `/api/WaifuRoll/audios`,
      method: "POST",
      query: query,
      body: data,
      type: ContentType.FormData,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollAudiosStreamList
   * @request GET:/api/WaifuRoll/audios/{id}/stream
   * @response `200` `void` OK
   */
  waifuRollAudiosStreamList = (id: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/WaifuRoll/audios/${id}/stream`,
      method: "GET",
      ...params,
    });
  /**
   * No description
   *
   * @tags WaifuRoll
   * @name WaifuRollAudiosDelete
   * @request DELETE:/api/WaifuRoll/audios/{id}
   * @response `200` `OperationResult` OK
   */
  waifuRollAudiosDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/WaifuRoll/audios/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
}
