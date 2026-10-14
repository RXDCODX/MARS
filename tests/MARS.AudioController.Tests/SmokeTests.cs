using System.Reflection;

namespace MARS.AudioController.Tests;

public class SmokeTests
{
    [Fact]
    public void ServiceAssemblyIsLoadable()
    {
        var assembly = Assembly.Load("MARS.AudioController");

        Assert.Equal("MARS.AudioController", assembly.GetName().Name);
    }
}
