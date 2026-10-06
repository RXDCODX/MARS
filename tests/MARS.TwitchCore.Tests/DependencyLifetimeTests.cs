using MARS.TestKit;

namespace MARS.TwitchCore.Tests;

/// <summary>
/// Lifetime-контракт регистраций <c>MARS.TwitchCore</c>.
/// </summary>
/// <remarks>
/// Стенд Development — единственное место, где контейнер проверяет граф: в
/// Production <c>ValidateScopes</c> и <c>ValidateOnBuild</c> выключены, и captive
/// зависимость остаётся незамеченной до первого обращения.
/// </remarks>
public class DependencyLifetimeTests
{
    /// <summary>Ни синглтон, ни фоновая служба не держат scoped-зависимость.</summary>
    [Fact]
    public void РегистрацииНеНарушаютLifetime()
    {
        var failures = DependencyLifetimeContract.Verify("MARS.TwitchCore");

        Assert.True(
            failures.Count == 0,
            "MARS.TwitchCore не собирает контейнер в Development:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, failures.Select(failure => "  " + failure))
        );
    }
}
