using MARS.AudioController.Services.Naaudio;
using MARS.TestKit;

namespace MARS.AudioController.Tests;

public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var options = new ContractOptions();

        options.IgnoredTypes.Add("MARS.AudioController.Services.Naaudio.NaaudioAudioDeviceService");
        options.IgnoredTypes.Add("MARS.AudioController.Services.Naaudio.NaaudioAudioMixerService");

        var result = PublicContractVerifier.VerifyAssembly("MARS.AudioController", options);

        Assert.Empty(result.Violations);
        Assert.True(
            result.TypesInspected > 0,
            "Проверка не нашла ни одного типа: сборка загрузилась, но её типы не перечислены."
        );
    }
}
