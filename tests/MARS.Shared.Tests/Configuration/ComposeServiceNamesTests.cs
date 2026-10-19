using MARS.Shared.Configuration;

namespace MARS.Shared.Tests.Configuration;

/// <summary>
/// Имя проекта (<c>MARS.TwitchCore</c>) → имя службы <c>docker-compose.yml</c>
/// (<c>twitch-core</c>).
/// </summary>
/// <remarks>
/// Преобразование нужно в двух местах локальной отладки: имя лейбла <c>container</c>
/// для логов на хосте и имя инстанса для Prometheus. Список соответствий в коде
/// разошёлся бы молча — опечатка в нём выглядит не ошибкой компиляции, а пустым
/// графиком в Grafana, и искать приходилось бы в другом месте.
/// </remarks>
public class ComposeServiceNamesTests
{
    /// <summary>
    /// Слова из одних заглавных не разбиваются на буквы: иначе TTS стал бы «t-t-s»,
    /// а OBS — «o-b-s», и ни одно из имён в compose не существовало бы.
    /// </summary>
    [Theory]
    [InlineData("MARS.Gateway", "gateway")]
    [InlineData("MARS.TwitchCore", "twitch-core")]
    [InlineData("MARS.WaifuGacha", "waifu-gacha")]
    [InlineData("MARS.SoundRequest", "sound-request")]
    [InlineData("MARS.CinemaQueue", "cinema-queue")]
    [InlineData("MARS.MediaStorage", "media-storage")]
    [InlineData("MARS.Videos365", "videos365")]
    [InlineData("MARS.TTS", "tts")]
    [InlineData("MARS.OBS", "obs")]
    [InlineData("MARS.AudioController", "audio-controller")]
    public void Имя_проекта_становится_именем_службы(string project, string expected)
    {
        Assert.Equal(expected, ComposeServiceNames.From(project));
    }

    /// <summary>
    /// Без имени нет и имени службы: возвращать строку из пробелов значило бы
    /// отдать Prometheus цель вида <c>host.docker.internal:</c> без порта.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("MARS.")]
    public void Без_имени_проекта_имени_службы_нет(string project)
    {
        Assert.Null(ComposeServiceNames.From(project));
    }
}
