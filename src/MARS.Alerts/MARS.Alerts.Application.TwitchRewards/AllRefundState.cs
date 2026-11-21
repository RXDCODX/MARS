namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Состояние активного окна «Алармагеддон»: пока активно, все редемпшены
/// возвращаются. Потокобезопасное хранилище одного значения.
/// </summary>
public sealed class AllRefundState
{
    private DateTimeOffset? _activeUntil;

    public bool IsActive => _activeUntil is { } until && DateTimeOffset.UtcNow < until;

    public void Activate(TimeSpan duration) => _activeUntil = DateTimeOffset.UtcNow + duration;

    public void Deactivate() => _activeUntil = null;
}
