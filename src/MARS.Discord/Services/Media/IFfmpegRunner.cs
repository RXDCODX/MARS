namespace MARS.Discord.Services.Media;

/// <summary>
/// Запуск ffmpeg и ffprobe.
/// </summary>
/// <remarks>
/// Seam нужен, чтобы проверять логику сжатия и нарезки без бинарников на
/// машине: <c>FFMpegCore</c> ищет <c>ffmpeg</c> в <c>PATH</c>, и тест, который
/// от него зависит, падал бы там, где ffmpeg не установлен.
/// </remarks>
public interface IFfmpegRunner
{
    /// <summary>
    /// Выполняет команду ffmpeg. false — процесс завершился с ошибкой или не
    /// отработал вовсе.
    /// </summary>
    Task<bool> RunAsync(string arguments, CancellationToken cancellationToken = default);

    /// <summary>Длительность медиа в секундах, как её сообщает ffprobe.</summary>
    Task<TimeSpan> GetDurationAsync(string path, CancellationToken cancellationToken = default);
}
