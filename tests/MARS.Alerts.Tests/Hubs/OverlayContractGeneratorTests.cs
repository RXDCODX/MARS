using System.Reflection;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;

namespace MARS.Alerts.Tests.Hubs;

/// <summary>
/// Генератор манифеста оверлея: источник — рефлексия по веткам сообщения и
/// сигнатурам клиента хаба. Обычный прогон только сверяет файл; с
/// <c>MARS_REGEN_OVERLAY=1</c> переписывает его (вызов из scripts/*/regenerate-overlay-contract).
/// </summary>
public class OverlayContractGeneratorTests
{
    private const string ManifestRelativePath =
        "src/MARS.Alerts/MARS.Alerts.Presentation.Overlay/Hubs/overlay-hub.manifest.json";

    [Fact]
    public void Regenerate_manifest_from_reflection()
    {
        var expected = BuildEntries();

        if (Environment.GetEnvironmentVariable("MARS_REGEN_OVERLAY") != "1")
        {
            return;
        }

        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar)
        );
        File.WriteAllText(path, OverlayManifestReader.Serialize(expected) + Environment.NewLine);
    }

    internal static IReadOnlyList<OverlayManifestEntry> BuildEntries()
    {
        var methods = typeof(IAlertsHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        return TelegramusEventContract
            .Branches.Select(branch => new OverlayManifestEntry(
                branch.Name,
                methods[branch.Name].GetParameters().Length > 0
            ))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MARS.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден корень репозитория (MARS.slnx).");
    }
}
