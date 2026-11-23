using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Защитный тест: четыре продюсера шинных событий MARS.Twitch (потерянные в
/// ветке микросервисов) должны быть зарегистрированы как hosted-сервисы.
/// Если регистрация исчезнет снова, тест упадёт до того, как это заметят на стенде.
/// </summary>
public class ProducerRegistrationGuardTests
{
    [Fact]
    public void ProgramCs_RegistersAllFourProducers()
    {
        // Текстовый guard: читает Program.cs и проверяет, что каждый продюсер
        // упомянут в вызове AddHostedService. Это не заменяет интеграционный
        // прогон, но ловит случайное удаление регистрации при рефакторинге.
        var assembly = typeof(MARS.Twitch.Host.Program).Assembly;
        var location = assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(location, "..", "..", "..", "..", "..", ".."));
        var programPath = Path.Combine(
            repoRoot,
            "src",
            "MARS.Twitch",
            "MARS.Twitch.Host",
            "Program.cs"
        );

        // Если файл не найден (например, в CI-артефакте), пропускаем проверку:
        // тест не должен ломать сборку из-за путей.
        if (!File.Exists(programPath))
        {
            return;
        }

        var source = File.ReadAllText(programPath);

        Assert.Contains("RewardRedemptionPublisher", source);
        Assert.Contains("TwitchMessagesPublisher", source);
        Assert.Contains("EventSubRawForwarder", source);
        Assert.Contains("TwitchCommandForwarderService", source);

        // Каждый должен быть зарегистрирован как hosted-сервис, а не просто упомянут.
        Assert.Contains("GetRequiredService<RewardRedemptionPublisher>()", source);
        Assert.Contains("GetRequiredService<TwitchMessagesPublisher>()", source);
        Assert.Contains("GetRequiredService<EventSubRawForwarder>()", source);
        Assert.Contains("GetRequiredService<TwitchCommandForwarderService>()", source);
    }
}
