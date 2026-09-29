using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Shared.Models.Media;

[method: SetsRequiredMembers]
public struct MediaDto(MediaInfo mediaInfo)
{
    [Required]
    public MediaInfo MediaInfo { get; init; } = mediaInfo;

    public DateTime UploadStartTime { get; set; } = DateTime.Now;
}
