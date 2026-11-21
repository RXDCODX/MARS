namespace MARS.Shared.Contracts.Voice;

public class TtsUser
{
    public string TwitchId { get; set; } = "";
    public string UserLogin { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ProfileImageUrl { get; set; } = "";
    public string ChatColor { get; set; } = "";
    public bool IsModerator { get; set; }
    public bool IsVip { get; set; }
    public string FollowedAt { get; set; } = "";
    public bool HasFollowedAt { get; set; }
    public string LastUpdated { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string AliasNickname { get; set; } = "";
    public bool IsInBlockList { get; set; }
}
