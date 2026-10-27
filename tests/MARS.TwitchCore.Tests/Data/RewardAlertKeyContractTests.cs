using MARS.Shared.Models.Rewards;

namespace MARS.TwitchCore.Tests.Data;

/// <summary>
/// Бэкфилл миграции <c>AddRewardAlertKey</c> и контракт ключей не разъезжаются.
/// </summary>
/// <remarks>
/// Ключ алерта — единственное, по чему оверлей понимает, что рисовать. Опечатка
/// в SQL оставила бы награду без маршрута (событие ушло бы с пустым ключом и
/// было бы отброшено издателем), а опечатка в контракте — сломала бы и клиент.
/// Обе стороны сверяются здесь, потому что список названий с эмодзи живёт только
/// в миграции.
/// </remarks>
public class RewardAlertKeyContractTests
{
    [Fact]
    public void Каждый_Ключ_Из_Миграции_Объявлен_В_Контракте()
    {
        var unknown = LegacyAlertKeySql
            .Keys.Where(key => !RewardAlertKeys.All.Contains(key))
            .ToArray();

        Assert.Empty(unknown);
    }

    /// <summary>
    /// Все награды, у которых было старое название, получают ключ. Исключения —
    /// <see cref="RewardAlertKeys.HelloVideo"/> (не награда Twitch, приходит из
    /// приветственного видео) и <see cref="RewardAlertKeys.WaifuRoll"/> (ролл по
    /// команде, в панели Twitch такой награды нет).
    /// </summary>
    [Fact]
    public void Миграция_Покрывает_Все_Награды_Кроме_НеTwitch()
    {
        var expected = RewardAlertKeys
            .All.Except([RewardAlertKeys.HelloVideo, RewardAlertKeys.WaifuRoll])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var actual = LegacyAlertKeySql.Keys.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Контракт_Не_Содержит_Повторов()
    {
        Assert.Equal(RewardAlertKeys.All.Length, RewardAlertKeys.All.Distinct().Count());
    }
}
