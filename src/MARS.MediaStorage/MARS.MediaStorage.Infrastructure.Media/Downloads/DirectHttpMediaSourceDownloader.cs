using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace MARS.MediaStorage.Infrastructure.Media.Downloads;

public sealed class DirectHttpMediaSourceDownloader(
    HttpClient httpClient,
    ILogger<DirectHttpMediaSourceDownloader> logger
)
{
    public async Task<string?> DownloadAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsSafeUrl(url))
        {
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(destinationPath);
            await source.CopyToAsync(target, cancellationToken);
            return destinationPath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Direct HTTP download failed for {Url}", url);
            return null;
        }
    }

    private static bool IsSafeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = uri.Host;
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IPAddress.IsLoopback(ip) || IsPrivateIp(ip))
            {
                return false;
            }
        }
        else
        {
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (bytes[0] == 10)
                return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
        }
        return false;
    }
}
