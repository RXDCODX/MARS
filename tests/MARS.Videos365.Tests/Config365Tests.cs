using MARS.Videos365.Configuration;

namespace MARS.Videos365.Tests;

public class Config365Tests
{
    private static Config365 Complete() =>
        new()
        {
            Site = "https://example.test",
            Login = "login",
            Password = "password",
            TelegramChannelId = -1001234567890,
        };

    [Fact]
    public void IsComplete_ReturnsTrue_WhenEveryFieldIsSet()
    {
        var result = Complete().IsComplete();

        Assert.True(result);
    }

    /// <summary>
    /// Конвейер не должен стартовать на неполной конфигурации: без логина и
    /// пароля источник отдаёт заглушку, и обход дедупликации по SiteId
    /// помечает как опубликованные видео, которых в канале нет.
    /// </summary>
    [Theory]
    [InlineData(nameof(Config365.Site))]
    [InlineData(nameof(Config365.Login))]
    [InlineData(nameof(Config365.Password))]
    public void IsComplete_ReturnsFalse_WhenTextFieldIsMissing(string fieldName)
    {
        var config = Complete();

        typeof(Config365).GetProperty(fieldName)!.SetValue(config, string.Empty);

        Assert.False(config.IsComplete());
    }

    [Fact]
    public void IsComplete_ReturnsFalse_WhenChannelIdIsZero()
    {
        var config = Complete();
        config.TelegramChannelId = 0;

        Assert.False(config.IsComplete());
    }

    /// <summary>
    /// Пустой (не заданный) конфиг — обычное состояние dev-развёртывания, а не
    /// ошибка конфигурации: воркер обязан корректно пропустить запуск.
    /// </summary>
    [Fact]
    public void IsComplete_ReturnsFalse_ForDefaultInstance()
    {
        var result = new Config365().IsComplete();

        Assert.False(result);
    }
}
