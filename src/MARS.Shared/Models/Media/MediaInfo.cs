using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MARS.Shared.Models.Media;

/// <summary>
/// Контракт медиа-алерта. Единственное определение на все микросервисы:
/// MARS.MediaStorage хранит его в БД, MARS.TwitchCore публикует его в RewardRedeemedEvent,
/// MARS.Alerts отправляет его подписчикам gRPC-сервиса TelegramusService.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public class MediaInfo
{
    [Key]
    [Required]
    public Guid Id { get; init; } = Guid.NewGuid();
    public required MediaTextInfo TextInfo { get; init; }
    public required MediaFileInfo FileInfo { get; init; }
    public required MediaPositionInfo PositionInfo { get; init; }
    public required MediaMetaInfo MetaInfo { get; init; }
    public required MediaStylesInfo StylesInfo { get; init; }

    public MediaInfo() { }

    [SetsRequiredMembers]
    protected MediaInfo(MediaInfo source)
    {
        Id = source.Id;
        TextInfo = source.TextInfo;
        FileInfo = source.FileInfo;
        PositionInfo = source.PositionInfo;
        MetaInfo = source.MetaInfo;
        StylesInfo = source.StylesInfo;
    }

    public MediaInfo CloneTo()
    {
        return (MediaInfo)MemberwiseClone();
    }
}
