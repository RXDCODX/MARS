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
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  HeadersDefaults,
  ResponseType,
} from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams extends Omit<
  AxiosRequestConfig,
  "data" | "params" | "url" | "responseType"
> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType;
  /** request body */
  body?: unknown;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> extends Omit<
  AxiosRequestConfig,
  "data" | "cancelToken"
> {
  securityWorker?: (
    securityData: SecurityDataType | null
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void;
  secure?: boolean;
  format?: ResponseType;
}

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

/*
 * Клиент сгенерирован по контракту монолита, где тело ответа было `{ data: … }`.
 * Сервисы микросервисной схемы отвечают конвертом `{ success, result,
 * errorMessage }`. Читать такой ответ на 52 местах вызова значило бы размазать
 * знание о форме по всему проекту, и любое новое место забыло бы про конверт.
 *
 * Поэтому форма приводится к той, на которую клиент написан: в `data` кладётся
 * полезная нагрузка, а сам конверт остаётся доступен рядом. Отказ
 * (`success === false`) превращается в исключение — иначе вызов выглядел бы
 * успешным, а полезная нагрузка была бы `null`, и падение уезжало бы в место
 * использования с сообщением вместо текста сервера.
 *
 * Проверка конверта точечная: тело должно быть объектом с булевым `success`.
 * Всё, что не похоже на конверт (файл, массив, ProblemDetails), проходит как
 * раньше.
 */
export type OperationResultEnvelope = {
  success: boolean;
  result?: unknown;
  errorMessage?: string | null;
  /**
   * Вторая форма конверта: `MARS.Admin` и `MARS.Discord` отвечают
   * `{ success, message, data }`, а не `{ success, result, errorMessage }`.
   *
   * Обе формы живут в репозитории: двадцать девять контроллеров используют
   * общий `MARS.Shared.Models.OperationResult`, а семь — свои. Знать об этом
   * должен разбор, а не вызывающий.
   */
  data?: unknown;
  message?: string | null;
};

function isOperationResult(body: unknown): body is OperationResultEnvelope {
  return (
    typeof body === "object" &&
    body !== null &&
    typeof (body as OperationResultEnvelope).success === "boolean"
  );
}

/**
 * Полезная нагрузка конверта.
 *
 * `result` берётся первым: у формы `{ success, result }` поля `data` нет, и
 * наоборот. Если отданы оба, побеждает `data` — в форме `{ success, data }`
 * именно она и несёт полезную нагрузку, а `result` в такой форме не значит
 * ничего и подставлять его вместо `data` значило бы убить данные молча.
 */
const payloadOf = (body: OperationResultEnvelope): unknown =>
  body.data !== undefined ? body.data : body.result;

/**
 * Приводит тело ответа к форме, на которую написан клиент.
 *
 * Конверт сохраняется целиком, а его полезная нагрузка кладётся и в `data`, и в
 * `result`: вызовы на проекте читают `result.data.data`, и это 52 места в 12
 * файлах. Править их по одному — значит размазать знание о двух формах ответа по
 * всему коду и получить расхождение при первом же новом вызове.
 */
export function unwrapOperationResult(body: unknown): unknown {
  if (!isOperationResult(body)) {
    return body;
  }

  if (!body.success) {
    throw new Error(
      body.errorMessage ??
        body.message ??
        "Запрос завершился ошибкой на стороне сервиса."
    );
  }

  const payload = payloadOf(body);

  return { ...body, data: payload, result: payload };
}

/** Разбор тела ответа. Всё, что не JSON, проходит как есть: файлы, потоки. */
export function parseResponseBody(raw: unknown): unknown {
  if (typeof raw !== "string") {
    return raw;
  }

  // HTML на месте данных — это catch-all клиента (`spa`, `Order: 1000` плюс
  // `try_files … /index.html` в nginx), а не ответ сервиса. Он приходит с кодом
  // 200, поэтому `response.ok` истинна, и вызов проходил как успешный: страницы
  // получали строку вместо данных, списки были пустыми, а тост показывал
  // исходный код страницы как «успешно». Отсекается здесь, а не в каждой
  // странице: иначе каждая новая страница обманывается отдельно.
  if (isSpaFallback(raw)) {
    throw new Error(
      "Запрос попал в раздачу клиента, а не в сервис: эндпоинт отсутствует."
    );
  }

  try {
    return unwrapOperationResult(JSON.parse(raw) as unknown);
  } catch (error) {
    if (error instanceof SyntaxError) {
      return raw;
    }

    throw error;
  }
}

/**
 * HTML-заглушка вместо ответа API.
 *
 * Проверяется начало тела, а не `Content-Type`: заголовок задаёт прокси, а
 * подменённое тело — то, что действительно пришло.
 */
function isSpaFallback(raw: string): boolean {
  const head = raw.trimStart().slice(0, 64).toLowerCase();

  return head.startsWith("<!doctype html") || head.startsWith("<html");
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance;
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private secure?: boolean;
  private format?: ResponseType;

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({
      ...axiosConfig,
      baseURL: axiosConfig.baseURL || "",
    });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig
  ): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...((method &&
          this.instance.defaults.headers[
            method.toLowerCase() as keyof HeadersDefaults
          ]) ||
          {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected stringifyFormItem(formItem: unknown) {
    if (typeof formItem === "object" && formItem !== null) {
      return JSON.stringify(formItem);
    } else {
      return `${formItem}`;
    }
  }

  protected createFormData(input: Record<string, unknown>): FormData {
    if (input instanceof FormData) {
      return input;
    }
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key];
      const propertyContent: any[] =
        property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(
          key,
          isFileType ? formItem : this.stringifyFormItem(formItem)
        );
      }

      return formData;
    }, new FormData());
  }

  public request = async <T = any, _E = any>({
    secure,
    path,
    type,
    query,
    format,
    body,
    ...params
  }: FullRequestParams): Promise<AxiosResponse<T>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const responseFormat = format || this.format || undefined;

    if (
      type === ContentType.FormData &&
      body &&
      body !== null &&
      typeof body === "object"
    ) {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (
      type === ContentType.Text &&
      body &&
      body !== null &&
      typeof body !== "string"
    ) {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...(requestParams.headers || {}),
        ...(type ? { "Content-Type": type } : {}),
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
      transformResponse: [
        parseResponseBody,
      ] as AxiosRequestConfig["transformResponse"],
    });
  };
}
