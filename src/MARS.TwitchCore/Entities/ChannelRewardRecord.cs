using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.TwitchCore.Entities;

public class ChannelRewardRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public int Cost { get; set; }

    public bool IsEnabled { get; set; } = true;
    public string? Prompt { get; set; }
    public string? BackgroundColor { get; set; } = "#9146FF";
    public bool IsUserInputRequired { get; set; }
    public bool IsMaxPerStreamEnabled { get; set; }
    public int? MaxPerStream { get; set; }
    public bool IsMaxPerUserPerStreamEnabled { get; set; }
    public int? MaxPerUserPerStream { get; set; }
    public bool IsGlobalCooldownEnabled { get; set; }
    public int? GlobalCooldownSeconds { get; set; }
    public bool ShouldRedemptionsSkipRequestQueue { get; set; }
    public bool IsDeleted { get; set; }
    public string? TwitchRewardId { get; set; }
    public Guid? MediaInfoId { get; set; }
}
