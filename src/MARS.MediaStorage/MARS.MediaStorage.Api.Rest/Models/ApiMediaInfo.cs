using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Api.Rest.Models;

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
