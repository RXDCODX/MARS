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

import type { OperationResult, Platform } from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class Commands<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserList
   * @request GET:/api/Commands/user
   * @response `200` `OperationResult<string[]>` OK
   */
  commandsUserList = (params: RequestParams = {}) =>
    this.request<OperationResult<string[]>, any>({
      path: `/api/Commands/user`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminList
   * @request GET:/api/Commands/admin
   * @response `200` `OperationResult<string[]>` OK
   */
  commandsAdminList = (params: RequestParams = {}) =>
    this.request<OperationResult<string[]>, any>({
      path: `/api/Commands/admin`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserDetail
   * @request GET:/api/Commands/user/{platform}
   * @response `200` `OperationResult<string[]>` OK
   */
  commandsUserDetail = (platform: Platform, params: RequestParams = {}) =>
    this.request<OperationResult<string[]>, any>({
      path: `/api/Commands/user/${platform}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminDetail
   * @request GET:/api/Commands/admin/{platform}
   * @response `200` `OperationResult<string[]>` OK
   */
  commandsAdminDetail = (platform: Platform, params: RequestParams = {}) =>
    this.request<OperationResult<string[]>, any>({
      path: `/api/Commands/admin/${platform}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsUserPlatformInfoList
   * @request GET:/api/Commands/user/platform/{platform}/info
   * @response `200` `OperationResult<BaseCommand[]>` OK
   */
  commandsUserPlatformInfoList = (
    platform: Platform,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BaseCommand[]>, any>({
      path: `/api/Commands/user/platform/${platform}/info`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsAdminPlatformInfoList
   * @request GET:/api/Commands/admin/platform/{platform}/info
   * @response `200` `OperationResult<BaseCommand[]>` OK
   */
  commandsAdminPlatformInfoList = (
    platform: Platform,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<BaseCommand[]>, any>({
      path: `/api/Commands/admin/platform/${platform}/info`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsParametersList
   * @request GET:/api/Commands/{commandName}/parameters
   * @response `200` `OperationResult<CommandParameterInfo[]>` OK
   */
  commandsParametersList = (commandName: string, params: RequestParams = {}) =>
    this.request<OperationResult<CommandParameterInfo[]>, any>({
      path: `/api/Commands/${commandName}/parameters`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Commands
   * @name CommandsExecuteCreate
   * @request POST:/api/Commands/{commandName}/execute
   * @response `200` `OperationResult<CommandResult>` OK
   */
  commandsExecuteCreate = (
    commandName: string,
    data: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<CommandResult>, any>({
      path: `/api/Commands/${commandName}/execute`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
}
