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
  CreateTwitchUserRequest,
  OperationResult,
  UpdateTwitchUserRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class TwitchUsers<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersList
   * @request GET:/api/TwitchUsers
   * @response `200` `OperationResult<TwitchUserDto[]>` OK
   */
  twitchUsersList = (params: RequestParams = {}) =>
    this.request<OperationResult<TwitchUserDto[]>, any>({
      path: `/api/TwitchUsers`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersCreate
   * @request POST:/api/TwitchUsers
   * @response `200` `OperationResult<TwitchUserDto>` OK
   */
  twitchUsersCreate = (
    data: CreateTwitchUserRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TwitchUserDto>, any>({
      path: `/api/TwitchUsers`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersByLoginDetail
   * @request GET:/api/TwitchUsers/by-login/{login}
   * @response `200` `OperationResult<string>` OK
   */
  twitchUsersByLoginDetail = (login: string, params: RequestParams = {}) =>
    this.request<OperationResult<string>, any>({
      path: `/api/TwitchUsers/by-login/${login}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersDetail
   * @request GET:/api/TwitchUsers/{id}
   * @response `200` `OperationResult<TwitchUserDto>` OK
   */
  twitchUsersDetail = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult<TwitchUserDto>, any>({
      path: `/api/TwitchUsers/${id}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersUpdate
   * @request PUT:/api/TwitchUsers/{id}
   * @response `200` `OperationResult<TwitchUserDto>` OK
   */
  twitchUsersUpdate = (
    id: string,
    data: UpdateTwitchUserRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TwitchUserDto>, any>({
      path: `/api/TwitchUsers/${id}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TwitchUsers
   * @name TwitchUsersDelete
   * @request DELETE:/api/TwitchUsers/{id}
   * @response `200` `OperationResult` OK
   */
  twitchUsersDelete = (id: string, params: RequestParams = {}) =>
    this.request<OperationResult, any>({
      path: `/api/TwitchUsers/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
}
