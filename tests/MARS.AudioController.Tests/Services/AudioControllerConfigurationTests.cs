using MARS.AudioController.Configuration;
using MARS.AudioController.Services;
using Microsoft.Extensions.Options;

namespace MARS.AudioController.Tests.Services;

public class AudioControllerConfigurationTests
{
    [Fact]
    public void DefaultExcludedProcessNames_ContainsObs()
    {
        var configuration = new AudioControllerConfiguration();

        Assert.Contains("obs64", configuration.DefaultExcludedProcessNames);
        Assert.Contains("obs32", configuration.DefaultExcludedProcessNames);
        Assert.Contains("obs-browser-page", configuration.DefaultExcludedProcessNames);
    }

    [Fact]
    public void DefaultExcludedProcessNames_IsNotEmpty()
    {
        var configuration = new AudioControllerConfiguration();

        Assert.NotEmpty(configuration.DefaultExcludedProcessNames);
    }
}
