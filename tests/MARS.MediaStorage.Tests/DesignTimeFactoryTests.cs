using MARS.TestKit;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Фабрика контекста для времени разработки собирается и отдаёт контекст с
/// реляционным провайдером: иначе поломка всплывёт только на
/// <c>dotnet ef migrations add</c>, то есть у разработчика, а не в CI.
/// </summary>
public class DesignTimeFactoryTests
{
    [Fact]
    public void ContextFactoryBuildsRelationalContext()
    {
        var failures = DesignTimeFactoryVerifier.Verify("MARS.MediaStorage");

        Assert.Empty(failures);
    }
}
