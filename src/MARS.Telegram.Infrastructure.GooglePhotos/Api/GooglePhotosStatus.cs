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
public class GooglePhotosStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
