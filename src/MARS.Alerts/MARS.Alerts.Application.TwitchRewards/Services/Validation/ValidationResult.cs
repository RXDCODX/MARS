namespace MARS.Alerts.Application.TwitchRewards.Services;

public sealed class ValidationResult
{
    private readonly List<string> _errors = [];

    public bool IsInvalid => _errors.Count > 0;
    public bool HasSilentFailure { get; set; }
    public string? FirstError => _errors.Count > 0 ? _errors[0] : null;

    public void AddError(string message) => _errors.Add(message);
}