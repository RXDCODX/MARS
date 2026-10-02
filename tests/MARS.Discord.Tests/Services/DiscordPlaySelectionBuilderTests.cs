using System.Reflection;
using DSharpPlus.Entities;
using MARS.Discord.Models;
using MARS.Discord.Services.PlayRequest;

namespace MARS.Discord.Tests.Services;

/// <summary>
/// Меню выбора трека в Discord.
///
/// Пользователь выбирает трек из выпадающего списка, поэтому у каждого трека есть
/// номер, а у списка — идентификатор сессии: без него ответ пришёл бы в другую
/// сессию и трек не включился бы.
/// </summary>
public class DiscordPlaySelectionBuilderTests
{
    /// <summary>
    /// Треки нумеруются с единицы в порядке выдачи: нумерация и есть порядок
    /// выбора.
    /// </summary>
    [Fact]
    public void TracksAreNumberedFromOne()
    {
        var select = BuildSelectComponent(Session());

        Assert.Equal(["1. Аяка", "2. Мику"], select.Options.Select(option => option.Label));
    }

    /// <summary>
    /// Значение опции — её номер: по нему выбирается трек.
    /// </summary>
    [Fact]
    public void OptionValuesAreIndexes()
    {
        var select = BuildSelectComponent(Session());

        Assert.Equal(["0", "1"], select.Options.Select(option => option.Value));
    }

    /// <summary>
    /// Идентификатор компонента включает сессию: иначе выбор из старой сессии
    /// применялся бы к новой.
    /// </summary>
    [Fact]
    public void ComponentIdCarriesSessionId()
    {
        var select = BuildSelectComponent(Session(sessionId: "abc123"));

        Assert.Contains("abc123", select.CustomId);
    }

    /// <summary>
    /// В сообщении перечислены все треки: пользователь выбирает вслепую, если их
    /// не видно.
    /// </summary>
    [Fact]
    public void MessageListsFoundTracks()
    {
        var message = BuildMessageBuilder(Session());

        Assert.Contains("Аяка", message.Content);
        Assert.Contains("Мику", message.Content);
    }

    /// <summary>
    /// Ответ на взаимодействие собирается так же, как сообщение: текст и выбор в
    /// одном ответе.
    /// </summary>
    [Fact]
    public void InteractionResponseCarriesTracks()
    {
        var response = BuildInteractionResponseBuilder(Session());

        Assert.NotNull(response);
    }

    private static DiscordPlaySelectionSession Session(string sessionId = "session-1") =>
        new()
        {
            SessionId = sessionId,
            ChannelId = 42,
            UserId = 43,
            Query = "аяка",
            Tracks =
            [
                new BaseTrackInfo
                {
                    TrackName = "Аяка",
                    Url = new Uri("https://example.test/1"),
                    Duration = TimeSpan.FromMinutes(3),
                },
                new BaseTrackInfo
                {
                    TrackName = "Мику",
                    Url = new Uri("https://example.test/2"),
                    Duration = TimeSpan.FromMinutes(4),
                },
            ],
        };

    private static DiscordSelectComponent BuildSelectComponent(
        DiscordPlaySelectionSession session
    ) => Call<DiscordSelectComponent>("BuildSelectComponent", session);

    private static DiscordMessageBuilder BuildMessageBuilder(DiscordPlaySelectionSession session) =>
        Call<DiscordMessageBuilder>("BuildMessageBuilder", session);

    private static DiscordInteractionResponseBuilder BuildInteractionResponseBuilder(
        DiscordPlaySelectionSession session
    ) => Call<DiscordInteractionResponseBuilder>("BuildInteractionResponseBuilder", session);

    private static T Call<T>(string name, DiscordPlaySelectionSession session)
    {
        var method = typeof(DiscordPlayRequestService).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        return (T)method.Invoke(null, [session])!;
    }
}
