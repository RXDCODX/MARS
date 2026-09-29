using System.ComponentModel.DataAnnotations;

namespace MARS.Shared.Models.Media;

public class MediaMetaInfo : IValidatableObject
{
    public int TwitchPointsCost { get; set; } = 0;
    public Guid? TwitchGuid { get; set; } = Guid.Empty;
    public bool Vip { get; set; } = false;
    public required string DisplayName { get; set; }
    public bool IsLooped { get; set; } = false;
    public bool IsFreezeRequired { get; set; } = false;
    public int Duration { get; set; } = 7;
    public MediaAlertPriority Priority { get; set; } = MediaAlertPriority.Normal;

    /// <summary>Громкость от 0 до 100.</summary>
    public int Volume { get; set; } = 100;

    public bool IsEnabled { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsFreezeRequired && Priority != MediaAlertPriority.High)
        {
            yield return new ValidationResult(
                "IsFreezeRequired может быть true только когда Priority = High.",
                [nameof(IsFreezeRequired), nameof(Priority)]
            );
        }
    }
}
