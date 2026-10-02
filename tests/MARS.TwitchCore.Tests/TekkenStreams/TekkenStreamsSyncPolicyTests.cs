using MARS.TwitchCore.Services.TekkenStreams;

namespace MARS.TwitchCore.Tests.TekkenStreams;

/// <summary>
/// Решения по каналам теккен-стримов: какие чаты подключать и какие
/// сообщения пересылать. Правила перенесены из
/// <c>TekkenStreamsDiscordForwarderService</c> монолита.
/// </summary>
public class TekkenStreamsSyncPolicyTests
{
    private const string OwnChannel = "mars";

    [Fact]
    public void BuildToJoin_ReturnsOnlyStreamsWeAreNotInYet()
    {
        var streams = new[] { "ann", "bob", "cid" };
        var joined = new[] { "bob" };

        var result = TekkenStreamsSyncPolicy.BuildToJoin(streams, joined, OwnChannel);

        Assert.Equal(["ann", "cid"], result);
    }

    /// <summary>
    /// Свой канал в списке не подключается: бот и так в нём сидит, а повторный
    /// Join в монолите приводил к дубликату подписки.
    /// </summary>
    [Fact]
    public void BuildToJoin_SkipsTheOwnChannel()
    {
        var result = TekkenStreamsSyncPolicy.BuildToJoin([OwnChannel], [], OwnChannel);

        Assert.Empty(result);
    }

    /// <summary>
    /// Регистр не важен: логин Twitch пишут как попало.
    /// </summary>
    [Fact]
    public void BuildToJoin_IgnoresCase()
    {
        var result = TekkenStreamsSyncPolicy.BuildToJoin(["ANN"], ["ann"], OwnChannel);

        Assert.Empty(result);
    }

    /// <summary>
    /// Из чатов покидаются все, кроме текущих стримов и своего канала: иначе
    /// бот остался бы в чатах завершившихся трансляций.
    /// </summary>
    [Fact]
    public void BuildToLeave_ReturnsJoinedChannelsThatAreNoLongerStreaming()
    {
        var joined = new[] { OwnChannel, "ann", "bob" };
        var streams = new[] { "ann" };

        var result = TekkenStreamsSyncPolicy.BuildToLeave(joined, streams, OwnChannel);

        Assert.Equal(["bob"], result);
    }

    [Fact]
    public void BuildToLeave_ReturnsNothing_WhenEveryJoinedChannelIsStillLive()
    {
        var joined = new[] { OwnChannel, "ann" };
        var streams = new[] { "ann" };

        Assert.Empty(TekkenStreamsSyncPolicy.BuildToLeave(joined, streams, OwnChannel));
    }

    /// <summary>
    /// Формат строки для Discord — с каналом и автором: без них пересылка
    /// неотличима от сообщений основного канала.
    /// </summary>
    [Fact]
    public void FormatMessage_PrefixesTheChannelAndTheAuthor()
    {
        var result = TekkenStreamsSyncPolicy.FormatMessage("ann", "Аня", "привет");

        Assert.Equal("[ann] Аня: привет", result);
    }

    /// <summary>
    /// Сообщение собственного канала не пересылается, и привязанный канал
    /// без настроенного Discord-канала — тоже: слать некуда.
    /// </summary>
    [Fact]
    public void ShouldForward_FiltersOwnChannelAndUnconfiguredTarget()
    {
        Assert.False(TekkenStreamsSyncPolicy.ShouldForward(OwnChannel, [OwnChannel], 0));
        Assert.False(TekkenStreamsSyncPolicy.ShouldForward("ann", ["ann"], 0));
        Assert.True(TekkenStreamsSyncPolicy.ShouldForward("ann", ["ann"], 123));
    }

    [Fact]
    public void ShouldForward_IgnoresCase()
    {
        Assert.False(TekkenStreamsSyncPolicy.ShouldForward(OwnChannel, ["ann"], 123));
    }

    /// <summary>
    /// У пустого сообщения пересылать нечего: в монолите Discord получал
    /// хвост вида «[ann] Аня: ».
    /// </summary>
    [Fact]
    public void FormatMessage_KeepsTheTextAsIs_WhenThereIsNothingToPrefix()
    {
        Assert.Equal("[ann] Аня: ", TekkenStreamsSyncPolicy.FormatMessage("ann", "Аня", ""));
    }
}
