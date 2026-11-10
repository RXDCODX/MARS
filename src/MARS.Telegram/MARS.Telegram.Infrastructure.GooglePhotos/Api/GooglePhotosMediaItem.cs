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
