using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using MARS.TwitchCore.Extensions;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client.Events;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using ChatMessage = TwitchLib.Client.Models.ChatMessage;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;

namespace MARS.TwitchCore.Entities;

[Table("TwitchUsers")]
[DebuggerDisplay("{DisplayName} ({UserLogin}) - ID: {TwitchId}")]
public class TwitchUser
{
    [Key]
    [Required]
    [MaxLength(50)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column(nameof(TwitchId))]
    public required string TwitchId
    {
        get;
        init
        {
            if (!IsValidTwitchId(value))
            {
                throw new ArgumentException("TwitchId was not valid");
            }
            field = value;
        }
    }

    [Required]
    [MaxLength(100)]
    public required string UserLogin { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(20)]
    public string? ChatColor { get; set; }

    public bool IsModerator { get; set; }

    public bool IsVip { get; set; }

    public bool IsBroadcaster => TwitchId == TwitchConstants.ChannelId;

    public DateTime? FollowedAt { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? AliasNickname { get; set; }

    public bool IsInBlockList { get; set; } = false;

    [NotMapped]
    public bool IsSimpleUser => !IsModerator && !IsVip && !IsBroadcaster;

    public override string ToString()
    {
        return $"{DisplayName} ({UserLogin}) - ID: {TwitchId}";
    }

    public override bool Equals(object? obj)
    {
        return obj is TwitchUser other && TwitchId == other.TwitchId;
    }

    public override int GetHashCode()
    {
        return TwitchId.GetHashCode();
    }

    private static bool IsValidTwitchId(string twitchId)
    {
        return !string.IsNullOrWhiteSpace(twitchId) && long.TryParse(twitchId, out _);
    }

    #region Static Factory Methods

    public static TwitchUser? FromChatMessage(ChatMessage? chatMessage)
    {
        TwitchUser? result = null;

        if (chatMessage != null && !string.IsNullOrWhiteSpace(chatMessage.UserId))
        {
            if (IsValidTwitchId(chatMessage.UserId))
            {
                result = new TwitchUser
                {
                    TwitchId = chatMessage.UserId,
                    UserLogin = chatMessage.Username,
                    DisplayName = chatMessage.DisplayName,
                    ChatColor = chatMessage.HexColor,
                    IsModerator = chatMessage.UserDetail.IsModerator,
                    IsVip = chatMessage.UserDetail.IsVip,
                    CreatedAt = DateTime.Now,
                    LastUpdated = DateTime.Now,
                };
            }
        }

        return result;
    }

    public static TwitchUser? FromOnMessageReceivedArgs(OnMessageReceivedArgs? args)
    {
        return args?.ChatMessage != null ? FromChatMessage(args.ChatMessage) : null;
    }

    public static TwitchUser? FromChannelPointsCustomRewardRedemptionArgs(
        ChannelPointsCustomRewardRedemptionArgs? args
    )
    {
        TwitchUser? result = null;

        if (args?.Payload?.Event != null)
        {
            var evt = args.Payload.Event;
            if (!string.IsNullOrWhiteSpace(evt.UserId) && IsValidTwitchId(evt.UserId))
            {
                result = new TwitchUser
                {
                    TwitchId = evt.UserId,
                    UserLogin = evt.UserLogin,
                    DisplayName = evt.UserName,
                    IsModerator = false,
                    IsVip = false,
                    CreatedAt = DateTime.Now,
                    LastUpdated = DateTime.Now,
                };
            }
        }

        return result;
    }

    public static TwitchUser? FromUser(User user)
    {
        return new TwitchUser
        {
            TwitchId = user.Id,
            UserLogin = user.Login ?? $"user_{user.Id}",
            DisplayName = user.DisplayName ?? user.Login ?? $"User{user.Id}",
            ProfileImageUrl = user.ProfileImageUrl,
            IsModerator = false,
            IsVip = false,
            CreatedAt = DateTime.Now,
            LastUpdated = DateTime.Now,
        };
    }

    public static TwitchUser FromModerator(Moderator mod)
    {
        return new TwitchUser
        {
            TwitchId = mod.UserId,
            UserLogin = mod.UserLogin,
            DisplayName = mod.UserName,
            CreatedAt = DateTime.Now,
            LastUpdated = DateTime.Now,
            IsModerator = true,
        };
    }

    public static TwitchUser? FromVip(ChannelVIPsResponseModel vip)
    {
        return new TwitchUser
        {
            DisplayName = vip.UserName,
            TwitchId = vip.UserId,
            UserLogin = vip.UserLogin,
            LastUpdated = DateTime.Now,
            CreatedAt = DateTime.Now,
            IsVip = true,
        };
    }

    public static TwitchUser? FromApiUser(User apiuser)
    {
        return new TwitchUser()
        {
            DisplayName = apiuser.DisplayName,
            TwitchId = apiuser.Id,
            UserLogin = apiuser.Login,
            CreatedAt = apiuser.CreatedAt,
            ProfileImageUrl = apiuser.ProfileImageUrl,
        };
    }

    #endregion
}
