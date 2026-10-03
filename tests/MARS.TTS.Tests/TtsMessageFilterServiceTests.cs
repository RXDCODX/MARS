using MARS.TTS.Services;

namespace MARS.TTS.Tests;

/// <summary>
/// Фильтр сообщений TTS: пустое не произносится, повторы схлопываются, а
/// повторное сообщение в окне дедупликации отбрасывается.
///
/// Схлопывание — то, что слышно на стриме: «аааааа» TTS произносит один раз,
/// а не шесть, и повтор фразы «да да да да» — тоже один раз.
/// </summary>
public class TtsMessageFilterServiceTests
{
    [Fact]
    public void EmptyMessageIsRejected()
    {
        var filter = new TtsMessageFilterService();

        Assert.False(filter.FilterMessage("   ").Success);
        Assert.False(filter.FilterMessage(string.Empty).Success);
    }

    [Fact]
    public void MessageIsPassedThroughWhenFilterIsOff()
    {
        var filter = new TtsMessageFilterService { IsFilterEnabled = false };

        Assert.True(filter.FilterMessage("аааааа").Success);
        Assert.Equal("аааааа", filter.FilterMessage("аааааа").Result);
    }

    /// <summary>
    /// Повтор слова схлопывается до одного вхождения: иначе TTS произносит
    /// «привет привет привет» вместо «привет».
    /// </summary>
    [Fact]
    public void LongWordRepeatsAreCollapsed()
    {
        var filter = new TtsMessageFilterService();

        var result = filter.FilterMessage("привет привет привет");

        Assert.True(result.Success);
        Assert.Equal("привет", result.Result);
    }

    /// <summary>
    /// Два повтора подряд остаются: «да да» — это ответ, а не заикание.
    /// </summary>
    [Fact]
    public void ShortRepeatsAreKept()
    {
        var filter = new TtsMessageFilterService();

        var result = filter.FilterMessage("да да");

        Assert.Equal("да да", result.Result);
    }

    [Fact]
    public void ThreeIdenticalWordsCollapseToOne()
    {
        var filter = new TtsMessageFilterService();

        var result = filter.FilterMessage("да да да");

        Assert.Equal("да", result.Result);
    }

    [Fact]
    public void RepeatedPhraseIsCollapsed()
    {
        var filter = new TtsMessageFilterService();

        var result = filter.FilterMessage("спасибо подписку спасибо подписку");

        Assert.Equal("спасибо подписку", result.Result);
    }

    [Fact]
    public void SingleWordIsReturnedAsIs()
    {
        var filter = new TtsMessageFilterService();

        Assert.Equal("привет", filter.FilterMessage("привет").Result);
    }

    [Fact]
    public void DuplicateInsideWindowIsRejected()
    {
        var filter = new TtsMessageFilterService(dedupWindow: TimeSpan.FromMinutes(10));

        Assert.True(filter.FilterMessage("привет").Success);
        Assert.False(filter.FilterMessage("ПРИВЕТ ").Success);
    }

    [Fact]
    public void ExpiredDuplicateIsAccepted()
    {
        var filter = new TtsMessageFilterService(dedupWindow: TimeSpan.FromMilliseconds(1));

        Assert.True(filter.FilterMessage("привет").Success);
        Thread.Sleep(20);
        Assert.True(filter.FilterMessage("привет").Success);
    }

    /// <summary>
    /// Загрузка состояния — заглушка: у фильтра нет постоянного хранилища, но
    /// интерфейс требует метод, и он не должен падать или блокировать.
    /// </summary>
    [Fact]
    public async Task LoadStateCompletesImmediately()
    {
        var filter = new TtsMessageFilterService();

        await filter.LoadStateAsync(TestContext.Current.CancellationToken);

        Assert.True(true);
    }

    /// <summary>
    /// Фильтр держит поток сообщений: после сотни принятых сообщений он чистит
    /// память от устаревших записей и продолжает отбрасывать дубликаты. Без
    /// проверки на объёме этот путь молча не выполнялся бы, а словарь рос бы
    /// вместе с чатом стримера.
    /// </summary>
    [Fact]
    public void DuplicatesAreStillRejectedAfterMemoryCleanup()
    {
        var filter = new TtsMessageFilterService();
        var first = filter.FilterMessage("первое сообщение");
        Assert.True(first.Success);

        for (var index = 0; index < 120; index++)
        {
            Assert.True(filter.FilterMessage($"сообщение {index}").Success);
        }

        var repeated = filter.FilterMessage("первое сообщение");

        Assert.False(repeated.Success);
        Assert.Equal("Обнаружен дубликат сообщения", repeated.ErrorMessage);
    }
}
