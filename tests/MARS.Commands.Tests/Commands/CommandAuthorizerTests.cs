using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Commands.Services.Entitys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Проверка прав была в монолите и была выпилена при переносе: все четыре
/// адаптера получили <c>IsAdmin =&gt; _ =&gt; false</c>, из-за чего 38 админ-команд
/// стали недостижимы на всех платформах. Гейт возвращается в исполнитель команд,
/// потому что личность вызывающего знает сервис платформы, а не MARS.Commands.
/// </summary>
public sealed class CommandAuthorizerTests
{
    [Fact]
    public async Task AdminCommandIsRefusedForNonAdmin()
    {
        var executor = await CreateExecutorAsync();

        var response = await executor.ExecuteCommandAsync(
            "setenv",
            string.Empty,
            Platform.Twitch,
            isAdmin: false,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(CommandErrorCode.NotAllowed, response.ErrorCode);
    }

    [Fact]
    public async Task UserCommandIsAllowedForNonAdmin()
    {
        var executor = await CreateExecutorAsync();

        var response = await executor.ExecuteCommandAsync(
            "srlist",
            string.Empty,
            Platform.Twitch,
            isAdmin: false,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotEqual(CommandErrorCode.NotAllowed, response.ErrorCode);
    }

    /// <summary>
    /// Алиас должен разрешаться до проверки прав, иначе <c>!adhdstart</c> прошёл бы
    /// как пользовательская команда, хотя <c>adhd</c> админская.
    /// </summary>
    [Fact]
    public async Task AdminAliasIsRefusedForNonAdmin()
    {
        var executor = await CreateExecutorAsync();

        var response = await executor.ExecuteCommandAsync(
            "adhdstart",
            "60",
            Platform.Twitch,
            isAdmin: false,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(CommandErrorCode.NotAllowed, response.ErrorCode);
    }

    /// <summary>
    /// Права администратора не дают обхода гейта: проверка идёт до исполнения.
    /// </summary>
    [Fact]
    public async Task AdminCommandPassesGateForAdmin()
    {
        var executor = await CreateExecutorAsync();

        var response = await executor.ExecuteCommandAsync(
            "setenv",
            string.Empty,
            Platform.Twitch,
            isAdmin: true,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotEqual(CommandErrorCode.NotAllowed, response.ErrorCode);
    }

    private static async Task<ICommandService> CreateExecutorAsync()
    {
        var executor = new CommandExecutorService(
            new CommandFactory(
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<CommandFactory>.Instance
            )
        );

        await executor.StartAsync(TestContext.Current.CancellationToken);

        return executor;
    }
}
