using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// <c>Platform</c> объявлен как <c>[Flags]</c>, но <c>IsAvailableOnPlatform</c>
/// сравнивал его через <c>Enumerable.Contains</c> — то есть сравнение велось как
/// для обычного перечисления. Из-за этого агрегат <c>Platform.All</c>
/// не означал «везде», а <c>/user/All</c> в контроллере давал бессмысленный ответ.
/// </summary>
public sealed class PlatformFlagsTests
{
    [Fact]
    public void AllMeansEveryPlatform()
    {
        var command = new ExampleCommand();

        Assert.True(command.IsAvailableOnPlatform(Platform.Twitch));
        Assert.True(command.IsAvailableOnPlatform(Platform.Telegram));
        Assert.True(command.IsAvailableOnPlatform(Platform.Discord));
        Assert.True(command.IsAvailableOnPlatform(Platform.Api));
    }

    /// <summary>
    /// <c>Platform.None</c> не должен проходить ни в одну команду: это «платформа
    /// не задана», а не «разрешено везде».
    /// </summary>
    [Fact]
    public void NoneIsNotEveryPlatform()
    {
        var command = new ExampleCommand();

        Assert.False(command.IsAvailableOnPlatform(Platform.None));
    }

    [Fact]
    public void AggregatePlatformIsAcceptedByFlagAwareCheck()
    {
        var command = new AggregatePlatformCommand();

        Assert.True(command.IsAvailableOnPlatform(Platform.Twitch));
        Assert.False(command.IsAvailableOnPlatform(Platform.Vk));
    }

    /// <summary>
    /// <see cref="Platform.All"/> обязан включать каждую платформу поимённо:
    /// агрегат без <c>Vk</c> прошёл бы сравнение <c>Contains</c> и тихо уронил
    /// бы команды, объявленные только под VK.
    /// </summary>
    [Fact]
    public void AllCoversEveryNamedPlatform()
    {
        foreach (
            var platform in new[]
            {
                Platform.Api,
                Platform.Telegram,
                Platform.Twitch,
                Platform.Discord,
                Platform.Vk,
            }
        )
        {
            Assert.True(Platform.All.HasFlag(platform), $"Platform.All не покрывает {platform}");
        }
    }
}
