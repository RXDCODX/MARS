namespace MARS.Telegram.Application.Services;

public interface IRandomMemService
{
    bool IsEnabled { get; }

    Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}