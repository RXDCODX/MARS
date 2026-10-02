using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Commands.Tests;

/// <summary>
/// Регистрация исполнителя команд в контейнере.
///
/// Проверяются именно разрешения, а не сам факт вызова расширения: ошибка
/// регистрации проявляется не сразу, а на живом стенде — когда очередь команд
/// не находит исполнителя и сообщение об ошибке уходит в лог команды, а не
/// наверх.
/// </summary>
public sealed class CommandExecutorRegistrationTests
{
    [Fact]
    public void ExecutorIsResolvableAsServiceAndAsHostedService()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddCommandExecutorServices()
            .BuildServiceProvider();

        var asService = provider.GetRequiredService<ICommandService>();
        var asHosted = provider.GetServices<IHostedService>();

        Assert.IsType<CommandExecutorService>(asService);
        Assert.Single(asHosted);
    }

    [Fact]
    public void SingletonRegistrationsResolveIntoTheSameInstance()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddCommandExecutorServices()
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<CommandFactory>();
        var adapter = provider.GetRequiredService<ApiCommandService>();

        Assert.Same(provider.GetRequiredService<CommandFactory>(), factory);
        Assert.NotNull(adapter);
        Assert.Same(
            provider.GetRequiredService<TwitchCommandService>(),
            provider.GetRequiredService<TwitchCommandService>()
        );
        Assert.Same(
            provider.GetRequiredService<ICommandService>(),
            provider.GetRequiredService<CommandExecutorService>()
        );
    }
}
