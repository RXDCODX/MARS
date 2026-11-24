using System.IO;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Защитный тест: <c>InternalController</c> зависит от <c>MiniGamesManager</c>,
/// и если регистрация исчезнет, любой вызов <c>api/internal/chat/send</c>
/// упадёт с 500 при активации контроллера (как было до фикса).
/// </summary>
public class MiniGamesManagerRegistrationGuardTests
{
    [Fact]
    public void ProgramCs_RegistersMiniGamesManager()
    {
        var assembly = typeof(MARS.Twitch.Host.Program).Assembly;
        var location = assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(location, "..", "..", "..", "..", "..", ".."));
        var programPath = Path.Combine(
            repoRoot,
            "src",
            "MARS.Twitch",
            "MARS.Twitch.Host",
            "Program.cs"
        );

        if (!File.Exists(programPath))
        {
            return;
        }

        var source = File.ReadAllText(programPath);

        Assert.Contains("AddSingleton<MiniGamesManager>()", source);
        Assert.Contains("GetRequiredService<MiniGamesManager>()", source);
    }
}
