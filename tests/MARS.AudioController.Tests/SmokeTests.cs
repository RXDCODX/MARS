using System.Reflection;

namespace MARS.AudioController.Tests;

public class SmokeTests
{
    [Fact]
    public void ServiceAssemblyIsLoadable()
    {
        var assembly = Assembly.Load("MARS.AudioController.Host");

        Assert.Equal("MARS.AudioController.Host", assembly.GetName().Name);
    }
}
