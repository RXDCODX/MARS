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

namespace MARS.Telegram.Infrastructure.Integrations.GooglePhotos.Api;

[Serializable]
public class GooglePhotosNewMediaItem
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("simpleMediaItem")]
    public GooglePhotosSimpleMediaItem? SimpleMediaItem { get; set; }
}
