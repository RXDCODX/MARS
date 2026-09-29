using System.ComponentModel.DataAnnotations;

namespace MARS.TwitchCore.Entities;

public class RootState
{
    [Key]
    public required string Name { get; set; }

    public string Value { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TypeDescription { get; set; } = string.Empty;
}

public static class RootStateKeys
{
    public const string PuntoSwitcherFilterEnabled = "PuntoSwitcherFilterEnabled";
    public const string TtsFilterEnabled = "TtsFilterEnabled";
}
