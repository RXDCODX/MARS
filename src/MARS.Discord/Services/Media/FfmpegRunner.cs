using System.Diagnostics;
using FFMpegCore;

namespace MARS.Discord.Services.Media;

/// <summary>
/// Реальный запуск ffmpeg и ffprobe.
/// </summary>
/// <remarks>
/// ffmpeg запускается как процесс напрямую, а не через <c>FFMpegArguments</c>:
/// для нарезки на сегменты нужен <c>-f segment</c> с шаблоном имени, а такой
/// вызов у FFMpegCore 5.4.0 недоступен. ffprobe для длительности по-прежнему
/// читается библиотекой — так же, как в <c>MARS.MediaStorage</c>.
/// </remarks>
public sealed class FfmpegRunner : IFfmpegRunner
{
    public async Task<bool> RunAsync(
        string arguments,
        CancellationToken cancellationToken = default
    )
    {
        var result = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startInfo = new ProcessStartInfo("ffmpeg")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var argument in SplitArguments(arguments))
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                result = false;
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
                result = process.ExitCode == 0;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            result = false;
        }

        return result;
    }

    public async Task<TimeSpan> GetDurationAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var duration = TimeSpan.Zero;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var analysis = await FFProbe.AnalyseAsync(path, cancellationToken: cancellationToken);
            duration = analysis.Duration;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            duration = TimeSpan.Zero;
        }

        return duration;
    }

    /// <summary>
    /// Аргументы разбираются по пробелам с сохранением кучек в кавычках:
    /// <c>-vf scale='min(1280,iw)':...</c> содержит пробел, и наивное
    /// разбиение порвало бы фильтр на три аргумента.
    /// </summary>
    private static IEnumerable<string> SplitArguments(string arguments)
    {
        var current = new System.Text.StringBuilder();
        var quote = '\0';

        foreach (var symbol in arguments)
        {
            if (quote != '\0')
            {
                if (symbol == quote)
                {
                    quote = '\0';
                }
                else
                {
                    current.Append(symbol);
                }
            }
            else if (symbol == '"' || symbol == '\'')
            {
                quote = symbol;
            }
            else if (char.IsWhiteSpace(symbol))
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
            {
                current.Append(symbol);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
