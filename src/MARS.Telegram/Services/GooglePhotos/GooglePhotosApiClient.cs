using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Models;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MARS.Telegram.Services.GooglePhotos;

public class GooglePhotosApiClient(
    IHttpClientFactory httpClientFactory,
    IGooglePhotosAuthService authService,
    ILogger<GooglePhotosApiClient> logger
)
{
    private const string GooglePhotosBaseUrl = "https://photoslibrary.googleapis.com/v1";

    public async Task<OperationResult<string>> UploadPhotoAsync(
        Stream photoStream,
        string fileName,
        CancellationToken ct
    )
    {
        var result = OperationResult<string>.Fail("Ошибка загрузки фото");

        var accessToken = await authService.GetValidAccessTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            result = OperationResult<string>.Fail(
                "Отсутствует действительный токен доступа. Требуется авторизация."
            );
        }
        else
        {
            try
            {
                var uploadUrl = $"{GooglePhotosBaseUrl}/uploads";

                using var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var memoryStream = new MemoryStream();
                await photoStream.CopyToAsync(memoryStream, ct);
                var photoBytes = memoryStream.ToArray();

                var content = new ByteArrayContent(photoBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "image/jpeg"
                );
                content.Headers.Add("X-Goog-Upload-File-Name", fileName);

                var uploadResponse = await httpClient.PostAsync(uploadUrl, content, ct);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var errorContent = await uploadResponse.Content.ReadAsStringAsync(ct);
                    logger.LogError("Google Photos upload error: {ErrorContent}", errorContent);
                    result = OperationResult<string>.Fail(
                        $"Ошибка загрузки на Google Photos: HTTP {uploadResponse.StatusCode}"
                    );
                }
                else
                {
                    var uploadToken = await uploadResponse.Content.ReadAsStringAsync(ct);

                    var createItemUrl = $"{GooglePhotosBaseUrl}/mediaItems:batchCreate";

                    var batchCreateRequest = new GooglePhotosBatchCreateRequest
                    {
                        NewMediaItems = new[]
                        {
                            new GooglePhotosNewMediaItem
                            {
                                Description = fileName,
                                SimpleMediaItem = new GooglePhotosSimpleMediaItem
                                {
                                    UploadToken = uploadToken,
                                },
                            },
                        },
                    };

                    var jsonRequest = JsonSerializer.Serialize(
                        batchCreateRequest,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        }
                    );
                    var httpContent = new StringContent(
                        jsonRequest,
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var createResponse = await httpClient.PostAsync(createItemUrl, httpContent, ct);

                    if (createResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await createResponse.Content.ReadAsStringAsync(ct);
                        var batchResponse =
                            JsonSerializer.Deserialize<GooglePhotosBatchCreateResponse>(
                                responseContent,
                                new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                }
                            );

                        if (batchResponse?.Results?.Length > 0)
                        {
                            var mediaItemResult = batchResponse.Results[0];
                            if (mediaItemResult?.Status?.Code == 0)
                            {
                                result = OperationResult<string>.Ok(
                                    mediaItemResult.MediaItem?.Id ?? "unknown"
                                );
                            }
                            else
                            {
                                var errorMsg =
                                    mediaItemResult?.Status?.Message ?? "Неизвестная ошибка";
                                logger.LogError(
                                    "Google Photos batch create error: {ErrorMessage}",
                                    errorMsg
                                );
                                result = OperationResult<string>.Fail(
                                    $"Ошибка при создании медиаэлемента: {errorMsg}"
                                );
                            }
                        }
                        else
                        {
                            result = OperationResult<string>.Fail(
                                "Пустой ответ от Google Photos API"
                            );
                        }
                    }
                    else
                    {
                        var errorContent = await createResponse.Content.ReadAsStringAsync(ct);
                        logger.LogError(
                            "Google Photos batch create error: {ErrorContent}",
                            errorContent
                        );
                        result = OperationResult<string>.Fail(
                            $"Ошибка при создании медиаэлемента: HTTP {createResponse.StatusCode}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Исключение при загрузке фото в Google Photos");
                result = OperationResult<string>.Fail($"Исключение: {ex.Message}");
            }
        }

        return result;
    }
}

// Google Photos API DTOs
[Serializable]
public class GooglePhotosBatchCreateRequest
{
    [JsonPropertyName("newMediaItems")]
    public GooglePhotosNewMediaItem[]? NewMediaItems { get; set; }
}

[Serializable]
public class GooglePhotosNewMediaItem
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("simpleMediaItem")]
    public GooglePhotosSimpleMediaItem? SimpleMediaItem { get; set; }
}

[Serializable]
public class GooglePhotosSimpleMediaItem
{
    [JsonPropertyName("uploadToken")]
    public string UploadToken { get; set; } = string.Empty;
}

[Serializable]
public class GooglePhotosBatchCreateResponse
{
    [JsonPropertyName("results")]
    public GooglePhotosCreateMediaItemResult[]? Results { get; set; }
}

[Serializable]
public class GooglePhotosCreateMediaItemResult
{
    [JsonPropertyName("uploadToken")]
    public string? UploadToken { get; set; }

    [JsonPropertyName("status")]
    public GooglePhotosStatus? Status { get; set; }

    [JsonPropertyName("mediaItem")]
    public GooglePhotosMediaItem? MediaItem { get; set; }
}

[Serializable]
public class GooglePhotosStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

[Serializable]
public class GooglePhotosMediaItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("productUrl")]
    public string? ProductUrl { get; set; }
}
