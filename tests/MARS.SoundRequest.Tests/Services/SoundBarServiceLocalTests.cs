using MARS.SoundRequest.Services.SoundBarService;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Заглушка звука для разработки.
///
/// Проверяется, что она ничего не делает и всегда отвечает «звук есть»: иначе на
/// машине без контроллера звука команды проигрывания выглядели бы поломанными.
/// </summary>
public class SoundBarServiceLocalTests
{
    private readonly SoundBarServiceLocal _service = new();

    [Fact]
    public async Task HealthIsAlwaysPositive()
    {
        Assert.True(await _service.CheckHealthAsync());
    }

    [Fact]
    public async Task BagCountIsNotAvailable()
    {
        Assert.Equal("Local: Bag count not available", await _service.GetBagCount());
    }

    /// <summary>
    /// Список процессов может быть пустым: команда «выключить звук» приходит без
    /// аргументов, и заглушка обязана это пережить.
    /// </summary>
    [Fact]
    public async Task MuteAndUnmuteDoNothing()
    {
        await _service.Mute("obs64");
        await _service.Mute();
        await _service.Unmute();
    }
}
