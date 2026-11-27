using System.Text.Json;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Entitys;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Entitys = MARS.Commands.Application.Services.Entitys;

namespace MARS.Commands.Tests;

public sealed class CommandParityManifestTests
{
    [Fact]
    public async Task RegisteredCommandsMatchParityManifest()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "parity-manifest.json");
        Assert.True(File.Exists(manifestPath), $"Manifest not found: {manifestPath}");

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, TestContext.Current.CancellationToken));
        var active = document.RootElement.GetProperty("activeCommands")
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deferred = document.RootElement.GetProperty("deferred")
            .EnumerateArray()
            .Select(element => element.GetProperty("name").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var decommissioned = document.RootElement.GetProperty("decommissioned")
            .EnumerateArray()
            .Select(element => element.GetProperty("name").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var services = new ServiceCollection();
        services.AddSingleton<IMarsGrpcClient>(new Mock<IMarsGrpcClient>().Object);
        services.AddSingleton<ICommandService>(new Mock<ICommandService>().Object);
        services.AddSingleton<IAudioControllerClient>(new Mock<IAudioControllerClient>().Object);
        var factory = new CommandFactory(services.BuildServiceProvider(), NullLogger<CommandFactory>.Instance);
        var registered = factory.CreateAllCommands().Values
            .Select(command => command.CommandName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(active.SetEquals(registered.Except(deferred)), $"Active: {string.Join(",", active.OrderBy(x => x))}; Registered: {string.Join(",", registered.OrderBy(x => x))}; Deferred: {string.Join(",", deferred)}");
        Assert.True(deferred.IsSubsetOf(registered), "Deferred commands must remain registered until their capability is implemented.");
        Assert.False(decommissioned.Overlaps(registered), "Decommissioned commands must not be registered.");
    }
}