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

namespace MARS.Telegram.Infrastructure.GooglePhotos.Api;

// Google Photos API DTOs
[Serializable]
public class GooglePhotosBatchCreateRequest
{
    [JsonPropertyName("newMediaItems")]
    public GooglePhotosNewMediaItem[]? NewMediaItems { get; set; }
}
