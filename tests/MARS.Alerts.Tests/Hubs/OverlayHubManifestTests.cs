using System.Reflection;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Манифест оверлейного хаба — производный контракт: имена берутся из веток
/// <see cref="TelegramusEvent"/>, а наличие аргумента — из сигнатуры
/// <see cref="IAlertsHub"/>. Ни один список в тесте не дублируется.
/// </summary>
public class OverlayHubManifestTests
{
    private const string ManifestResourceName =
        "MARS.Alerts.Presentation.Overlay.Hubs.overlay-hub.manifest.json";

    private static IReadOnlyList<OverlayManifestEntry> ReadManifest()
    {
        using var stream = typeof(IAlertsHub).Assembly.GetManifestResourceStream(
            ManifestResourceName
        );

        Assert.True(stream is not null, $"Ресурс не найден: {ManifestResourceName}");

        return OverlayManifestReader.Read(stream!);
    }

    private static string[] BranchNames() =>
        TelegramusEventContract
            .Branches.Select(branch => branch.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static string[] InterfaceMethods() =>
        typeof(IAlertsHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void Manifest_exists()
    {
        Assert.NotEmpty(ReadManifest());
    }

    [Fact]
    public void Manifest_matches_event_branches()
    {
        var names = ReadManifest()
            .Select(entry => entry.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(BranchNames(), names);
    }

    [Fact]
    public void Manifest_matches_hub_interface()
    {
        var names = ReadManifest()
            .Select(entry => entry.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(InterfaceMethods(), names);
    }

    [Fact]
    public void Manifest_covers_every_event_exactly_once()
    {
        var manifest = ReadManifest();

        Assert.Equal(TelegramusEventContract.Branches.Count, manifest.Count);
        Assert.Equal(manifest.Count, manifest.Select(entry => entry.Name).Distinct().Count());
    }

    [Fact]
    public void Payload_flags_match_hub_signatures()
    {
        var methods = typeof(IAlertsHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        foreach (var entry in ReadManifest())
        {
            Assert.True(methods.TryGetValue(entry.Name, out var method), entry.Name);
            Assert.Equal(method!.GetParameters().Length > 0, entry.HasPayload);
        }
    }

    /// <summary>
    /// Пустые события не несут информации, поэтому их методы не принимают
    /// аргументов. Список — про форму вызова, а не про имена событий.
    /// </summary>
    [Fact]
    public void Empty_events_have_no_parameters()
    {
        var expectedWithoutPayload = new[]
        {
            nameof(IAlertsHub.AudioQuizStop),
            nameof(IAlertsHub.Credits),
            nameof(IAlertsHub.Explosion),
            nameof(IAlertsHub.LeroyAlert),
            nameof(IAlertsHub.MichaelJackson),
            nameof(IAlertsHub.PhonkEdit),
        };

        var withPayload = typeof(IAlertsHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetParameters().Length != 1)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedWithoutPayload, withPayload);
    }

    [Fact]
    public void Alerts_hub_interface_declares_all_events_directly()
    {
        Assert.Empty(typeof(IAlertsHub).GetInterfaces());
        Assert.Equal(BranchNames(), InterfaceMethods());
    }
}
