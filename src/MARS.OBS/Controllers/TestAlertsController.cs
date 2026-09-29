using System.Text.Json;
using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Hubs.Models;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MARS.OBS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestAlertsController(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    IWebHostEnvironment env
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [HttpPost("alert")]
    public async Task<ActionResult<OperationResult>> SendAlert([FromBody] MediaDto dto)
    {
        await hubContext.Clients.All.Alert(dto);

        ActionResult<OperationResult> result = Ok(OperationResult.Ok());
        return result;
    }

    [HttpPost("alerts-batch")]
    public async Task<ActionResult<OperationResult>> SendAlertsBatch([FromBody] MediaDto[] dtos)
    {
        await hubContext.Clients.All.Alerts(dtos);

        ActionResult<OperationResult> result = Ok(OperationResult.Ok());
        return result;
    }

    [HttpPost("alert-by-type")]
    public async Task<ActionResult<OperationResult<MediaDto>>> SendAlertByType(
        [FromQuery] string mediaType,
        [FromQuery] int duration = 5,
        [FromQuery] string? text = null
    )
    {
        var filePath = FindFileForType(mediaType);
        if (filePath == null)
        {
            ActionResult<OperationResult<MediaDto>> badResult = Ok(
                OperationResult<MediaDto>.Fail($"Не удалось создать алерт для типа {mediaType}")
            );
            return badResult;
        }

        var mediaTypeEnum = mediaType.ToLowerInvariant() switch
        {
            "image" => MediaType.Image,
            "audio" => MediaType.Audio,
            "gif" => MediaType.Gif,
            "voice" => MediaType.Voice,
            _ => MediaType.Video,
        };

        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).TrimStart('.');

        var mediaInfo = new MediaInfo
        {
            TextInfo = new MediaTextInfo { Text = text },
            FileInfo = new MediaFileInfo
            {
                Type = mediaTypeEnum,
                FilePath = filePath,
                FileName = fileName,
                Extension = extension,
                IsLocalFile = true,
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo
            {
                DisplayName = string.Empty,
                Duration = duration,
                Priority = MediaAlertPriority.Normal,
            },
            StylesInfo = new MediaStylesInfo(),
        };

        var dto = new MediaDto(mediaInfo);
        await hubContext.Clients.All.Alert(dto);

        ActionResult<OperationResult<MediaDto>> result = Ok(OperationResult<MediaDto>.Ok(dto));
        return result;
    }

    [HttpGet("settings")]
    public ActionResult<OperationResult<List<AlertSettingsEntry>>> GetAlertSettings()
    {
        var settingsPath = Path.Combine(env.WebRootPath, "Alerts", "settings.json");

        if (!System.IO.File.Exists(settingsPath))
        {
            ActionResult<OperationResult<List<AlertSettingsEntry>>> notFound = Ok(
                OperationResult<List<AlertSettingsEntry>>.Fail("settings.json не найден")
            );
            return notFound;
        }

        var json = System.IO.File.ReadAllText(settingsPath);
        var entries = JsonSerializer.Deserialize<List<AlertSettingsEntry>>(json, JsonOptions) ?? [];

        ActionResult<OperationResult<List<AlertSettingsEntry>>> result = Ok(
            OperationResult<List<AlertSettingsEntry>>.Ok(entries)
        );
        return result;
    }

    [HttpGet("available-files")]
    public ActionResult<OperationResult<Dictionary<string, string[]>>> GetAvailableFiles()
    {
        var alertsDir = Path.Combine(env.WebRootPath, "Alerts");
        var resultDict = new Dictionary<string, string[]>();

        if (Directory.Exists(alertsDir))
        {
            var imageFiles = Directory
                .GetFiles(alertsDir, "*.*")
                .Where(f =>
                    f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                )
                .Select(f => "Alerts/" + Path.GetFileName(f))
                .ToArray();

            var videoFiles = Directory
                .GetFiles(alertsDir, "*.*")
                .Where(f =>
                    f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                )
                .Select(f => "Alerts/" + Path.GetFileName(f))
                .ToArray();

            var audioFiles = Directory
                .GetFiles(alertsDir, "*.*")
                .Where(f =>
                    f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                )
                .Select(f => "Alerts/" + Path.GetFileName(f))
                .ToArray();

            if (imageFiles.Length > 0)
            {
                resultDict["Image"] = imageFiles;
            }

            if (videoFiles.Length > 0)
            {
                resultDict["Video"] = videoFiles;
            }

            if (audioFiles.Length > 0)
            {
                resultDict["Audio"] = audioFiles;
            }
        }

        var facesDir = Path.Combine(env.WebRootPath, "faces");
        if (Directory.Exists(facesDir))
        {
            var gifFiles = Directory
                .GetFiles(facesDir, "*.gif")
                .Select(f => "faces/" + Path.GetFileName(f))
                .ToArray();

            if (gifFiles.Length > 0)
            {
                resultDict["Gif"] = gifFiles;
            }
        }

        ActionResult<OperationResult<Dictionary<string, string[]>>> result = Ok(
            OperationResult<Dictionary<string, string[]>>.Ok(resultDict)
        );
        return result;
    }

    private string? FindFileForType(string mediaType)
    {
        var root = env.WebRootPath;

        return mediaType.ToLowerInvariant() switch
        {
            "image" => FindFirstFile(root, "Alerts", "*.jpg", "*.png"),
            "video" => FindFirstFile(root, "Alerts", "*.webm", "*.mp4"),
            "audio" => FindFirstFile(root, "Alerts", "*.mp3", "*.wav"),
            "gif" => FindFirstFile(root, "faces", "*.gif"),
            "voice" => FindFirstFile(root, "Alerts", "*.wav"),
            _ => null,
        };
    }

    private static string? FindFirstFile(string root, string subDir, params string[] patterns)
    {
        var dir = Path.Combine(root, subDir);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return subDir + "/" + Path.GetRelativePath(dir, files[0]).Replace('\\', '/');
            }
        }

        return null;
    }

    public class AlertSettingsEntry
    {
        public int TwitchPointsCost { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int Duration { get; set; }
        public bool RandomCoordinates { get; set; }
        public int XCoordinate { get; set; }
        public int YCoordinate { get; set; }
        public int Type { get; set; }
        public string TextPosition { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
        public string KeyWordsColor { get; set; } = string.Empty;
        public bool VIP { get; set; }
        public bool IsBorder { get; set; }
        public bool IsProportion { get; set; }
        public bool IsResizeRequires { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsRotated { get; set; }
        public int Rotation { get; set; }
        public bool IsLooped { get; set; }
    }
}
