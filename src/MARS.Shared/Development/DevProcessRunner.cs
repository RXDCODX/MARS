using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MARS.Shared.Development;

/// <summary>
/// Запуск внешнего процесса и ожидание его завершения.
/// </summary>
/// <remarks>
/// <para>
/// Исключений не бросает: отказ возвращается результатом. <c>docker</c>, которого
/// нет в PATH, — это состояние машины разработчика, а не ошибка сервиса, и
/// вызывающий код обязан получить причину строкой.
/// </para>
/// <para>
/// Вывод читается событиями процесса, а не по очереди через <c>ReadToEnd()</c>:
/// синхронное чтение одного потока, пока второй заполняет свой буфер, вешает
/// ожидание навсегда.
/// </para>
/// </remarks>
public sealed class DevProcessRunner : IDevProcessRunner
{
    /// <summary>Сколько ждать после принудительной остановки, чтобы дочитать вывод.</summary>
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Предельное время жизни процесса: <c>-1</c> — ждать сколько угодно.</summary>
    private static int TimeoutMilliseconds(TimeSpan? timeout)
    {
        var milliseconds =
            timeout is null || timeout <= TimeSpan.Zero ? -1 : (int)timeout.Value.TotalMilliseconds;

        return milliseconds;
    }

    /// <summary>Настройки запуска: аргументы списком, вывод перенаправлен.</summary>
    private static ProcessStartInfo StartInfo(DevProcessRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    /// <summary>
    /// Строка вывода: копится для сообщения об ошибке и, если попросили,
    /// печатается в консоль. Замок — потому что события stdout и stderr приходят
    /// с потоков пула, а читается буфер из вызывающего.
    /// </summary>
    private static void Write(string? line, StringBuilder buffer, TextWriter? echo)
    {
        if (line is not null)
        {
            lock (buffer)
            {
                buffer.AppendLine(line);
            }

            echo?.WriteLine(line);
        }
    }

    private static string Read(StringBuilder buffer)
    {
        lock (buffer)
        {
            return buffer.ToString();
        }
    }

    /// <summary>Останавливает процесс, не считая это событие ошибкой шага.</summary>
    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception)
        {
            // Процесс успел завершиться сам или уже недоступен: решение всё равно
            // принимается по тому, жив ли он, а не по исключению остановки.
        }
    }

    /// <inheritdoc />
    public DevProcessResult Run(DevProcessRequest request)
    {
        var result = new DevProcessResult(
            -1,
            string.Empty,
            string.Empty,
            false,
            $"Процесс {request.FileName} не выполнился"
        );

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var echoOut = request.StreamOutput ? Console.Out : null;
        var echoErr = request.StreamOutput ? Console.Error : null;

        using var process = new Process { StartInfo = StartInfo(request) };

        try
        {
            process.OutputDataReceived += (_, args) => Write(args.Data, output, echoOut);
            process.ErrorDataReceived += (_, args) => Write(args.Data, errors, echoErr);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timedOut = !process.WaitForExit(TimeoutMilliseconds(request.Timeout));

            if (timedOut)
            {
                Kill(process);
                process.WaitForExit((int)DrainTimeout.TotalMilliseconds);
            }
            else
            {
                // Ожидание с пределом не гарантирует доставку строк: без вызова без
                // предела stdout и stderr могут остаться недопрочитанными.
                process.WaitForExit();
            }

            var running = !process.HasExited;

            result = new DevProcessResult(
                running ? -1 : process.ExitCode,
                Read(output),
                Read(errors),
                timedOut,
                running ? $"Процесс {request.FileName} не остановился принудительно" : null
            );
        }
        catch (Exception exception)
        {
            result = new DevProcessResult(
                -1,
                Read(output),
                Read(errors),
                false,
                $"Не удалось выполнить {request.FileName}: {exception.Message}"
            );
        }

        return result;
    }
}
