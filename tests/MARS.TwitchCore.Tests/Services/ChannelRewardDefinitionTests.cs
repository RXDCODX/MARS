using MARS.Twitch.Api.Services.ChannelRewards;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Определение награды канала превращается в запрос создания награды Twitch.
/// </summary>
/// <remarks>
/// Преобразование — единственное место, где настройки награды попадают в
/// Helix. Потерянное поле означало бы награду, созданную с молчаливыми
/// значениями по умолчанию: зритель заплатил бы цену, которой не видел.
/// </remarks>
public class ChannelRewardDefinitionTests
{
    [Fact]
    public void Переносит_Настройки_В_Запрос()
    {
        var definition = new PyroAlertRewardDefinition
        {
            Title = "Пиро",
            Cost = 500,
            Prompt = "подсказка",
            BackgroundColor = "#9146FF",
            IsUserInputRequired = true,
            IsMaxPerStreamEnabled = true,
            MaxPerStream = 3,
            IsGlobalCooldownEnabled = true,
            GlobalCooldownSeconds = 60,
        };

        var request = definition.ToCreateRequest();

        Assert.Equal("Пиро", request.Title);
        Assert.Equal(500, request.Cost);
        Assert.True(request.IsEnabled);
        Assert.Equal("подсказка", request.Prompt);
        Assert.Equal("#9146FF", request.BackgroundColor);
        Assert.True(request.IsUserInputRequired);
        Assert.True(request.IsMaxPerStreamEnabled);
        Assert.Equal(3, request.MaxPerStream);
        Assert.True(request.IsGlobalCooldownEnabled);
        Assert.Equal(60, request.GlobalCooldownSeconds);
    }

    /// <summary>
    /// Награда без подсказки и без ограничений — законное состояние: значения
    /// по умолчанию TwitchLib совпадают с нашими.
    /// </summary>
    [Fact]
    public void Награда_С_Настройками_По_Умолчанию()
    {
        var definition = new PyroAlertRewardDefinition { Title = "Пиро", Cost = 100 };

        var request = definition.ToCreateRequest();

        Assert.Equal("Пиро", request.Title);
        Assert.Equal(100, request.Cost);
        Assert.Null(request.Prompt);
        Assert.False(request.IsUserInputRequired);
        Assert.False(request.IsMaxPerStreamEnabled);
        Assert.False(request.IsGlobalCooldownEnabled);
    }
}
