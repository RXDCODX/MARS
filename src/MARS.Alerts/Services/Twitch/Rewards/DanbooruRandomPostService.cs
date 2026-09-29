using System.Text;
using MARS.Alerts.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace MARS.Alerts.Services.Twitch.Rewards;

public class DanbooruRandomPostService(
    IOptions<BooruConfiguration> options,
    IHttpClientFactory factory
)
{
    private readonly Uri _uri = new("https://danbooru.donmai.us/");

    public async Task<DanbooruPost[]?> GetRandomPostAsync(string tags, int limit = 3)
    {
        const string userAgent = "RxdcodxStreamerBot/1.0";

        using var httpClient = factory.CreateClient(userAgent);

        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {GetCredentials()}");

        httpClient.BaseAddress = _uri;

        var posts = await GetPostsByTagsAsync(httpClient, tags, limit);
        if (posts == null || posts.Length == 0)
        {
            return null;
        }

        return posts;
    }

    private static async Task<DanbooruPost[]?> GetPostsByTagsAsync(
        HttpClient httpClient,
        string tags,
        int limit
    )
    {
        var url =
            $"posts.json?tags={Uri.EscapeDataString(tags + " random:" + limit)}&limit={limit}";

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return DanbooruPost.FromJson(content);
    }

    private string GetCredentials()
    {
        var configuration = options.Value;

        ArgumentNullException.ThrowIfNull(configuration);

        return Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{configuration.Login}:{configuration.ApiKey}")
        );
    }
}
