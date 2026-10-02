using MARS.Shared.Models;

namespace MARS.Shared.Tests.Models;

/// <summary>
/// Пользователь Twitch.
///
/// Сравнение идёт по идентификатору: коллекции участников чата и подписчиков
/// должны схлопывать одно и то же человека, даже если подпись в чате изменилась.
/// </summary>
public class TwitchUserTests
{
    [Fact]
    public void SameIdMeansSameUser()
    {
        var first = User("123", "pyro", "Pyro");
        var second = User("123", "pyro2", "Pyro2");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentIdMeansDifferentUser()
    {
        Assert.NotEqual(User("1", "pyro", "Pyro"), User("2", "pyro", "Pyro"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pyro")]
    public void OtherTypeIsNotEqual(object? other)
    {
        Assert.False(User("123", "pyro", "Pyro").Equals(other));
    }

    /// <summary>
    /// Подпись пользователя содержит имя, логин и идентификатор: она попадает в
    /// сообщения сервиса, и по ней должно быть видно, о ком речь.
    /// </summary>
    [Fact]
    public void DescriptionContainsNameAndLogin()
    {
        var description = User("123", "pyro", "Pyro").ToString();

        Assert.Contains("Pyro", description);
        Assert.Contains("pyro", description);
        Assert.Contains("123", description);
    }

    /// <summary>
    /// Новый пользователь появляется в списке как давно известный: без дат в
    /// подписках отображались бы годы.
    /// </summary>
    [Fact]
    public void TimestampsAreFilledByDefault()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var user = User("123", "pyro", "Pyro");
        var after = DateTime.Now.AddSeconds(1);

        Assert.InRange(user.CreatedAt, before, after);
        Assert.InRange(user.LastUpdated, before, after);
    }

    private static TwitchUser User(string id, string login, string displayName) =>
        new()
        {
            TwitchId = id,
            UserLogin = login,
            DisplayName = displayName,
        };
}
