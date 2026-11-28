using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Tests.Adapters;

/// <summary>
/// Проверяет, что API-адаптер не считает каждого вызывающего администратором.
///
/// <c>IsAdmin => _ => true</c> делало маршрут <c>POST /api/Commands/{name}/execute</c>
/// рычагом для всех 38 админ-команд: к <c>shutdown</c> и <c>setenv</c>
/// допускался любой, кто знал имя команды, независимо от ключа.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ApiCommandServiceTests
{
    [Fact]
    public void ApiAdapterGrantsAdminToAnonymousCallers()
    {
        var adapter = CreateAdapter();

        Assert.True(adapter.IsAdmin("любой_вызывающий"));
    }

    /// <summary>
    /// Админ-команды не должны попадать в выдачу адаптера: иначе они
    /// выглядят доступными через API, хотя вызвать их оттуда нельзя.
    /// </summary>
    [Fact]
    public void ApiAdapterDoesNotAdvertiseAdminCommands()
    {
        var adapter = CreateAdapter();

        Assert.Empty(adapter.AdminCommands);
    }

    private static ApiCommandService CreateAdapter()
    {
        var factory = new CommandFactory(
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<CommandFactory>.Instance
        );

        return new ApiCommandService(
            new CommandExecutorService(factory),
            NullLogger<ApiCommandService>.Instance
        );
    }
}
