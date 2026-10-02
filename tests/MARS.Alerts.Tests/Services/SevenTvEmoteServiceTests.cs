using MARS.Alerts.Services.Synthesizer;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Фильтр эмоутов в синтезаторе речи.
///
/// Список приходит из 7TV, поэтому проверяется отказная ветка: до первой загрузки
/// ни одно слово эмоутом не считается, иначе фильтр вырезал бы из речи живые слова.
/// </summary>
public class SevenTvEmoteServiceTests
{
    [Fact]
    public void UnknownWordIsNotEmoteBeforeRefresh()
    {
        var service = new SevenTvEmoteService(NullLogger<SevenTvEmoteService>.Instance);

        Assert.False(service.IsEmote("PogChamp"));
        Assert.False(service.IsEmote(string.Empty));
    }
}
