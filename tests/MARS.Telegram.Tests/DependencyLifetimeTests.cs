using MARS.TestKit;

namespace MARS.Telegram.Tests;

/// <summary>
/// Lifetime-контракт регистраций MARS.Telegram.Host.
/// </summary>
public class DependencyLifetimeTests
{
    [Fact]
    public void RegistrationsDoNotViolateLifetime()
    {
        var failures = DependencyLifetimeContract.Verify(
            "MARS.Telegram.Host",
            "src/MARS.Telegram/MARS.Telegram.Host"
        );

        Assert.True(
            failures.Count == 0,
            "MARS.Telegram.Host не собирает контейнер в Development:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, failures.Select(failure => "  " + failure))
        );
    }
}
