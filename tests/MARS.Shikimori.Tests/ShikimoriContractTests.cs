using MARS.Shared.Clients;

namespace MARS.Shikimori.Tests;

/// <summary>
/// Контракт между MARS.Shikimori и потребителями не должен молча меняться:
/// именно на нём держатся MARS.WaifuGacha, MARS.Admin и команда randomanime.
/// </summary>
public class ShikimoriContractTests
{
    /// <summary>
    /// Картинка отдаётся в двух видах: абсолютной ссылкой и тем же путём, каким
    /// его отдал Shikimori. Второй вид нужен MARS.WaifuGacha: там картинки
    /// хранятся в базе относительными путями, и <c>WaifuRollController</c>
    /// нормализует по этой форме.
    /// </summary>
    [Fact]
    public void CharacterRef_CarriesBothTheAbsoluteAndTheRawImage()
    {
        var character = new ShikimoriCharacterRef(
            1,
            "Naruto",
            "Наруто",
            "Описание",
            "https://shikimori.one/images/42/original.jpg",
            "/images/42/original.jpg",
            "Наруто",
            null
        );

        Assert.StartsWith("https://", character.ImageUrl);
        Assert.StartsWith("/", character.ImagePath);
    }

    /// <summary>
    /// Время окон отдаётся в секундах: <c>TimeSpan</c> в JSON зависит от версии
    /// сериализатора, и панель админа ломалась бы на формате "00:00:00.5".
    /// </summary>
    [Fact]
    public void RateLimiterInfo_UsesSecondsForTheWindows()
    {
        var info = new ShikimoriRateLimiterInfo(5, 90, 0.5, 12.25);

        Assert.Equal(0.5, info.SecondsToResetSecondWindow);
        Assert.Equal(12.25, info.SecondsToResetMinuteWindow);
    }
}
