using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Entities;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public class ApiMediaInfo : MediaInfo
{
    public ApiMediaInfo() { }

    [SetsRequiredMembers]
    public ApiMediaInfo(ApiMediaInfo source)
        : base(source) { }

    [SetsRequiredMembers]
    public ApiMediaInfo(MediaInfo source)
        : base(source) { }
}
