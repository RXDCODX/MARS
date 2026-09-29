using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Admin.Entities;

/// <summary>
/// Информация о фоловере канала
/// </summary>
public class FollowerInfo
{
    [Key]
    [Required]
    public required string UserId { get; init; }

    [ForeignKey(nameof(UserId))]
    public TwitchUser? TwitchUser { get; set; }
}
