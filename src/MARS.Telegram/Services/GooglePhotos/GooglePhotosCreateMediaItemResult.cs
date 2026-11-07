using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MARS.Telegram.Services.GooglePhotos;

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
