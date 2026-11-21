using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

<<<<<<<< HEAD:src/MARS.TTS/MARS.TTS.Infrastructure/Services/SevenTvEmote.cs
namespace MARS.TTS.Infrastructure.Services;
========
namespace MARS.Twitch.Domain.Entities;
>>>>>>>> origin/main:src/MARS.Twitch/MARS.Twitch.Domain/Entities/SevenTvEmote.cs

[Table("SevenTvEmotes")]
public class SevenTvEmote
{
    [Key]
    [MaxLength(100)]
    public required string Name { get; set; }

    public DateTime LoadedAt { get; set; } = DateTime.Now;
}
