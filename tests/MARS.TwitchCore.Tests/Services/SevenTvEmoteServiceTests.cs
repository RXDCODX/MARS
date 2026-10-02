using System.Reflection;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.Synthesizer;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Эмоуты 7TV для фильтра TTS.
///
/// Список эмоутов загружается из базы: обращение к 7TV на старте не происходит, если
/// список уже есть. Проверяется именно это — иначе сервис ждал бы сеть при каждом
/// запуске и стартовал бы медленнее.
/// </summary>
public class SevenTvEmoteServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();

    /// <summary>
    /// Эмоуты из баты становятся доступны сразу: TTS читает их при первой же
    /// проверке сообщения.
    /// </summary>
    [Fact]
    public async Task StoredEmotesAreAvailableWithoutNetwork()
    {
        await SeedAsync("PogChamp", "monkaW");
        var service = Create();

        await Execute(service);

        Assert.True(service.IsEmote("PogChamp"));
        Assert.True(service.IsEmote("monkaW"));
    }

    /// <summary>
    /// Слово не из списка не считается эмоутом: иначе фильтр вырезал бы из речи
    /// обычные слова.
    /// </summary>
    [Fact]
    public async Task UnknownWordIsNotEmote()
    {
        await SeedAsync("PogChamp");
        var service = Create();

        await Execute(service);

        Assert.False(service.IsEmote("привет"));
    }

    /// <summary>
    /// Регистр не важен: в чате эмоуты пишут как попало.
    /// </summary>
    [Fact]
    public async Task EmoteNameIsCaseInsensitive()
    {
        await SeedAsync("PogChamp");
        var service = Create();

        await Execute(service);

        Assert.True(service.IsEmote("pogchamp"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private SevenTvEmoteService Create() => new(_factory, NullLogger<SevenTvEmoteService>.Instance);

    private async Task SeedAsync(params string[] names)
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        foreach (var name in names)
        {
            db.SevenTvEmotes.Add(new SevenTvEmote { Name = name, LoadedAt = DateTime.Now });
        }

        await db.SaveChangesAsync(Token);
    }

    /// <summary>
    /// Загрузка идёт до первого ожидания, а цикл затем живёт десять минут. Поэтому
    /// тест ждёт появления эмоутов и только затем отменяет цикл.
    /// </summary>
    private static async Task Execute(SevenTvEmoteService service)
    {
        var method = typeof(SevenTvEmoteService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

        using var cts = new CancellationTokenSource();
        var running = (Task)method.Invoke(service, [cts.Token])!;

        for (var attempt = 0; attempt < 100 && !service.IsEmote("PogChamp"); attempt++)
        {
            await Task.Delay(20, Token);
        }

        await cts.CancelAsync();
        await running;
    }
}
