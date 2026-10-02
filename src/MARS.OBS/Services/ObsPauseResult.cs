namespace MARS.OBS.Services;

public class ObsPauseResult
{
    public bool Success { get; set; }
    public bool IsPaused { get; set; }
    public string? Error { get; set; }
    public string? ScreenshotPath { get; set; }

    public static ObsPauseResult Ok(bool isPaused, string? screenshotPath = null) =>
        new()
        {
            Success = true,
            IsPaused = isPaused,
            ScreenshotPath = screenshotPath,
        };

    public static ObsPauseResult Fail(string error) => new() { Success = false, Error = error };
}
