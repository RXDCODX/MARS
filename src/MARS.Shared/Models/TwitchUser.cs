using System.Diagnostics;

namespace MARS.Shared.Models;

[DebuggerDisplay("{DisplayName} ({UserLogin}) - ID: {TwitchId}")]
public class TwitchUser
{
    public required string TwitchId { get; init; }
    public required string UserLogin { get; set; }
    public required string DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ChatColor { get; set; }
    public bool IsModerator { get; set; }
    public bool IsVip { get; set; }
    public DateTime? FollowedAt { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? AliasNickname { get; set; }
    public bool IsInBlockList { get; set; } = false;

    public override string ToString() => $"{DisplayName} ({UserLogin}) - ID: {TwitchId}";

    public override bool Equals(object? obj) =>
        obj is TwitchUser other && TwitchId == other.TwitchId;

    public override int GetHashCode() => TwitchId.GetHashCode();
}
