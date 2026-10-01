using System.Reflection;
using MARS.Commands.Services;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Реестр команд наполняется в <c>BackgroundService.ExecuteAsync</c>.
/// <c>StartAsync</c> этот метод не доводит до конца: <c>ExecuteAsync</c>
/// возвращает уже завершённую задачу, и хост стартует с пустым реестром —
/// ни одна команда не находится, все обращения отвечают «команда не найдена».
/// </summary>
public sealed class CommandRegistryTests
{
    [Fact]
    public async Task RegistryIsPopulatedAfterHostStart()
    {
        var factory = new CommandFactory(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<CommandFactory>.Instance
        );

        var executor = new CommandExecutorService(factory);

        await executor.StartAsync(TestContext.Current.CancellationToken);

        var registered = ReadRegistry(executor);

        Assert.True(
            registered.Count > 0,
            "После StartAsync реестр пуст — ни одна команда не будет найдена."
        );

        await executor.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// <c>shutdown</c> и <c>setenv</c> отвечали сообщением об успехе, ничего не
    /// выполняя: «Сервер будет остановлен» и «Переменная окружения установлена».
    /// </summary>
    [Theory]
    [InlineData("shutdown", "Сервер будет остановлен")]
    [InlineData("setenv", "Переменная окружения установлена")]
    public async Task CommandDoesNotPromiseSuccess(string commandName, string falsePromise)
    {
        var factory = new CommandFactory(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<CommandFactory>.Instance
        );

        var executor = new CommandExecutorService(factory);

        await executor.StartAsync(TestContext.Current.CancellationToken);

        var response = await executor.ExecuteCommandAsync(
            commandName,
            string.Empty,
            Platform.Api,
            isAdmin: true,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.NotEqual(falsePromise, response);

        await executor.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Два класса объявляют одно и то же имя (<c>tanya</c>, <c>systeminfo</c>),
    /// и словарь реестра оставляет последнего победителя по порядку обхода
    /// <c>assembly.GetTypes()</c>, который не гарантирован. Команда ведёт себя
    /// случайно от запуска к запуску.
    /// </summary>
    [Fact]
    public void EveryDeclaredCommandClassGetsItsOwnRegistryEntry()
    {
        var factory = new CommandFactory(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<CommandFactory>.Instance
        );

        var declaredClasses = typeof(BaseCommand)
            .Assembly.GetTypes()
            .Where(t =>
                typeof(BaseCommand).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(BaseCommand)
            )
            .Select(t => factory.CreateCommand(t))
            .Where(c => c is not null)
            .Select(c => c!.CommandName)
            .ToArray();

        var registered = factory.CreateAllCommands();

        Assert.Equal(registered.Count, declaredClasses.Length);
    }

    private static Dictionary<string, BaseCommand> ReadRegistry(CommandExecutorService executor)
    {
        var field = typeof(CommandExecutorService).GetField(
            "_commands",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        return (Dictionary<string, BaseCommand>)field!.GetValue(executor)!;
    }
}
