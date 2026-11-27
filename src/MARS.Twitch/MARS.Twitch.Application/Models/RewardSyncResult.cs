namespace MARS.Twitch.Application.Models;

/// <summary>
/// Результат одного цикла синхронизации Twitch-наград.
/// </summary>
public sealed class RewardSyncResult
{
    public bool DryRun { get; init; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
    public int Skipped { get; set; }
    public int Ambiguities { get; set; }
    public List<string> Errors { get; init; } = [];
    public bool HasErrors => Errors.Count > 0;
}
