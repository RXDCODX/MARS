using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MARS.Telegram.Infrastructure.MTProto.WTelegram;

/// <summary>
/// Подключает WTelegram к Telegram через HTTP CONNECT-прокси.
/// </summary>
/// <remarks>
/// WTelegram сам по себе ни SOCKS, ни HTTP-прокси не умеет: он отдаёт создание
/// соединения наружу через <c>TcpHandler</c>. Здесь мы сначала соединяемся с
/// прокси, отправляем <c>CONNECT host:port HTTP/1.1</c>, читаем ответ до
/// пустой строки и, если прокси ответил 2xx, возвращаем тот же
/// <see cref="TcpClient"/> — дальше по нему пойдёт уже MTProto-трафик.
/// </remarks>
internal sealed class TelegramHttpProxyConnector(
    string proxyHost,
    int proxyPort,
    string? proxyUser,
    string? proxyPassword,
    ILogger logger
)
{
    public async Task<TcpClient> ConnectAsync(string targetHost, int targetPort)
    {
        var tcpClient = new TcpClient();
        try
        {
            await tcpClient.ConnectAsync(proxyHost, proxyPort);

            var stream = tcpClient.GetStream();
            var request = BuildRequest(targetHost, targetPort, proxyUser, proxyPassword);
            var bytes = Encoding.ASCII.GetBytes(request);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();

            // Читаем ответ побайтово, чтобы не съесть первый байт MTProto-
            // трафика, который прокси начнёт пересылать после пустой строки.
            var response = await ReadResponseAsync(stream);
            if (!IsSuccess(response))
            {
                tcpClient.Dispose();
                throw new InvalidOperationException(
                    $"HTTP-прокси {proxyHost}:{proxyPort} отклонил CONNECT {targetHost}:{targetPort}: "
                        + response.Trim()
                );
            }

            logger.LogDebug(
                "HTTP-прокси {Proxy} пропустил CONNECT {Target}",
                $"{proxyHost}:{proxyPort}",
                $"{targetHost}:{targetPort}"
            );

            return tcpClient;
        }
        catch
        {
            tcpClient.Dispose();
            throw;
        }
    }

    private static string BuildRequest(
        string targetHost,
        int targetPort,
        string? user,
        string? password
    )
    {
        var builder = new StringBuilder();
        builder.Append($"CONNECT {targetHost}:{targetPort} HTTP/1.1\r\n");
        builder.Append($"Host: {targetHost}:{targetPort}\r\n");

        if (!string.IsNullOrWhiteSpace(user))
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{user}:{password ?? string.Empty}")
            );
            builder.Append($"Proxy-Authorization: Basic {credentials}\r\n");
        }

        builder.Append("\r\n");
        return builder.ToString();
    }

    private static async Task<string> ReadResponseAsync(NetworkStream stream)
    {
        var buffer = new byte[1];
        var builder = new StringBuilder();
        var lastThree = new Queue<byte>(4);

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, 1);
            if (read == 0)
            {
                break;
            }

            var b = buffer[0];
            builder.Append((char)b);
            lastThree.Enqueue(b);
            if (lastThree.Count > 4)
            {
                lastThree.Dequeue();
            }

            // Конец заголовков: \r\n\r\n
            if (
                lastThree.Count == 4
                && lastThree.ToArray()[0] == '\r'
                && lastThree.ToArray()[1] == '\n'
                && lastThree.ToArray()[2] == '\r'
                && lastThree.ToArray()[3] == '\n'
            )
            {
                break;
            }

            if (builder.Length > 8192)
            {
                throw new InvalidOperationException(
                    "HTTP-прокси прислал слишком длинный ответ на CONNECT"
                );
            }
        }

        return builder.ToString();
    }

    private static bool IsSuccess(string response)
    {
        // Строка статуса: "HTTP/1.1 200 Connection established"
        var lines = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return false;
        }

        var parts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !int.TryParse(parts[1], out var status))
        {
            return false;
        }

        return status is >= 200 and < 300;
    }
}
