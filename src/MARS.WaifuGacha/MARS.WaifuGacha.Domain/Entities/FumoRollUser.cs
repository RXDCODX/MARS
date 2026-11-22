using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.WaifuGacha.Domain.Entities;

/// <summary>
/// Факт «пользователь получал фумо» (перенесён из MARS.Twitch, где хранился
/// в таблице <c>twitch.FumoUsers</c>). Используется для проверки в hello-видео
/// и других местах, где важно знать, есть ли у зрителя фумо вообще.
/// </summary>
[Table("FumoRollUsers")]
public class FumoRollUser
{
    [Key]
    [Required]
    [MaxLength(50)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string TwitchId { get; set; }

    public DateTime LastTime { get; set; }
}
