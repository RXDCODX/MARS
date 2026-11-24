using MARS.Shared.Kernel;

namespace MARS.Discord.Infrastructure.Services.Media;

/// <summary>
/// Часть видео, получившаяся после нарезки слишком большого файла.
/// </summary>
public sealed record CompressedVideoSegment(Stream Stream, string FileName);
