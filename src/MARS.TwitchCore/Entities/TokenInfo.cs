using System.ComponentModel.DataAnnotations;

namespace MARS.TwitchCore.Entities;

public class TokenInfo
{
#pragma warning disable CS8618
    [Key]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccessToken { get; set; }

    [Required]
    public string RefreshToken { get; set; }

    [Required]
    public TimeSpan ExpiresIn { get; set; }
    public DateTime WhenCreated { get; set; }
    public DateTime WhenExpires => WhenCreated.Add(ExpiresIn);
#pragma warning restore CS8618
}
