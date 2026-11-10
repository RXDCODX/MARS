using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Twitch.Api.Entities;

public class FumoUser
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string TwitchId { get; set; }

    [ForeignKey(nameof(TwitchId))]
    public TwitchUser? TwitchUser { get; set; }

    public DateTime LastTime { get; set; }
}
