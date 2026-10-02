using MARS.TTS.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TTS.Tests;

/// <summary>
/// Фильтр эмоутов в TTS.
///
/// Список приходит из 7TV, поэтому проверяется только отказная ветка: до первой
/// загрузки ни одно слово эмоутом не считается. Иначе фильтр вырезал бы из речи
/// живые слова при каждом запуске.
/// </summary>
public class SevenTvEmoteServiceTests
{
    [Fact]
    public void UnknownWordIsNotEmoteBeforeRefresh()
    {
        var service = new SevenTvEmoteService(NullLogger<SevenTvEmoteService>.Instance);

        Assert.False(service.IsEmote("PogChamp"));
        Assert.False(service.IsEmote("привет"));
    }

    /// <summary>
    /// Пустое слово не эмоут: в проверку может попасть пробел.
    /// </summary>
    [Fact]
    public void EmptyWordIsNotEmote()
    {
        var service = new SevenTvEmoteService(NullLogger<SevenTvEmoteService>.Instance);

        Assert.False(service.IsEmote(string.Empty));
    }
}
