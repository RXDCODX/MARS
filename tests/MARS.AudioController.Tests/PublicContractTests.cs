using MARS.TestKit;

namespace MARS.AudioController.Tests;

public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var options = new ContractOptions();

        options.IgnoredTypes.Add(
            "MARS.AudioController.Infrastructure.Services.Naaudio.NaaudioAudioDeviceService"
        );
        options.IgnoredTypes.Add(
            "MARS.AudioController.Infrastructure.Services.Naaudio.NaaudioAudioMixerService"
        );

        var assemblies = new[] { "MARS.AudioController.Host", "MARS.AudioController.Application" };
        var results = assemblies
            .Select(assembly => PublicContractVerifier.VerifyAssembly(assembly))
            .ToArray();

        var failures = results.SelectMany(result => result.Violations);
        Assert.Empty(failures);
        Assert.True(
            results.All(result => result.TypesInspected > 0),
            "Проверка не нашла ни одного типа: сборка загрузилась, но её типы не перечислены."
        );

        // Infrastructure содержит только NAudio-адаптеры, требующие COM-среду Windows;
        // они добавлены в IgnoredTypes, поэтому проверяем их отдельно на отсутствие нарушений.
        var infra = PublicContractVerifier.VerifyAssembly(
            "MARS.AudioController.Infrastructure",
            options
        );
        Assert.Empty(infra.Violations);
    }
}
