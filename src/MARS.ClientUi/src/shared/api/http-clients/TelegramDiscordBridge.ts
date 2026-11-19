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
  TelegramDiscordBindingCreateRequest,
  TelegramDiscordBindingSetEnabledRequest,
} from "../types/data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class TelegramDiscordBridge<
  SecurityDataType = unknown,
> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeBindingsList
   * @request GET:/api/TelegramDiscordBridge/bindings
   * @response `200` `OperationResult<TelegramDiscordBindingDto[]>` OK
   */
  telegramDiscordBridgeBindingsList = (params: RequestParams = {}) =>
    this.request<OperationResult<TelegramDiscordBindingDto[]>, any>({
      path: `/api/TelegramDiscordBridge/bindings`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeBindingsCreate
   * @request POST:/api/TelegramDiscordBridge/bindings
   * @response `200` `OperationResult<TelegramDiscordBindingDto>` OK
   */
  telegramDiscordBridgeBindingsCreate = (
    data: TelegramDiscordBindingCreateRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TelegramDiscordBindingDto>, any>({
      path: `/api/TelegramDiscordBridge/bindings`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeBindingsDelete
   * @request DELETE:/api/TelegramDiscordBridge/bindings/{id}
   * @response `200` `OperationResult` OK
   */
  telegramDiscordBridgeBindingsDelete = (
    id: string,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult, any>({
      path: `/api/TelegramDiscordBridge/bindings/${id}`,
      method: "DELETE",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeBindingsEnabledUpdate
   * @request PUT:/api/TelegramDiscordBridge/bindings/{id}/enabled
   * @response `200` `OperationResult<TelegramDiscordBindingDto>` OK
   */
  telegramDiscordBridgeBindingsEnabledUpdate = (
    id: string,
    data: TelegramDiscordBindingSetEnabledRequest,
    params: RequestParams = {}
  ) =>
    this.request<OperationResult<TelegramDiscordBindingDto>, any>({
      path: `/api/TelegramDiscordBridge/bindings/${id}/enabled`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeStatesList
   * @request GET:/api/TelegramDiscordBridge/states
   * @response `200` `OperationResult<TelegramDiscordChannelStateDto[]>` OK
   */
  telegramDiscordBridgeStatesList = (params: RequestParams = {}) =>
    this.request<OperationResult<TelegramDiscordChannelStateDto[]>, any>({
      path: `/api/TelegramDiscordBridge/states`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeTelegramChannelsList
   * @request GET:/api/TelegramDiscordBridge/telegram-channels
   * @response `200` `OperationResult<TelegramChannelOptionDto[]>` OK
   */
  telegramDiscordBridgeTelegramChannelsList = (params: RequestParams = {}) =>
    this.request<OperationResult<TelegramChannelOptionDto[]>, any>({
      path: `/api/TelegramDiscordBridge/telegram-channels`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags TelegramDiscordBridge
   * @name TelegramDiscordBridgeDiscordChannelsList
   * @request GET:/api/TelegramDiscordBridge/discord-channels
   * @response `200` `OperationResult<DiscordChannelOptionDto[]>` OK
   */
  telegramDiscordBridgeDiscordChannelsList = (params: RequestParams = {}) =>
    this.request<OperationResult<DiscordChannelOptionDto[]>, any>({
      path: `/api/TelegramDiscordBridge/discord-channels`,
      method: "GET",
      format: "json",
      ...params,
    });
}
