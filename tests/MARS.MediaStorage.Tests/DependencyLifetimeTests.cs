using MARS.TestKit;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Lifetime-контракт регистраций <c>MARS.MediaStorage</c>.
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
        var assemblies = new[]
        {
            "MARS.MediaStorage",
            "MARS.MediaStorage.Api.Rest",
            "MARS.MediaStorage.Application",
            "MARS.MediaStorage.Domain",
            "MARS.MediaStorage.Infrastructure.Git",
            "MARS.MediaStorage.Infrastructure.Media",
            "MARS.MediaStorage.Infrastructure.Postgres",
            "MARS.MediaStorage.Infrastructure.Telegram",
        };
        var failures = assemblies
            .SelectMany(assembly =>
                DependencyLifetimeContract.Verify(assembly, "src/MARS.MediaStorage")
            )
            .ToArray();

        Assert.True(
            failures.Length == 0,
            "MARS.MediaStorage не собирает контейнер в Development:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, failures.Select(failure => "  " + failure))
        );
    }
}
